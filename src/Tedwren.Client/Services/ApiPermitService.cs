using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Permits;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IPermitService"/> backed by the Tedwren Web API (<c>/api/permits</c>). Raises permits and
/// lists a company's permits.
/// </summary>
public sealed class ApiPermitService : IPermitService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiPermitService(HttpClient http) => _http = http;

    /// <summary>Raises a permit via the API and returns its new identifier.</summary>
    public async Task<Guid> CreateAsync(CreatePermitRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/permits", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken);
    }

    /// <summary>Gets a company's permits from the API, newest first.</summary>
    public async Task<IReadOnlyList<PermitDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<PermitDto>>($"api/permits/company/{companyId}", cancellationToken)
        ?? Array.Empty<PermitDto>();
}
