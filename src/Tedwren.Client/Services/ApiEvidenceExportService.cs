using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IEvidenceExportService"/> backed by the Tedwren Web API (<c>/api/evidence</c>). The caller's company
/// is resolved server-side from the request claims (R15), so the <c>companyId</c> argument is not sent.
/// </summary>
public sealed class ApiEvidenceExportService : IEvidenceExportService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiEvidenceExportService(HttpClient http) => _http = http;

    /// <summary>Gets the sections the export will contain and their record counts.</summary>
    public async Task<EvidenceSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<EvidenceSummaryDto>("api/evidence/summary", cancellationToken)
            ?? new EvidenceSummaryDto(Array.Empty<EvidenceSectionDto>(), 0, DateTimeOffset.UtcNow);

    /// <summary>Downloads the generated evidence pack (ZIP) from the API.</summary>
    public async Task<EvidenceExportFileDto> BuildZipAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/evidence/export", cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/zip";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"evidence-pack-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip";
        return new EvidenceExportFileDto(fileName, contentType, content);
    }
}
