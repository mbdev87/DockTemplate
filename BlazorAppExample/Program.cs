using BlazorAppExample.Components;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

// Find available port dynamically
var port = FindAvailablePort();
var baseUrl = $"http://localhost:{port}";

// Configure Kestrel to use the dynamic port
builder.WebHost.UseUrls(baseUrl);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, port);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

// Announce the startup
Console.WriteLine($"🌐 BlazorAppExample starting on: {baseUrl}");
Console.WriteLine($"📋 Copy this URL to the WebHost component: {baseUrl}");

// Create a file to signal DockTemplate about our URL (for auto-integration)
try
{
    var signalFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "blazor-app-url.txt");
    await File.WriteAllTextAsync(signalFile, baseUrl);
    Console.WriteLine($"📝 Created signal file: {signalFile}");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Could not create signal file: {ex.Message}");
}

app.Run();

// Helper function to find available port
static int FindAvailablePort()
{
    // Try ports in range 5000-5100 first (common Blazor dev range)
    for (int port = 5000; port <= 5100; port++)
    {
        if (IsPortAvailable(port))
        {
            return port;
        }
    }
    
    // Fallback: let the system choose
    using var socket = new TcpListener(IPAddress.Loopback, 0);
    socket.Start();
    var chosenPort = ((IPEndPoint)socket.LocalEndpoint).Port;
    socket.Stop();
    return chosenPort;
}

static bool IsPortAvailable(int port)
{
    try
    {
        using var socket = new TcpListener(IPAddress.Loopback, port);
        socket.Start();
        socket.Stop();
        return true;
    }
    catch
    {
        return false;
    }
}