using System.Globalization;
using Avalonia.Data.Converters;

namespace DockTemplate.Converters;

public class FileExtensionToIconConverter : IValueConverter
{
    public static readonly FileExtensionToIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath)
            return "\uf15b"; // Default file icon

        // Handle directories
        if (Directory.Exists(filePath))
            return "\uf07b"; // Folder icon

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".cs" => "\uf81a",      // C# file (code icon)
            ".csproj" => "\uf1b2",  // Project file (cube icon)
            ".sln" => "\uf1b3",     // Solution file (cubes icon)
            ".txt" => "\uf15c",     // Text file
            ".md" => "\uf48a",      // Markdown file (markdown icon)
            ".json" => "\uf1c9",    // JSON file (code bracket icon)
            ".xml" => "\uf1c9",     // XML file (code bracket icon)
            ".xaml" => "\uf1c9",    // XAML file (code bracket icon)
            ".axaml" => "\uf1c9",   // Avalonia XAML file (code bracket icon)
            ".js" => "\uf3b8",      // JavaScript file
            ".ts" => "\uf3b8",      // TypeScript file
            ".py" => "\uf3e2",      // Python file
            ".html" => "\uf13b",    // HTML file
            ".css" => "\uf13c",     // CSS file
            ".scss" => "\uf13c",    // SCSS file
            ".less" => "\uf13c",    // LESS file
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".ico" => "\uf03e", // Image file
            ".pdf" => "\uf1c1",     // PDF file
            ".zip" or ".rar" or ".7z" => "\uf1c6", // Archive file
            ".exe" => "\uf013",     // Executable file (gear icon)
            ".dll" => "\uf1c2",     // Library file
            ".config" => "\uf013",  // Configuration file (gear icon)
            ".log" => "\uf15c",     // Log file (text file icon)
            ".gitignore" => "\uf1d3", // Git ignore (git icon)
            ".gitattributes" => "\uf1d3", // Git attributes (git icon)
            ".yml" or ".yaml" => "\uf1c9", // YAML file
            ".toml" => "\uf1c9",    // TOML file
            ".ini" => "\uf013",     // INI file (gear icon)
            _ => "\uf15b"           // Default file icon
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}