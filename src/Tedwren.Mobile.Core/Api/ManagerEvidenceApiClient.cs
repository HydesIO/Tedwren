using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Evidence;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the manager evidence surface (M7): the field-evidence captures operatives recorded on the app
/// (review list + detail) and the compliance evidence-pack summary (reports), over the console-token auth handler.
/// Read-only; a non-success status throws (the evidence-pack summary is <c>hse</c>-gated and returns 403 without
/// the module — the caller shows a "not enabled" state).
/// </summary>
public sealed class ManagerEvidenceApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public ManagerEvidenceApiClient(HttpClient http) => _http = http;

    /// <summary>The company's evidence captures (newest first), optionally filtered to one operative.</summary>
    public async Task<IReadOnlyList<EvidenceCaptureDto>> GetCapturesAsync(Guid? personId = null, CancellationToken cancellationToken = default)
    {
        var url = personId is { } pid ? $"api/evidence-captures?personId={pid}" : "api/evidence-captures";
        return await _http.GetFromJsonAsync<IReadOnlyList<EvidenceCaptureDto>>(url, cancellationToken) ?? Array.Empty<EvidenceCaptureDto>();
    }

    /// <summary>A single evidence capture the company owns, or null when not found.</summary>
    public Task<EvidenceCaptureDto?> GetCaptureAsync(Guid id, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<EvidenceCaptureDto>($"api/evidence-captures/{id}", cancellationToken);

    /// <summary>The compliance evidence-pack summary (section counts) for the reports surface.</summary>
    public Task<EvidenceSummaryDto?> GetEvidenceSummaryAsync(CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<EvidenceSummaryDto>("api/evidence/summary", cancellationToken);
}
