using System.IO;
using System.Text;
using Compress.SevenZip.Structure;
using Compress.Support.Compression.LZMA;
using Compress.Support.Utils;

namespace Compress.SevenZip
{
    public partial class SevenZ
    {
        private void Create7ZStructure()
        {
            int fileCount = _localFiles.Count;

            //FileInfo
            _header.FileInfo = new Structure.FileInfo
            {
                Names = new string[fileCount]
            };

            ulong emptyStreamCount = 0;
            ulong emptyFileCount = 0;
            for (int i = 0; i < fileCount; i++)
            {
                string fName = _localFiles[i].Filename;
                //should maybe also check for IsDirectory when removing this trailing slash.
                if (fName.Substring(fName.Length - 1, 1) == @"/")
                    fName = fName.Substring(0, fName.Length - 1);

                _header.FileInfo.Names[i] = fName;

                if (_localFiles[i].UncompressedSize != 0)
                {
                    continue;
                }

                if (!_localFiles[i].IsDirectory)
                {
                    emptyFileCount += 1;
                }

                emptyStreamCount += 1;
            }

            _header.FileInfo.EmptyStreamFlags = null;
            _header.FileInfo.EmptyFileFlags = null;
            _header.FileInfo.Attributes = null;

            if (emptyStreamCount > 0)
            {
                if (emptyStreamCount != emptyFileCount) //then we found directories and need to set the attributes
                {
                    _header.FileInfo.Attributes = new uint[fileCount];
                }

                if (emptyFileCount > 0)
                {
                    _header.FileInfo.EmptyFileFlags = new bool[emptyStreamCount];
                }

                emptyStreamCount = 0;
                _header.FileInfo.EmptyStreamFlags = new bool[fileCount];
                for (int i = 0; i < fileCount; i++)
                {
                    if (_localFiles[i].UncompressedSize != 0)
                    {
                        continue;
                    }

                    if (_localFiles[i].IsDirectory)
                    {
                        if (_header.FileInfo.Attributes != null)
                            _header.FileInfo.Attributes[i] = 0x10; // set attributes to directory
                    }
                    else
                    {
                        if (_header.FileInfo.EmptyFileFlags != null)
                            _header.FileInfo.EmptyFileFlags[emptyStreamCount] = true; // set empty file flag
                    }

                    _header.FileInfo.EmptyStreamFlags[i] = true;
                    emptyStreamCount += 1;
                }
            }


            //StreamsInfo

            _header.StreamsInfo = new StreamsInfo { PackPosition = 0 };

            _header.StreamsInfo.PackedStreams = new PackedStreamInfo[_packedOutStreams.Count];
            for (int i = 0; i < _packedOutStreams.Count; i++)
            {
                _header.StreamsInfo.PackedStreams[i] = new PackedStreamInfo { PackedSize = _packedOutStreams[i].packedSize };
            }

            _header.StreamsInfo.Folders = new Folder[_packedOutStreams.Count];
            for (int i = 0; i < _packedOutStreams.Count; i++)
            {
                ulong unpackedStreamSize = 0;
                foreach (UnpackedStreamInfo v in _packedOutStreams[i].unpackedStreams)
                    unpackedStreamSize += v.UnpackedSize;

                _header.StreamsInfo.Folders[i] = new Folder()
                {
                    BindPairs = null,
                    Coders = new Coder[] {
                         new Coder {
                            Method = _packedOutStreams[i].Method,
                            NumInStreams = 1,
                            NumOutStreams = 1,
                            Properties = _packedOutStreams[i].Properties
                        }
                    },
                    PackedStreamIndices = new ulong[] { (ulong)i },
                    UnpackedStreamSizes = new ulong[] { unpackedStreamSize },
                    UnpackedStreamInfo = _packedOutStreams[i].unpackedStreams.ToArray(),
                    UnpackCRC = null
                };
            }
        }


        public delegate void HeaderFunction();

