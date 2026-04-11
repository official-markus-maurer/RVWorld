using Compress;
using DATReader.DatStore;

namespace DATReader.DatClean
{
    public static class DatSetCompressionType
    {
        public static bool ChdStrictCueGdi = false;

        private static FileType GetFileTypeFromDir(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Dir:
                    return FileType.File;
                case FileType.Zip:
                    return FileType.FileZip;
                case FileType.SevenZip:
                    return FileType.FileSevenZip;
                case FileType.CHD:
                    return FileType.FileCHD;
                default:
                    return FileType.File;
            }
        }

        public static void SetType(DatBase inDat, FileType fileType, ZipStructure zs, bool fix)
        {
            if (inDat is DatFile dFile)
            {
                dFile.FileType = GetFileTypeFromDir(fileType);
                if (fileType == FileType.CHD && dFile.isDisk == false)
                {
                    string ext = System.IO.Path.GetExtension(dFile.Name)?.ToLowerInvariant() ?? "";
                    if (ext == ".cue" || ext == ".gdi")
                    {
                        if (!ChdStrictCueGdi)
                            dFile.DatStatus = DatStatus.InDatMerged;
                    }
                }
                return;
            }

            if (!(inDat is DatDir dDir))
                return;

            if (dDir.DGame == null || fileType == FileType.Dir)
            {
                dDir.FileType = FileType.Dir;
            }
            else
            {
                if (dDir.FileType!=FileType.UnSet)
                {
                    if (dDir.FileType == FileType.Dir)
                    {
                        fileType = FileType.Dir;
                        zs = ZipStructure.None;
                    }
                    if (dDir.FileType == FileType.Zip)
                    {
                        fileType = FileType.Zip;
                        zs = ZipStructure.ZipTrrnt;
                    }
                    if (dDir.FileType == FileType.SevenZip)
                    {
                        fileType = FileType.SevenZip;
                        zs = ZipStructure.SevenZipNZSTD;
                    }
                    if (dDir.FileType == FileType.CHD)
                    {
                        fileType = FileType.CHD;
                        zs = ZipStructure.None;
                    }
                }
                dDir.FileType = fileType;

                ZipStructure zsChecked = IsTrrntzipDateTimes(dDir, zs) ? ZipStructure.ZipTrrnt : zs;
                dDir.SetDatStruct(zsChecked, fix);
            }


            DatBase[] children = dDir.ToArray();
            if (children == null)
                return;

            dDir.ChildrenClear();

            foreach (DatBase child in children)
            {
                SetType(child, fileType, zs, fix);
                dDir.ChildAdd(child);
            }

        }


        private static bool IsTrrntzipDateTimes(DatDir dDir, ZipStructure zs)
        {
            if (dDir.FileType != FileType.Zip || zs != ZipStructure.ZipTDC)
                return false;

            DatBase[] children = dDir.ToArray();
            foreach (DatBase child in children)
            {
                if (child is DatFile)
                {
                    if (child.DateModified != Compress.StructuredZip.StructuredZip.TrrntzipDosDateTime)
                        return false;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }


    }
}
