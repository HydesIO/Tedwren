using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Timesheets;
using Tedwren.Abstractions.Contracts.Workforce;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the authenticated operative read surface (<c>/api/mobile/*</c>, M3). The bearer token and
/// silent refresh are handled by <see cref="OperativeAuthMessageHandler"/> on the wrapped <see cref="HttpClient"/>,
/// so these methods just shape the JSON. A non-success status throws (surfaced to the cache-then-network reader).
/// </summary>
public sealed class OperativeApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over the auth-handled <see cref="HttpClient"/> (BaseAddress = API root).</summary>
    public OperativeApiClient(HttpClient http) => _http = http;

    /// <summary>The operative's own profile, cards and compliance.</summary>
    public Task<OperativeDetailDto?> GetMeAsync(CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<OperativeDetailDto>("api/mobile/me", cancellationToken);

    /// <summary>The operative's dashboard overview.</summary>
    public Task<OperativeDashboardDto?> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<OperativeDashboardDto>("api/mobile/dashboard", cancellationToken);

    /// <summary>The operative's recorded hours for a week (current week when <paramref name="week"/> is null, SUB-27).</summary>
    public Task<OperativeHoursDto?> GetMyHoursAsync(DateOnly? week = null, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<OperativeHoursDto>(
            week is null ? "api/mobile/my-hours" : $"api/mobile/my-hours?week={week:yyyy-MM-dd}", cancellationToken);

    /// <summary>The company's sites with geofences (for offline sign-in caching).</summary>
    public async Task<IReadOnlyList<MobileSiteDto>> GetSitesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<MobileSiteDto>>("api/mobile/sites", cancellationToken) ?? Array.Empty<MobileSiteDto>();
}
