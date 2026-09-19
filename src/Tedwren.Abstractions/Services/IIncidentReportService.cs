using Tedwren.Abstractions.Contracts.Safety;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Accident / incident recording (PRD §8.2): a structured record with an investigation (causes + corrective
/// actions), a RIDDOR-reportable flag with its category, and close-out. Scoped to the company (R15); part of the
/// paid HSE module.
/// </summary>
public interface IIncidentReportService
{
    /// <summary>Records an accident/incident and returns it with its reference.</summary>
    Task<IncidentReportDto> ReportAsync(Guid companyId, string reportedBy, ReportIncidentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's incident records, newest first.</summary>
    Task<IReadOnlyList<IncidentReportDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single incident record, or null when missing/cross-tenant (R15).</summary>
    Task<IncidentReportDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Records/updates the investigation (causes, actions, RIDDOR flag) and moves it to under-investigation. Returns false when missing/cross-tenant.</summary>
    Task<bool> UpdateInvestigationAsync(Guid companyId, Guid id, UpdateIncidentInvestigationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Closes an incident's investigation out. Returns false when missing/cross-tenant.</summary>
    Task<bool> CloseAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
}
