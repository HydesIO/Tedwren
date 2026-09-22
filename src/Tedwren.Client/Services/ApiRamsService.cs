using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IRamsService"/> backed by the Tedwren Web API (<c>/api/rams</c>). The caller's company and reviewer
/// identity are resolved server-side from the request claims (R15), so the <c>companyId</c>/<c>reviewer</c>
/// arguments are not sent.
/// </summary>
public sealed class ApiRamsService : IRamsService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiRamsService(HttpClient http) => _http = http;

    /// <summary>Submits a RAMS via the API and returns it with its reference.</summary>
    public async Task<RamsSubmissionDto> SubmitAsync(Guid companyId, SubmitRamsRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/rams", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RamsSubmissionDto>(cancellationToken))!;
    }

    /// <summary>Gets the caller company's RAMS submissions, newest first.</summary>
    public async Task<IReadOnlyList<RamsSubmissionDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<RamsSubmissionDto>>("api/rams", cancellationToken) ?? Array.Empty<RamsSubmissionDto>();

    /// <summary>Gets the submissions awaiting review.</summary>
    public async Task<IReadOnlyList<RamsSubmissionDto>> GetReviewQueueAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<RamsSubmissionDto>>("api/rams/queue", cancellationToken) ?? Array.Empty<RamsSubmissionDto>();

    /// <summary>Approves a RAMS via the API; returns true on success.</summary>
    public async Task<bool> ApproveAsync(Guid companyId, Guid id, string reviewer, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/rams/{id}/approve", content: null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Approves a RAMS with comments (a required note) via the API; returns true on success.</summary>
    public async Task<bool> ApproveWithCommentsAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/rams/{id}/approve-with-comments", new ReviewRamsRequest(note), cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Not used from the browser client — the subcontractor-RAMS bridge (spec Stage 2→3) runs server-side in the API.</summary>
    public Task<RamsSubmissionDto> RegisterFromDocumentAsync(Guid companyId, RegisterRamsFromDocumentRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Registering a RAMS from an uploaded document happens server-side during subcontractor onboarding.");

    /// <summary>Rejects a RAMS (with a note) via the API; returns true on success.</summary>
    public async Task<bool> RejectAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/rams/{id}/reject", new ReviewRamsRequest(note), cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Returns a RAMS for changes (with a note) via the API; returns true on success.</summary>
    public async Task<bool> ReturnAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/rams/{id}/return", new ReviewRamsRequest(note), cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
