using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Entitlements;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IEntitlementService"/> backed by the Tedwren Web API over HTTP (SF-22, Q2) — used in the
/// standard <c>DataSource=Api</c> mode. Drives navigation showing only the modules the customer holds.
/// </summary>
public sealed class ApiEntitlementService : IEntitlementService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiEntitlementService(HttpClient http) => _http = http;

    /// <summary>Gets every catalogue module with the company's enabled state.</summary>
    public async Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<ModuleEntitlementDto>>($"api/entitlements/{companyId}", cancellationToken)
        ?? Array.Empty<ModuleEntitlementDto>();

    /// <summary>Whether a module is enabled for the company (fails closed on the server, Q2).</summary>
    public async Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<bool>($"api/entitlements/{companyId}/{moduleKey}", cancellationToken);
}
