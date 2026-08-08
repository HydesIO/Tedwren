using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IEngagementRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryEngagementRepository : IEngagementRepository
{
    private readonly InMemoryOrganisationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryEngagementRepository(InMemoryOrganisationStore store) => _store = store;

    /// <summary>Returns the active engagements for a company, ordered by name.</summary>
    public Task<IReadOnlyList<Engagement>> GetActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Engagement> engagements = _store.Engagements.Values
            .Where(e => e.CompanyId == companyId && e.Status == EngagementStatus.Active)
            .OrderBy(e => e.Name)
            .ToList();
        return Task.FromResult(engagements);
    }

    /// <summary>Returns the active engagements of a person across all companies.</summary>
    public Task<IReadOnlyList<Engagement>> GetActiveByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Engagement> engagements = _store.Engagements.Values
            .Where(e => e.PersonId == personId && e.Status == EngagementStatus.Active)
            .ToList();
        return Task.FromResult(engagements);
    }

    /// <summary>Returns a specific engagement owned by the company, or null (tenant-scoped, R15).</summary>
    public Task<Engagement?> GetAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Engagements.Values.FirstOrDefault(e => e.Id == engagementId && e.CompanyId == companyId));

    /// <summary>Returns the engagement (any status) of a person within a company, or null.</summary>
    public Task<Engagement?> GetByCompanyAndPersonAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Engagements.Values.FirstOrDefault(e => e.CompanyId == companyId && e.PersonId == personId));

    /// <summary>Counts the active engagements for a company.</summary>
    public Task<int> CountActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Engagements.Values.Count(e => e.CompanyId == companyId && e.Status == EngagementStatus.Active));

    /// <summary>Adds an engagement to the store.</summary>
    public Task AddAsync(Engagement engagement, CancellationToken cancellationToken = default)
    {
        _store.Engagements[engagement.Id] = engagement;
        return Task.CompletedTask;
    }

    /// <summary>Updates an engagement in the store.</summary>
    public Task UpdateAsync(Engagement engagement, CancellationToken cancellationToken = default)
    {
        _store.Engagements[engagement.Id] = engagement;
        return Task.CompletedTask;
    }
}
