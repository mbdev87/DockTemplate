namespace FluentBlazorExample.Models;

public class FileInfoItem
{
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long Size { get; set; }
    public int Lines { get; set; }
    public DateTime Modified { get; set; }
    public string Type { get; set; } = string.Empty;
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

public class FileTypeStats
{
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
    public long TotalSize { get; set; }
    public double Percentage { get; set; }
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

public class DashboardData
{
    public List<FileInfoItem> Files { get; set; } = new();
    public List<FileTypeStats> FileTypeStats { get; set; } = new();
    public int TotalFiles => Files.Count;
    public long TotalSize => Files.Sum(f => f.Size);
    public string TotalSizeFormatted => FormatSize(TotalSize);

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