using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DockTemplate.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public static readonly LogLevelToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string level)
            return Brushes.Gray;

        return level.ToUpperInvariant() switch
        {
            "DEBUG" => new SolidColorBrush(Color.FromRgb(108, 117, 125)), // Gray
            "INFO" => new SolidColorBrush(Color.FromRgb(13, 110, 253)),   // Blue
            "WARN" => new SolidColorBrush(Color.FromRgb(238, 176, 7)),    // Yellow/Orange
            "ERROR" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),   // Red
            "FATAL" => new SolidColorBrush(Color.FromRgb(111, 66, 193)),  // Purple
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}