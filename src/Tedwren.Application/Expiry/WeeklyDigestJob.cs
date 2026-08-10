using System.Text;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Expiry;

/// <summary>
/// The weekly digest (SUB-5): one email per company listing everything expiring in the next 60 days,
/// ordered by date. Currently covers worker qualification cards; company insurances and accreditations
/// join the same digest when they are modelled (SUB-4). Data-store agnostic.
/// </summary>
public sealed class WeeklyDigestJob
{
    private const int WindowDays = 60;

    private readonly ICompanyRepository _companies;
    private readonly IEngagementRepository _engagements;
    private readonly IQualificationCardRepository _cards;
    private readonly IQualificationTypeRepository _types;
    private readonly IEmailSender _email;

    /// <summary>Creates the job over its repositories and the email sender.</summary>
    public WeeklyDigestJob(
        ICompanyRepository companies,
        IEngagementRepository engagements,
        IQualificationCardRepository cards,
        IQualificationTypeRepository types,
        IEmailSender email)
    {
        _companies = companies;
        _engagements = engagements;
        _cards = cards;
        _types = types;
        _email = email;
    }

    /// <summary>Builds and sends each company's digest of items expiring within 60 days as of <paramref name="asOf"/>.</summary>
    public async Task<DigestResultDto> RunAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        var typeNames = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id, t => t.Name);
        var emailsSent = 0;

        foreach (var company in companies)
        {
            if (string.IsNullOrWhiteSpace(company.ContactEmail))
            {
                continue;
            }

            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            var items = new List<(string Name, DateOnly Expiry, int Days)>();
            foreach (var engagement in engagements)
            {
                var cards = await _cards.GetByPersonAsync(engagement.PersonId, cancellationToken);
                foreach (var card in cards.Where(c => !c.IsSuperseded && c.ExpiresOn is not null))
                {
                    var days = card.ExpiresOn!.Value.DayNumber - asOf.DayNumber;
                    if (days <= WindowDays)
                    {
                        var name = typeNames.TryGetValue(card.QualificationTypeId, out var n) ? n : "Qualification";
                        items.Add(($"{engagement.Name} — {name}", card.ExpiresOn.Value, days));
                    }
                }
            }

            if (items.Count == 0)
            {
                continue;
            }

            var ordered = items.OrderBy(i => i.Expiry).ToList();
            await _email.SendAsync(company.ContactEmail!, $"Weekly expiry digest — {ordered.Count} item(s)", BuildBody(ordered), cancellationToken);
            emailsSent++;
        }

        return new DigestResultDto(companies.Count, emailsSent);
    }

    /// <summary>Renders the digest body, one line per item ordered by date.</summary>
    private static string BuildBody(IReadOnlyList<(string Name, DateOnly Expiry, int Days)> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Expiring within the next 60 days:");
        foreach (var (name, expiry, days) in items)
        {
            var when = days < 0 ? $"expired {-days} day(s) ago" : $"in {days} day(s)";
            sb.AppendLine($"- {name}: {expiry:dd MMM yyyy} ({when})");
        }

        return sb.ToString();
    }
}
