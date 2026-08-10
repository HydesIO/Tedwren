using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.Timesheets;

/// <summary>One effective timesheet line — a day's hours at a site (SUB-7). Corrections are folded in so this
/// carries the current hours; <see cref="IsCorrected"/> flags that an original was superseded (R16).</summary>
public sealed record TimesheetLineDto(
    Guid EntryId,
    DateOnly WorkDate,
    Guid SiteId,
    string SiteName,
    decimal Hours,
    bool IsCorrected,
    string? Author,
    string? Reason);

/// <summary>A full timesheet: its header (operative, week, status) and its effective lines (SUB-7/SUB-8).</summary>
public sealed record TimesheetDto(
    Guid Id,
    Guid CompanyId,
    Guid PersonId,
    string OperativeName,
    DateOnly WeekStart,
    TimesheetState Status,
    string StatusLabel,
    decimal TotalHours,
    DateTimeOffset? SubmittedUtc,
    string? DecidedBy,
    DateTimeOffset? DecidedUtc,
    ApprovalScope? DecisionScope,
    string? ReturnReason,
    IReadOnlyList<TimesheetLineDto> Lines);

/// <summary>A timesheet in the company/valuation-period list (MC-24) — header totals without the lines.</summary>
public sealed record TimesheetSummaryDto(
    Guid Id,
    Guid PersonId,
    string OperativeName,
    DateOnly WeekStart,
    TimesheetState Status,
    string StatusLabel,
    decimal TotalHours);

/// <summary>An operative's own real-time hours for a week (SUB-27).</summary>
public sealed record OperativeHoursDto(
    Guid PersonId,
    DateOnly WeekStart,
    decimal TotalHours,
    TimesheetState Status,
    string StatusLabel,
    IReadOnlyList<TimesheetLineDto> Lines);

/// <summary>A request to correct a line's hours — recorded as a new line referencing the original (R16).</summary>
public sealed record CorrectLineRequest(Guid EntryId, decimal CorrectedHours, string Author, string Reason);

/// <summary>A request to approve a timesheet at a chosen granularity (SUB-9).</summary>
public sealed record ApproveTimesheetRequest(string Approver, ApprovalScope Scope);

/// <summary>A request to return a timesheet for correction, with a reason (SUB-12, R18).</summary>
public sealed record ReturnTimesheetRequest(string Approver, string Reason);
