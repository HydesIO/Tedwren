using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Timesheets;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ITimesheetService"/> backed by the Tedwren Web API over HTTP — used when the client is
/// configured with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode.
/// </summary>
public sealed class ApiTimesheetService : ITimesheetService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiTimesheetService(HttpClient http) => _http = http;

    /// <summary>Lists a company's timesheets for a week (MC-24).</summary>
    public async Task<IReadOnlyList<TimesheetSummaryDto>> GetForCompanyWeekAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<TimesheetSummaryDto>>($"api/timesheets/company/{companyId}?week={Week(weekStart)}", cancellationToken)
        ?? Array.Empty<TimesheetSummaryDto>();

    /// <summary>Gets a timesheet by id, or null.</summary>
    public async Task<TimesheetDto?> GetTimesheetAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<TimesheetDto>($"api/timesheets/{timesheetId}", cancellationToken);

    /// <summary>Gets or creates an operative's timesheet for a week (SUB-7).</summary>
    public async Task<TimesheetDto> GetOrCreateForWeekAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        (await _http.GetFromJsonAsync<TimesheetDto>($"api/timesheets/operative?companyId={companyId}&personId={personId}&week={Week(weekStart)}", cancellationToken))!;

    /// <summary>Returns an operative's own real-time hours for a week (SUB-27).</summary>
    public async Task<OperativeHoursDto> GetOperativeHoursAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        (await _http.GetFromJsonAsync<OperativeHoursDto>($"api/timesheets/operative/hours?companyId={companyId}&personId={personId}&week={Week(weekStart)}", cancellationToken))!;

    /// <summary>Submits a timesheet for approval (SUB-27).</summary>
    public Task<TimesheetDto?> SubmitAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/timesheets/{timesheetId}/submit", (object?)null, cancellationToken);

    /// <summary>Approves a submitted timesheet at the chosen granularity (SUB-9).</summary>
    public Task<TimesheetDto?> ApproveAsync(Guid timesheetId, ApproveTimesheetRequest request, CancellationToken cancellationToken = default) =>
        PostAsync($"api/timesheets/{timesheetId}/approve", request, cancellationToken);

    /// <summary>Returns a submitted timesheet for correction (SUB-12).</summary>
    public Task<TimesheetDto?> ReturnAsync(Guid timesheetId, ReturnTimesheetRequest request, CancellationToken cancellationToken = default) =>
        PostAsync($"api/timesheets/{timesheetId}/return", request, cancellationToken);

    /// <summary>Corrects a line's hours — appends a new line (R16).</summary>
    public Task<TimesheetDto?> CorrectLineAsync(Guid timesheetId, CorrectLineRequest request, CancellationToken cancellationToken = default) =>
        PostAsync($"api/timesheets/{timesheetId}/correct", request, cancellationToken);

    /// <summary>Downloads a single timesheet as CSV (SUB-10).</summary>
    public Task<string> ExportTimesheetCsvAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        _http.GetStringAsync($"api/timesheets/{timesheetId}/export.csv", cancellationToken);

    /// <summary>Downloads a company's week of timesheets as CSV (SUB-10).</summary>
    public Task<string> ExportCompanyWeekCsvAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        _http.GetStringAsync($"api/timesheets/company/{companyId}/export.csv?week={Week(weekStart)}", cancellationToken);

    /// <summary>Downloads a company's week of timesheets as an Excel workbook (SUB-10).</summary>
    public Task<byte[]> ExportCompanyWeekXlsxAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        _http.GetByteArrayAsync($"api/timesheets/company/{companyId}/export.xlsx?week={Week(weekStart)}", cancellationToken);

    /// <summary>Posts a body and reads back the updated timesheet, or null on a not-found response.</summary>
    private async Task<TimesheetDto?> PostAsync(string url, object? body, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(url, body, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TimesheetDto>(cancellationToken)
            : null;
    }

    /// <summary>Formats a week-start date for the query string.</summary>
    private static string Week(DateOnly weekStart) => weekStart.ToString("yyyy-MM-dd");
}
