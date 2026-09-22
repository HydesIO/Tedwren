using Tedwren.Application.Persistence;
using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Expiry.Sources;

/// <summary>
/// The qualification-card expiry source (SF-8/SF-9). Projects every current (non-superseded) card that carries an
/// expiry date into one <see cref="ExpiryItem"/> per engaging company (SF-2: each company engaging the operative is
/// told), resolving the operative's mobile for the text and that company's email for the email. A card whose
/// operative has no active engagement still yields one item so the worker is texted, with no company email.
/// </summary>
public sealed class CardExpirySource : IExpirySource
{
    private readonly IQualificationCardRepository _cards;
    private readonly IQualificationTypeRepository _types;
    private readonly IPersonRepository _people;
    private readonly IEngagementRepository _engagements;
    private readonly ICompanyRepository _companies;

    /// <summary>Creates the source over the card, type, person, engagement and company repositories.</summary>
    public CardExpirySource(
        IQualificationCardRepository cards,
        IQualificationTypeRepository types,
        IPersonRepository people,
        IEngagementRepository engagements,
        ICompanyRepository companies)
    {
        _cards = cards;
        _types = types;
        _people = people;
        _engagements = engagements;
        _companies = companies;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpiryItem>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var cards = await _cards.GetCurrentWithExpiryAsync(cancellationToken);
        var typeNames = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id, t => t.Name);
        var items = new List<ExpiryItem>();

        foreach (var card in cards)
        {
            var label = typeNames.TryGetValue(card.QualificationTypeId, out var name) ? name : "Qualification";
            var person = await _people.GetByIdAsync(card.PersonId, cancellationToken);
            var workerNumber = person?.PhoneNumber.Value;
            var engagements = await _engagements.GetActiveByPersonAsync(card.PersonId, cancellationToken);

            if (engagements.Count == 0)
            {
                // No engaging company: still warn the worker (SF-9), with no company email — matches the prior engine.
                items.Add(new ExpiryItem(
                    ExpirySource.Card, card.Id, card.PersonId, Guid.Empty, card.ExpiresOn!.Value, label,
                    PersonName: null, workerNumber, AdminEmail: null));
                continue;
            }

            foreach (var engagement in engagements)
            {
                var company = await _companies.GetByIdAsync(engagement.CompanyId, cancellationToken);
                items.Add(new ExpiryItem(
                    ExpirySource.Card, card.Id, card.PersonId, engagement.CompanyId, card.ExpiresOn!.Value, label,
                    engagement.Name, workerNumber, company?.ContactEmail));
            }
        }

        return items;
    }
}
