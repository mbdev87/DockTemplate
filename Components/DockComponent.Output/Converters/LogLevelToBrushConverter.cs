using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DockComponent.Output.Converters
{
    public class LogLevelToBrushConverter : IValueConverter
    {
        public static readonly LogLevelToBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string level)
            {
                return level.ToLower() switch
                {
                    "debug" => new SolidColorBrush(Color.Parse("#6B7280")), // Gray
                    "info" => new SolidColorBrush(Color.Parse("#3B82F6")), // Blue
                    "warn" => new SolidColorBrush(Color.Parse("#F59E0B")), // Orange
                    "error" => new SolidColorBrush(Color.Parse("#EF4444")), // Red
                    _ => new SolidColorBrush(Color.Parse("#6B7280")) // Default gray
                };
            }
            return new SolidColorBrush(Color.Parse("#6B7280"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}