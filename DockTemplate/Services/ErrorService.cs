using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ReactiveUI;
using NLog;

namespace DockTemplate.Services;

public class ErrorService : ReactiveObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ObservableCollection<ErrorEntry> _errors = new();

    public ObservableCollection<ErrorEntry> Errors => _errors;

    public int ErrorCount => _errors.Count(e => e.Level == "Error");
    public int WarningCount => _errors.Count(e => e.Level == "Warning");
    public int InfoCount => _errors.Count(e => e.Level == "Info");

    public void AddError(string message, string? sourceFile = null, int line = 0, int column = 0, string code = "", [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var error = new ErrorEntry
        {
            Message = message,
            Source = sourceFile ?? callerFile ?? "",
            Line = line > 0 ? line : callerLine,
            Column = column,
            Level = "Error",
            Exception = code,
            Timestamp = DateTime.Now,
            FullException = $"{code}: {message}"
        };

        _errors.Insert(0, error); // Insert at beginning for newest first
        Logger.Error($"[{Path.GetFileName(error.Source)}:{error.Line}] {code}: {message}");
        
        // Raise property changed for counts
        this.RaisePropertyChanged(nameof(ErrorCount));
        this.RaisePropertyChanged(nameof(WarningCount));
        this.RaisePropertyChanged(nameof(InfoCount));
    }

    public void AddWarning(string message, string? sourceFile = null, int line = 0, int column = 0, string code = "", [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var warning = new ErrorEntry
        {
            Message = message,
            Source = sourceFile ?? callerFile ?? "",
            Line = line > 0 ? line : callerLine,
            Column = column,
            Level = "Warning", 
            Exception = code,
            Timestamp = DateTime.Now,
            FullException = $"{code}: {message}"
        };

        _errors.Insert(0, warning);
        Logger.Warn($"[{Path.GetFileName(warning.Source)}:{warning.Line}] {code}: {message}");
        
        this.RaisePropertyChanged(nameof(ErrorCount));
        this.RaisePropertyChanged(nameof(WarningCount));
        this.RaisePropertyChanged(nameof(InfoCount));
    }

    public void AddInfo(string message, string? sourceFile = null, int line = 0, int column = 0, string code = "", [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var info = new ErrorEntry
        {
            Message = message,
            Source = sourceFile ?? callerFile ?? "",
            Line = line > 0 ? line : callerLine,
            Column = column,
            Level = "Info",
            Exception = code,
            Timestamp = DateTime.Now,
            FullException = $"{code}: {message}"
        };

        _errors.Insert(0, info);
        Logger.Info($"[{Path.GetFileName(info.Source)}:{info.Line}] {code}: {message}");
        
        this.RaisePropertyChanged(nameof(ErrorCount));
        this.RaisePropertyChanged(nameof(WarningCount));
        this.RaisePropertyChanged(nameof(InfoCount));
    }

    public void Clear()
    {
        _errors.Clear();
        this.RaisePropertyChanged(nameof(ErrorCount));
        this.RaisePropertyChanged(nameof(WarningCount));
        this.RaisePropertyChanged(nameof(InfoCount));
        Logger.Info("Error list cleared");
    }

    public void RemoveError(ErrorEntry error)
    {
        if (_errors.Remove(error))
        {
            this.RaisePropertyChanged(nameof(ErrorCount));
            this.RaisePropertyChanged(nameof(WarningCount));
            this.RaisePropertyChanged(nameof(InfoCount));
        }
    }
}