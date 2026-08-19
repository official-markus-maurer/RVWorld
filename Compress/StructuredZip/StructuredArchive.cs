namespace Compress.StructuredZip
{
    public enum ZipStructure
    {
        None = 0,    // No structure
        ZipTrrnt = 1, // Original Trrntzip
        ZipTDC = 2,   // Total DOS Collection, Date Time Deflate
        ZipDTD = 3,   // Date Time Deflate
        SevenZipTrrnt = 4, // this is the original t7z format
        ZipZSTD = 5,       // ZSTD Compression
        ZipDTZ = 6,     // Date Time ZSTD
        SevenZipSLZMA = 8, // Solid-LZMA this is rv7zip today
        SevenZipNLZMA = 9, // NonSolid-LZMA
        SevenZipSZSTD = 10, // Solid-zSTD
        SevenZipNZSTD = 11, // NonSolid-zSTD
    }

    public enum zipDateType
    {
        Undefined,
        None,
        TrrntZip,
        DateTime
    }

    public static class StructuredArchive
    {

        public static ZipCompression GetCompressionType(ZipStructure zipStruct,ulong? uncompressSize)
        {
            switch (zipStruct)
            {
                case ZipStructure.None:
                    return ZipCompression.Stored;

                case ZipStructure.ZipTrrnt:
                case ZipStructure.ZipTDC:
                case ZipStructure.ZipDTD:
                    return ZipCompression.Deflated;
                case ZipStructure.SevenZipTrrnt:
                    return (ZipCompression)ushort.MaxValue;
                case ZipStructure.ZipZSTD:
                case ZipStructure.ZipDTZ:
                    return uncompressSize == 0 ? ZipCompression.Stored : ZipCompression.ZStandard;
                case ZipStructure.SevenZipSLZMA:
                case ZipStructure.SevenZipNLZMA:
                    return ZipCompression.LZMA;
                case ZipStructure.SevenZipSZSTD:
                case ZipStructure.SevenZipNZSTD:
                    return ZipCompression.ZStandard;
            }
            return (ZipCompression)ushort.MaxValue;
        }

        public static bool IsSolid(ZipStructure zipStruct)
        {
            switch (zipStruct)
            {
                case ZipStructure.SevenZipSLZMA:
                case ZipStructure.SevenZipSZSTD:
                    return true;
                case ZipStructure.SevenZipNLZMA:
                case ZipStructure.SevenZipNZSTD:
                    return false;
            }
            return true;
        }

        public static string GetZipCommentId(ZipStructure zipStruct)
        {
            switch (zipStruct)
            {
                case ZipStructure.ZipTrrnt:
                    return "TORRENTZIPPED-";
                case ZipStructure.ZipTDC:
                    return "TDC-";
                case ZipStructure.ZipZSTD:
                    return "RVZSTD-";
                case ZipStructure.ZipDTD:
                    return "DTD-";
                case ZipStructure.ZipDTZ:
                    return "DTZ-";
                default:
                    return "";
            }
        }

        public static string GetZipStructureName(ZipStructure zipStruct)
        {
            switch (zipStruct)
            {
                case ZipStructure.None:
                    return "Unstructured";

                case ZipStructure.ZipTrrnt:
                    return "TrrntZip";
                case ZipStructure.ZipTDC:
                    return "TDC-Zip";
                case ZipStructure.ZipDTD:
                    return "DTD-Zip";

                case ZipStructure.SevenZipTrrnt:
                    return "T7Z";
                case ZipStructure.ZipZSTD:
                    return "ZSTD-Zip";
                case ZipStructure.ZipDTZ:
                    return "DTZ-Zip";
                case ZipStructure.SevenZipSLZMA:
                    return "7Z-Solid-LZMA";
                case ZipStructure.SevenZipNLZMA:
                    return "7Z-NonSolid-LZMA";
                case ZipStructure.SevenZipSZSTD:
                    return "7Z-Solid-zSTD";
                case ZipStructure.SevenZipNZSTD:
                    return "7Z-NonSolid-zSTD";
                default:
                    return "Undefined";
            }
        }

        public static zipDateType GetZipDateTimeType(ZipStructure zipStruct)
        {
            switch (zipStruct)
            {
                case ZipStructure.ZipTrrnt:
                    return zipDateType.TrrntZip;

                case ZipStructure.ZipTDC:
                case ZipStructure.ZipDTD:
                case ZipStructure.ZipDTZ:
                    return zipDateType.DateTime;

                case ZipStructure.ZipZSTD:
                    return zipDateType.None;

            }
            return zipDateType.Undefined;
        }
    }


}
