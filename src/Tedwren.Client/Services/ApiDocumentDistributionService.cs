using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Documents;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IDocumentDistributionService"/> backed by the Tedwren Web API (<c>/api/documents</c>). The caller's
/// company and sender identity are resolved server-side from the request claims (R15), so the <c>companyId</c>/
/// <c>sentBy</c> arguments are not sent.
/// </summary>
public sealed class ApiDocumentDistributionService : IDocumentDistributionService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiDocumentDistributionService(HttpClient http) => _http = http;

    /// <summary>Distributes a document via the API and returns it with its completion counts.</summary>
    public async Task<DocumentDistributionDto> CreateAsync(Guid companyId, string sentBy, CreateDistributionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/documents", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DocumentDistributionDto>(cancellationToken))!;
    }

    /// <summary>Gets the caller company's distributions, newest first.</summary>
    public async Task<IReadOnlyList<DocumentDistributionDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<DocumentDistributionDto>>("api/documents", cancellationToken) ?? Array.Empty<DocumentDistributionDto>();

    /// <summary>Gets a distribution with its completion matrix, or null when missing/cross-tenant.</summary>
    public async Task<DocumentDistributionDetailDto?> GetAsync(Guid companyId, Guid distributionId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/documents/{distributionId}", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<DocumentDistributionDetailDto>(cancellationToken)
            : null;
    }

    /// <summary>Records a recipient's acknowledgement via the API; returns true on success.</summary>
    public async Task<bool> AcknowledgeAsync(Guid companyId, Guid acknowledgementId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/documents/acknowledgements/{acknowledgementId}/sign", content: null, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
