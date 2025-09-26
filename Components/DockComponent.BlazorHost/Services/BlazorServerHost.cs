using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DockComponent.BlazorHost.Services;

public class BlazorServerHost : IHostedService, IDisposable
{
    private readonly ILogger<BlazorServerHost> _logger;
    private Process? _blazorProcess;
    private readonly string _blazorPath;
    private readonly string _signalFilePath;
    private int _port;
    private bool _isDisposed;

    public BlazorServerHost(ILogger<BlazorServerHost> logger, string? blazorPath = null)
    {
        _logger = logger;
        _blazorPath = blazorPath ?? ExtractEmbeddedBlazorApp();
        _signalFilePath = Path.Combine(Path.GetTempPath(), "blazor-app-url.txt");
    }

    private string ExtractEmbeddedBlazorApp()
    {
        try
        {
            // For embedded hosting, point directly to the built FluentBlazorExample
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var pluginDir = Path.GetDirectoryName(assemblyLocation);

            // Look for FluentBlazorExample.dll in various likely locations
            var possiblePaths = new[]
            {
                // Direct reference to built output
                Path.Combine(AppContext.BaseDirectory, "FluentBlazorExample.dll"),
                // Relative to plugin directory
                Path.Combine(pluginDir, "FluentBlazorExample.dll"),
                Path.Combine(pluginDir, "..", "FluentBlazorExample.dll"),
                Path.Combine(pluginDir, "..", "..", "FluentBlazorExample.dll"),
                // Development build location
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FluentBlazorExample", "bin", "Debug", "net9.0", "FluentBlazorExample.dll"),
                // Built in solution
                Path.Combine(Directory.GetCurrentDirectory(), "FluentBlazorExample", "bin", "Debug", "net9.0", "FluentBlazorExample.dll")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation("Found Blazor app at: {BlazorPath}", path);
                    Console.WriteLine($"🎯 Found Blazor app at: {path}");
                    return path;
                }
                else
                {
                    Console.WriteLine($"🔍 Checked path: {path} (not found)");
                }
            }

            // List the current directory contents for debugging
            var currentDir = Directory.GetCurrentDirectory();
            Console.WriteLine($"🔍 Current directory: {currentDir}");
            Console.WriteLine($"🔍 AppContext.BaseDirectory: {AppContext.BaseDirectory}");
            Console.WriteLine($"🔍 Assembly location: {assemblyLocation}");

            throw new FileNotFoundException("Could not find FluentBlazorExample.dll in any expected location");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to locate Blazor app");
            Console.WriteLine($"❌ Failed to locate Blazor app: {ex.Message}");
            throw;
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(targetDir, fileName), true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(targetDir, dirName));
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Find available port
            _port = FindAvailablePort(5000, 5020);

            _logger.LogInformation("Starting Blazor server from: {BlazorPath}", _blazorPath);
            Console.WriteLine($"🚀 Starting Blazor server from: {_blazorPath}");

            if (!File.Exists(_blazorPath))
            {
                _logger.LogError("Blazor application not found at: {BlazorPath}", _blazorPath);
                Console.WriteLine($"❌ Blazor application not found at: {_blazorPath}");
                return;
            }

            // Launch Blazor as separate process
            var workingDir = Path.GetDirectoryName(_blazorPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_blazorPath}\" --urls=http://localhost:{_port} --environment=Development",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Console.WriteLine($"🔧 Command: {startInfo.FileName} {startInfo.Arguments}");
            Console.WriteLine($"🔧 Working directory: {workingDir}");

            _blazorProcess = new Process { StartInfo = startInfo };

            // Set up output handlers
            _blazorProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("Blazor: {Output}", e.Data);
                    Console.WriteLine($"🌐 Blazor stdout: {e.Data}");
                }
            };

            _blazorProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogWarning("Blazor Error: {Error}", e.Data);
                    Console.WriteLine($"❌ Blazor stderr: {e.Data}");
                }
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