using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace DockComponent.BlazorHost.Converters;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
        {
            return isRunning ? Brushes.LimeGreen : Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToServerStatusConverter : IValueConverter
{
    public static readonly BoolToServerStatusConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
        {
            return isRunning ? "Running" : "Stopped";
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}