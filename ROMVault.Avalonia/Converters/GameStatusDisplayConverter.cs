using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RomVaultCore;
using RomVaultCore.RvDB;

namespace ROMVault.Avalonia.Converters
{
    public class GameStatusItem
    {
        public Bitmap? Icon { get; set; }
        public int Count { get; set; }
    }

    public class GameStatusDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is RvFile tRvDir && tRvDir.IsDirectory)
            {
                var items = new List<GameStatusItem>();

                // Check for null DirStatus
                if (tRvDir.DirStatus == null) return null;

                foreach (RepStatus status in RepairStatus.DisplayOrder)
                {
                    int count = tRvDir.DirStatus.Get(status);
                    if (count <= 0) continue;

                    string assetName = "G_" + status;
                    Bitmap? icon = null;
                    try
                    {
                        // Note: Using "avares://" URI scheme for embedded resources
                        var uri = new Uri($"avares://ROMVault.Avalonia/Assets/{assetName}.png");
                        if (AssetLoader.Exists(uri))
                        {
                            icon = new Bitmap(AssetLoader.Open(uri));
                        }
                    }
                    catch { }

                    if (icon != null)
                    {
                        items.Add(new GameStatusItem { Icon = icon, Count = count });
                    }
                }
                return items;
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
