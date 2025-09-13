using ReactiveUI;

namespace DockComponent.BlazorHost.Services;

public class WebHostManager : ReactiveObject
{
    private string _currentUrl = string.Empty;
    private bool _isLoaded;

    public string CurrentUrl
    {
        get => _currentUrl;
        set => this.RaiseAndSetIfChanged(ref _currentUrl, value);
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        set => this.RaiseAndSetIfChanged(ref _isLoaded, value);
    }

    public void LoadUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        
        CurrentUrl = url;
        IsLoaded = !string.IsNullOrEmpty(url);
    }

    public void Clear()
    {
        CurrentUrl = string.Empty;
        IsLoaded = false;
    }
}