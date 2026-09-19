using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Safety;

/// <summary>
/// The store-agnostic accident / incident recording service (PRD §8.2). A record gets an immediate reference; the
/// investigation captures the immediate/root causes, corrective actions and a RIDDOR-reportable flag; close-out is
/// append-only. Everything is scoped to the company (R15).
/// </summary>
public sealed class IncidentReportService : IIncidentReportService
{
    private readonly IIncidentReportRepository _reports;

    /// <summary>Creates the service over the incident repository.</summary>
    public IncidentReportService(IIncidentReportRepository reports) => _reports = reports;

    /// <summary>Records an accident/incident and returns it with its reference.</summary>
    public async Task<IncidentReportDto> ReportAsync(Guid companyId, string reportedBy, ReportIncidentRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("A description is required.", nameof(request));
        }

        var report = new IncidentReport
        {
            CompanyId = companyId,
            Reference = BuildReference(),
            Kind = SafetyEnum.Parse(request.Kind, IncidentKind.Accident),
            Description = request.Description.Trim(),
            Location = Clean(request.Location),
            OccurredUtc = request.OccurredUtc ?? DateTimeOffset.UtcNow,
            InjuredPersonName = Clean(request.InjuredPersonName),
            InjuryDetail = Clean(request.InjuryDetail),
            Severity = SafetyEnum.Parse(request.Severity, SafetySeverity.Medium),
            Status = IncidentStatus.Reported,
            ReportedBy = string.IsNullOrWhiteSpace(reportedBy) ? "System" : reportedBy.Trim(),
        };
        await _reports.AddAsync(report, cancellationToken);
        return ToDto(report);
    }

    /// <summary>Returns a company's incident records, newest first.</summary>
    public async Task<IReadOnlyList<IncidentReportDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        (await _reports.GetByCompanyAsync(companyId, cancellationToken)).Select(ToDto).ToList();

    /// <summary>Returns a single incident record, or null when missing/cross-tenant (R15).</summary>
    public async Task<IncidentReportDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetAsync(id, cancellationToken);
        return report is null || report.CompanyId != companyId ? null : ToDto(report);
    }

    /// <summary>Records/updates the investigation and moves the record to under-investigation, scoped to the company (R15).</summary>
    public async Task<bool> UpdateInvestigationAsync(Guid companyId, Guid id, UpdateIncidentInvestigationRequest request, CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetAsync(id, cancellationToken);
        if (report is null || report.CompanyId != companyId)
        {
            return false;
        }

        report.ImmediateCause = Clean(request.ImmediateCause);
        report.RootCause = Clean(request.RootCause);
        report.CorrectiveActions = Clean(request.CorrectiveActions);
        report.RiddorReportable = request.RiddorReportable;
        report.RiddorCategory = request.RiddorReportable ? Clean(request.RiddorCategory) : null;
        report.InvestigatedBy = Clean(request.InvestigatedBy);
        if (report.Status == IncidentStatus.Reported)
        {
            report.Status = IncidentStatus.UnderInvestigation;
        }

        await _reports.UpdateAsync(report, cancellationToken);
        return true;
    }

    /// <summary>Closes an incident's investigation out, scoped to the company (R15).</summary>
    public async Task<bool> CloseAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetAsync(id, cancellationToken);
        if (report is null || report.CompanyId != companyId)
        {
            return false;
        }

        report.Status = IncidentStatus.Closed;
        report.ClosedUtc = DateTimeOffset.UtcNow;
        await _reports.UpdateAsync(report, cancellationToken);
        return true;
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Builds a human-readable incident reference (date + short random suffix).</summary>
    private static string BuildReference() =>
        $"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    /// <summary>Maps an incident record entity to its DTO (enum values as strings).</summary>
    private static IncidentReportDto ToDto(IncidentReport r) => new(
        r.Id, r.Reference, r.Kind.ToString(), r.Description, r.Location, r.OccurredUtc, r.InjuredPersonName,
        r.InjuryDetail, r.Severity.ToString(), r.ImmediateCause, r.RootCause, r.CorrectiveActions,
        r.Status.ToString(), r.RiddorReportable, r.RiddorCategory, r.ReportedBy, r.ReportedUtc, r.InvestigatedBy,
        r.ClosedUtc);
}
