using System.IO;
using Compress.StructuredZip;

namespace Compress
{
    public interface ICompress
    {
        int LocalFilesCount { get; }

        FileHeader GetFileHeader(int i);

        ZipOpenType ZipOpen { get; }

        ZipReturn ZipFileOpen(string newFilename, long timestamp = -1, bool readHeaders = true, int bufferSize = 4096);

        ZipReturn ZipFileOpen(Stream inStream);
        void ZipFileClose();

        ZipReturn ZipFileOpenReadStream(int index, out Stream stream, out ulong streamSize);
        ZipReturn ZipFileCloseReadStream();


        ZipStructure ZipStruct { get; }

        string ZipFilename { get; }
        long TimeStamp { get; }

        string FileComment { get; }

        ZipReturn ZipFileCreate(string newFilename);
        ZipReturn ZipFileOpenWriteStream(bool raw, string filename, ulong uncompressedSize, ZipCompression compressionMethod, out Stream stream, long? modTime = null, int? threadCount = null, byte[] properties = null);
        ZipReturn ZipFileCloseWriteStream(byte[] crc32);
        void ZipFileCloseFailed();
    }
}
