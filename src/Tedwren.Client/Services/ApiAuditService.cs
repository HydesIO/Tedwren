using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IAuditService"/> backed by the Tedwren Web API over HTTP — used when the client is configured
/// with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode.
/// </summary>
public sealed class ApiAuditService : IAuditService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiAuditService(HttpClient http) => _http = http;

    /// <summary>Searches the audit trail via the API (SF-20).</summary>
    public async Task<IReadOnlyList<AuditEntryDto>> SearchAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<AuditEntryDto>>(BuildQuery("api/audit", companyId, text, from, to), cancellationToken)
        ?? Array.Empty<AuditEntryDto>();

    /// <summary>Downloads the CSV export of the matching entries from the API (SF-20).</summary>
    public async Task<string> ExportCsvAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default) =>
        await _http.GetStringAsync(BuildQuery("api/audit/export", companyId, text, from, to), cancellationToken);

    /// <summary>Records an audit entry via the API.</summary>
    public async Task RecordAsync(RecordAuditRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/audit", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Builds the audit search query string from the optional filters.</summary>
    private static string BuildQuery(string path, Guid? companyId, string? text, DateOnly? from, DateOnly? to)
    {
        var query = new List<string>();
        if (companyId is { } c)
        {
            query.Add($"companyId={c}");
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            query.Add($"text={Uri.EscapeDataString(text)}");
        }

        if (from is { } f)
        {
            query.Add($"from={f:yyyy-MM-dd}");
        }

        if (to is { } t)
        {
            query.Add($"to={t:yyyy-MM-dd}");
        }

        return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
    }
}
