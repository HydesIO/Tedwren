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

    /// <summary>Persists a new assignment.</summary>
    Task AddAsync(FormAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Removes an assignment.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
