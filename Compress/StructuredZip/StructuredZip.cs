using CodePage;
using Compress.ZipFile;
using SortMethods;
using System;
using System.IO;

namespace Compress.StructuredZip
{
    public class StructuredZip : Zip, ICompress
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

            if (ExtraDataFoundOnEndOfFile || offset != 0)
                ZipStruct = ZipStructure.None;

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

            if (ExtraDataFoundOnEndOfFile || offset != 0)
                ZipStruct = ZipStructure.None;

            return ZipReturn.ZipGood;
        }


        public ZipReturn ZipFileCreate(string newFilename, ZipStructure zipType)
        {
            ZipStruct = zipType;
            ZipReturn zr = base.ZipFileCreate(newFilename);

            return zr;
        }

        public ZipReturn ZipFileCreate(Stream zipFs, ZipStructure zipType)
        {
            ZipStruct = zipType;
            ZipReturn zr = base.ZipFileCreate(zipFs);

            return zr;
        }


        public new void ZipFileClose()
        {
            switch (ZipOpen)
            {
                case ZipOpenType.Closed:
                    return;

                case ZipOpenType.OpenRead:
                    zipFileCloseRead();
                    return;

                default:
                    int crc = CentralDirectoryWrite();

                    bool structureValid = ValidateFileHeader(ZipStruct, LocalFilesCount);

                    if (!structureValid)
                    {
                        FileComment = "";
                        ZipStruct = ZipStructure.None;
                    }
                    else
                    {
                        FileComment = WriteComments(ZipStruct, crc);
                    }

                    EndOfCentralDirectoryWrite();
                    zipFileCloseWrite();
                    return;
            }
        }
        public void ZipCreateFake(ZipStructure zipType)
        {
            ZipStruct = zipType;
            base.ZipCreateFake();
        }

        public void ZipFileCloseFake(ulong fileOffset, out byte[] centralDir)
        {
            centralDir = null;
            if (ZipOpen != ZipOpenType.OpenFakeWrite)
            {
                return;
            }

            ZipFileFakeOpenMemoryStream();

            int crc = CentralDirectoryWrite(fileOffset);

            bool structureValid = ValidateFileHeader(ZipStruct, LocalFilesCount);

            if (!structureValid)
            {
                FileComment = "";
                ZipStruct = ZipStructure.None;
            }
            else
            {
                FileComment = WriteComments(ZipStruct, crc);
            }

            EndOfCentralDirectoryWrite(fileOffset);
            centralDir = ZipFileFakeCloseMemoryStream();
        }


        public new ZipReturn ZipFileOpenWriteStream(bool raw, string filename, ulong uncompressedSize, ZipCompression compressionMethod, out Stream stream, long? modTime = null, int? threadCount = null, byte[] properties = null)
        {
            stream = null;

            // if we are requirering a trrrntzp file and it is not a trrntzip formated supplied stream then error out
            if (ZipStruct == ZipStructure.ZipTrrnt || ZipStruct == ZipStructure.ZipTDC || ZipStruct == ZipStructure.ZipDTD || ZipStruct == ZipStructure.ZipZSTD || ZipStruct == ZipStructure.ZipDTZ)
            {
                //invalid torrentZip Input If:

                ZipCompression expectedComressionMethod = StructuredArchive.GetCompressionType(ZipStruct,uncompressedSize);
                if (compressionMethod != expectedComressionMethod)
                    return ZipReturn.ZipTrrntzipIncorrectCompressionUsed;

                if (filename.Contains("\\"))
                    return ZipReturn.ZipTrrntZipIncorrectDirectorySeparator;

                int localFilesCount = LocalFilesCount;
                if (localFilesCount > 0)
                {
                    // check that filenames are in trrntzip order
                    string lastFilename = GetFileHeader(localFilesCount - 1).Filename;
                    if (Sorters.TrrntZipStringCompareCase(lastFilename, filename) > 0)
                        return ZipReturn.ZipTrrntzipIncorrectFileOrder;

                    // this should be move out to a fuction
                    if (ZipStruct == ZipStructure.ZipTrrnt || ZipStruct == ZipStructure.ZipZSTD)
                    {
                        // check that no un-needed directory entries are added
                        if (GetFileHeader(localFilesCount - 1).IsDirectory && filename.Length > lastFilename.Length)
                        {
                            if (Sorters.TrrntZipStringCompareCase(lastFilename, filename.Substring(0, lastFilename.Length)) == 0)
                                return ZipReturn.ZipTrrntzipIncorrectDirectoryAddedToZip;
                        }
                    }
                }
            }

            // this should be calling the zip date/time call
            if (ZipStruct == ZipStructure.ZipTrrnt)
                modTime = TrrntzipDosDateTime;
            else if (ZipStruct == ZipStructure.ZipZSTD)
                modTime = 0;

            return base.ZipFileOpenWriteStream(raw, filename, uncompressedSize, compressionMethod, out stream, modTime, threadCount, properties);
        }

        internal void ValidateStructure()
        {
            string lFileComment = FileComment;
            string zcrc = GetCRC();
            foreach (ZipStructure val in Enum.GetValues(typeof(ZipStructure)))
            {
                if (!CheckZipComments(val, lFileComment, zcrc))
                    continue;

                HeaderZipStruct = val;
                if (validateFilesStructure(val))
                    ZipStruct = val;
                return;
            }
        }

        private bool CheckZipComments(ZipStructure zipTestStructure, string lFileComment, string zcrc)
        {
            string id = StructuredArchive.GetZipCommentId(zipTestStructure);
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (lFileComment.Length != id.Length + 8)
                return false;

            if (lFileComment.Substring(0, id.Length) != id)
                return false;

            if (lFileComment.Substring(id.Length) != zcrc)
                return false;

            return true;
        }


        private bool validateFilesStructure(ZipStructure zipStructure)
        {
            int localFilesCount = LocalFilesCount;

            if (!ValidateFileHeader(zipStructure, localFilesCount))
                return false;

            if (!ValidateFileOrder(localFilesCount))
                return false;

            if (!ValidateDirectories(zipStructure, localFilesCount))
                return false;

            // Possibly should be checking for repeat filenames

            if (!ValidateCompressionStream(zipStructure, localFilesCount))
                return false;

            return true;
        }

        private bool ValidateFileHeader(ZipStructure zipStructure, int localFilesCount)
        {
            for (int i = 0; i < localFilesCount; i++)
            {
                ZipFileData localFiles = (ZipFileData)GetFileHeader(i);

                if (localFiles.GetStatus(LocalFileStatus.HeadersMismatch | LocalFileStatus.FilenameMisMatch | LocalFileStatus.DirectoryLengthError | LocalFileStatus.DateTimeMisMatch))
                    return false;

                // Check: Version needed to extract?

                if (localFiles.ExtraDataFound)
                    return false;

                ZipCompression expectedComressionMethod = StructuredArchive.GetCompressionType(zipStructure,localFiles.UncompressedSize);
        
                if (localFiles.CompressionMethod != expectedComressionMethod)
                    return false;

                if (localFiles.Filename.Contains("\\"))
                    return false;

                if (CodePage437.IsCodePage437(localFiles.Filename) != ((localFiles.GeneralPurposeBitFlag & (1 << 11)) == 0))
                    return false;


                switch (StructuredArchive.GetZipDateTimeType(zipStructure))
                {
                    case zipDateType.DateTime:
                        // any date time is good
                        break;
                    case zipDateType.None:
                        if (localFiles.LastModified != 0)
                            return false;
                        break;
                    case zipDateType.TrrntZip:
                        if (!IsTzipDate(localFiles.LastModified))
                            return false;
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        private bool ValidateFileOrder(int localFilesCount)
        {
            for (int i = 0; i < localFilesCount - 1; i++)
            {
                if (Sorters.TrrntZipStringCompare(GetFileHeader(i).Filename, GetFileHeader(i + 1).Filename) >= 0)
                    return false;
            }
            return true;
        }

        private bool ValidateDirectories(ZipStructure zipStructure, int localFilesCount)
        {
            // this should be move out to a function
            if (zipStructure == ZipStructure.ZipTrrnt || zipStructure == ZipStructure.ZipZSTD)
            {
                for (int i = 0; i < localFilesCount - 1; i++)
                {
                    // see if we found a directory
                    string filename0 = GetFileHeader(i).Filename;
                    int filenameLength = filename0.Length;
                    if (filenameLength > 0 && filename0.Substring(filenameLength - 1, 1) != "/")
                        continue;

                    // see if the next file is in that directory
                    string filename1 = GetFileHeader(i + 1).Filename;
                    if (filename1.Length <= filename0.Length)
                        continue;

                    if (Sorters.TrrntZipStringCompare(filename0, filename1.Substring(0, filename0.Length)) != 0)
                        continue;

                    // if we found a file in the directory then we do not need the directory entry
                    return false;
                }
            }
            return true;
        }

        private readonly static byte[] trrntzero = [0x03, 0x00];
        private readonly static byte[] zstdEnd = [0x01, 0x00, 0x00];
        private readonly static byte[] zstdZero = [0x28, 0xb5, 0x2f, 0xfd, 0x00, 0x68, 0x01, 0x00, 0x00];


        private bool ValidateCompressionStream(ZipStructure zipStructure, int localFilesCount)
        {
            if (zipStructure == ZipStructure.ZipZSTD)
            {
                Stream stream;
                ulong streamSize;
                ZipCompression compressionMethod;

                for (int i = 0; i < localFilesCount; i++)
                {
                    ZipFileData fh = (ZipFileData)GetFileHeader(i);
                    if (fh.UncompressedSize == 0)
                    {
                        if (fh.CompressedSize != 0)
                            return false;

                        /*
                        ZipFileOpenReadStream(i, true, out stream, out streamSize, out compressionMethod);
                        byte[] testZero = new byte[9];
                        stream.Read(testZero, 0, 9);
                        ZipFileCloseReadStream();

                        for (int j = 0; j < 9; j++)
                            if (testZero[j] != zstdZero[j])
                                return false;
                        */
                        continue;
                    }

                    // check the last 3 bytes of the stream are 01,00,00
                    if (fh.CompressedSize < 3)
                        return false;

                    ZipFileOpenReadStream(i, true, out stream, out streamSize, out compressionMethod);
                    byte[] testEnd = new byte[3];
                    stream.Seek((long)(streamSize - 3), SeekOrigin.Current);
                    stream.Read(testEnd, 0, 3);
                    ZipFileCloseReadStream();

                    for (int j = 0; j < 3; j++)
                        if (testEnd[j] != zstdEnd[j])
                            return false;
                }
                return true;
            }
            else if (zipStructure == ZipStructure.ZipTrrnt || zipStructure == ZipStructure.ZipTDC)
            {
                for (int i = 0; i < localFilesCount; i++)
                {
                    ZipFileData fh = (ZipFileData)GetFileHeader(i);
                    if (fh.UncompressedSize == 0)
                    {
                        if (fh.CompressedSize != 2)
                            return false;

                        Stream stream;
                        ulong streamSize;
                        ZipCompression compressionMethod;
                        ZipFileOpenReadStream(i, true, out stream, out streamSize, out compressionMethod);
                        byte[] testZero = new byte[2];
                        stream.Read(testZero, 0, 2);
                        ZipFileCloseReadStream();

                        for (int j = 0; j < 2; j++)
                            if (testZero[j] != trrntzero[j])
                                return false;
                    }
                }
                return true;
            }
            return true;
        }





        internal static string WriteComments(ZipStructure zipStruct, int crc)
        {
            string zipCommentId = StructuredArchive.GetZipCommentId(zipStruct);
            if (string.IsNullOrWhiteSpace(zipCommentId))
                return "";

            return zipCommentId + crc.ToString("X8");
        }


        public static long TrrntzipDateTime = 629870671200000000;
        public static long TrrntzipDosDateTime = 563657728;

        public static bool IsTzipDate(long ticks)
        {
            if (ticks <= 0xffffffff)
                return ticks == TrrntzipDosDateTime;
            else
                return ticks == TrrntzipDateTime;
        }


    }
}
