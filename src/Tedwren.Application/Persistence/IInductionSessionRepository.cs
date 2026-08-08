using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for induction sessions (MC-1–MC-7). A session's workflow fields change as the operative
/// progresses; reads are company-scoped (R15).
/// </summary>
public interface IInductionSessionRepository
{
    /// <summary>Returns a session by id, or null.</summary>
    Task<InductionSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's sessions, newest first (MC-15).</summary>
    Task<IReadOnlyList<InductionSession>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the operative's latest non-superseded session for a template, or null (re-induction supersede check, MC-7).</summary>
    Task<InductionSession?> GetLatestForPersonAsync(Guid companyId, Guid templateId, Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new session.</summary>
    Task AddAsync(InductionSession session, CancellationToken cancellationToken = default);

    /// <summary>Persists workflow changes to a session.</summary>
    Task UpdateAsync(InductionSession session, CancellationToken cancellationToken = default);
}
