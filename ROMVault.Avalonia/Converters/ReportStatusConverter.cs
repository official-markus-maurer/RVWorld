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
    /// <summary>
    /// Converts a Report Status (RepStatus) to a background Brush color.
    /// </summary>
    public class ReportStatusToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Dims the color if dark mode is enabled.
        /// </summary>
        /// <param name="c">The color to adjust.</param>
        /// <returns>The adjusted color.</returns>
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

        /// <summary>
        /// Converts a RepStatus to a SolidColorBrush.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>A SolidColorBrush corresponding to the status.</returns>
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

    /// <summary>
    /// Converts a Report Status (RepStatus) to a foreground Brush color (text color).
    /// </summary>
    public class ReportStatusToForegroundConverter : IValueConverter
    {
        /// <summary>
        /// Converts a RepStatus to a SolidColorBrush for text.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>A SolidColorBrush for the text.</returns>
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
