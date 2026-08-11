using System.Collections.Concurrent;
using Tedwren.Abstractions.Contracts.Settings;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="ISettingsRepository"/> keyed by company id.</summary>
public sealed class InMemorySettingsRepository : ISettingsRepository
{
    private readonly ConcurrentDictionary<Guid, GeneralSettingsDto> _byCompany = new();

    /// <summary>Returns the company's saved settings, or null.</summary>
    public Task<GeneralSettingsDto?> GetAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byCompany.TryGetValue(companyId, out var settings) ? settings : null);

    /// <summary>Upserts the company's settings.</summary>
    public Task SetAsync(Guid companyId, GeneralSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _byCompany[companyId] = settings;
        return Task.CompletedTask;
    }
}
