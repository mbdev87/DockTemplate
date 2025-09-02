using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DockTemplate.Services;
using DockTemplate.ViewModels.Documents;
using NLog;

namespace DockTemplate.ViewModels.Tools;

public class SolutionExplorerViewModel : ToolViewModel
{
    private readonly TextMateService _textMateService;
    private readonly Action<string>? _openFileCallback;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Reactive] public ObservableCollection<FileSystemItemViewModel> Items { get; set; } = new();
    [Reactive] public string RootPath { get; set; } = string.Empty;

    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }
    public ICommand RefreshCommand { get; }

    public SolutionExplorerViewModel(TextMateService textMateService, Action<string>? openFileCallback = null) : base("SolutionExplorer", "Solution Explorer")
    {
        _textMateService = textMateService;
        _openFileCallback = openFileCallback;
        
        ExpandAllCommand = ReactiveCommand.Create(ExpandAll);
        CollapseAllCommand = ReactiveCommand.Create(CollapseAll);
        RefreshCommand = ReactiveCommand.Create(LoadDirectory);
        
        LoadDirectory();
    }

    private void LoadDirectory()
    {
        try
        {
            RootPath = new DirectoryInfo(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
            Items.Clear();
            
            var rootItem = new FileSystemItemViewModel(RootPath, _textMateService, OnFileOpened);
            Items.Add(rootItem);
            
            Logger.Info($"[SolutionExplorer] Loaded directory: {RootPath}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[SolutionExplorer] Error loading directory: {ex.Message}");
        }
    }

    private void ExpandAll()
    {
        Logger.Info($"[SolutionExplorer] ExpandAll called - {Items.Count} root items");
        foreach (var item in Items)
        {
            item.ExpandAll();
        }
    }

    private void CollapseAll()
    {
        Logger.Info($"[SolutionExplorer] CollapseAll called - {Items.Count} root items");
        foreach (var item in Items)
        {
            item.CollapseAll();
        }
    }

    private void OnFileOpened(string filePath)
    {
        Logger.Info($"[SolutionExplorer] File opened: {filePath}");
        
        // Use the callback to open the file via DockFactory
        _openFileCallback?.Invoke(filePath);
    }
}

public class FileSystemItemViewModel : ReactiveObject
{
    private readonly TextMateService _textMateService;
    private readonly Action<string> _onFileOpened;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Reactive] public string Name { get; set; } = string.Empty;
    [Reactive] public string FullPath { get; set; } = string.Empty;
    [Reactive] public bool IsDirectory { get; set; }
    [Reactive] public bool IsExpanded { get; set; }
    [Reactive] public ObservableCollection<FileSystemItemViewModel> Children { get; set; } = new();
    
    public ICommand ToggleExpandCommand { get; }
    public ICommand OpenFileCommand { get; }

    public FileSystemItemViewModel(string path, TextMateService textMateService, Action<string> onFileOpened)
    {
        _textMateService = textMateService;
        _onFileOpened = onFileOpened;
        
        FullPath = path;
        Name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name))
            Name = path; // Root directory case
            
        IsDirectory = Directory.Exists(path);
        
        ToggleExpandCommand = ReactiveCommand.Create(ToggleExpand);
        OpenFileCommand = ReactiveCommand.Create(OpenFile);
        
        if (IsDirectory)
        {
            LoadChildren();
        }
    }

    private void LoadChildren()
    {
        if (!IsDirectory) return;
        
        try
        {
            Children.Clear();
            
            var directories = Directory.GetDirectories(FullPath)
                .Where(d => !ShouldIgnoreDirectory(d))
                .OrderBy(d => Path.GetFileName(d));
                
            var files = Directory.GetFiles(FullPath)
                .Where(f => !ShouldIgnoreFile(f))
                .OrderBy(f => Path.GetFileName(f));

            foreach (var dir in directories)
            {
                Children.Add(new FileSystemItemViewModel(dir, _textMateService, _onFileOpened));
            }
            
            foreach (var file in files)
            {
                Children.Add(new FileSystemItemViewModel(file, _textMateService, _onFileOpened));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[FileSystemItem] Error loading children for {FullPath}: {ex.Message}");
        }
    }

    private bool ShouldIgnoreDirectory(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        return name.StartsWith('.') || 
               name == "bin" || 
               name == "obj" || 
               name == "node_modules" ||
               name == ".vs" ||
               name == ".git";
    }

    private bool ShouldIgnoreFile(string path)
    {
        var name = Path.GetFileName(path);
        return name.StartsWith('.') ||
               name.EndsWith(".user") ||
               name.EndsWith(".tmp");
    }

    private void ToggleExpand()
    {
        if (!IsDirectory) return;
        
        IsExpanded = !IsExpanded;
        
        if (IsExpanded)
        {
            LoadChildren();
        }
    }

    private void OpenFile()
    {
        if (IsDirectory) 
        {
            ToggleExpand();
            return;
        }
        
        if (IsTextFile(FullPath))
        {
            _onFileOpened?.Invoke(FullPath);
        }
        else if (IsImageFile(FullPath))
        {
            Logger.Warn($"[FileSystemItem] Image file detected: {FullPath} - TODO: Open with image viewer");
            // TODO: Implement image viewer or open with system default
        }
        else
        {
            var extension = Path.GetExtension(FullPath).ToLowerInvariant();
            Logger.Error($"[FileSystemItem] Binary/unsupported file type: {extension}");
        }
    }

    private bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Known text file extensions
        var knownTextExtensions = new[]
        {
            ".cs", ".txt", ".md", ".json", ".xml", ".xaml", ".axaml",
            ".js", ".ts", ".html", ".htm", ".css", ".scss", ".less",
            ".py", ".java", ".cpp", ".c", ".h", ".hpp",
            ".sql", ".php", ".rb", ".go", ".rs", ".swift",
            ".yml", ".yaml", ".toml", ".ini", ".cfg", ".conf",
            ".log", ".gitignore", ".gitattributes", ".editorconfig",
            ".dockerfile", ".bat", ".sh", ".ps1", ".cmd",
            ".csproj", ".sln", ".props", ".targets",
            ".manifest", ".config", ".settings", ".resx",
            ".csv", ".tsv", ".rtf", ".tex", ".latex"
        };
        
        if (knownTextExtensions.Contains(extension))
            return true;
            
        // For files without extensions or unknown extensions, check content
        return IsTextFileByContent(filePath);
    }

    private bool IsTextFileByContent(string filePath)
    {
        try
        {
            // Read first 512 bytes to detect binary content
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[512];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            
            // Check for null bytes (common in binary files)
            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 0)
                    return false; // Likely binary
            }
            
            // Check for high percentage of printable ASCII characters
            int printableCount = 0;
            for (int i = 0; i < bytesRead; i++)
            {
                byte b = buffer[i];
                if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13) // Printable ASCII + tab/newline
                    printableCount++;
            }
            
            // If 80% or more characters are printable, consider it text
            return bytesRead > 0 && (double)printableCount / bytesRead >= 0.8;
        }
        catch
        {
            return false; // If we can't read it, don't try to open it
        }
    }

    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg", ".webp", ".tiff", ".tif" };
        return imageExtensions.Contains(extension);
    }

    public void ExpandAll()
    {
        if (IsDirectory)
        {
            Logger.Info($"[FileSystemItem] Expanding {Name}");
            IsExpanded = true;
            LoadChildren();
            
            foreach (var child in Children)
            {
                child.ExpandAll();
            }
        }
    }

    public void CollapseAll()
    {
        Logger.Info($"[FileSystemItem] Collapsing {Name}");
        IsExpanded = false;
        
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }
}