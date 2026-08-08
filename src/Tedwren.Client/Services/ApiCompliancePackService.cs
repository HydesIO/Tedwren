using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.CompliancePacks;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ICompliancePackService"/> backed by the Tedwren Web API over HTTP — used when the client is
/// configured with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode.
/// </summary>
public sealed class ApiCompliancePackService : ICompliancePackService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiCompliancePackService(HttpClient http) => _http = http;

    /// <summary>Previews readiness for a selection (SUB-14).</summary>
    public async Task<IReadOnlyList<ReadinessIssueDto>> PreviewReadinessAsync(Guid companyId, IReadOnlyList<Guid> operativeIds, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/packs/readiness", new { companyId, operativeIds }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<ReadinessIssueDto>>(cancellationToken) ?? Array.Empty<ReadinessIssueDto>();
    }

    /// <summary>Builds/sends a pack; a 409 carries the unacknowledged readiness issues (SUB-14).</summary>
    public async Task<BuildPackResult> BuildAsync(BuildPackRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/packs", request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict)
        {
            return (await response.Content.ReadFromJsonAsync<BuildPackResult>(cancellationToken))!;
        }

        response.EnsureSuccessStatusCode();
        return new BuildPackResult(false, null, null, null, Array.Empty<ReadinessIssueDto>());
    }

    /// <summary>Lists a company's packs (SUB-20).</summary>
    public async Task<IReadOnlyList<PackListItemDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<PackListItemDto>>($"api/packs/company/{companyId}", cancellationToken)
        ?? Array.Empty<PackListItemDto>();

    /// <summary>Recipient view via token + passcode (R8).</summary>
    public async Task<PackAccessResult> ViewAsync(string token, string passcode, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/packs/view?token={Uri.EscapeDataString(token)}&passcode={Uri.EscapeDataString(passcode)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new PackAccessResult(false, "Access refused.", null);
        }

        response.EnsureSuccessStatusCode();
        var pack = await response.Content.ReadFromJsonAsync<CompliancePackViewDto>(cancellationToken);
        return new PackAccessResult(true, null, pack);
    }

    /// <summary>Recipient CSV download (SUB-16).</summary>
    public Task<byte[]?> DownloadCsvAsync(string token, string passcode, CancellationToken cancellationToken = default) =>
        DownloadAsync("csv", token, passcode, cancellationToken);

    /// <summary>Recipient ZIP download (SUB-16).</summary>
    public Task<byte[]?> DownloadZipAsync(string token, string passcode, CancellationToken cancellationToken = default) =>
        DownloadAsync("zip", token, passcode, cancellationToken);

    /// <summary>Recipient PDF download (SUB-16).</summary>
    public Task<byte[]?> DownloadPdfAsync(string token, string passcode, CancellationToken cancellationToken = default) =>
        DownloadAsync("pdf", token, passcode, cancellationToken);

    /// <summary>Revokes a pack (SUB-21).</summary>
    public async Task<bool> RevokeAsync(Guid companyId, Guid packId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/packs/company/{companyId}/{packId}/revoke", content: null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Downloads a pack in the given format, or null when access is refused.</summary>
    private async Task<byte[]?> DownloadAsync(string format, string token, string passcode, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync($"api/packs/download.{format}?token={Uri.EscapeDataString(token)}&passcode={Uri.EscapeDataString(passcode)}", cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(cancellationToken) : null;
    }
}
