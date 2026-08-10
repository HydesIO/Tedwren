using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Expiry;

/// <summary>
/// The expiry-warning engine (SF-9). For each current card with an expiry date, it works out which schedule
/// stages are due, and for each stage not already sent, texts the worker and emails each engaging company's
/// administrator — recording every send so running twice in a day never sends twice. Data-store agnostic:
/// the same logic runs over the in-memory and Dapper repositories.
/// </summary>
public sealed class ExpiryWarningJob
{
    private readonly IQualificationCardRepository _cards;
    private readonly IQualificationTypeRepository _types;
    private readonly IPersonRepository _people;
    private readonly IEngagementRepository _engagements;
    private readonly ICompanyRepository _companies;
    private readonly INotificationLogRepository _log;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;

    /// <summary>Creates the job over its repositories and notification providers.</summary>
    public ExpiryWarningJob(
        IQualificationCardRepository cards,
        IQualificationTypeRepository types,
        IPersonRepository people,
        IEngagementRepository engagements,
        ICompanyRepository companies,
        INotificationLogRepository log,
        ISmsSender sms,
        IEmailSender email)
    {
        _cards = cards;
        _types = types;
        _people = people;
        _engagements = engagements;
        _companies = companies;
        _log = log;
        _sms = sms;
        _email = email;
    }

    /// <summary>Evaluates all current cards as of <paramref name="asOf"/> and sends any due warnings (idempotently).</summary>
    public async Task<ExpiryScanResultDto> RunAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var cards = await _cards.GetCurrentWithExpiryAsync(cancellationToken);
        var typeNames = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id, t => t.Name);
        var sent = 0;

        foreach (var card in cards)
        {
            var due = ExpiryWarningSchedule.DueStages(card.ExpiresOn!.Value, asOf);
            if (due.Count == 0)
            {
                continue;
            }

            var qualificationName = typeNames.TryGetValue(card.QualificationTypeId, out var name) ? name : "a qualification";
            var person = await _people.GetByIdAsync(card.PersonId, cancellationToken);
            var workerNumber = person?.PhoneNumber.Value;
            var adminEmails = await AdminEmailsForPersonAsync(card.PersonId, cancellationToken);

            foreach (var stage in due)
            {
                if (!string.IsNullOrWhiteSpace(workerNumber))
                {
                    sent += await SendOnceAsync(
                        card.Id, stage, NotificationChannel.Sms, workerNumber!,
                        () => _sms.SendAsync(workerNumber!, WorkerMessage(qualificationName, card.ExpiresOn!.Value, asOf), cancellationToken),
                        cancellationToken);
                }

                foreach (var adminEmail in adminEmails)
                {
                    sent += await SendOnceAsync(
                        card.Id, stage, NotificationChannel.Email, adminEmail,
                        () => _email.SendAsync(adminEmail, "Qualification expiry warning", AdminMessage(qualificationName, card.ExpiresOn!.Value, asOf), cancellationToken),
                        cancellationToken);
                }
            }
        }

        return new ExpiryScanResultDto(cards.Count, sent);
    }

    /// <summary>Distinct contact emails of the companies actively engaging the person (SF-2: each is told about its worker).</summary>
    private async Task<IReadOnlyList<string>> AdminEmailsForPersonAsync(Guid personId, CancellationToken cancellationToken)
    {
        var engagements = await _engagements.GetActiveByPersonAsync(personId, cancellationToken);
        var emails = new List<string>();
        foreach (var engagement in engagements)
        {
            var company = await _companies.GetByIdAsync(engagement.CompanyId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(company?.ContactEmail))
            {
                emails.Add(company!.ContactEmail!);
            }
        }

        return emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Sends a warning only if it has not already been logged, then records it. Returns 1 if sent, else 0.</summary>
    private async Task<int> SendOnceAsync(
        Guid cardId, ExpiryWarningStage stage, NotificationChannel channel, string recipient,
        Func<Task> send, CancellationToken cancellationToken)
    {
        if (await _log.ExistsAsync(cardId, stage, channel, recipient, cancellationToken))
        {
            return 0;
        }

        await send();
        await _log.AddAsync(
            new ExpiryNotification { CardId = cardId, Stage = stage, Channel = channel, Recipient = recipient },
            cancellationToken);
        return 1;
    }

    /// <summary>The worker's SMS wording.</summary>
    private static string WorkerMessage(string qualificationName, DateOnly expiresOn, DateOnly asOf)
    {
        var days = expiresOn.DayNumber - asOf.DayNumber;
        return days < 0
            ? $"Your {qualificationName} expired on {expiresOn:dd MMM yyyy}. Please renew it and update your record."
            : $"Your {qualificationName} expires on {expiresOn:dd MMM yyyy} ({days} day(s)). Please renew it in time.";
    }

    /// <summary>The administrator's email wording.</summary>
    private static string AdminMessage(string qualificationName, DateOnly expiresOn, DateOnly asOf)
    {
        var days = expiresOn.DayNumber - asOf.DayNumber;
        var when = days < 0 ? $"expired on {expiresOn:dd MMM yyyy}" : $"expires on {expiresOn:dd MMM yyyy} ({days} day(s))";
        return $"An operative's {qualificationName} {when}. Review their record before their next shift.";
    }
}
