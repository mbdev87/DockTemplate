using FluentBlazorExample.Models;

namespace FluentBlazorExample.Services;

public class DashboardService : IDashboardService
{
    private DashboardData _cachedData = new();
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    public event Action<DashboardData>? DataUpdated;

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        if (_cachedData.Files.Count == 0)
        {
            await RefreshDataAsync();
        }
        return _cachedData;
    }

    public async Task RefreshDataAsync()
    {
        await _refreshSemaphore.WaitAsync();
        try
        {
            var newData = await Task.Run(LoadDashboardData);
            _cachedData = newData;
            DataUpdated?.Invoke(_cachedData);
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    private DashboardData LoadDashboardData()
    {
        try
        {
            // Get the actual project root directory
            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = FindDockTemplateRoot(currentDir);

            Console.WriteLine($"🔍 [DASHBOARD] Current dir: {currentDir}");
            Console.WriteLine($"🎯 [DASHBOARD] Project root: {projectRoot}");

            var files = Directory.GetFiles(projectRoot, "*", SearchOption.AllDirectories)
                .Where(f => !IsIgnoredPath(f))
                .Take(1000) // Limit for performance
                .ToList();

            var fileInfoItems = files.Select(CreateFileInfoItem).ToList();
            var typeStats = CalculateFileTypeStats(fileInfoItems);

            return new DashboardData
            {
                Files = fileInfoItems.OrderByDescending(f => f.Size).ToList(),
                FileTypeStats = typeStats.OrderByDescending(s => s.Count).ToList()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dashboard refresh error: {ex.Message}");
            return new DashboardData();
        }
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
        return new[] { ".cs", ".xaml", ".axaml", ".json", ".xml", ".txt", ".md", ".js", ".ts", ".css", ".html", ".py", ".java", ".cpp", ".h", ".razor" }
            .Contains(extension);
    }

    private string GetFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cs" => "C# Source",
            ".axaml" or ".xaml" => "XAML",
            ".razor" => "Blazor",
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

    private string FindDockTemplateRoot(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);

        // Strategy 1: Look for DockTemplate directory going up the tree
        while (currentDir != null)
        {
            // Check if we're in a DockTemplate directory or one of its children
            if (currentDir.Name.Equals("DockTemplate", StringComparison.OrdinalIgnoreCase))
            {
                return currentDir.FullName;
            }

            // Check if there's a DockTemplate subdirectory
            var dockTemplateSubdir = Path.Combine(currentDir.FullName, "DockTemplate");
            if (Directory.Exists(dockTemplateSubdir))
            {
                return dockTemplateSubdir;
            }

            currentDir = currentDir.Parent;
        }

        // Strategy 2: Look for solution files (.sln) or key project files
        currentDir = new DirectoryInfo(startPath);
        while (currentDir != null)
        {
            if (currentDir.GetFiles("*.sln").Any() ||
                currentDir.GetDirectories("Components").Any() ||
                currentDir.GetFiles("DockTemplate.csproj").Any())
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }

        // Fallback: return start path
        Console.WriteLine($"⚠️ [DASHBOARD] Could not find DockTemplate root, using: {startPath}");
        return startPath;
    }

    private bool IsIgnoredPath(string path)
    {
        var ignoredPaths = new[] { "bin", "obj", ".git", "node_modules", ".vs", ".vscode", "packages" };
        return ignoredPaths.Any(ignored => path.Contains($"{Path.DirectorySeparatorChar}{ignored}{Path.DirectorySeparatorChar}") ||
                                          path.Contains($"{Path.DirectorySeparatorChar}{ignored}"));
    }
}