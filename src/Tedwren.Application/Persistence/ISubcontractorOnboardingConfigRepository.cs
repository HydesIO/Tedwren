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

    /// <summary>Persists changes to an existing configuration.</summary>
    Task UpdateAsync(SubcontractorOnboardingConfig config, CancellationToken cancellationToken = default);
}
