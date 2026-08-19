using Compress.SevenZip;
using Compress.SevenZip.Structure;
using Compress.Support.Utils;
using SortMethods;
using System;
using System.IO;
using System.Text;

namespace Compress.StructuredZip
{
    public class Structured7Zip : SevenZ, ICompress
    {

        public new ZipStructure ZipStruct { get; private set; }
        public ZipStructure HeaderZipStruct { get; private set; }

        // From Interface
        // readHeaders = true
        // buffer = 4096
        public new ZipReturn ZipFileOpen(string newFilename, long timestamp, bool readHeaders, int buffer)
        {
            HeaderZipStruct = ZipStructure.None;
            ZipStruct = ZipStructure.None;

            ZipReturn zr = base.ZipFileOpen(newFilename, timestamp, readHeaders, buffer);
            if (zr != ZipReturn.ZipGood)
                return zr;

            ValidateStructure();

            return ZipReturn.ZipGood;
        }

        public new ZipReturn ZipFileOpen(Stream inStream)
        {
            HeaderZipStruct = ZipStructure.None;
            ZipStruct = ZipStructure.None;

            ZipReturn zr = base.ZipFileOpen(inStream);
            if (zr != ZipReturn.ZipGood)
                return zr;

            ValidateStructure();

            return ZipReturn.ZipGood;
        }


        public ZipReturn ZipFileCreateFromUncompressedSize(string newFilename, ZipStructure zipType, ulong unCompressedSize)
        {
            ZipStruct = zipType;
            ZipCompression zCompType = StructuredArchive.GetCompressionType(zipType, 0);
            bool zIsSolid = StructuredArchive.IsSolid(zipType);
            int dictionarySize = GetDictionarySizeFromUncompressedSize(unCompressedSize);

            return ZipFileCreate(newFilename, zCompType, zIsSolid, dictionarySize);
        }

        public new void ZipFileClose()
        {
            switch (ZipOpen)
            {
                case ZipOpenType.Closed:
                    return;
                case ZipOpenType.OpenRead:
                    ZipFileCloseReadStream();
                    ZipFileCloseRead();
                    return;
                case ZipOpenType.OpenWrite:
                    CloseWriting7Zip(WriteRomVault7Zip);
                    return;
            }
        }

        public new ZipReturn ZipFileOpenWriteStream(bool raw, string filename, ulong uncompressedSize, ZipCompression compressionMethod, out Stream stream, long? modTime = null, int? threadCount = null, byte[] properties = null)
        {
            stream = null;

            // should check for correct directory separator characters

            ZipCompression expectedComressionMethod = StructuredArchive.GetCompressionType(ZipStruct, uncompressedSize);
            if (compressionMethod != expectedComressionMethod)
                return ZipReturn.ZipTrrntzipIncorrectCompressionUsed;

            if (filename.Contains("\\"))
                return ZipReturn.ZipTrrntZipIncorrectDirectorySeparator;

            int localFilesCount = LocalFilesCount;
            if (localFilesCount > 0)
            {
                // check that filenames are in Trrnt7Zip order
                string lastFilename = GetFileHeader(localFilesCount - 1).Filename;
                if (Sorters.Trrnt7ZipStringCompare(lastFilename, filename) > 0)
                    return ZipReturn.ZipTrrntzipIncorrectFileOrder;
            }

            return base.ZipFileOpenWriteStream(raw, filename, uncompressedSize, compressionMethod, out stream, modTime, threadCount, properties);
        }

        internal void ValidateStructure()
        {
            ZipStructure zsRet = ZipStructure.None;
            zsRet = IsRomVault7Z();
            if (zsRet != ZipStructure.None)
            {
                HeaderZipStruct = zsRet;
                if (validateFilesStructure(zsRet))
                    ZipStruct = zsRet;
                return;
            }
            HeaderZipStruct = Istorrent7Z();
            ZipStruct = HeaderZipStruct;
        }



        private bool validateFilesStructure(ZipStructure zipStructure)
        {
            int localFilesCount = LocalFilesCount;

            if (!ValidateFileHeader(localFilesCount))
                return false;

            if (!ValidateFileOrder(localFilesCount))
                return false;

            if (!ValidateExtraDirs(localFilesCount))
                return false;

            if (!ValidateCompressionStream(zipStructure))
                return false;


            return true;
        }
        private bool ValidateFileHeader(int localFilesCount)
        {
            for (int i = 0; i < localFilesCount; i++)
            {
                FileHeader localFiles = GetFileHeader(i);

                if (localFiles.Filename.Contains("\\"))
                    return false;
            }
            return true;
        }

