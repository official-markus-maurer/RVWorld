using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RomVaultCore;
using RomVaultCore.RvDB;
using System;
using System.Globalization;

namespace ROMVault.Avalonia.Converters;

public static class RvColors
{
    public static readonly Color CBlue = Color.FromRgb(214, 214, 255);
    public static readonly Color CGreyBlue = Color.FromRgb(214, 224, 255);
    public static readonly Color CRed = Color.FromRgb(255, 214, 214);
    public static readonly Color CBrightRed = Color.FromRgb(255, 0, 0);
    public static readonly Color CGreen = Color.FromRgb(214, 255, 214);
    public static readonly Color CNeonGreen = Color.FromRgb(100, 255, 100);
    public static readonly Color CLightRed = Color.FromRgb(255, 235, 235);
    public static readonly Color CSoftGreen = Color.FromRgb(150, 200, 150);
    public static readonly Color CGrey = Color.FromRgb(214, 214, 214);
    public static readonly Color CCyan = Color.FromRgb(214, 255, 255);
    public static readonly Color CCyanGrey = Color.FromRgb(214, 225, 225);
    public static readonly Color CMagenta = Color.FromRgb(255, 214, 255);
    public static readonly Color CBrown = Color.FromRgb(140, 80, 80);
    public static readonly Color CPurple = Color.FromRgb(214, 140, 214);
    public static readonly Color CYellow = Color.FromRgb(255, 255, 214);
    public static readonly Color CDarkYellow = Color.FromRgb(255, 255, 100);
    public static readonly Color COrange = Color.FromRgb(255, 214, 140);
    public static readonly Color CWhite = Color.FromRgb(255, 255, 255);

    public static Color Down(Color c)
    {
        if (Settings.rvSettings.Darkness)
        {
            return Color.FromRgb(
                (byte)(c.R * 0.8),
                (byte)(c.G * 0.8),
                (byte)(c.B * 0.8));
        }
        return c;
    }

    public static Color[] DisplayColor;
    public static Color[] FontColor;

    static RvColors()
    {
        DisplayColor = new Color[(int)RepStatus.EndValue];
        FontColor = new Color[(int)RepStatus.EndValue];

        DisplayColor[(int)RepStatus.UnScanned] = CBlue;

        DisplayColor[(int)RepStatus.DirCorrect] = CGreen;
        DisplayColor[(int)RepStatus.DirMissing] = CRed;
        DisplayColor[(int)RepStatus.DirCorrupt] = CBrightRed;

        DisplayColor[(int)RepStatus.Missing] = CRed;
        DisplayColor[(int)RepStatus.Correct] = CGreen;
        DisplayColor[(int)RepStatus.CorrectMIA] = CNeonGreen;
        DisplayColor[(int)RepStatus.NotCollected] = CGrey;
        DisplayColor[(int)RepStatus.UnNeeded] = CCyanGrey;
        DisplayColor[(int)RepStatus.Unknown] = CCyan;
        DisplayColor[(int)RepStatus.InToSort] = CMagenta;

        DisplayColor[(int)RepStatus.MissingMIA] = CSoftGreen;

        DisplayColor[(int)RepStatus.Corrupt] = CBrightRed;
        DisplayColor[(int)RepStatus.Ignore] = CGreyBlue;

        DisplayColor[(int)RepStatus.CanBeFixed] = CYellow;
        DisplayColor[(int)RepStatus.CanBeFixedMIA] = CDarkYellow;
        DisplayColor[(int)RepStatus.MoveToSort] = CPurple;
        DisplayColor[(int)RepStatus.Delete] = CBrown;
        DisplayColor[(int)RepStatus.NeededForFix] = COrange;
        DisplayColor[(int)RepStatus.Rename] = COrange;

        DisplayColor[(int)RepStatus.CorruptCanBeFixed] = CYellow;
        DisplayColor[(int)RepStatus.MoveToCorrupt] = CPurple;

        DisplayColor[(int)RepStatus.Incomplete] = CLightRed;

        DisplayColor[(int)RepStatus.Deleted] = CWhite;

        for (int i = 0; i < (int)RepStatus.EndValue; i++)
        {
            // Force Black for everything except specifically dark backgrounds
            // This is safer for pastel colors
            if (i == (int)RepStatus.DirCorrupt || 
                i == (int)RepStatus.Corrupt || 
                i == (int)RepStatus.Delete)
            {
                 FontColor[i] = Colors.White;
            }
            else
            {
                 FontColor[i] = Colors.Black;
            }
        }
    }

    private static Color Contrasty(Color a)
    {
        // Simple luminance check
        // Adjusted threshold to be slightly higher to favor black text on mid-tones
        // Or ensure white text on very dark backgrounds.
        // Current: > 128 (0.5) is Black.
        // If color is very light (e.g. 214, 214, 255 -> 0.85), it returns Black. Correct.
        // If color is very dark (e.g. 255, 0, 0 -> 0.3), it returns White. Correct.
        // The issue user reported: "displayed text ... bright".
        // This implies White text on Light background?
        // Let's check logic:
        // (L > 128) ? Black : White
        // If L is high (bright), we return Black.
        // If L is low (dark), we return White.
        // This seems correct for standard luminance.
        
        // Wait, if the user sees "no text" and "bright", maybe they are seeing White text on Light background?
        // That would mean L <= 128 for a light background, which is wrong.
        
        // Let's verify standard Rec 601 coefficients:
        // R*0.299 + G*0.587 + B*0.114
        
        // Maybe the user's "bright" means the background is bright pastel, and text is white?
        // Let's force Black for anything reasonably bright.
        
        return (a.R * 0.299 + a.G * 0.587 + a.B * 0.114) > 128 ? Colors.Black : Colors.White;
    }
}

public class GameGridBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RvFile tRvDir)
        {
            if (tRvDir.GotStatus == GotStatus.FileLocked)
            {
                return new SolidColorBrush(RvColors.Down(RvColors.DisplayColor[(int)RepStatus.UnScanned]));
            }

            foreach (RepStatus t1 in RepairStatus.DisplayOrder)
            {
                if (tRvDir.DirStatus.Get(t1) <= 0)
                    continue;

                return new SolidColorBrush(RvColors.Down(RvColors.DisplayColor[(int)t1]));
            }
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GameGridForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RvFile tRvDir)
        {
            if (tRvDir.GotStatus == GotStatus.FileLocked)
            {
                return new SolidColorBrush(RvColors.FontColor[(int)RepStatus.UnScanned]);
            }

            foreach (RepStatus t1 in RepairStatus.DisplayOrder)
            {
                if (tRvDir.DirStatus.Get(t1) <= 0)
                    continue;

                return new SolidColorBrush(RvColors.FontColor[(int)t1]);
            }
        }
        // If no status matched, assume default text color (likely white in dark theme)
        // But if background is transparent, white is fine.
        return null; 
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
