using Tedwren.Abstractions.Contracts.Settings;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Settings;

/// <summary>
/// Store-agnostic general-settings service. Returns per-company settings, falling back to sensible defaults
/// (seeded from the company name where available) when a company has saved none.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;
    private readonly ICompanyRepository _companies;

    /// <summary>Creates the service over the settings and company repositories.</summary>
    public SettingsService(ISettingsRepository repository, ICompanyRepository companies)
    {
        _repository = repository;
        _companies = companies;
    }

    /// <summary>Returns the company's general settings, or defaults when none have been saved.</summary>
    public async Task<GeneralSettingsDto> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.GetAsync(companyId, cancellationToken);
        if (saved is not null)
        {
            return saved;
        }

        var company = await _companies.GetByIdAsync(companyId, cancellationToken);
        return Defaults(company?.Name ?? string.Empty);
    }

    /// <summary>Saves (upserts) the company's general settings.</summary>
    public async Task SaveForCompanyAsync(Guid companyId, GeneralSettingsDto settings, CancellationToken cancellationToken = default) =>
        await _repository.SetAsync(companyId, settings, cancellationToken);

    /// <summary>The default settings applied before a company saves its own.</summary>
    private static GeneralSettingsDto Defaults(string organisationName) => new(
        OrganisationName: organisationName,
        TimeZone: "Europe/London",
        EnforceGeofencing: true,
        RequirePasscodeOnPackLink: true,
        BlockNonCompliantEntry: true,
        NotifyOnExpiry: false);
}
