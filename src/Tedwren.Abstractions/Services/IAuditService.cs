using Tedwren.Abstractions.Contracts.Audit;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The audit-trail service (SF-20). Search covers name/reference and a date range, and results export. Read
/// operations are scoped to the customer where a company is supplied (R15).
/// </summary>
public interface IAuditService
{
    /// <summary>Searches the audit trail by free text (actor/action/entity/reference) and date range.</summary>
    Task<IReadOnlyList<AuditEntryDto>> SearchAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    /// <summary>Exports the matching audit entries as CSV (SF-20).</summary>
    Task<string> ExportCsvAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    /// <summary>Records an audit entry.</summary>
    Task RecordAsync(RecordAuditRequest request, CancellationToken cancellationToken = default);
}
