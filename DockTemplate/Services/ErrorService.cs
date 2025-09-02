using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ReactiveUI;
using NLog;

namespace DockTemplate.Services;

public class ErrorService : ReactiveObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ObservableCollection<ErrorEntry> _errors = new();

    public ObservableCollection<ErrorEntry> Errors => _errors;

    public ErrorService()
    {
        // Subscribe to error messages from NLog via MessageBus
        MessageBus.Current.Listen<ErrorMessage>()
            .Subscribe(errorMsg =>
            {

                if (errorMsg.Entry != null)
                {
                    _errors.Insert(0, errorMsg.Entry);
                
                    // Raise property changed for counts
                    this.RaisePropertyChanged(nameof(ErrorCount));
                    this.RaisePropertyChanged(nameof(WarningCount));
                    this.RaisePropertyChanged(nameof(InfoCount));
                }
            });
    }

    public int ErrorCount => _errors.Count(e => e.Level == "Error");
    public int WarningCount => _errors.Count(e => e.Level == "Warning");
    public int InfoCount => _errors.Count(e => e.Level == "Info");

    public void Clear()
    {
        _errors.Clear();
        this.RaisePropertyChanged(nameof(ErrorCount));
        this.RaisePropertyChanged(nameof(WarningCount));
        this.RaisePropertyChanged(nameof(InfoCount));
        // DON'T log here - would cause infinite loop!
    }

    public void GenerateTestErrors()
    {
        // Add direct test error that we know will show up
        var testError = new ErrorEntry
        {
            Message = "Test error: double click on me to jump to source",
            Source = @"C:\git\github\claude\dock_template_root\DockTemplate\DockTemplate\Services\ErrorService.cs",
            Line = 142, // This line number
            Level = "Error",
            Exception = "TEST001",
            Timestamp = DateTime.Now,
            FullException = "TEST001: Test error for demonstration"
        };

        _errors.Insert(0, testError);
        
        // Also add a warning
        var testWarning = new ErrorEntry
        {
            Message = "Test warning: this is a sample warning message",
            Source = @"C:\git\github\claude\dock_template_root\DockTemplate\DockTemplate\Views\Tools\ErrorListView.axaml.cs",
            Line = 50,
            Level = "Warning",
            Exception = "WARN001",
            Timestamp = DateTime.Now,
            FullException = "WARN001: Test warning for demonstration"
        };

        _errors.Insert(0, testWarning);
        
        // Raise property changed for counts
        this.RaisePropertyChanged(nameof(ErrorCount));
        this.RaisePropertyChanged(nameof(WarningCount));
        this.RaisePropertyChanged(nameof(InfoCount));
        
        // DON'T log here - would cause infinite loop!
        System.Console.WriteLine($"[ErrorService] Generated {_errors.Count} test errors for Error List demonstration");
    }

}