using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Timesheets;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Cache-then-network reader for the operative surface (M3): when online it fetches fresh data and caches it;
/// when offline (or the call fails) it returns the last cached value, so operative screens stay usable in the
/// field. Read-only; the site-entry decision and attendance are never cached (R2/R3) and don't go through here.
/// </summary>
public sealed class OperativeDataService
{
    private const string DashboardKey = "operative.dashboard";
    private const string ProfileKey = "operative.me";
    private const string HoursKey = "operative.hours";
    private const string SitesKey = "operative.sites";

    private readonly OperativeApiClient _api;
    private readonly IReadCache _cache;
    private readonly IConnectivityService _connectivity;

    /// <summary>Creates the reader over the API client, the read cache and the connectivity service.</summary>
    public OperativeDataService(OperativeApiClient api, IReadCache cache, IConnectivityService connectivity)
    {
        _api = api;
        _cache = cache;
        _connectivity = connectivity;
    }

    /// <summary>The operative dashboard (fresh when online, else cached).</summary>
    public Task<OperativeDashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(DashboardKey, () => _api.GetDashboardAsync(cancellationToken), cancellationToken);

    /// <summary>The operative's own profile (fresh when online, else cached).</summary>
    public Task<OperativeDetailDto?> GetProfileAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(ProfileKey, () => _api.GetMeAsync(cancellationToken), cancellationToken);

    /// <summary>The operative's current-week hours (fresh when online, else cached).</summary>
    public Task<OperativeHoursDto?> GetHoursAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(HoursKey, () => _api.GetMyHoursAsync(cancellationToken: cancellationToken), cancellationToken);

    /// <summary>The company's sites with geofences (fresh when online, else cached).</summary>
    public Task<IReadOnlyList<MobileSiteDto>?> GetSitesAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<MobileSiteDto>>(SitesKey, async () => await _api.GetSitesAsync(cancellationToken), cancellationToken);

    private async Task<T?> ReadAsync<T>(string key, Func<Task<T?>> fetch, CancellationToken cancellationToken)
    {
        if (_connectivity.IsConnected)
        {
            try
            {
                var fresh = await fetch();
                if (fresh is not null)
                {
                    await _cache.SetAsync(key, fresh, cancellationToken);
                    return fresh;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or ApiException or TaskCanceledException)
            {
                // Fall through to the cached copy — a transient/offline failure must not blank the screen.
            }
        }

        return await _cache.GetAsync<T>(key, cancellationToken);
    }
}
