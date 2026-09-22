using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="ISubcontractorOnboardingConfigRepository"/> (spec Stage 1 / §4).</summary>
public sealed class InMemorySubcontractorOnboardingConfigRepository : ISubcontractorOnboardingConfigRepository
{
    private readonly ConcurrentDictionary<Guid, SubcontractorOnboardingConfig> _configs = new();

    /// <summary>Persists a new configuration.</summary>
    public Task AddAsync(SubcontractorOnboardingConfig config, CancellationToken cancellationToken = default)
    {
        _configs[config.Id] = config;
        return Task.CompletedTask;
    }

    /// <summary>Returns the configuration for a trade invite, or null.</summary>
    public Task<SubcontractorOnboardingConfig?> GetByTradeInviteAsync(Guid tradeInviteId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_configs.Values.FirstOrDefault(c => c.TradeInviteId == tradeInviteId));

    /// <summary>Returns the most recent configuration for a subcontractor company, or null.</summary>
    public Task<SubcontractorOnboardingConfig?> GetBySubcontractorCompanyAsync(Guid subcontractorCompanyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_configs.Values
            .Where(c => c.SubcontractorCompanyId == subcontractorCompanyId)
            .OrderByDescending(c => c.CreatedUtc)
            .FirstOrDefault());

    /// <summary>Returns every configuration created by an inviting main contractor, newest first (R15).</summary>
    public Task<IReadOnlyList<SubcontractorOnboardingConfig>> GetByInviterCompanyAsync(Guid inviterCompanyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SubcontractorOnboardingConfig> rows = _configs.Values
            .Where(c => c.InviterCompanyId == inviterCompanyId)
            .OrderByDescending(c => c.CreatedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns every configuration with a RAMS review cycle set, across tenants (the reminder engine's candidate set).</summary>
    public Task<IReadOnlyList<SubcontractorOnboardingConfig>> GetWithReviewCycleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SubcontractorOnboardingConfig> rows = _configs.Values
            .Where(c => c.RamsReviewCycleMonths is not null)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Persists changes to an existing configuration.</summary>
    public Task UpdateAsync(SubcontractorOnboardingConfig config, CancellationToken cancellationToken = default)
    {
        _configs[config.Id] = config;
        return Task.CompletedTask;
    }

    /// <summary>Records when a RAMS re-review reminder was last sent for a configuration (idempotency marker).</summary>
    public Task UpdateReviewReminderAsync(Guid id, DateTimeOffset remindedUtc, CancellationToken cancellationToken = default)
    {
        if (_configs.TryGetValue(id, out var config))
        {
            config.LastRamsReviewReminderUtc = remindedUtc;
        }

        return Task.CompletedTask;
    }
}
