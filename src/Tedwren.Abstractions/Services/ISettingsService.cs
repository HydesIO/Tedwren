using Tedwren.Abstractions.Contracts.Settings;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Reads and writes a company's organisation-wide general settings (System Configuration). Settings are
/// per company (R15); an unset company returns defaults rather than null.
/// </summary>
public interface ISettingsService
{
    /// <summary>Returns the company's general settings, or defaults when none have been saved.</summary>
    Task<GeneralSettingsDto> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Saves (upserts) the company's general settings.</summary>
    Task SaveForCompanyAsync(Guid companyId, GeneralSettingsDto settings, CancellationToken cancellationToken = default);
}
