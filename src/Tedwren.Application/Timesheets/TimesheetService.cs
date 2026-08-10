using Tedwren.Abstractions.Contracts.Timesheets;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Export;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using NeutralScope = Tedwren.Abstractions.Common.ApprovalScope;
using NeutralState = Tedwren.Abstractions.Common.TimesheetState;

namespace Tedwren.Application.Timesheets;

/// <summary>
/// The timesheet business service (SUB-7–SUB-12, SUB-27, MC-24). A timesheet is a first-class, stateful,
/// approvable object rolled up from the append-only attendance log; a correction is a new line referencing the
/// original (R16); approval records who/when and at what granularity (SUB-9); the vocabulary is neutral — a
/// timesheet is returned, never denied (R18). Store-agnostic.
/// </summary>
public sealed class TimesheetService : ITimesheetService
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IAttendanceRepository _attendance;
    private readonly IEngagementRepository _engagements;
    private readonly ISiteRepository _sites;

    /// <summary>Creates the service over its repositories.</summary>
    public TimesheetService(
        ITimesheetRepository timesheets, IAttendanceRepository attendance,
        IEngagementRepository engagements, ISiteRepository sites)
    {
        _timesheets = timesheets;
        _attendance = attendance;
        _engagements = engagements;
        _sites = sites;
    }

    /// <summary>Lists a company's timesheets for a week — the valuation-period view (MC-24).</summary>
    public async Task<IReadOnlyList<TimesheetSummaryDto>> GetForCompanyWeekAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var headers = await _timesheets.GetByCompanyWeekAsync(companyId, weekStart, cancellationToken);
        var summaries = new List<TimesheetSummaryDto>(headers.Count);
        foreach (var header in headers)
        {
            var entries = await _timesheets.GetEntriesAsync(header.Id, cancellationToken);
            summaries.Add(new TimesheetSummaryDto(
                header.Id, header.PersonId, header.OperativeName, header.WeekStart,
                ToState(header.Status), Label(header.Status), TotalHours(entries)));
        }

        return summaries;
    }

    /// <summary>Gets a timesheet by id, or null.</summary>
    public async Task<TimesheetDto?> GetTimesheetAsync(Guid timesheetId, CancellationToken cancellationToken = default)
    {
        var header = await _timesheets.GetAsync(timesheetId, cancellationToken);
        if (header is null)
        {
            return null;
        }

        var entries = await _timesheets.GetEntriesAsync(timesheetId, cancellationToken);
        return ToDto(header, entries);
    }

    /// <summary>Gets or creates an operative's timesheet for a week, rolling up attendance on first access (SUB-7).</summary>
    public async Task<TimesheetDto> GetOrCreateForWeekAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var existing = await _timesheets.GetByWeekAsync(companyId, personId, weekStart, cancellationToken);
        if (existing is not null)
        {
            var currentEntries = await _timesheets.GetEntriesAsync(existing.Id, cancellationToken);
            return ToDto(existing, currentEntries);
        }

        var fromUtc = new DateTimeOffset(weekStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = fromUtc.AddDays(7);
        var attendance = await _attendance.GetByPersonInRangeAsync(personId, fromUtc, toUtc, cancellationToken);
        var rolledUp = TimesheetHoursCalculator.Roll(attendance);

        var engagement = await _engagements.GetByCompanyAndPersonAsync(companyId, personId, cancellationToken);
        var timesheet = new Timesheet
        {
            CompanyId = companyId,
            PersonId = personId,
            OperativeName = engagement?.Name ?? "Operative",
            WeekStart = weekStart,
            Status = TimesheetStatus.Draft,
        };

        var siteNames = new Dictionary<Guid, string>();
        var entries = new List<TimesheetEntry>(rolledUp.Count);
        foreach (var line in rolledUp)
        {
            entries.Add(new TimesheetEntry
            {
                TimesheetId = timesheet.Id,
                WorkDate = line.WorkDate,
                SiteId = line.SiteId,
                SiteName = await SiteNameAsync(line.SiteId, siteNames, cancellationToken),
                Hours = line.Hours,
            });
        }

        await _timesheets.AddAsync(timesheet, entries, cancellationToken);
        return ToDto(timesheet, entries);
    }

    /// <summary>Returns an operative's own real-time hours for a week (SUB-27).</summary>
    public async Task<OperativeHoursDto> GetOperativeHoursAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var dto = await GetOrCreateForWeekAsync(companyId, personId, weekStart, cancellationToken);
        return new OperativeHoursDto(personId, weekStart, dto.TotalHours, dto.Status, dto.StatusLabel, dto.Lines);
    }

    /// <summary>Submits a timesheet for approval (SUB-27).</summary>
    public Task<TimesheetDto?> SubmitAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        MutateAsync(timesheetId, TimesheetWorkflow.CanSubmit, header =>
        {
            header.Status = TimesheetStatus.Submitted;
            header.SubmittedUtc = DateTimeOffset.UtcNow;
        }, cancellationToken);

    /// <summary>Approves a submitted timesheet at the chosen granularity (SUB-9).</summary>
    public Task<TimesheetDto?> ApproveAsync(Guid timesheetId, ApproveTimesheetRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(timesheetId, TimesheetWorkflow.CanApprove, header =>
        {
            header.Status = TimesheetStatus.Approved;
            header.DecidedBy = request.Approver;
            header.DecidedUtc = DateTimeOffset.UtcNow;
            header.DecisionScope = ToDomainScope(request.Scope);
            header.ReturnReason = null;
        }, cancellationToken);

    /// <summary>Returns a submitted timesheet for correction, with a reason (SUB-12, R18).</summary>
    public Task<TimesheetDto?> ReturnAsync(Guid timesheetId, ReturnTimesheetRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(timesheetId, TimesheetWorkflow.CanReturn, header =>
        {
            header.Status = TimesheetStatus.Returned;
            header.DecidedBy = request.Approver;
            header.DecidedUtc = DateTimeOffset.UtcNow;
            header.ReturnReason = request.Reason;
        }, cancellationToken);

    /// <summary>Corrects a line's hours — a new line referencing the original, retaining it (R16).</summary>
    public async Task<TimesheetDto?> CorrectLineAsync(Guid timesheetId, CorrectLineRequest request, CancellationToken cancellationToken = default)
    {
        var header = await _timesheets.GetAsync(timesheetId, cancellationToken);
        if (header is null || !TimesheetWorkflow.CanCorrect(header.Status))
        {
            return null;
        }

        var entries = await _timesheets.GetEntriesAsync(timesheetId, cancellationToken);
        var original = entries.FirstOrDefault(e => e.Id == request.EntryId);
        if (original is null)
        {
            return null;
        }

        var correction = new TimesheetEntry
        {
            TimesheetId = timesheetId,
            WorkDate = original.WorkDate,
            SiteId = original.SiteId,
            SiteName = original.SiteName,
            Hours = request.CorrectedHours,
            CorrectsEntryId = original.Id,
            Author = request.Author,
            Reason = request.Reason,
        };
        await _timesheets.AddEntryAsync(correction, cancellationToken);

        var updated = await _timesheets.GetEntriesAsync(timesheetId, cancellationToken);
        return ToDto(header, updated);
    }

    /// <summary>Exports a single timesheet as CSV (SUB-10).</summary>
    public async Task<string> ExportTimesheetCsvAsync(Guid timesheetId, CancellationToken cancellationToken = default)
    {
        var header = await _timesheets.GetAsync(timesheetId, cancellationToken);
        if (header is null)
        {
            return string.Empty;
        }

        var entries = await _timesheets.GetEntriesAsync(timesheetId, cancellationToken);
        return CsvWriter.Write(TimesheetSheet(header, EffectiveLines(entries)));
    }

    /// <summary>Exports a company's week of timesheets as site-level CSV (SUB-10).</summary>
    public async Task<string> ExportCompanyWeekCsvAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        CsvWriter.Write(await CompanyWeekSheetAsync(companyId, weekStart, cancellationToken));

    /// <summary>Exports a company's week of timesheets as an Excel (.xlsx) workbook (SUB-10).</summary>
    public async Task<byte[]> ExportCompanyWeekXlsxAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        XlsxWriter.Write(await CompanyWeekSheetAsync(companyId, weekStart, cancellationToken));

    /// <summary>Applies a guarded workflow mutation to a timesheet header and persists it.</summary>
    private async Task<TimesheetDto?> MutateAsync(Guid timesheetId, Func<TimesheetStatus, bool> guard, Action<Timesheet> mutate, CancellationToken cancellationToken)
    {
        var header = await _timesheets.GetAsync(timesheetId, cancellationToken);
        if (header is null || !guard(header.Status))
        {
            return null;
        }

        mutate(header);
        await _timesheets.UpdateAsync(header, cancellationToken);
        var entries = await _timesheets.GetEntriesAsync(timesheetId, cancellationToken);
        return ToDto(header, entries);
    }

    /// <summary>Resolves and caches a site name.</summary>
    private async Task<string> SiteNameAsync(Guid siteId, IDictionary<Guid, string> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(siteId, out var name))
        {
            return name;
        }

        var site = await _sites.GetByIdAsync(siteId, cancellationToken);
        return cache[siteId] = site?.Name ?? "Site";
    }

    /// <summary>Builds the company-week site-level report sheet (shared by CSV and Excel export).</summary>
    private async Task<TabularSheet> CompanyWeekSheetAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken)
    {
        var headers = await _timesheets.GetByCompanyWeekAsync(companyId, weekStart, cancellationToken);
        var rows = new List<IReadOnlyList<string>>();
        foreach (var header in headers.OrderBy(h => h.OperativeName))
        {
            var entries = await _timesheets.GetEntriesAsync(header.Id, cancellationToken);
            foreach (var bySite in EffectiveLines(entries).GroupBy(l => l.SiteName).OrderBy(g => g.Key))
            {
                rows.Add(new[]
                {
                    header.OperativeName,
                    bySite.Key,
                    weekStart.ToString("yyyy-MM-dd"),
                    Label(header.Status),
                    bySite.Sum(l => l.Hours).ToString("0.##"),
                });
            }
        }

        return new TabularSheet(
            $"Timesheets {weekStart:yyyy-MM-dd}",
            new[] { "Operative", "Site", "Week starting", "Status", "Hours" },
            rows);
    }

    /// <summary>Builds a single-timesheet per-line report sheet.</summary>
    private static TabularSheet TimesheetSheet(Timesheet header, IReadOnlyList<EffectiveLine> lines) => new(
        $"{header.OperativeName} {header.WeekStart:yyyy-MM-dd}",
        new[] { "Date", "Site", "Hours", "Corrected", "Reason" },
        lines.Select(IReadOnlyList<string> (l) => new[]
        {
            l.WorkDate.ToString("yyyy-MM-dd"),
            l.SiteName,
            l.Hours.ToString("0.##"),
            l.IsCorrected ? "Yes" : string.Empty,
            l.Reason ?? string.Empty,
        }).ToList());

    /// <summary>Maps a timesheet + its lines to the DTO, folding corrections into effective lines.</summary>
    private static TimesheetDto ToDto(Timesheet header, IReadOnlyList<TimesheetEntry> entries)
    {
        var lines = EffectiveLines(entries);
        return new TimesheetDto(
            header.Id, header.CompanyId, header.PersonId, header.OperativeName, header.WeekStart,
            ToState(header.Status), Label(header.Status), lines.Sum(l => l.Hours),
            header.SubmittedUtc, header.DecidedBy, header.DecidedUtc,
            header.DecisionScope is { } scope ? ToNeutralScope(scope) : null,
            header.ReturnReason,
            lines.Select(l => new TimesheetLineDto(
                l.EntryId, l.WorkDate, l.SiteId, l.SiteName, l.Hours, l.IsCorrected, l.Author, l.Reason)).ToList());
    }

    /// <summary>The current, correction-folded lines: for each (day, site) the most recent line wins; originals are retained but not shown as separate lines (R16).</summary>
    private static IReadOnlyList<EffectiveLine> EffectiveLines(IReadOnlyList<TimesheetEntry> entries) =>
        entries
            .GroupBy(e => (e.WorkDate, e.SiteId))
            .Select(group =>
            {
                var latest = group.OrderBy(e => e.CreatedUtc).Last();
                var corrected = group.Count() > 1 || latest.IsCorrection;
                return new EffectiveLine(latest.Id, latest.WorkDate, latest.SiteId, latest.SiteName, latest.Hours, corrected, latest.Author, latest.Reason);
            })
            .OrderBy(l => l.WorkDate).ThenBy(l => l.SiteName)
            .ToList();

    /// <summary>Sums the effective hours across a timesheet's lines.</summary>
    private static decimal TotalHours(IReadOnlyList<TimesheetEntry> entries) => EffectiveLines(entries).Sum(l => l.Hours);

    /// <summary>A correction-folded line used for DTO mapping and export.</summary>
    private sealed record EffectiveLine(Guid EntryId, DateOnly WorkDate, Guid SiteId, string SiteName, decimal Hours, bool IsCorrected, string? Author, string? Reason);

    /// <summary>Human-readable status label; a returned timesheet is never "denied" (R18).</summary>
    private static string Label(TimesheetStatus status) => status switch
    {
        TimesheetStatus.Draft => "Draft",
        TimesheetStatus.Submitted => "Submitted",
        TimesheetStatus.Approved => "Approved",
        TimesheetStatus.Returned => "Returned for correction",
        _ => status.ToString(),
    };

    /// <summary>Maps the domain status to the neutral contract state.</summary>
    private static NeutralState ToState(TimesheetStatus status) => status switch
    {
        TimesheetStatus.Draft => NeutralState.Draft,
        TimesheetStatus.Submitted => NeutralState.Submitted,
        TimesheetStatus.Approved => NeutralState.Approved,
        TimesheetStatus.Returned => NeutralState.Returned,
        _ => NeutralState.Draft,
    };

    /// <summary>Maps the domain approval scope to the neutral contract scope.</summary>
    private static NeutralScope ToNeutralScope(ApprovalScope scope) => scope switch
    {
        ApprovalScope.Line => NeutralScope.Line,
        ApprovalScope.Site => NeutralScope.Site,
        ApprovalScope.Project => NeutralScope.Project,
        _ => NeutralScope.All,
    };

    /// <summary>Maps the neutral contract approval scope to the domain scope.</summary>
    private static ApprovalScope ToDomainScope(NeutralScope scope) => scope switch
    {
        NeutralScope.Line => ApprovalScope.Line,
        NeutralScope.Site => ApprovalScope.Site,
        NeutralScope.Project => ApprovalScope.Project,
        _ => ApprovalScope.All,
    };
}
