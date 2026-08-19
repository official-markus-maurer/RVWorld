using Compress.ZipFile;
using Compress;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace FileScanner;



public class memBlock
{
    public byte[] buffer = null;
    public int bufferSize = -1;
}

public class ParallelReaderStream : Stream
{
    private long _position;
    private BlockingCollection<memBlock> _blockBufferIn;
    private BlockingCollection<memBlock> _blockBufferReturn;

    private memBlock _currentBlock;
    private int _currentBlockPos;
    private bool _readAllBlocks = false;

    public ParallelReaderStream(BlockingCollection<memBlock> blockBufferIn, BlockingCollection<memBlock> blockBufferReturn)
    {
        _blockBufferIn = blockBufferIn;
        _blockBufferReturn = blockBufferReturn;
        _position = 0;

        _currentBlock = null;
        _currentBlockPos = 0;
    }

    public override void Close()
    {
        base.Close();
        _currentBlockPos = 0;


        if (_currentBlock != null)
        {
            _blockBufferReturn.Add(_currentBlock);
            _currentBlock = null;
        }

        if (_readAllBlocks)
            return;

         // If the stream was corrupt we could get here before the reader has finished reading all the blocks
        // We should add a flag here to tell the reader to stop reading if it still has blocks to go.

        // check (and return) any remaining blocks in the buffer
        _currentBlock = _blockBufferIn.Take();
        while (_currentBlock != null)
        {
            _blockBufferReturn.Add(_currentBlock);
            _currentBlock = _blockBufferIn.Take();
        }
        _readAllBlocks = true;
    }


    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position { get => _position; set => throw new NotImplementedException(); }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int localOffset = offset;
        int read = 0;

        while (read < count)
        {
            if (_currentBlock == null)
            {
                _currentBlock = _blockBufferIn.Take();
                _currentBlockPos = 0;
                if (_currentBlock == null)
                {
                    _readAllBlocks = true;
                    return read;
                }
            }

            int availableInCurrentBlock = _currentBlock.bufferSize - _currentBlockPos;

            int readFromThisBlock = availableInCurrentBlock < count ? availableInCurrentBlock : count;
            Array.Copy(_currentBlock.buffer, _currentBlockPos, buffer, localOffset, readFromThisBlock);
            _currentBlockPos += readFromThisBlock;
            localOffset += readFromThisBlock;

            if (_currentBlockPos == _currentBlock.bufferSize)
            {
                _blockBufferReturn.Add(_currentBlock);
                _currentBlock = null;
                _currentBlockPos = 0;
            }

            _position += readFromThisBlock;
            read += readFromThisBlock;
        }
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}


internal class pZipReader : IDisposable
{
    private BlockingCollection<memBlock> _mainBlockBuffer;
    private int _numBlocks;
    private int _blockSize;
    private Zip _zip;
    private int _zipLocalFilesCount = 0;

    private BlockingCollection<memBlock>[] _fileBlockBuffer;

    private Thread _readerThread;

    internal pZipReader(int numBlocks, int blockSize)
    {
        _numBlocks = numBlocks;
        _blockSize = blockSize;
        _mainBlockBuffer = new BlockingCollection<memBlock>();
        for (int i = 0; i < _numBlocks; i++)
            _mainBlockBuffer.Add(new memBlock());
    }
    public void Dispose()
    {
        _mainBlockBuffer.Dispose();
    }

    internal void StartLoader(Zip zip)
    {
        _zip = zip;
        _zipLocalFilesCount = _zip.LocalFilesCount;

        _fileBlockBuffer = new BlockingCollection<memBlock>[_zipLocalFilesCount];
        for (int i = 0; i < _zipLocalFilesCount; i++)
        {
            if (_zip.GetLocalFileData(i).IsDirectory)
            {
                _fileBlockBuffer[i] = null;
                continue;
            }
            _fileBlockBuffer[i] = new BlockingCollection<memBlock>();
        }
        _readerThread = new Thread(StartReader);
        _readerThread.Start();
    }

    // must call this when finished with a zip file to dispose of the fileBlockBuffer correctly
    internal void JoinReader()
    {
        _readerThread.Join();
        for (int i = 0; i < _zipLocalFilesCount; i++)
            _fileBlockBuffer[i]?.Dispose();
        _zipLocalFilesCount = 0;
    }

    private void StartReader()
    {
        for (int i = 0; i < _zip.LocalFilesCount; i++)
        {
            try
            {
                FileHeader localFile = _zip.GetLocalFileData(i);
                if (localFile.IsDirectory)
                    continue;

                ZipReturn zr = _zip.ZipFileOpenReadStream(i, true, out Stream rawStream, out ulong streamSize, out ZipCompression compressionMethod);

                ulong memRead = 0;
                while (memRead < streamSize)
                {
                    memBlock mb = _mainBlockBuffer.Take();
                    if (mb.buffer == null)
                        mb.buffer = new byte[_blockSize];
                    int blockRead = (streamSize - memRead) < (ulong)_blockSize ? (int)(streamSize - memRead) : _blockSize;
                    int read = rawStream.Read(mb.buffer, 0, blockRead);
                    mb.bufferSize = read;
                    _fileBlockBuffer[i].Add(mb);

                    memRead += (ulong)read;
                }

                _zip.ZipFileCloseReadStream();
            }
            catch { }
            _fileBlockBuffer[i].Add(null);
        }
    }

    public ParallelReaderStream GetReadStream(int i)
    {
        return new ParallelReaderStream(_fileBlockBuffer[i], _mainBlockBuffer);
    }

}