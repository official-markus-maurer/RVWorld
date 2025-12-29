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
            if (value is RvFile tRvDir)
            {
                if (tRvDir.DirStatus == null) return null;

                var items = new List<GameStatusItem>();

                // Ensure RepairStatus is initialized
                if (RepairStatus.DisplayOrder == null)
                {
                    RepairStatus.InitStatusCheck();
                }

                if (RepairStatus.DisplayOrder == null) return null;

                foreach (RepStatus status in RepairStatus.DisplayOrder)
                {
                    int count = tRvDir.DirStatus.Get(status);
                    
                    if (count <= 0) continue;

                    Bitmap? icon = null;
                    
                    // Try different naming conventions for the asset
                    string[] assetNames = new[] 
                    { 
                        $"G_{status}",      // Standard (G_Missing, G_Correct)
                        $"{status}",        // Direct (DirMissing)
                        status.ToString().Replace("Dir", "") // Fallback (DirCorrect -> Correct?) - risky, maybe manual map better
                    };

                    foreach (string name in assetNames)
                    {
                        try
                        {
                            var uri = new Uri($"avares://ROMVault.Avalonia/Assets/{name}.png");
                            if (AssetLoader.Exists(uri))
                            {
                                icon = new Bitmap(AssetLoader.Open(uri));
                                break;
                            }
                        }
                        catch { }
                    }

                    // Manual overrides if still null
                    if (icon == null && status.ToString() == "DirCorrect")
                    {
                         try { icon = new Bitmap(AssetLoader.Open(new Uri("avares://ROMVault.Avalonia/Assets/Dir.png"))); } catch {}
                    }

                    if (icon != null)
                    {
                        items.Add(new GameStatusItem { Icon = icon, Count = count });
                    }
                    else
                    {
                        // Fallback: Add item without icon if count > 0, so at least the number shows up
                        items.Add(new GameStatusItem { Icon = null, Count = count });
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
