using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using Dock.Model.Mvvm.Controls;

namespace DockComponent.Dashboard.ViewModels;

public class FileInfoItem : ReactiveObject
{
    private string _name = string.Empty;
    private string _extension = string.Empty;
    private long _size;
    private int _lines;
    private DateTime _modified;
    private string _type = string.Empty;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Extension
    {
        get => _extension;
        set => this.RaiseAndSetIfChanged(ref _extension, value);
    }

    public long Size
    {
        get => _size;
        set => this.RaiseAndSetIfChanged(ref _size, value);
    }

    public int Lines
    {
        get => _lines;
        set => this.RaiseAndSetIfChanged(ref _lines, value);
    }

    public DateTime Modified
    {
        get => _modified;
        set => this.RaiseAndSetIfChanged(ref _modified, value);
    }

    public string Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    public string SizeFormatted => FormatSize(Size);
    public string ModifiedFormatted => Modified.ToString("yyyy-MM-dd HH:mm");

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class FileTypeStats : ReactiveObject
{
    private string _extension = string.Empty;
    private int _count;
    private long _totalSize;
    private double _percentage;

    public string Extension
    {
        get => _extension;
        set => this.RaiseAndSetIfChanged(ref _extension, value);
    }

    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    public long TotalSize
    {
        get => _totalSize;
        set => this.RaiseAndSetIfChanged(ref _totalSize, value);
    }

    public double Percentage
    {
        get => _percentage;
        set => this.RaiseAndSetIfChanged(ref _percentage, value);
    }

    public string Label => $"{Extension} ({Count} files)";
    public string SizeFormatted => FormatSize(TotalSize);

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class DashboardViewModel : Document
{
    private ObservableCollection<FileInfoItem> _files = new();
    private ObservableCollection<FileTypeStats> _fileTypeStats = new();
    private int _totalFiles;
    private long _totalSize;
    private string _selectedPath = string.Empty;

    public DashboardViewModel()
    {
        Title = "📊 Dashboard";
        Id = "Dashboard";

        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshData);
        
        _ = RefreshData();
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public ObservableCollection<FileInfoItem> Files
    {
        get => _files;
        set => _files = value;
    }

    public ObservableCollection<FileTypeStats> FileTypeStats
    {
        get => _fileTypeStats;
        set => _fileTypeStats = value;
    }

    public int TotalFiles
    {
        get => _totalFiles;
        set => _totalFiles = value;
    }

    public long TotalSize
    {
        get => _totalSize;
        set => _totalSize = value;
    }

    public string TotalSizeFormatted => FormatSize(TotalSize);

    public string SelectedPath
    {
        get => _selectedPath;
        set => _selectedPath = value;
    }

    private async Task RefreshData()
    {
        await Task.Run(() =>
        {
            try
            {
                // Get the actual project root directory (go up from bin/Debug/net9.0)
                var currentDir = Directory.GetCurrentDirectory();
                var projectRoot = currentDir;
                
                // If we're running from bin/Debug/net9.0, go up to find project root
                if (currentDir.Contains("bin") && currentDir.Contains("Debug"))
                {
                    var dirInfo = new DirectoryInfo(currentDir);
                    while (dirInfo?.Parent != null && !File.Exists(Path.Combine(dirInfo.FullName, "*.csproj")))
                    {
                        dirInfo = dirInfo.Parent;
                    }
                    
                    // Look for .csproj file to confirm we found project root
                    if (dirInfo != null && Directory.GetFiles(dirInfo.FullName, "*.csproj").Any())
                    {
                        projectRoot = dirInfo.FullName;
                    }
                    else
                    {
                        // Fallback: go up 3 levels from bin/Debug/net9.0
                        var parts = currentDir.Split(Path.DirectorySeparatorChar);
                        if (parts.Length >= 3)
                        {
                            projectRoot = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(parts.Length - 3));
                        }
                    }
                }
                
                SelectedPath = projectRoot;

                var files = Directory.GetFiles(projectRoot, "*", SearchOption.AllDirectories)
                    .Where(f => !IsIgnoredPath(f))
                    .Take(1000) // Limit for performance
                    .ToList();

                var fileInfoItems = files.Select(CreateFileInfoItem).ToList();
                var typeStats = CalculateFileTypeStats(fileInfoItems);

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Files.Clear();
                    FileTypeStats.Clear();

                    foreach (var file in fileInfoItems.OrderByDescending(f => f.Size))
                        Files.Add(file);

                    foreach (var stat in typeStats.OrderByDescending(s => s.Count))
                        FileTypeStats.Add(stat);

                    TotalFiles = fileInfoItems.Count;
                    TotalSize = fileInfoItems.Sum(f => f.Size);
                });
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Dashboard refresh error: {ex.Message}");
            }
        });
    }

    private FileInfoItem CreateFileInfoItem(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var lines = CountLines(filePath);
        
        return new FileInfoItem
        {
            Name = fileInfo.Name,
            Extension = fileInfo.Extension.ToLowerInvariant(),
            Size = fileInfo.Length,
            Lines = lines,
            Modified = fileInfo.LastWriteTime,
            Type = GetFileType(fileInfo.Extension)
        };
    }

    private int CountLines(string filePath)
    {
        try
        {
            if (IsTextFile(filePath))
            {
                return File.ReadAllLines(filePath).Length;
            }
        }
        catch
        {
            // Ignore errors when counting lines
        }
        return 0;
    }

    private bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return new[] { ".cs", ".xaml", ".axaml", ".json", ".xml", ".txt", ".md", ".js", ".ts", ".css", ".html", ".py", ".java", ".cpp", ".h" }
            .Contains(extension);
    }

    private string GetFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cs" => "C# Source",
            ".axaml" or ".xaml" => "XAML",
            ".json" => "JSON",
            ".xml" => "XML",
            ".txt" => "Text",
            ".md" => "Markdown",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".css" => "CSS",
            ".html" => "HTML",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "Image",
            ".dll" or ".exe" => "Binary",
            ".csproj" or ".sln" => "Project",
            _ => "Unknown"
        };
    }

    private List<FileTypeStats> CalculateFileTypeStats(List<FileInfoItem> files)
    {
        var stats = files.GroupBy(f => f.Extension)
            .Select(g => new FileTypeStats
            {
                Extension = string.IsNullOrEmpty(g.Key) ? "(no extension)" : g.Key,
                Count = g.Count(),
                TotalSize = g.Sum(f => f.Size)
            })
            .ToList();

        var totalCount = stats.Sum(s => s.Count);
        foreach (var stat in stats)
        {
            stat.Percentage = totalCount > 0 ? (double)stat.Count / totalCount * 100 : 0;
        }

        return stats;
    }

    private bool IsIgnoredPath(string path)
    {
        var ignoredPaths = new[] { "bin", "obj", ".git", "node_modules", ".vs", ".vscode" };
        return ignoredPaths.Any(ignored => path.Contains($"{Path.DirectorySeparatorChar}{ignored}{Path.DirectorySeparatorChar}") ||
                                          path.Contains($"{Path.DirectorySeparatorChar}{ignored}"));
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}