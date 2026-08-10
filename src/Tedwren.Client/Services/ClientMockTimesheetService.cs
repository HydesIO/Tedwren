using System.Text;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Timesheets;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ITimesheetService"/> for client <c>DataSource=Mock</c>. It fabricates a small, deterministic
/// set of timesheets — mirroring the API's mock seed so the Time &amp; Attendance screen looks the same across
/// the switch — and supports the read + workflow operations in-proc so approve/return/correct are demonstrable
/// without a backend. Excel export is exercised in <c>Api</c> mode against the real service (SUB-10).
/// </summary>
public sealed class ClientMockTimesheetService : ITimesheetService
{
    /// <summary>The demo company the fabricated timesheets belong to (matches the API mock seed).</summary>
    public static readonly Guid DemoCompanyId = Guid.Parse("44444444-4444-4444-8444-000000000001");

    /// <summary>The demo timesheet week (matches the API mock seed).</summary>
    public static readonly DateOnly DemoWeekStart = new(2026, 8, 3);

    private static readonly Guid DemoSiteId = Guid.Parse("44444444-4444-4444-8444-000000000010");
    private readonly Dictionary<Guid, TimesheetDto> _timesheets = new();

    /// <summary>Creates the service and fabricates the demo week.</summary>
    public ClientMockTimesheetService()
    {
        Seed("M. Adeyemi", TimesheetState.Submitted, new[] { 8m, 8m, 7.5m, 8m, 6m });
        Seed("J. Fletcher", TimesheetState.Approved, new[] { 8m, 8m, 8m, 8m, 8m });
        Seed("R. Okafor", TimesheetState.Draft, new[] { 7m, 8m, 8m });
    }

