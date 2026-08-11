using Tedwren.Abstractions.Contracts.Settings;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for a company's general settings (stored as one JSON row per company).</summary>
public interface ISettingsRepository
{
    /// <summary>Returns the company's saved settings, or null when none exist.</summary>
    Task<GeneralSettingsDto?> GetAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Upserts the company's settings.</summary>
    Task SetAsync(Guid companyId, GeneralSettingsDto settings, CancellationToken cancellationToken = default);
}
