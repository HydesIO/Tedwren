using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IFormAssignmentRepository"/> over the shared store (test-only mock mode).</summary>
public sealed class InMemoryFormAssignmentRepository : IFormAssignmentRepository
{
    private readonly InMemoryFormStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryFormAssignmentRepository(InMemoryFormStore store) => _store = store;

    /// <summary>Returns a company's assignments, newest first.</summary>
    public Task<IReadOnlyList<FormAssignment>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FormAssignment> assignments = _store.Assignments.Values
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedUtc)
            .ToList();
        return Task.FromResult(assignments);
    }

    /// <summary>Returns an assignment by id, or null.</summary>
    public Task<FormAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Assignments.GetValueOrDefault(id));

    /// <summary>Returns the assignments of a form family within a company.</summary>
    public Task<IReadOnlyList<FormAssignment>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FormAssignment> assignments = _store.Assignments.Values
            .Where(a => a.CompanyId == companyId && a.FormTemplateFamilyId == familyId)
            .ToList();
        return Task.FromResult(assignments);
    }

    /// <summary>Returns every assignment on a recurring schedule (not AdHoc), across all companies (R12).</summary>
    public Task<IReadOnlyList<FormAssignment>> GetRecurringAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FormAssignment> assignments = _store.Assignments.Values
            .Where(a => a.Schedule != Domain.Enums.FormSchedule.AdHoc)
            .OrderBy(a => a.CompanyId).ThenBy(a => a.CreatedUtc)
            .ToList();
        return Task.FromResult(assignments);
    }

    /// <summary>Adds an assignment to the store.</summary>
    public Task AddAsync(FormAssignment assignment, CancellationToken cancellationToken = default)
    {
        _store.Assignments[assignment.Id] = assignment;
        return Task.CompletedTask;
    }

    /// <summary>Records the last recurring-reminder time on the stored assignment (per-period idempotency).</summary>
    public Task UpdateLastReminderAsync(Guid id, DateTimeOffset sentUtc, CancellationToken cancellationToken = default)
    {
        if (_store.Assignments.TryGetValue(id, out var assignment))
        {
            assignment.LastReminderUtc = sentUtc;
        }

        return Task.CompletedTask;
    }

    /// <summary>Removes an assignment from the store.</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Assignments.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
