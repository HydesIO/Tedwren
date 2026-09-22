using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Expiry;

/// <summary>
/// The expiry-warning engine (SF-9). Gathers every current expiry-bearing item across its sources — qualification
/// cards, company documents (SUB-4) and inductions — works out which schedule stages are due for each, and for each
/// stage not already sent texts the operative (when the item has one) and emails the responsible company, recording
/// every send so running twice in a day never sends twice (idempotent per source + subject). Data-store agnostic:
/// the same logic runs over the in-memory and Dapper repositories.
/// </summary>
public sealed class ExpiryWarningJob
{
    private readonly IEnumerable<IExpirySource> _sources;
    private readonly INotificationLogRepository _log;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;

    /// <summary>Creates the job over its expiry sources, the idempotency log and the notification providers.</summary>
    public ExpiryWarningJob(
        IEnumerable<IExpirySource> sources,
        INotificationLogRepository log,
        ISmsSender sms,
        IEmailSender email)
    {
        _sources = sources;
        _log = log;
        _sms = sms;
        _email = email;
    }

    /// <summary>Evaluates every current expiry item as of <paramref name="asOf"/> and sends any due warnings (idempotently).</summary>
    public async Task<ExpiryScanResultDto> RunAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var items = new List<ExpiryItem>();
        foreach (var source in _sources)
        {
            items.AddRange(await source.GetCurrentAsync(cancellationToken));
        }

        var sent = 0;
        foreach (var item in items)
        {
            var due = ExpiryWarningSchedule.DueStages(item.ExpiresOn, asOf);
            if (due.Count == 0)
            {
                continue;
            }

            foreach (var stage in due)
            {
                if (!string.IsNullOrWhiteSpace(item.WorkerNumber))
                {
                    sent += await SendOnceAsync(
                        item.Source, item.SubjectId, stage, NotificationChannel.Sms, item.WorkerNumber!,
                        () => _sms.SendAsync(item.WorkerNumber!, WorkerMessage(item.Label, item.ExpiresOn, asOf), cancellationToken),
                        cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(item.AdminEmail))
                {
                    sent += await SendOnceAsync(
                        item.Source, item.SubjectId, stage, NotificationChannel.Email, item.AdminEmail!,
                        () => _email.SendAsync(item.AdminEmail!, "Expiry warning", AdminMessage(item.Label, item.PersonName, item.ExpiresOn, asOf), cancellationToken),
                        cancellationToken);
                }
            }
        }

        return new ExpiryScanResultDto(items.Count, sent);
    }

    /// <summary>Sends a warning only if it has not already been logged, then records it. Returns 1 if sent, else 0.</summary>
    private async Task<int> SendOnceAsync(
        ExpirySource source, Guid subjectId, ExpiryWarningStage stage, NotificationChannel channel, string recipient,
        Func<Task> send, CancellationToken cancellationToken)
    {
        if (await _log.ExistsAsync(source, subjectId, stage, channel, recipient, cancellationToken))
        {
            return 0;
        }

        await send();
        await _log.AddAsync(
            new ExpiryNotification { Source = source, SubjectId = subjectId, Stage = stage, Channel = channel, Recipient = recipient },
            cancellationToken);
        return 1;
    }

    /// <summary>The worker's SMS wording.</summary>
    private static string WorkerMessage(string label, DateOnly expiresOn, DateOnly asOf)
    {
        var days = expiresOn.DayNumber - asOf.DayNumber;
        return days < 0
            ? $"Your {label} expired on {expiresOn:dd MMM yyyy}. Please renew it and update your record."
            : $"Your {label} expires on {expiresOn:dd MMM yyyy} ({days} day(s)). Please renew it in time.";
    }

    /// <summary>The administrator's email wording. Names the operative for a worker item, or the company for a company document.</summary>
    private static string AdminMessage(string label, string? personName, DateOnly expiresOn, DateOnly asOf)
    {
        var days = expiresOn.DayNumber - asOf.DayNumber;
        var when = days < 0 ? $"expired on {expiresOn:dd MMM yyyy}" : $"expires on {expiresOn:dd MMM yyyy} ({days} day(s))";
        var subject = personName is null ? $"The company's {label}" : $"{personName}'s {label}";
        return $"{subject} {when}. Please review the record.";
    }
}
