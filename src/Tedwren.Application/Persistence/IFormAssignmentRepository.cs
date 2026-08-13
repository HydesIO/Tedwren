using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="FormAssignment"/> (the PRD-Phase 2 Forms Library). Tenant-agnostic — the service scopes by company (R15).</summary>
public interface IFormAssignmentRepository
{
    /// <summary>Returns a company's form assignments, newest first.</summary>
    Task<IReadOnlyList<FormAssignment>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single assignment by id, or null.</summary>
    Task<FormAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the assignments of a given form family within a company (used to resolve failed-check alerts).</summary>
    Task<IReadOnlyList<FormAssignment>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Returns every assignment with a recurring schedule (Daily/Weekly/Monthly) across all companies, for the reminder job (R12).</summary>
    Task<IReadOnlyList<FormAssignment>> GetRecurringAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a new assignment.</summary>
    Task AddAsync(FormAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Records when a recurring reminder was last sent for an assignment (the per-period idempotency marker).</summary>
    Task UpdateLastReminderAsync(Guid id, DateTimeOffset sentUtc, CancellationToken cancellationToken = default);

    /// <summary>Removes an assignment.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
