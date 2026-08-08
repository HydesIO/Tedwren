using Tedwren.Abstractions.Contracts.Timesheets;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The timesheet service (SUB-7–SUB-12, SUB-27, MC-24). A timesheet is a first-class, stateful, approvable
/// object rolled up from attendance; corrections are new lines (R16); approval is configurable in granularity
/// (SUB-9); export is available as CSV and Excel at operative and site level (SUB-10). Reads are scoped to the
/// owning company (R15).
/// </summary>
public interface ITimesheetService
{
    /// <summary>Lists a company's timesheets for a week — the valuation-period view (MC-24).</summary>
    Task<IReadOnlyList<TimesheetSummaryDto>> GetForCompanyWeekAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default);

    /// <summary>Gets (creating and rolling up from attendance on first access) an operative's timesheet for a week.</summary>
    Task<TimesheetDto?> GetTimesheetAsync(Guid timesheetId, CancellationToken cancellationToken = default);

    /// <summary>Gets or creates an operative's timesheet for a week, rolling up attendance (SUB-7).</summary>
    Task<TimesheetDto> GetOrCreateForWeekAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default);

    /// <summary>Returns an operative's own real-time hours for a week (SUB-27).</summary>
    Task<OperativeHoursDto> GetOperativeHoursAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default);

    /// <summary>Submits a timesheet for approval (SUB-27). Returns the updated timesheet, or null if not found.</summary>
    Task<TimesheetDto?> SubmitAsync(Guid timesheetId, CancellationToken cancellationToken = default);

    /// <summary>Approves a submitted timesheet at the chosen granularity (SUB-9). Null if not found.</summary>
    Task<TimesheetDto?> ApproveAsync(Guid timesheetId, ApproveTimesheetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a submitted timesheet for correction, with a reason (SUB-12). Null if not found.</summary>
    Task<TimesheetDto?> ReturnAsync(Guid timesheetId, ReturnTimesheetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Corrects a line's hours — a new line referencing the original (R16). Null if not found.</summary>
    Task<TimesheetDto?> CorrectLineAsync(Guid timesheetId, CorrectLineRequest request, CancellationToken cancellationToken = default);

    /// <summary>Exports a timesheet as CSV (SUB-10).</summary>
    Task<string> ExportTimesheetCsvAsync(Guid timesheetId, CancellationToken cancellationToken = default);

    /// <summary>Exports a company's week of timesheets as CSV at site level (SUB-10).</summary>
    Task<string> ExportCompanyWeekCsvAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default);

    /// <summary>Exports a company's week of timesheets as an Excel (.xlsx) workbook (SUB-10).</summary>
    Task<byte[]> ExportCompanyWeekXlsxAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default);
}
