using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Workforce;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the manager operative-lookup surface (M7): the company's operative register and a single
/// operative's profile (cards, compliance, history), over the console-token auth handler. Read-only; a non-success
/// status throws.
/// </summary>
public sealed class ManagerWorkforceApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public ManagerWorkforceApiClient(HttpClient http) => _http = http;

    /// <summary>The company's operative register (list rows).</summary>
    public async Task<IReadOnlyList<OperativeListItemDto>> GetOperativesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<OperativeListItemDto>>("api/workforce", cancellationToken) ?? Array.Empty<OperativeListItemDto>();

    /// <summary>A single operative's full profile by slug, or null when not found.</summary>
    public Task<OperativeDetailDto?> GetOperativeAsync(string slug, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<OperativeDetailDto>($"api/workforce/{Uri.EscapeDataString(slug)}", cancellationToken);
}
