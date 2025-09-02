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
                }
            });
    }

    public int ErrorCount => _errors.Count(e => e.Level == "Error");
    public int WarningCount => _errors.Count(e => e.Level == "Warning");

    public void Clear()
    {
        _errors.Clear();
        this.RaisePropertyChanged(nameof(ErrorCount));
        this.RaisePropertyChanged(nameof(WarningCount));
    }

    public void GenerateTestErrors()
    {
         Logger.Error("Test error: double click on me to jump to source");
         Logger.Warn("Test warning: this is a sample warning message");
    }

}