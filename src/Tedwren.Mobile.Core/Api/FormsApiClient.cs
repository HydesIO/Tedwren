using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the operative forms surface (<c>/api/mobile/forms/*</c>, M6) over the
/// <see cref="OperativeAuthMessageHandler"/>-wrapped <see cref="HttpClient"/>. Assignments + templates are cached
/// (templates by their immutable version id); the submit is queued to the outbox and posted here — idempotent
/// server-side on the request's client id (R4/R16). A non-success status throws (the sync engine classifies it).
/// </summary>
public sealed class FormsApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over the auth-handled <see cref="HttpClient"/> (BaseAddress = API root).</summary>
    public FormsApiClient(HttpClient http) => _http = http;

    /// <summary>The forms assigned to the operative (organisation/operator/attended-site scope).</summary>
    public async Task<IReadOnlyList<MobileFormAssignmentDto>> GetAssignmentsAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<MobileFormAssignmentDto>>("api/mobile/forms/assignments", cancellationToken)
        ?? Array.Empty<MobileFormAssignmentDto>();

    /// <summary>A published template version to fill (immutable; safe to cache forever). Null when not found.</summary>
    public Task<FormTemplateDto?> GetTemplateAsync(Guid versionId, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<FormTemplateDto?>($"api/mobile/forms/templates/{versionId}", cancellationToken);

    /// <summary>Submits a completed form (idempotent on <see cref="CreateFormSubmissionRequest.ClientId"/>).</summary>
    public async Task SubmitAsync(CreateFormSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/forms/submissions", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
