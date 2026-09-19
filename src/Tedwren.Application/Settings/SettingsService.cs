using Tedwren.Abstractions.Contracts.Settings;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Settings;

/// <summary>
/// Store-agnostic general-settings service. Returns per-company settings, falling back to sensible defaults
/// (seeded from the company name where available) when a company has saved none. Read and write are scoped to
/// the caller's tenant (R15): the general settings carry security-relevant toggles (geofencing, non-compliant-entry
/// blocking, pack-link passcode), so a tenant-scoped caller may only ever touch their own company's row.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;
    private readonly ICompanyRepository _companies;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// Creates the service over the settings and company repositories. <paramref name="currentUser"/> is optional
    /// (supplied by DI) so the route company id is overridden with the signed-in tenant's company (R15); when it is
    /// absent (direct construction in unit tests) the requested id is used unchanged.
    /// </summary>
    public SettingsService(ISettingsRepository repository, ICompanyRepository companies, ICurrentUserService? currentUser = null)
    {
        _repository = repository;
        _companies = companies;
        _currentUser = currentUser;
    }

    /// <summary>Returns the company's general settings, or defaults when none have been saved.</summary>
    public async Task<GeneralSettingsDto> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        companyId = await ResolveCompanyAsync(companyId, cancellationToken);
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
        await _repository.SetAsync(await ResolveCompanyAsync(companyId, cancellationToken), settings, cancellationToken);

    /// <summary>
    /// The company whose settings the caller may act on (R15): the caller's own company for a tenant-scoped user,
    /// or the requested id for a platform administrator / an unscoped caller (unit tests, no current user).
    /// </summary>
    private async Task<Guid> ResolveCompanyAsync(Guid requested, CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return requested;
        }

        var current = await _currentUser.GetCurrentAsync(cancellationToken);
        return current.IsPlatformAdmin || current.CompanyId is not { } companyId ? requested : companyId;
    }

    /// <summary>The default settings applied before a company saves its own.</summary>
    private static GeneralSettingsDto Defaults(string organisationName) => new(
        OrganisationName: organisationName,
        TimeZone: "Europe/London",
        EnforceGeofencing: true,
        RequirePasscodeOnPackLink: true,
        BlockNonCompliantEntry: true,
        NotifyOnExpiry: false);
}