    /// <summary>Lists the company's timesheets for a week (MC-24).</summary>
    public Task<IReadOnlyList<TimesheetSummaryDto>> GetForCompanyWeekAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TimesheetSummaryDto> results = _timesheets.Values
            .Where(t => t.CompanyId == companyId && t.WeekStart == weekStart)
            .OrderBy(t => t.OperativeName)
            .Select(t => new TimesheetSummaryDto(t.Id, t.PersonId, t.OperativeName, t.WeekStart, t.Status, t.StatusLabel, t.TotalHours))
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Gets a timesheet by id, or null.</summary>
    public Task<TimesheetDto?> GetTimesheetAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_timesheets.GetValueOrDefault(timesheetId));

    /// <summary>Returns an operative's timesheet for a week (the fabricated set is static, so this finds an existing one).</summary>
    public Task<TimesheetDto> GetOrCreateForWeekAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        Task.FromResult(_timesheets.Values.First(t => t.CompanyId == companyId && t.PersonId == personId && t.WeekStart == weekStart));

    /// <summary>Returns an operative's own hours for a week (SUB-27).</summary>
    public async Task<OperativeHoursDto> GetOperativeHoursAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var dto = await GetOrCreateForWeekAsync(companyId, personId, weekStart, cancellationToken);
        return new OperativeHoursDto(personId, weekStart, dto.TotalHours, dto.Status, dto.StatusLabel, dto.Lines);
    }

    /// <summary>Submits a timesheet (Draft/Returned → Submitted).</summary>
    public Task<TimesheetDto?> SubmitAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        Transition(timesheetId, s => s is TimesheetState.Draft or TimesheetState.Returned,
            t => t with { Status = TimesheetState.Submitted, StatusLabel = "Submitted", SubmittedUtc = DateTimeOffset.UtcNow });

    /// <summary>Approves a submitted timesheet at the chosen granularity (SUB-9).</summary>
    public Task<TimesheetDto?> ApproveAsync(Guid timesheetId, ApproveTimesheetRequest request, CancellationToken cancellationToken = default) =>
        Transition(timesheetId, s => s is TimesheetState.Submitted,
            t => t with { Status = TimesheetState.Approved, StatusLabel = "Approved", DecidedBy = request.Approver, DecidedUtc = DateTimeOffset.UtcNow, DecisionScope = request.Scope, ReturnReason = null });

    /// <summary>Returns a submitted timesheet for correction (never denied, R18).</summary>
    public Task<TimesheetDto?> ReturnAsync(Guid timesheetId, ReturnTimesheetRequest request, CancellationToken cancellationToken = default) =>
        Transition(timesheetId, s => s is TimesheetState.Submitted,
            t => t with { Status = TimesheetState.Returned, StatusLabel = "Returned for correction", DecidedBy = request.Approver, DecidedUtc = DateTimeOffset.UtcNow, ReturnReason = request.Reason });

    /// <summary>Corrects a line's hours by folding the new value into the effective line (demo of R16).</summary>
    public Task<TimesheetDto?> CorrectLineAsync(Guid timesheetId, CorrectLineRequest request, CancellationToken cancellationToken = default)
    {
        if (!_timesheets.TryGetValue(timesheetId, out var dto) || dto.Status == TimesheetState.Approved)
        {
            return Task.FromResult<TimesheetDto?>(null);
        }

        var lines = dto.Lines
            .Select(l => l.EntryId == request.EntryId
                ? l with { Hours = request.CorrectedHours, IsCorrected = true, Author = request.Author, Reason = request.Reason }
                : l)
            .ToList();
        var updated = dto with { Lines = lines, TotalHours = lines.Sum(l => l.Hours) };
        _timesheets[timesheetId] = updated;
        return Task.FromResult<TimesheetDto?>(updated);
    }

    /// <summary>Exports a single timesheet as CSV (SUB-10).</summary>
    public Task<string> ExportTimesheetCsvAsync(Guid timesheetId, CancellationToken cancellationToken = default)
    {
        if (!_timesheets.TryGetValue(timesheetId, out var dto))
        {
            return Task.FromResult(string.Empty);
        }

        var sb = new StringBuilder("Date,Site,Hours,Corrected,Reason\n");
        foreach (var l in dto.Lines)
        {
            sb.Append(l.WorkDate.ToString("yyyy-MM-dd")).Append(',').Append(Csv(l.SiteName)).Append(',')
                .Append(l.Hours.ToString("0.##")).Append(',').Append(l.IsCorrected ? "Yes" : string.Empty)
                .Append(',').Append(Csv(l.Reason ?? string.Empty)).Append('\n');
        }

        return Task.FromResult(sb.ToString());
    }

    /// <summary>Exports the company's week as site-level CSV (SUB-10).</summary>
    public async Task<string> ExportCompanyWeekCsvAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var summaries = await GetForCompanyWeekAsync(companyId, weekStart, cancellationToken);
        var sb = new StringBuilder("Operative,Week starting,Status,Hours\n");
        foreach (var s in summaries)
        {
            sb.Append(Csv(s.OperativeName)).Append(',').Append(weekStart.ToString("yyyy-MM-dd")).Append(',')
                .Append(Csv(s.StatusLabel)).Append(',').Append(s.TotalHours.ToString("0.##")).Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>Excel export is proven in <c>Api</c> mode; the mock returns no bytes (the UI offers Excel only over the API).</summary>
    public Task<byte[]> ExportCompanyWeekXlsxAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());

    /// <summary>Applies a guarded transition and stores the result, or returns null when not allowed.</summary>
    private Task<TimesheetDto?> Transition(Guid id, Func<TimesheetState, bool> guard, Func<TimesheetDto, TimesheetDto> mutate)
    {
        if (!_timesheets.TryGetValue(id, out var dto) || !guard(dto.Status))
        {
            return Task.FromResult<TimesheetDto?>(null);
        }

        var updated = mutate(dto);
        _timesheets[id] = updated;
        return Task.FromResult<TimesheetDto?>(updated);
    }

    /// <summary>Fabricates a demo timesheet with one line per supplied day.</summary>
    private void Seed(string operative, TimesheetState status, IReadOnlyList<decimal> dailyHours)
    {
        var id = Guid.NewGuid();
        var lines = dailyHours
            .Select((h, day) => new TimesheetLineDto(Guid.NewGuid(), DemoWeekStart.AddDays(day), DemoSiteId, "Meridian Tower", h, false, null, null))
            .ToList();

        _timesheets[id] = new TimesheetDto(
            id, DemoCompanyId, Guid.NewGuid(), operative, DemoWeekStart, status, StatusLabel(status),
            lines.Sum(l => l.Hours),
            status is TimesheetState.Submitted or TimesheetState.Approved ? DateTimeOffset.UtcNow : null,
            status is TimesheetState.Approved ? "Priya Shah" : null,
            status is TimesheetState.Approved ? DateTimeOffset.UtcNow : null,
            status is TimesheetState.Approved ? ApprovalScope.All : null,
            null, lines);
    }

    /// <summary>Human-readable status label (a returned timesheet is never "denied", R18).</summary>
    private static string StatusLabel(TimesheetState status) => status switch
    {
        TimesheetState.Draft => "Draft",
        TimesheetState.Submitted => "Submitted",
        TimesheetState.Approved => "Approved",
        TimesheetState.Returned => "Returned for correction",
        _ => status.ToString(),
    };

    /// <summary>Quotes a CSV field when needed.</summary>
    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}
