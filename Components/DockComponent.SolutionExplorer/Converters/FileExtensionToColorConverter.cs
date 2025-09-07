using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DockComponent.SolutionExplorer.Converters;

public class FileExtensionToColorConverter : IValueConverter
{
    public static readonly FileExtensionToColorConverter Instance = new();

    // Beautiful VS Code inspired colors
    private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(83, 151, 255));      // #5397FF (C#, TypeScript)
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(106, 176, 76));     // #6AB04C (HTML, CSS)
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(255, 193, 7));     // #FFC107 (JavaScript, JSON)
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(255, 111, 97));       // #FF6F61 (Git, Config)
    private static readonly SolidColorBrush PurpleBrush = new(Color.FromRgb(155, 89, 182));    // #9B59B6 (Python, C++)
    private static readonly SolidColorBrush TealBrush = new(Color.FromRgb(26, 188, 156));      // #1ABC9C (Images)
    private static readonly SolidColorBrush YellowBrush = new(Color.FromRgb(241, 196, 15));    // #F1C40F (Folders)
    private static readonly SolidColorBrush GrayBrush = new(Color.FromRgb(149, 165, 166));     // #95A5A6 (Default)
    private static readonly SolidColorBrush PinkBrush = new(Color.FromRgb(230, 126, 34));      // #E67E22 (Archives)

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath)
            return GrayBrush;

        // Handle directories - beautiful yellow-gold for folders
        if (Directory.Exists(filePath))
        {
            var folderName = Path.GetFileName(filePath).ToLowerInvariant();
            return folderName switch
            {
                "bin" or "obj" or "debug" or "release" => GrayBrush,
                ".git" => RedBrush,
                "assets" or "images" or "icons" => TealBrush,
                _ => YellowBrush
            };
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            // .NET - Beautiful blue
            ".cs" or ".csproj" or ".sln" or ".vb" or ".fs" => BlueBrush,

            // Web - Green for markup, Orange for scripts
            ".html" or ".htm" or ".css" or ".scss" or ".sass" or ".less" => GreenBrush,
            ".js" or ".ts" or ".jsx" or ".tsx" => OrangeBrush,

            // Configuration & Data - Orange
            ".json" or ".xml" or ".xaml" or ".axaml" or ".yml" or ".yaml" or ".toml" => OrangeBrush,

            // Programming Languages - Purple
            ".py" or ".java" or ".cpp" or ".cxx" or ".cc" or ".c" or ".h" or ".hpp" 
            or ".go" or ".rs" or ".php" or ".rb" or ".swift" or ".kt" or ".r" => PurpleBrush,

            // Images - Teal
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".ico" or ".svg" or ".webp" => TealBrush,

            // Archives - Pink/Orange
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => PinkBrush,

            // Documentation - Green
            ".md" or ".txt" or ".pdf" or ".docx" or ".doc" => GreenBrush,

            // Git & Config - Red
            ".gitignore" or ".gitattributes" or ".config" or ".ini" => RedBrush,

            // Default - Gray
            _ => GrayBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}