        internal void CloseWriting7Zip(HeaderFunction addStructuredHeader = null)
        {
            if (_zIsSolid)
            {
                if (_packedOutStreams.Count > 0)
                {
                    if (_compressStream is LzmaStream dfStream)
                    {
                        dfStream.Close();
                        dfStream.Dispose();
                    }
                    else if (_compressStream is RVZstdSharp.CompressionStream dfStream2)
                    {
                        dfStream2.Close();
                        dfStream2.Dispose();
                    }

                    _packedOutStreams[0].packedSize = (ulong)_zipFs.Position - _packedOutStreams[0].packedStart;
                }
            }
            Create7ZStructure();

            byte[] mainHeaderBytes;
            using (MemoryStream headerMem = new MemoryStream())
            {
                using BinaryWriter headerBw = new(headerMem, Encoding.UTF8, true);
                _header.WriteHeader(headerBw);

                mainHeaderBytes = headerMem.ToArray();
            }

            uint mainHeaderCRC = CRC.CalculateDigest(mainHeaderBytes, 0, (uint)mainHeaderBytes.Length);

            #region Header Compression
            long packedHeaderPos = _zipFs.Position;
            LzmaEncoderProperties ep = new(true, GetDictionarySizeFromUncompressedSize((ulong)mainHeaderBytes.Length), 64);
            LzmaStream lzs = new(ep, false, _zipFs);
            byte[] lzmaStreamProperties = lzs.Properties;
            lzs.Write(mainHeaderBytes, 0, mainHeaderBytes.Length);
            lzs.Close();

            StreamsInfo streamsInfo = new()
            {
                PackPosition = (ulong)(packedHeaderPos - _signatureHeader.BaseOffset),
                Folders = new[] {
                        new Folder {
                            BindPairs = new BindPair[0],
                            Coders = new [] {
                                new Coder {
                                    Method = new byte[] { 3, 1, 1 },
                                    NumInStreams = 1,
                                    NumOutStreams = 1,
                                    Properties = lzmaStreamProperties
                                }
                            },
                            UnpackedStreamSizes = new[] {(ulong) mainHeaderBytes.Length},
                            UnpackCRC = mainHeaderCRC
                        }
                    },
                PackedStreams = new[] {
                        new PackedStreamInfo
                        {
                            PackedSize = (ulong)(_zipFs.Position - packedHeaderPos),
                            StreamPosition = 0
                        }
                    }
            };

            using (MemoryStream headerMem = new MemoryStream())
            {
                using BinaryWriter bw = new(headerMem, Encoding.UTF8, true);
                bw.Write((byte)HeaderProperty.kEncodedHeader);
                streamsInfo.WriteHeader(bw);
                mainHeaderBytes = headerMem.ToArray();
            }
            mainHeaderCRC = CRC.CalculateDigest(mainHeaderBytes, 0, (uint)mainHeaderBytes.Length);
            #endregion



            long StructuredHeaderPosition = _zipFs.Position;

            // write an empty header for now, we will come back and write the correct one after we know the size and position of the main header.
            if (addStructuredHeader != null)
                addStructuredHeader();

            _signatureHeader.NextHeaderLocation = (ulong)_zipFs.Position;
            _signatureHeader.NextHeaderSize = (ulong)mainHeaderBytes.Length;
            _signatureHeader.NextHeaderCRC = mainHeaderCRC;


            _zipFs.Write(mainHeaderBytes, 0, mainHeaderBytes.Length);

            // goes right back to the start of the file and writes the signature header with the correct values.
            _signatureHeader.Write(_zipFs);


            // go back and write the correct values into the structured header.
            if (addStructuredHeader!=null)
            {
                _zipFs.Seek(StructuredHeaderPosition, SeekOrigin.Begin);
                addStructuredHeader();
            }

            _zipFs.Close();
            _zipFs.Dispose();
            _zipFs = null;
            _zipFileInfo = _zipFileInfo == null ? null : new RVIO.FileInfo(_zipFileInfo.FullName);

            ZipOpen = ZipOpenType.Closed;
        }

    }
}
