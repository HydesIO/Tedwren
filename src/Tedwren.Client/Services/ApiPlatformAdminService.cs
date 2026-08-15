using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IPlatformAdminService"/> backed by the Tedwren Web API's platform-admin surface
/// (<c>/api/admin</c>). Those endpoints are gated by the <c>PlatformAdmin</c> policy server-side, so a
/// non-platform-admin request returns 403 (and the admin pages are never shown to them in the first place).
/// </summary>
public sealed class ApiPlatformAdminService : IPlatformAdminService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiPlatformAdminService(HttpClient http) => _http = http;

    /// <summary>Gets every company on the platform from the admin API.</summary>
    public async Task<IReadOnlyList<CompanySummary>> GetCompaniesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<CompanySummary>>("api/admin/companies", cancellationToken)
        ?? Array.Empty<CompanySummary>();

    /// <summary>Gets every console user on the platform from the admin API.</summary>
    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<UserDto>>("api/admin/users", cancellationToken)
        ?? Array.Empty<UserDto>();
}
