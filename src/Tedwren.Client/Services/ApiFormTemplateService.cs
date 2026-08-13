using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IFormTemplateService"/> backed by the Tedwren Web API over HTTP (the Forms Library, PRD-Phase 2).
/// The UI injects the same interface as the server; tenant scoping and versioning happen on the server (R15).
/// </summary>
public sealed class ApiFormTemplateService : IFormTemplateService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiFormTemplateService(HttpClient http) => _http = http;

    /// <summary>Lists the caller's library (latest version per family).</summary>
    public async Task<IReadOnlyList<FormTemplateSummaryDto>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<FormTemplateSummaryDto>>("api/forms/templates", cancellationToken)
        ?? Array.Empty<FormTemplateSummaryDto>();

    /// <summary>Gets a template version by id, or null.</summary>
    public Task<FormTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetOrNullAsync($"api/forms/templates/{id}", cancellationToken);

    /// <summary>Gets a published template ready to fill, or null.</summary>
    public Task<FormTemplateDto?> GetTemplateForFillAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetOrNullAsync($"api/forms/templates/{id}/fill", cancellationToken);

    /// <summary>Creates a new template (Draft v1) and returns its id.</summary>
    public async Task<Guid> CreateAsync(CreateFormTemplateRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/forms/templates", request, cancellationToken);
        await ThrowIfBadRequestAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedTemplate>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Updates a template (draft in place; published → new draft version). Null when not found.</summary>
    public async Task<FormTemplateDto?> UpdateAsync(Guid id, UpdateFormTemplateRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/forms/templates/{id}", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await ThrowIfBadRequestAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FormTemplateDto>(cancellationToken);
    }

    /// <summary>Publishes a draft. Null when not found.</summary>
    public Task<FormTemplateDto?> PublishAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostForTemplateAsync($"api/forms/templates/{id}/publish", cancellationToken);

    /// <summary>Archives a template. Null when not found.</summary>
    public Task<FormTemplateDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostForTemplateAsync($"api/forms/templates/{id}/archive", cancellationToken);

    private async Task<FormTemplateDto?> GetOrNullAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FormTemplateDto>(cancellationToken);
    }

    private async Task<FormTemplateDto?> PostForTemplateAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(url, null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FormTemplateDto>(cancellationToken);
    }

    private static async Task ThrowIfBadRequestAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<FormError>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The form could not be saved.");
        }
    }

    private sealed record CreatedTemplate(Guid Id);
    private sealed record FormError(string Error);
}
