using FluentBlazorExample.Models;

namespace FluentBlazorExample.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
    Task RefreshDataAsync();
    event Action<DashboardData>? DataUpdated;
}