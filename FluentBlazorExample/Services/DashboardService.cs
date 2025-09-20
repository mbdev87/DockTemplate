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
            var projectRoot = currentDir;

            // Navigate up to find the DockTemplate root
            if (currentDir.Contains("FluentBlazorExample"))
            {
                var dirInfo = new DirectoryInfo(currentDir);
                while (dirInfo?.Parent != null && !dirInfo.Name.Equals("DockTemplate", StringComparison.OrdinalIgnoreCase))
                {
                    dirInfo = dirInfo.Parent;
                }

                if (dirInfo != null)
                {
                    projectRoot = dirInfo.FullName;
                }
            }

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

    private bool IsIgnoredPath(string path)
    {
        var ignoredPaths = new[] { "bin", "obj", ".git", "node_modules", ".vs", ".vscode", "packages" };
        return ignoredPaths.Any(ignored => path.Contains($"{Path.DirectorySeparatorChar}{ignored}{Path.DirectorySeparatorChar}") ||
                                          path.Contains($"{Path.DirectorySeparatorChar}{ignored}"));
    }
}