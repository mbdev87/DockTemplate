using Microsoft.FluentUI.AspNetCore.Components;
using FluentBlazorExample.Components;
using FluentBlazorExample.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DockComponent.BlazorHost.Services;

public static class FluentBlazorStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Add services to the container - EXACT copy from FluentBlazorExample Program.cs
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        services.AddFluentUIComponents();

        // Add application services
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IThemeService, ThemeService>();
    }

    public static void Configure(WebApplication app)
    {
        // Configure the HTTP request pipeline - EXACT copy from FluentBlazorExample Program.cs
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
    }
}