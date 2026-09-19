using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the operative capture endpoints (M5): the multipart photo upload plus the evidence and hazard
/// submits, over the <see cref="OperativeAuthMessageHandler"/>-wrapped <see cref="HttpClient"/>. A non-success status
/// throws (the sync engine classifies transient vs permanent). The submits are idempotent server-side on the
/// request's client id (R4/R16).
/// </summary>
public sealed class CaptureApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over the auth-handled <see cref="HttpClient"/> (BaseAddress = API root).</summary>
    public CaptureApiClient(HttpClient http) => _http = http;

    /// <summary>Uploads a photo (multipart) and returns its image-store reference.</summary>
    public async Task<string> UploadPhotoAsync(byte[] bytes, string contentType, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(part, "file", "photo");

        using var response = await _http.PostAsync("api/mobile/uploads", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MobileUploadResultDto>(cancellationToken);
        return result!.Reference;
    }

    /// <summary>Submits a generic evidence capture (ungated).</summary>
    public async Task ReportEvidenceAsync(MobileReportEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/evidence", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Submits a hazard / near-miss report (hse-gated server-side).</summary>
    public async Task ReportHazardAsync(MobileReportHazardRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/hazards", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
