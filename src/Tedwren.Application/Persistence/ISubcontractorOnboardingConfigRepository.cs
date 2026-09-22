using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for subcontractor onboarding configurations (spec Stage 1 / §4).</summary>
public interface ISubcontractorOnboardingConfigRepository
{
    /// <summary>Persists a new configuration.</summary>
    Task AddAsync(SubcontractorOnboardingConfig config, CancellationToken cancellationToken = default);

    /// <summary>Returns the configuration for a trade invite, or null.</summary>
    Task<SubcontractorOnboardingConfig?> GetByTradeInviteAsync(Guid tradeInviteId, CancellationToken cancellationToken = default);

    /// <summary>Returns the (most recent) configuration for a subcontractor company, or null — the Gate 1/5 lookup.</summary>
    Task<SubcontractorOnboardingConfig?> GetBySubcontractorCompanyAsync(Guid subcontractorCompanyId, CancellationToken cancellationToken = default);

    /// <summary>Returns every configuration created by an inviting main contractor, newest first (R15 — the MC's own subcontractors).</summary>
    Task<IReadOnlyList<SubcontractorOnboardingConfig>> GetByInviterCompanyAsync(Guid inviterCompanyId, CancellationToken cancellationToken = default);

    /// <summary>Returns every configuration with a RAMS review cycle set, across tenants — the reminder engine's candidate set (Phase 4, beyond PRD).</summary>
    Task<IReadOnlyList<SubcontractorOnboardingConfig>> GetWithReviewCycleAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing configuration.</summary>
    Task UpdateAsync(SubcontractorOnboardingConfig config, CancellationToken cancellationToken = default);

    /// <summary>Records when a RAMS re-review reminder was last sent for a configuration (the reminder engine's idempotency marker).</summary>
    Task UpdateReviewReminderAsync(Guid id, DateTimeOffset remindedUtc, CancellationToken cancellationToken = default);
}
