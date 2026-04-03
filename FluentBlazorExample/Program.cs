using Microsoft.FluentUI.AspNetCore.Components;
using FluentBlazorExample.Components;
using FluentBlazorExample.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;

// Configure builder with explicit paths for embedded hosting
var builder = WebApplication.CreateBuilder(args);

// CRITICAL: Set content root to current directory when running as embedded server
// This ensures static assets are resolved from the copied location, not the host app
var currentDirectory = Directory.GetCurrentDirectory();
var assemblyDirectory =
    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()
        .Location) ?? currentDirectory;

builder.Configuration.SetBasePath(assemblyDirectory);
builder.Environment.ContentRootPath = assemblyDirectory;
builder.Environment.WebRootPath = Path.Combine(assemblyDirectory, "wwwroot");
builder.WebHost.UseStaticWebAssets();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });
builder.Services.AddFluentUIComponents();

// Add application services
builder.Services.AddSingleton<IDashboardService, DashboardService>();
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<LifecycleService>();
builder.Services.AddHostedService<LifecycleService>(provider =>
    provider.GetRequiredService<LifecycleService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// DISABLED: HTTP redirection conflicts with embedded hosting
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add lifecycle management endpoints
var lifecycleService = app.Services.GetRequiredService<LifecycleService>();

app.MapGet("/heartbeat", () =>
{
    lifecycleService.UpdateHeartbeat();
    return Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow });
});

app.MapPost("/shutdown", () =>
{
    // Trigger graceful shutdown
    _ = Task.Run(async () =>
    {
        await Task.Delay(100); // Give time to return response
        lifecycleService.TriggerShutdown();
    });
    return Results.Ok(new
        { status = "shutting down", timestamp = DateTime.UtcNow });
});

app.Run();