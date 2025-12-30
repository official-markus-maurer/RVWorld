using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RomVaultCore;
using RomVaultCore.RvDB;
using System;
using System.Globalization;

namespace ROMVault.Avalonia.Converters;

/// <summary>
/// Defines color constants and helper methods for the ROMVault UI.
/// </summary>
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

    /// <summary>
    /// dims the color if dark mode is enabled.
    /// </summary>
    /// <param name="c">The color to dim.</param>
    /// <returns>The adjusted color.</returns>
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
        return (a.R * 0.299 + a.G * 0.587 + a.B * 0.114) > 128 ? Colors.Black : Colors.White;
    }
}

/// <summary>
/// Converts a file/directory status to a background color for the game grid.
/// </summary>
public class GameGridBackgroundConverter : IValueConverter
{
    /// <summary>
    /// Converts the value.
    /// </summary>
    /// <param name="value">The RvFile to evaluate.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>A SolidColorBrush based on the status.</returns>
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

    /// <summary>
    /// Converts back. Not implemented.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a file/directory status to a foreground (text) color for the game grid.
/// </summary>
public class GameGridForegroundConverter : IValueConverter
{
    /// <summary>
    /// Converts the value.
    /// </summary>
    /// <param name="value">The RvFile to evaluate.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>A SolidColorBrush for the text.</returns>
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

    /// <summary>
    /// Converts back. Not implemented.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
