using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IOrganisationService"/> backed by the Tedwren Web API over HTTP — used when the client is
/// configured with <c>DataSource=Api</c>. The UI is unaware of the transport: it injects the same
/// interface as in mock mode.
/// </summary>
public sealed class ApiOrganisationService : IOrganisationService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiOrganisationService(HttpClient http) => _http = http;

    /// <summary>Gets the company list from the API.</summary>
    public async Task<IReadOnlyList<CompanySummary>> GetCompaniesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<CompanySummary>>("api/organisation/companies", cancellationToken)
        ?? Array.Empty<CompanySummary>();

    /// <summary>Gets a company by slug from the API, or null on 404.</summary>
    public async Task<CompanyDetailDto?> GetCompanyAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/organisation/companies/{slug}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompanyDetailDto>(cancellationToken);
    }

    /// <summary>Creates a company via the API and returns its new id.</summary>
    public async Task<Guid> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/organisation/companies", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Updates a company via the API. False on 404.</summary>
    public async Task<bool> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/organisation/companies/{companyId}", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Adds a company document via the API and returns its new id (SUB-4).</summary>
    public async Task<Guid> AddCompanyDocumentAsync(CreateCompanyDocumentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/organisation/companies/{request.CompanyId}/documents", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Adds an operative via the API, returning the outcome (success or SF-2 refusal).</summary>
    public async Task<AddOperativeResult> AddOperativeAsync(AddOperativeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/organisation/operatives", request, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<AddOperativeResult>(cancellationToken);
        return result ?? new AddOperativeResult(false, null, null, "No response from the server.");
    }

    /// <summary>Archives an engagement via the API.</summary>
    public async Task<bool> ArchiveEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(
            $"api/organisation/companies/{companyId}/operatives/{engagementId}/archive", content: null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Reactivates an engagement via the API.</summary>
    public async Task<bool> ReactivateEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(
            $"api/organisation/companies/{companyId}/operatives/{engagementId}/reactivate", content: null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Shape of the create-company response body.</summary>
    private sealed record CreatedResponse(Guid Id);
}
