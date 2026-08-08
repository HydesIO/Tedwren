using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IInductionSessionRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryInductionSessionRepository : IInductionSessionRepository
{
    private readonly InMemoryInductionStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryInductionSessionRepository(InMemoryInductionStore store) => _store = store;

    /// <summary>Returns a session by id, or null.</summary>
    public Task<InductionSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Sessions.GetValueOrDefault(sessionId));

    /// <summary>Returns a company's sessions, newest first.</summary>
    public Task<IReadOnlyList<InductionSession>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InductionSession> sessions = _store.Sessions.Values
            .Where(s => s.CompanyId == companyId)
            .OrderByDescending(s => s.StartedUtc)
            .ToList();
        return Task.FromResult(sessions);
    }

    /// <summary>Returns the operative's latest non-superseded session for a template, or null (MC-7).</summary>
    public Task<InductionSession?> GetLatestForPersonAsync(Guid companyId, Guid templateId, Guid personId, CancellationToken cancellationToken = default)
    {
        var latest = _store.Sessions.Values
            .Where(s => s.CompanyId == companyId && s.TemplateId == templateId && s.PersonId == personId && s.Status != InductionStatus.Superseded)
            .OrderByDescending(s => s.StartedUtc)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    /// <summary>Returns the operative's latest passed induction for the company across any template, or null (MC-8).</summary>
    public Task<InductionSession?> GetLatestPassedForPersonAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default)
    {
        var latest = _store.Sessions.Values
            .Where(s => s.CompanyId == companyId && s.PersonId == personId && s.Status == InductionStatus.Passed)
            .OrderByDescending(s => s.CompletedUtc ?? s.StartedUtc)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    /// <summary>Persists a new session.</summary>
    public Task AddAsync(InductionSession session, CancellationToken cancellationToken = default)
    {
        _store.Sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    /// <summary>Persists workflow changes to a session.</summary>
    public Task UpdateAsync(InductionSession session, CancellationToken cancellationToken = default)
    {
        _store.Sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
