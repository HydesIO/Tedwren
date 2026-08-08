using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the audit trail (SF-20). Scoped by company where supplied (R15).</summary>
public interface IAuditRepository
{
    /// <summary>Searches by optional company, free text (actor/action/entity/reference) and date range, newest first.</summary>
    Task<IReadOnlyList<AuditEntry>> SearchAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    /// <summary>Appends an audit entry.</summary>
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
