using Compress.StructuredZip;

namespace TrrntZip
{
    public enum zipType
    {
        zip,
        sevenzip,
        archive,
        file,
        dir,
        all
    }

    public class Settings
    {
        public bool VerboseLogging = true;
        public bool Repair = false;
        public bool DryRun = false;
        public zipType InZip = zipType.zip;
        public ZipStructure OutZip = ZipStructure.ZipTrrnt;
        public object lockObj = new object();
    }
}