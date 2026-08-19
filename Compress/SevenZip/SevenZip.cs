using System.Collections.Generic;
using System.IO;
using System.Text;
using Compress.SevenZip.Structure;
using Compress.StructuredZip;
using FileInfo = RVIO.FileInfo;

namespace Compress.SevenZip
{
    public partial class SevenZ : ICompress
    {
        private Header _header;


        private class SevenZipLocalFile : FileHeader
        {
            public int StreamIndex;
            public ulong StreamOffset;
        }


        private List<SevenZipLocalFile> _localFiles = new();

        private FileInfo _zipFileInfo;

        internal Stream _zipFs;

        internal SignatureHeader _signatureHeader { get; private set; }

        public string ZipFilename => _zipFileInfo != null ? _zipFileInfo.FullName : "";

        public long TimeStamp => _zipFileInfo?.LastWriteTime ?? 0;

        public string FileComment => null;

        public ZipOpenType ZipOpen { get; private set; }

        public ZipStructure ZipStruct => ZipStructure.None;

        public int LocalFilesCount => _localFiles.Count;
        
        public FileHeader GetFileHeader(int i)
        {
            return _localFiles[i];
        }

        public void ZipFileCloseFailed()
        {
            switch (ZipOpen)
            {
                case ZipOpenType.Closed:
                    return;
                case ZipOpenType.OpenRead:
                    ZipFileCloseReadStream();
                    if (_zipFs != null)
                    {
                        _zipFs.Close();
                        _zipFs.Dispose();
                    }
                    break;
                case ZipOpenType.OpenWrite:
                    _zipFs.Flush();
                    _zipFs.Close();
                    _zipFs.Dispose();
                    if (_zipFileInfo != null)
                        RVIO.File.Delete(_zipFileInfo.FullName);
                    _zipFileInfo = null;
                    break;
            }

            ZipOpen = ZipOpenType.Closed;
        }
        

        public void ZipFileClose()
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
                    CloseWriting7Zip();
                    break;
            }

            ZipOpen = ZipOpenType.Closed;
        }

        internal void ZipFileCloseRead()
        {
            if (_zipFs != null)
            {
                _zipFs.Close();
                _zipFs.Dispose();
            }
            ZipOpen = ZipOpenType.Closed;
        }


        public StringBuilder HeaderReport()
        {
            StringBuilder sb = new();

            if (_header == null)
            {
                sb.AppendLine("Null Header");
                return sb;
            }

            _header.Report(ref sb);

            return sb;
        }

    }
}