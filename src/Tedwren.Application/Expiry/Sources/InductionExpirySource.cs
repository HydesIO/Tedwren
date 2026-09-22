using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Enums;
using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Expiry.Sources;

/// <summary>
/// The induction-expiry source (MC-7). Projects each current (non-superseded) passed induction that carries a
/// completion expiry into an <see cref="ExpiryItem"/> for its owning company, texting the operative and emailing the
/// company. Induction-expiry alerts go beyond the strict SF-9 wording (which names cards), so this source is
/// <b>gated per-company behind the <c>subcontractor-onboarding</c> entitlement and fails closed</b> — mirroring the
/// Phase-4 RAMS review-cycle reminder. The completion's <see cref="Domain.Entities.InductionSession.ExpiresUtc"/> is
/// date-folded to feed the shared <c>DateOnly</c> schedule.
/// </summary>
public sealed class InductionExpirySource : IExpirySource
{
    /// <summary>The module a company must hold for induction-expiry alerts to fire (beyond-PRD extension).</summary>
    private const string ModuleKey = "subcontractor-onboarding";

    private readonly ICompanyRepository _companies;
    private readonly IInductionSessionRepository _inductions;
    private readonly IPersonRepository _people;
    private readonly IEntitlementService _entitlements;

    /// <summary>Creates the source over the company, induction-session and person repositories and the entitlement service.</summary>
    public InductionExpirySource(
        ICompanyRepository companies,
        IInductionSessionRepository inductions,
        IPersonRepository people,
        IEntitlementService entitlements)
    {
        _companies = companies;
        _inductions = inductions;
        _people = people;
        _entitlements = entitlements;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpiryItem>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        var items = new List<ExpiryItem>();

        foreach (var company in companies)
        {
            // Beyond-PRD extension: only companies that hold the subcontractor-onboarding module get induction alerts.
            if (!await _entitlements.IsEnabledAsync(company.Id, ModuleKey, cancellationToken))
            {
                continue;
            }

            var sessions = await _inductions.GetByCompanyAsync(company.Id, cancellationToken);
            foreach (var session in sessions.Where(s =>
                s.Status == InductionStatus.Passed && s.SupersededBySessionId is null && s.ExpiresUtc is not null))
            {
                var person = await _people.GetByIdAsync(session.PersonId, cancellationToken);
                items.Add(new ExpiryItem(
                    ExpirySource.Induction,
                    session.Id,
                    session.PersonId,
                    company.Id,
                    DateOnly.FromDateTime(session.ExpiresUtc!.Value.UtcDateTime),
                    "Site induction",
                    session.PersonName,
                    person?.PhoneNumber.Value,
                    company.ContactEmail));
            }
        }

        return items;
    }
}
