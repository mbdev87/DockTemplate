using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DockTemplate.Services;

public class BlazorServerHost : IHostedService, IDisposable
{
    private readonly ILogger<BlazorServerHost> _logger;
    private Process? _blazorProcess;
    private readonly string _blazorPath;
    private readonly string _signalFilePath;
    private int _port;
    private bool _isDisposed;

    public BlazorServerHost(ILogger<BlazorServerHost> logger)
    {
        _logger = logger;
        _blazorPath = Path.Combine(AppContext.BaseDirectory, "Blazor", "FluentBlazorExample.dll");
        _signalFilePath = Path.Combine(Directory.GetCurrentDirectory(), "blazor-app-url.txt");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Find available port
            _port = FindAvailablePort(5000, 5020);

            _logger.LogInformation("Starting Blazor server from: {BlazorPath}", _blazorPath);

            if (!File.Exists(_blazorPath))
            {
                _logger.LogError("Blazor application not found at: {BlazorPath}", _blazorPath);
                return;
            }

            // Launch Blazor as separate process
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_blazorPath}\" --urls=http://localhost:{_port}",
                WorkingDirectory = Path.GetDirectoryName(_blazorPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _blazorProcess = new Process { StartInfo = startInfo };

            // Set up output handlers
            _blazorProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogInformation("Blazor: {Output}", e.Data);
            };

            _blazorProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.LogWarning("Blazor Error: {Error}", e.Data);
            };

            _blazorProcess.Start();
            _blazorProcess.BeginOutputReadLine();
            _blazorProcess.BeginErrorReadLine();

            // Wait a moment for the server to start
            await Task.Delay(2000, cancellationToken);

            // Write URL to signal file for auto-integration
            var url = $"http://localhost:{_port}";
            await File.WriteAllTextAsync(_signalFilePath, url, cancellationToken);

            _logger.LogInformation("Blazor server started at: {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Blazor server");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_blazorProcess != null && !_blazorProcess.HasExited)
            {
                // Try graceful shutdown first
                _blazorProcess.CloseMainWindow();

                // Wait a bit for graceful shutdown
                if (!_blazorProcess.WaitForExit(2000))
                {
                    // Force kill if graceful shutdown failed
                    _blazorProcess.Kill(entireProcessTree: true);
                }

                await _blazorProcess.WaitForExitAsync(cancellationToken);
            }

            // Clean up signal file
            if (File.Exists(_signalFilePath))
            {
                File.Delete(_signalFilePath);
            }

            _logger.LogInformation("Blazor server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Blazor server");
        }
    }

    private static int FindAvailablePort(int startPort, int endPort)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var usedPorts = ipGlobalProperties.GetActiveTcpListeners();

        for (int port = startPort; port <= endPort; port++)
        {
            bool isPortUsed = false;
            foreach (var endpoint in usedPorts)
            {
                if (endpoint.Port == port)
                {
                    isPortUsed = true;
                    break;
                }
            }

            if (!isPortUsed)
                return port;
        }

        return startPort; // Fallback
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        try
        {
            if (_blazorProcess != null && !_blazorProcess.HasExited)
            {
                _blazorProcess.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Blazor process");
        }

        _blazorProcess?.Dispose();
    }
}