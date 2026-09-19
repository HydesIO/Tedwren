using Tedwren.Abstractions.Contracts.Safety;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Hazard / near-miss reporting (PRD §8.2): a worker reports what they saw (with an optional photo + location), a
/// responsible person triages it (assign → close), and the register yields leading-indicator statistics. Scoped to
/// the reporting company (R15); part of the paid HSE module.
/// </summary>
public interface IHazardReportService
{
    /// <summary>Records a hazard report and returns it with its reference.</summary>
    Task<HazardReportDto> ReportAsync(Guid companyId, string reportedBy, ReportHazardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's hazard reports, newest first.</summary>
    Task<IReadOnlyList<HazardReportDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single hazard report, or null when missing/cross-tenant (R15).</summary>
    Task<HazardReportDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Assigns a report to a responsible person (sets it to Assigned). Returns false when missing/cross-tenant.</summary>
    Task<bool> AssignAsync(Guid companyId, Guid id, string assignedTo, string? category, CancellationToken cancellationToken = default);

    /// <summary>Closes a report out with a required note. Returns false when missing/cross-tenant.</summary>
    Task<bool> CloseAsync(Guid companyId, Guid id, string note, CancellationToken cancellationToken = default);

    /// <summary>Returns leading-indicator counts (by status and kind) for the company's hazard register.</summary>
    Task<HazardStatsDto> GetStatsAsync(Guid companyId, CancellationToken cancellationToken = default);
}
