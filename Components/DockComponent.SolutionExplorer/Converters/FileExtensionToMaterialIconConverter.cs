using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Material.Icons;

namespace DockComponent.SolutionExplorer.Converters;

public class FileExtensionToMaterialIconConverter : IValueConverter
{
    public static readonly FileExtensionToMaterialIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath)
            return MaterialIconKind.File; // Default file icon

        // Handle directories
        if (Directory.Exists(filePath))
        {
            // Check for special folders
            var folderName = Path.GetFileName(filePath).ToLowerInvariant();
            return folderName switch
            {
                "bin" or "obj" => MaterialIconKind.FolderSettings,
                "debug" or "release" => MaterialIconKind.FolderSettings,
                "properties" => MaterialIconKind.FolderCog,
                ".git" => MaterialIconKind.Git,
                ".vs" or ".vscode" => MaterialIconKind.FolderSettings,
                "assets" or "images" or "icons" => MaterialIconKind.FolderImage,
                "fonts" => MaterialIconKind.FormatFont,
                "docs" or "documentation" => MaterialIconKind.FolderText,
                "tests" or "test" => MaterialIconKind.FolderPlay,
                "models" => MaterialIconKind.FolderAccount,
                "views" => MaterialIconKind.FolderEye,
                "viewmodels" => MaterialIconKind.FolderEye,
                "services" => MaterialIconKind.FolderCog,
                "controllers" => MaterialIconKind.FolderCog,
                "converters" => MaterialIconKind.FolderSwap,
                _ => MaterialIconKind.Folder // Regular folder
            };
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

        // Special files by name
        if (fileName switch
        {
            "readme" => (MaterialIconKind?)MaterialIconKind.FileDocument,
            "license" => MaterialIconKind.Certificate,
            "changelog" or "changes" => MaterialIconKind.ClipboardText,
            "dockerfile" => MaterialIconKind.Docker,
            "makefile" => MaterialIconKind.Hammer,
            "gruntfile" or "gulpfile" => MaterialIconKind.Cog,
            _ => null
        } is MaterialIconKind specialIcon)
        {
            return specialIcon;
        }

        return extension switch
        {
            // .NET Files
            ".cs" => MaterialIconKind.LanguageCsharp,
            ".csproj" => MaterialIconKind.MicrosoftVisualStudio,
            ".sln" => MaterialIconKind.FolderMultiple,
            ".vb" => MaterialIconKind.FileCode,
            ".fs" => MaterialIconKind.FileCode,

            // Web Development
            ".html" or ".htm" => MaterialIconKind.LanguageHtml5,
            ".css" => MaterialIconKind.LanguageCss3,
            ".scss" or ".sass" => MaterialIconKind.Sass,
            ".less" => MaterialIconKind.LanguageCss3,
            ".js" => MaterialIconKind.LanguageJavascript,
            ".ts" => MaterialIconKind.LanguageTypescript,
            ".jsx" or ".tsx" => MaterialIconKind.React,
            ".vue" => MaterialIconKind.Vuejs,
            ".angular" or ".ng" => MaterialIconKind.Angular,

            // Markup & Config
            ".xml" => MaterialIconKind.Xml,
            ".xaml" => MaterialIconKind.Xaml,
            ".axaml" => MaterialIconKind.Xaml,
            ".json" => MaterialIconKind.CodeJson,
            ".yml" or ".yaml" => MaterialIconKind.CodeBraces,
            ".toml" => MaterialIconKind.FileCode,
            ".ini" => MaterialIconKind.Cog,
            ".config" => MaterialIconKind.Cog,

            // Documentation
            ".md" => MaterialIconKind.LanguageMarkdown,
            ".txt" => MaterialIconKind.FileDocument,
            ".pdf" => MaterialIconKind.FilePdfBox,
            ".docx" or ".doc" => MaterialIconKind.FileWord,
            ".xlsx" or ".xls" => MaterialIconKind.FileExcel,
            ".pptx" or ".ppt" => MaterialIconKind.FilePowerpoint,

            // Programming Languages
            ".py" => MaterialIconKind.LanguagePython,
            ".java" => MaterialIconKind.LanguageJava,
            ".cpp" or ".cxx" or ".cc" => MaterialIconKind.LanguageCpp,
            ".c" => MaterialIconKind.LanguageC,
            ".h" or ".hpp" => MaterialIconKind.FileCode,
            ".go" => MaterialIconKind.LanguageGo,
            ".rs" => MaterialIconKind.LanguageRust,
            ".php" => MaterialIconKind.LanguagePhp,
            ".rb" => MaterialIconKind.LanguageRuby,
            ".swift" => MaterialIconKind.LanguageSwift,
            ".kt" => MaterialIconKind.LanguageKotlin,
            ".r" => MaterialIconKind.LanguageR,
            ".sql" => MaterialIconKind.DatabaseSettings,

            // Images
            ".png" or ".jpg" or ".jpeg" => MaterialIconKind.FileImage,
            ".gif" => MaterialIconKind.FileImage,
            ".bmp" => MaterialIconKind.FileImage,
            ".ico" => MaterialIconKind.FileImage,
            ".svg" => MaterialIconKind.Svg,
            ".webp" => MaterialIconKind.FileImage,

            // Archives
            ".zip" => MaterialIconKind.FolderZip,
            ".rar" => MaterialIconKind.FolderZip,
            ".7z" => MaterialIconKind.FolderZip,
            ".tar" or ".gz" => MaterialIconKind.FolderZip,

            // Executables & Libraries
            ".exe" => MaterialIconKind.ApplicationCog,
            ".msi" => MaterialIconKind.Package,
            ".dll" => MaterialIconKind.Library,
            ".so" => MaterialIconKind.Library,
            ".dylib" => MaterialIconKind.Library,

            // Fonts
            ".ttf" or ".otf" => MaterialIconKind.FormatFont,
            ".woff" or ".woff2" => MaterialIconKind.FormatFont,

            // Audio/Video
            ".mp3" or ".wav" or ".flac" => MaterialIconKind.FileMusic,
            ".mp4" or ".avi" or ".mkv" => MaterialIconKind.FileVideo,

            // Git
            ".gitignore" => MaterialIconKind.Git,
            ".gitattributes" => MaterialIconKind.Git,

            // Logs
            ".log" => MaterialIconKind.FileDocument,

            // Default
            _ => MaterialIconKind.File
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}