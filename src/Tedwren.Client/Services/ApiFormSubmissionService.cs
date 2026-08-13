using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IFormSubmissionService"/> backed by the Tedwren Web API over HTTP (the Forms Library, PRD-Phase 2).
/// Completing, listing and reviewing forms all go through the server, which validates and scopes by tenant (R15).
/// The file-download helper is served from the API's file route.
/// </summary>
public sealed class ApiFormSubmissionService : IFormSubmissionService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiFormSubmissionService(HttpClient http) => _http = http;

    /// <summary>Lists the caller's submissions.</summary>
    public async Task<IReadOnlyList<FormSubmissionSummaryDto>> GetSubmissionsAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<FormSubmissionSummaryDto>>("api/forms/submissions", cancellationToken)
        ?? Array.Empty<FormSubmissionSummaryDto>();

    /// <summary>Gets a submission with answers and file metadata, or null.</summary>
    public async Task<FormSubmissionDetailDto?> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/forms/submissions/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FormSubmissionDetailDto>(cancellationToken);
    }

    /// <summary>Submits a completed form and returns its id; surfaces a validation failure as an exception.</summary>
    public async Task<Guid> SubmitAsync(CreateFormSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/forms/submissions", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<FormError>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The form could not be submitted.");
        }

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Created>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Approves a submission. Null when not found.</summary>
    public Task<FormSubmissionDetailDto?> ApproveAsync(Guid id, ReviewFormSubmissionRequest request, CancellationToken cancellationToken = default) =>
        ReviewAsync($"api/forms/submissions/{id}/approve", request, cancellationToken);

    /// <summary>Rejects a submission (reason required). Null when not found.</summary>
    public Task<FormSubmissionDetailDto?> RejectAsync(Guid id, ReviewFormSubmissionRequest request, CancellationToken cancellationToken = default) =>
        ReviewAsync($"api/forms/submissions/{id}/reject", request, cancellationToken);

    /// <summary>Downloads a captured file's bytes, or null.</summary>
    public async Task<FormFileContent?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/forms/submissions/files/{fileId}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "file";
        return new FormFileContent(fileName, contentType, bytes);
    }

    private async Task<FormSubmissionDetailDto?> ReviewAsync(string url, ReviewFormSubmissionRequest request, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(url, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<FormError>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The submission could not be reviewed.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FormSubmissionDetailDto>(cancellationToken);
    }

    private sealed record Created(Guid Id);
    private sealed record FormError(string Error);
}
