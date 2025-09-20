using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using ReactiveUI;
using System.Net;
using System.Net.Sockets;
using FluentBlazorExample.Components;
using FluentBlazorExample.Services;

namespace DockComponent.BlazorHost.Services;

public class BlazorServerManager : ReactiveObject, IDisposable
{
    private WebApplication? _app;
    private bool _isRunning;
    private string _currentUrl = string.Empty;
    private int _currentPort;
    private bool _disposed;

    public bool IsRunning
    {
        get => _isRunning;
        private set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }

    public string CurrentUrl
    {
        get => _currentUrl;
        private set => this.RaiseAndSetIfChanged(ref _currentUrl, value);
    }

    public int CurrentPort
    {
        get => _currentPort;
        private set => this.RaiseAndSetIfChanged(ref _currentPort, value);
    }

    public event Action<string>? ServerStarted;
    public event Action? ServerStopped;
    public event Action<Exception>? ServerError;

    public async Task<string> StartAsync(IServiceProvider? sharedServices = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BlazorServerManager));

        if (IsRunning)
        {
            return CurrentUrl;
        }

        try
        {
            var port = GetAvailablePort();
            var url = $"http://localhost:{port}";

            var builder = WebApplication.CreateBuilder();

            // Configure services using EXACT FluentBlazorExample setup
            FluentBlazorStartup.ConfigureServices(builder.Services);

            // Add shared services if provided
            if (sharedServices != null)
            {
                CopySharedServices(builder.Services, sharedServices);
            }

            // Configure web host
            builder.WebHost.UseUrls(url);
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            _app = builder.Build();

            // Configure middleware using EXACT FluentBlazorExample setup
            FluentBlazorStartup.Configure(_app);

            CurrentPort = port;
            CurrentUrl = url;

            // Start the server asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _app.RunAsync();
                }
                catch (Exception ex) when (!_disposed)
                {
                    ServerError?.Invoke(ex);
                }
            });

            // Wait for server to be ready
            await WaitForServerReady(url);

            IsRunning = true;
            ServerStarted?.Invoke(url);

            return url;
        }
        catch (Exception ex)
        {
            ServerError?.Invoke(ex);
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning || _app == null)
            return;

        try
        {
            await _app.StopAsync();
            IsRunning = false;
            CurrentUrl = string.Empty;
            CurrentPort = 0;
            ServerStopped?.Invoke();
        }
        catch (Exception ex)
        {
            ServerError?.Invoke(ex);
        }
    }

    private static int GetAvailablePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    private static void CopySharedServices(IServiceCollection services, IServiceProvider sharedServices)
    {
        // Copy specific shared services from the main application
        // This allows bi-directional state sharing

        // Theme services
        var themeService = sharedServices.GetService<FluentBlazorExample.Services.IThemeService>();
        if (themeService != null)
        {
            services.AddSingleton(themeService);
        }

        // Dashboard services
        var dashboardService = sharedServices.GetService<FluentBlazorExample.Services.IDashboardService>();
        if (dashboardService != null)
        {
            services.AddSingleton(dashboardService);
        }
    }


    private static async Task WaitForServerReady(string url, int timeoutMs = 5000)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

        var endTime = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < endTime)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Server did not become ready within {timeoutMs}ms");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore disposal errors
        }

        // WebApplication doesn't implement IDisposable directly
    }
}