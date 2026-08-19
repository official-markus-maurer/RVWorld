using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Compress.SevenZip.Structure;
using Compress.Support.Utils;
using FileInfo = RVIO.FileInfo;
using FileStream = RVIO.FileStream;

namespace Compress.SevenZip
{
    public partial class SevenZ
    {
        public ZipReturn ZipFileOpen(string filename, long timestamp, bool readHeaders, int bufferSize = 4096)
        {
            ZipFileClose();
            Debug.WriteLine(filename);
            #region open file stream

            try
            {
                if (!RVIO.File.Exists(filename))
                {
                    ZipFileClose();
                    return ZipReturn.ZipErrorFileNotFound;
                }
                _zipFileInfo = new FileInfo(filename);
                if ((timestamp != -1) && (_zipFileInfo.LastWriteTime != timestamp))
                {
                    ZipFileClose();
                    return ZipReturn.ZipErrorTimeStamp;
                }
                int errorCode = FileStream.OpenFileRead(filename, bufferSize, out _zipFs);
                if (errorCode != 0)
                {
                    ZipFileClose();
                    return ZipReturn.ZipErrorOpeningFile;
                }
            }
            catch (PathTooLongException)
            {
                ZipFileClose();
                return ZipReturn.ZipFileNameToLong;
            }
            catch (IOException)
            {
                ZipFileClose();
                return ZipReturn.ZipErrorOpeningFile;
            }

            #endregion

            ZipOpen = ZipOpenType.OpenRead;

            return ZipFileReadHeaders();
        }


        public ZipReturn ZipFileOpen(Stream inStream)
        {
            ZipFileClose();
            _zipFileInfo = null;
            _zipFs = inStream;
            ZipOpen = ZipOpenType.OpenRead;

            return ZipFileReadHeaders();
        }

        private ZipReturn ZipFileReadHeaders()
        {
            try
            {
                _signatureHeader = new();
                if (!_signatureHeader.Read(_zipFs))
                    return ZipReturn.ZipSignatureError;

                _zipFs.Seek((long)_signatureHeader.NextHeaderLocation, SeekOrigin.Begin);
                byte[] mainHeader = new byte[_signatureHeader.NextHeaderSize];
                _zipFs.Read(mainHeader, 0, (int)_signatureHeader.NextHeaderSize);
                if (!CRC.VerifyDigest(_signatureHeader.NextHeaderCRC, mainHeader, 0, (uint)_signatureHeader.NextHeaderSize))
                    return ZipReturn.Zip64EndOfCentralDirError;

                if (_signatureHeader.NextHeaderSize != 0)
                {
                    _zipFs.Seek((long)_signatureHeader.NextHeaderLocation, SeekOrigin.Begin);
                    ZipReturn zr = Header.ReadHeaderOrPackedHeader(_zipFs, _signatureHeader.BaseOffset, out _header);
                    if (zr != ZipReturn.ZipGood)
                        return zr;
                }
                
                PopulateLocalFiles(out _localFiles);

                return ZipReturn.ZipGood;
            }
            catch
            {
                ZipFileClose();
                return ZipReturn.ZipErrorReadingFile;
            }
        }


        private void PopulateLocalFiles(out List<SevenZipLocalFile> localFiles)
        {
            int emptyFileIndex = 0;
            int folderIndex = 0;
            int unpackedStreamsIndex = 0;
            ulong streamOffset = 0;
            localFiles = new List<SevenZipLocalFile>();

            if (_header == null)
                return;

            for (int i = 0; i < _header.FileInfo.Names.Length; i++)
            {
                SevenZipLocalFile lf = new() { Filename = _header.FileInfo.Names[i] };

                if ((_header.FileInfo.EmptyStreamFlags == null) || !_header.FileInfo.EmptyStreamFlags[i])
                {
                    lf.StreamIndex = folderIndex;
                    lf.StreamOffset = streamOffset;
                    lf.UncompressedSize = _header.StreamsInfo.Folders[folderIndex].UnpackedStreamInfo[unpackedStreamsIndex].UnpackedSize;
                    lf.CRC = Util.UIntToBytes(_header.StreamsInfo.Folders[folderIndex].UnpackedStreamInfo[unpackedStreamsIndex].Crc);

                    streamOffset += lf.UncompressedSize;
                    unpackedStreamsIndex++;

                    if (unpackedStreamsIndex >= _header.StreamsInfo.Folders[folderIndex].UnpackedStreamInfo.Length)
                    {
                        folderIndex++;
                        unpackedStreamsIndex = 0;
                        streamOffset = 0;
                    }
                }
                else
                {
                    lf.UncompressedSize = 0;
                    lf.CRC = [0, 0, 0, 0];
                    lf.IsDirectory = (_header.FileInfo.EmptyFileFlags == null) || !_header.FileInfo.EmptyFileFlags[emptyFileIndex++];

                    if (lf.IsDirectory)
                    {
                        if (lf.Filename.Substring(lf.Filename.Length - 1, 1) != "/")
                        {
                            lf.Filename += "/";
                        }
                    }
                }

                if (_header.FileInfo.TimeLastWrite != null)
                    lf.ModifiedTime = DateTime.FromFileTimeUtc((long)_header.FileInfo.TimeLastWrite[i]).Ticks;
                if (_header.FileInfo.TimeCreation != null)
                    lf.CreatedTime = DateTime.FromFileTimeUtc((long)_header.FileInfo.TimeCreation[i]).Ticks;
                if (_header.FileInfo.TimeLastAccess != null)
                    lf.AccessedTime = DateTime.FromFileTimeUtc((long)_header.FileInfo.TimeLastAccess[i]).Ticks;

                localFiles.Add(lf);
            }
        }


    }
}
