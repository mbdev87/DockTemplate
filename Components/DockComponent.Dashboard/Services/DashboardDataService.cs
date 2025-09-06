using System;
using System.Threading.Tasks;

namespace DockComponent.Dashboard.Services;

public class DashboardDataService
{
    public async Task<string> GetProjectStatsAsync()
    {
        // Simulate async data fetching
        await Task.Delay(100);
        return $"Dashboard Data Service - Loaded at {DateTime.Now:HH:mm:ss}";
    }

    public string GetComponentInfo()
    {
        return "Dashboard Component with DI-registered services working! 🚀";
    }
}