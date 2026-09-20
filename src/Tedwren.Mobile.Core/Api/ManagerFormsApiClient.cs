using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the manager forms surface (M7): the template library, assignments (assign / remove) and
/// submission review (list, view, approve / reject with a note), over the console-token auth handler. Reuses the
/// existing <c>/api/forms/*</c> endpoints unchanged. Reads throw on a non-success status; writes surface a failure
/// (e.g. a read-only Auditor's 403) as an <see cref="ApiException"/>.
/// </summary>
public sealed class ManagerFormsApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public ManagerFormsApiClient(HttpClient http) => _http = http;

    /// <summary>The company's form templates (library rows).</summary>
    public async Task<IReadOnlyList<FormTemplateSummaryDto>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<FormTemplateSummaryDto>>("api/forms/templates", cancellationToken) ?? Array.Empty<FormTemplateSummaryDto>();

    /// <summary>A single template version (its sections + fields), for resolving answer labels in review, or null.</summary>
    public Task<FormTemplateDto?> GetTemplateAsync(Guid versionId, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<FormTemplateDto>($"api/forms/templates/{versionId}", cancellationToken);

    /// <summary>The bytes of a file captured against a submission (an attachment/photo), or null when unavailable (R9).</summary>
    public async Task<byte[]?> GetSubmissionFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/forms/submissions/files/{fileId}", cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(cancellationToken) : null;
    }

    /// <summary>The company's current form assignments.</summary>
    public async Task<IReadOnlyList<FormAssignmentDto>> GetAssignmentsAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<FormAssignmentDto>>("api/forms/assignments", cancellationToken) ?? Array.Empty<FormAssignmentDto>();

    /// <summary>Creates a form assignment (organisation / site / operator + schedule + failure-alert email).</summary>
    public async Task CreateAssignmentAsync(CreateFormAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/forms/assignments", request, cancellationToken);
        EnsureSuccess(response, "Create assignment");
    }

    /// <summary>Removes a form assignment.</summary>
    public async Task DeleteAssignmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"api/forms/assignments/{id}", cancellationToken);
        EnsureSuccess(response, "Remove assignment");
    }

    /// <summary>The company's form submissions (review list).</summary>
    public async Task<IReadOnlyList<FormSubmissionSummaryDto>> GetSubmissionsAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<FormSubmissionSummaryDto>>("api/forms/submissions", cancellationToken) ?? Array.Empty<FormSubmissionSummaryDto>();

    /// <summary>A single submission's detail (answers + file metadata + review note), or null when not found.</summary>
    public Task<FormSubmissionDetailDto?> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<FormSubmissionDetailDto>($"api/forms/submissions/{id}", cancellationToken);

    /// <summary>Approves a submitted form, with an optional note.</summary>
    public Task<FormSubmissionDetailDto?> ApproveAsync(Guid id, string? note, CancellationToken cancellationToken = default) =>
        ReviewAsync($"api/forms/submissions/{id}/approve", note, cancellationToken);

    /// <summary>Rejects a submitted form, with a required note.</summary>
    public Task<FormSubmissionDetailDto?> RejectAsync(Guid id, string? note, CancellationToken cancellationToken = default) =>
        ReviewAsync($"api/forms/submissions/{id}/reject", note, cancellationToken);

    private async Task<FormSubmissionDetailDto?> ReviewAsync(string url, string? note, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(url, new ReviewFormSubmissionRequest(note), cancellationToken);
        EnsureSuccess(response, "Review submission");
        return await response.Content.ReadFromJsonAsync<FormSubmissionDetailDto>(cancellationToken);
    }

    private static void EnsureSuccess(HttpResponseMessage response, string action)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"{action} failed ({(int)response.StatusCode}).");
        }
    }
}
