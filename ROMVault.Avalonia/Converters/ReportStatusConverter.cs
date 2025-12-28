using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RomVaultCore.RvDB;
using RomVaultCore;

namespace ROMVault.Avalonia.Converters
{
    public class ReportStatusToBrushConverter : IValueConverter
    {
        private static Color Down(Color c)
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

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is RepStatus status)
            {
                int index = (int)status;
                if (index >= 0 && index < RvColors.DisplayColor.Length)
                {
                    return new SolidColorBrush(Down(RvColors.DisplayColor[index]));
                }
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ReportStatusToForegroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is RepStatus status)
            {
                int index = (int)status;
                if (index >= 0 && index < RvColors.FontColor.Length)
                {
                     // Return the pre-calculated contrast color (Black or White)
                     return new SolidColorBrush(RvColors.FontColor[index]);
                }
            }
            // Default to null to allow inheritance if no status matched, or Black if explicit
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