        private bool ValidateFileOrder(int localFilesCount)
        {
            for (int i = 0; i < localFilesCount - 1; i++)
            {
                if (Sorters.Trrnt7ZipStringCompare(GetFileHeader(i).Filename, GetFileHeader(i + 1).Filename) >= 0)
                    return false;
            }
            return true;
        }


        private bool ValidateExtraDirs(int localFilesCount)
        {
            string[] dirSortTest = new string[localFilesCount];
            for (int i = 0; i < localFilesCount; i++)
                dirSortTest[i] = GetFileHeader(i).Filename;

            Array.Sort(dirSortTest, Sorters.StringCompare);

            for (int i = 0; i < dirSortTest.Length - 1; i++)
            {
                // check if this is a directory entry
                if (dirSortTest[i][dirSortTest[i].Length - 1] != '/')
                    continue;

                // check if the next filename is shorter or equal to this filename.
                // if it is shorter or equal it cannot be a file in the directory.
                if (dirSortTest[i + 1].Length <= dirSortTest[i].Length)
                    continue;

                // check if the directory part of the two file enteries match
                // if they do we found an incorrect directory entry.
                bool dirPartIsTheSame = true;
                for (int j = 0; j < dirSortTest[i].Length; j++)
                {
                    if (dirSortTest[i][j] != dirSortTest[i + 1][j])
                    {
                        dirPartIsTheSame = false;
                        break;
                    }
                }
                if (dirPartIsTheSame)
                    return false;
            }
            return true;
        }

        private readonly static byte[] zstdEnd = [0x01, 0x00, 0x00];

        private bool ValidateCompressionStream(ZipStructure zipStructure)
        {
            if (zipStructure != ZipStructure.SevenZipSZSTD && zipStructure != ZipStructure.SevenZipNZSTD)
                return true;

            int steamCount = StreamCount;
            for (int i = 0; i < StreamCount; i++)
            {
                PackedStreamInfo psi = GetPackedStreamInfo(i);

                // check the last 3 bytes of the stream are 01,00,00
                if (psi.PackedSize < 3)
                    return false;

                _zipFs.Seek((long)psi.StreamPosition + _signatureHeader.BaseOffset + (long)psi.PackedSize - 3, SeekOrigin.Begin);

                byte[] testEnd = new byte[3];
                _zipFs.Read(testEnd, 0, 3);

                for (int j = 0; j < 3; j++)
                    if (testEnd[j] != zstdEnd[j])
                        return false;

            }
            return true;
        }

        private void WriteRomVault7Zip()
        {
            using BinaryWriter bw = new(_zipFs, Encoding.UTF8, true);
            string index = "0";
            switch (ZipStruct)
            {
                case ZipStructure.SevenZipSLZMA: index = "1"; break;
                case ZipStructure.SevenZipNLZMA: index = "2"; break;
                case ZipStructure.SevenZipSZSTD: index = "3"; break;
                case ZipStructure.SevenZipNZSTD: index = "4"; break;
            }
            string sig = "RomVault7Z0" + index;
            byte[] RV7Zid = Util.Enc.GetBytes(sig);

            // RomVault 7Zip torrent header
            // 12 bytes :  RomVault7Zip
            //  4 bytes :  HeaderCRC
            //  8 bytes :  HeaderPos
            //  8 bytes :  HeaderLength

            bw.Write(RV7Zid);
            bw.Write(_signatureHeader.NextHeaderCRC);
            bw.Write(_signatureHeader.NextHeaderLocation);
            bw.Write(_signatureHeader.NextHeaderSize);
        }

        //testBaseOffset is 
        internal ZipStructure IsRomVault7Z()
        {
            long length = _zipFs.Length;
            if (length < 32)
                return ZipStructure.None;

            _zipFs.Seek((long)_signatureHeader.NextHeaderLocation - 32, SeekOrigin.Begin);

            const string sig = "RomVault7Z0";
            byte[] rv7Zid = Util.Enc.GetBytes(sig);
            byte[] header = new byte[12];
            _zipFs.Read(header, 0, 12);


            for (int i = 0; i < 11; i++)
            {
                if (header[i] != rv7Zid[i])
                    return ZipStructure.None;
            }

            uint headerCRC;
            ulong headerOffset; // is location of header in file
            ulong headerSize;
            using (BinaryReader br = new(_zipFs, Encoding.UTF8, true))
            {
                headerCRC = br.ReadUInt32();
                headerOffset = br.ReadUInt64();
                headerSize = br.ReadUInt64();
            }

            if (headerCRC != _signatureHeader.NextHeaderCRC)
                return ZipStructure.None;

            if (headerOffset != _signatureHeader.NextHeaderLocation)
                return ZipStructure.None;

            if (headerSize != _signatureHeader.NextHeaderSize)
                return ZipStructure.None;

            switch (header[11])
            {
                case (byte)'1': return ZipStructure.SevenZipSLZMA;
                case (byte)'2': return ZipStructure.SevenZipNLZMA;
                case (byte)'3': return ZipStructure.SevenZipSZSTD;
                case (byte)'4': return ZipStructure.SevenZipNZSTD;
            }
            return ZipStructure.None;
        }

