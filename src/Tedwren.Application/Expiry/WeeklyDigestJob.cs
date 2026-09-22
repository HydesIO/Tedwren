using System.Text;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Notifications;

namespace Tedwren.Application.Expiry;

/// <summary>
/// The weekly digest (SUB-5): one email per company listing everything expiring in the next 60 days, ordered by
/// date — worker qualification cards, company insurances and accreditations (SUB-4) and inductions together, drawn
/// from the shared expiry sources so all three registers appear in one list. Data-store agnostic.
/// </summary>
public sealed class WeeklyDigestJob
{
    private const int WindowDays = 60;

    private readonly IEnumerable<IExpirySource> _sources;
    private readonly IEmailSender _email;

    /// <summary>Creates the job over its expiry sources and the email sender.</summary>
    public WeeklyDigestJob(IEnumerable<IExpirySource> sources, IEmailSender email)
    {
        _sources = sources;
        _email = email;
    }

    /// <summary>Builds and sends each company's digest of items expiring within 60 days as of <paramref name="asOf"/>.</summary>
    public async Task<DigestResultDto> RunAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var items = new List<ExpiryItem>();
        foreach (var source in _sources)
        {
            items.AddRange(await source.GetCurrentAsync(cancellationToken));
        }

        // One digest per company that has an email on file and at least one item within the window, listing the
        // company's cards, documents and inductions together, ordered by expiry date.
        var byCompany = items
            .Where(i => !string.IsNullOrWhiteSpace(i.AdminEmail))
            .Where(i => i.ExpiresOn.DayNumber - asOf.DayNumber <= WindowDays)
            .GroupBy(i => i.AdminEmail!)
            .ToList();

        var emailsSent = 0;
        foreach (var group in byCompany)
        {
            var ordered = group
                .Select(i => (
                    Name: i.PersonName is null ? i.Label : $"{i.PersonName} — {i.Label}",
                    i.ExpiresOn,
                    Days: i.ExpiresOn.DayNumber - asOf.DayNumber))
                .OrderBy(x => x.ExpiresOn)
                .ToList();
            await _email.SendAsync(group.Key, $"Weekly expiry digest — {ordered.Count} item(s)", BuildBody(ordered), cancellationToken);
            emailsSent++;
        }

        return new DigestResultDto(byCompany.Count, emailsSent);
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
