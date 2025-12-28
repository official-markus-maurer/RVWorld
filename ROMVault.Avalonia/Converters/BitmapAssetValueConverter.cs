using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RomVaultCore.RvDB;

namespace ROMVault.Avalonia.Converters
{
    public class BitmapAssetValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string assetName && !string.IsNullOrEmpty(assetName))
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

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