        internal ZipStructure Istorrent7Z()
        {
            const int crcsz = 128;
            const int t7ZsigSize = 16 + 1 + 9 + 4 + 4;
            byte[] kSignature = [(byte)'7', (byte)'z', 0xBC, 0xAF, 0x27, 0x1C];
            int kSignatureSize = kSignature.Length;
            const string sig = "\xa9\x9f\xd1\x57\x08\xa9\xd7\xea\x29\x64\xb2\x36\x1b\x83\x52\x33\x01torrent7z_0.9beta";
            byte[] t7Zid = Util.Enc.GetBytes(sig);
            int t7ZidSize = t7Zid.Length;

            const int tmpbufsize = 256 + t7ZsigSize + 8 + 4;
            byte[] buffer = new byte[tmpbufsize];

            // read fist 128 bytes, pad with zeros if less bytes
            int bufferPos = 0;
            _zipFs.Seek(0, SeekOrigin.Begin);
            int ar = _zipFs.Read(buffer, bufferPos, crcsz);
            if (ar < crcsz)
            {
                Util.MemSet(buffer, bufferPos + ar, 0, crcsz - ar);
            }
            bufferPos = crcsz;

            long foffs = _zipFs.Length;
            int endReadLength = crcsz + t7ZsigSize + 4;
            foffs = foffs < endReadLength ? 0 : foffs - endReadLength;

            _zipFs.Seek(foffs, SeekOrigin.Begin);

            ar = _zipFs.Read(buffer, bufferPos, endReadLength);
            if (ar < endReadLength)
            {
                if (ar >= t7ZsigSize + 4)
                {
                    ar -= t7ZsigSize + 4;
                }
                if (ar < kSignatureSize)
                {
                    ar = kSignatureSize;
                }
                Util.MemSet(buffer, bufferPos + ar, 0, crcsz - ar);
                Util.MemCrypt(buffer, crcsz * 2 + 8, buffer, bufferPos + ar, t7ZsigSize + 4);
            }
            else
            {
                Util.MemCrypt(buffer, crcsz * 2 + 8, buffer, crcsz * 2, t7ZsigSize + 4);
            }

            foffs = _zipFs.Length;
            foffs -= t7ZsigSize + 4;

            //memcpy(buffer, crcsz * 2, &foffs, 8);
            buffer[crcsz * 2 + 0] = (byte)((foffs >> 0) & 0xff);
            buffer[crcsz * 2 + 1] = (byte)((foffs >> 8) & 0xff);
            buffer[crcsz * 2 + 2] = (byte)((foffs >> 16) & 0xff);
            buffer[crcsz * 2 + 3] = (byte)((foffs >> 24) & 0xff);
            buffer[crcsz * 2 + 4] = (byte)((foffs >> 32) & 0xff);
            buffer[crcsz * 2 + 5] = (byte)((foffs >> 40) & 0xff);
            buffer[crcsz * 2 + 6] = (byte)((foffs >> 48) & 0xff);
            buffer[crcsz * 2 + 7] = (byte)((foffs >> 56) & 0xff);

            if (Util.MemCmp(buffer, 0, kSignature, kSignatureSize))
            {
                t7Zid[16] = buffer[crcsz * 2 + 4 + 8 + 16];
                if (Util.MemCmp(buffer, crcsz * 2 + 4 + 8, t7Zid, t7ZidSize))
                {
                    uint inCrc32 = (uint)(buffer[crcsz * 2 + 8 + 0] +
                                           (buffer[crcsz * 2 + 8 + 1] << 8) +
                                           (buffer[crcsz * 2 + 8 + 2] << 16) +
                                           (buffer[crcsz * 2 + 8 + 3] << 24));

                    buffer[crcsz * 2 + 8 + 0] = 0xff;
                    buffer[crcsz * 2 + 8 + 1] = 0xff;
                    buffer[crcsz * 2 + 8 + 2] = 0xff;
                    buffer[crcsz * 2 + 8 + 3] = 0xff;

                    uint calcCrc32 = CRC.CalculateDigest(buffer, 0, crcsz * 2 + 8 + t7ZsigSize + 4);

                    if (inCrc32 == calcCrc32)
                    {
                        return ZipStructure.SevenZipTrrnt;
                    }
                }
            }

            return ZipStructure.None;
        }


    }
}
