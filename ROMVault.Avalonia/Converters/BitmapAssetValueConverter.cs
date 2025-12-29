using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RomVaultCore.RvDB;
using Compress;

namespace ROMVault.Avalonia.Converters
{
    public class BitmapAssetValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? assetName = null;

            if (value is string name)
            {
                assetName = name;
            }
            else if (value is RvFile rvFile)
            {
                assetName = GetBitmapFromType(rvFile.FileType, rvFile.newZipStruct);
                
                // Handle "Missing" suffix logic if needed (simplified check based on assets)
                // For now, sticking to the base logic found in WinForms snippets
                if (assetName != null && (assetName.StartsWith("Zip") || assetName.StartsWith("SevenZip")))
                {
                     if (rvFile.GotStatus == GotStatus.NotGot)
                     {
                         // Check if Missing asset exists
                         if (AssetLoader.Exists(new Uri($"avares://ROMVault.Avalonia/Assets/{assetName}Missing.png")))
                         {
                             assetName += "Missing";
                         }
                     }
                }
            }

            if (!string.IsNullOrEmpty(assetName))
            {
                try
                {
                    var uri = new Uri($"avares://ROMVault.Avalonia/Assets/{assetName}.png");
                    if (AssetLoader.Exists(uri))
                    {
                        return new Bitmap(AssetLoader.Open(uri));
                    }
                }
                catch { }
            }
            return null;
        }

        private string? GetBitmapFromType(FileType ft, ZipStructure zs)
        {
            switch (ft)
            {
                case FileType.Zip:
                    if (zs == ZipStructure.None) { return "Zip"; }
                    if (zs == ZipStructure.ZipTrrnt) { return "ZipTrrnt"; }
                    if (zs == ZipStructure.ZipTDC) { return "ZipTDC"; }
                    if (zs == ZipStructure.ZipZSTD) { return "ZipZSTD"; }
                    return "Zip";
                case FileType.SevenZip:
                    if (zs == ZipStructure.None) { return "SevenZip"; }
                    if (zs == ZipStructure.SevenZipTrrnt) { return "SevenZipTrrnt"; }
                    if (zs == ZipStructure.SevenZipSLZMA) { return "SevenZipSLZMA"; }
                    if (zs == ZipStructure.SevenZipNLZMA) { return "SevenZipNLZMA"; }
                    if (zs == ZipStructure.SevenZipSZSTD) { return "SevenZipSZSTD"; }
                    if (zs == ZipStructure.SevenZipNZSTD) { return "SevenZipNZSTD"; }
                    return "SevenZip";
                case FileType.Dir:
                    return "Dir";
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
