using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.LaunchList;
using Tedwren.Application.Common;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Notifications.Email;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.LaunchList;

/// <summary>
/// The launch-list service (Web Content Spec §6.9): captures pre-launch interest and later sends the branded
/// launch announcement. Signup is deduplicated on the email; the announcement is sent to each subscriber
/// individually so no address is exposed to another. Holds the rules; the repository only holds records.
/// </summary>
public sealed class LaunchListService : ILaunchListService
{
    private readonly ILaunchSignupRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;

    /// <summary>Injects the repository, email sender and email options.</summary>
    public LaunchListService(
        ILaunchSignupRepository repository,
        IEmailSender emailSender,
        EmailOptions emailOptions)
    {
        _repository = repository;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
    }

    /// <summary>Adds an email to the launch list, ignoring a duplicate (case-insensitive on the address).</summary>
    public async Task<LaunchSignupResultDto> SignUpAsync(CreateLaunchSignupRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!EmailValidation.IsValid(email))
        {
            throw new ArgumentException("A valid email address is required.", nameof(request));
        }

        var existing = await _repository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return new LaunchSignupResultDto(Accepted: true, AlreadyOnList: true);
        }

        var signup = new LaunchSignup
        {
            Email = email,
            Source = request.Source,
            UtmSource = request.UtmSource,
            UtmMedium = request.UtmMedium,
            UtmCampaign = request.UtmCampaign,
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
        };
        await _repository.AddAsync(signup, cancellationToken);
        return new LaunchSignupResultDto(Accepted: true, AlreadyOnList: false);
    }

    /// <summary>Returns every launch-list subscriber, newest first.</summary>
    public async Task<IReadOnlyList<LaunchSignupDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var signups = await _repository.ListAsync(cancellationToken);
        return signups.Select(ToDto).ToList();
    }

    /// <summary>Sends the launch announcement to each subscriber individually, marking each notified.</summary>
    public async Task<NotifyLaunchResultDto> NotifyAsync(NotifyLaunchRequest request, CancellationToken cancellationToken = default)
    {
        var all = await _repository.ListAsync(cancellationToken);
        // Opted-out addresses are never emailed (PECR/GDPR).
        var targets = all
            .Where(s => !s.Unsubscribed)
            .Where(s => !request.OnlyUnnotified || !s.Notified)
            .ToList();

        var apiBase = string.IsNullOrWhiteSpace(_emailOptions.PublicBaseUrl)
            ? "https://tedwren.co.uk"
            : _emailOptions.PublicBaseUrl.TrimEnd('/');
        var startUrl = apiBase;

        var sent = 0;
        var failed = 0;
        foreach (var signup in targets)
        {
            try
            {
                // Ensure the subscriber has an unsubscribe token, then build a per-recipient one-click opt-out link.
                if (string.IsNullOrWhiteSpace(signup.UnsubscribeToken))
                {
                    signup.UnsubscribeToken = Guid.NewGuid().ToString("N");
                }

                var unsubscribeUrl = $"{apiBase}/api/launch-signups/unsubscribe?token={signup.UnsubscribeToken}";
                var content = LaunchAnnouncementEmail.BuildContent(startUrl, unsubscribeUrl);

                await _emailSender.SendHtmlAsync(signup.Email, LaunchAnnouncementEmail.Subject, content, cancellationToken);
                signup.Notified = true;
                signup.NotifiedUtc = DateTimeOffset.UtcNow;
                await _repository.UpdateAsync(signup, cancellationToken);
                sent++;
            }
            catch (Exception)
            {
                // A single bad address must not abort the whole send — count it and carry on.
                failed++;
            }
        }

        return new NotifyLaunchResultDto(Targeted: targets.Count, Sent: sent, Failed: failed);
    }

    /// <summary>Opts a subscriber out via their unsubscribe token. Idempotent; returns false when the token is unknown.</summary>
    public async Task<bool> UnsubscribeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var signup = await _repository.GetByUnsubscribeTokenAsync(token, cancellationToken);
        if (signup is null)
        {
            return false;
        }

        if (!signup.Unsubscribed)
        {
            signup.Unsubscribed = true;
            await _repository.UpdateAsync(signup, cancellationToken);
        }

        return true;
    }

    /// <summary>Removes a subscriber (admin).</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var all = await _repository.ListAsync(cancellationToken);
        if (all.All(s => s.Id != id))
        {
            return false;
        }

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }

    /// <summary>Exports every subscriber as CSV bytes (admin).</summary>
    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var signups = await _repository.ListAsync(cancellationToken);
        var rows = signups
            .Select(s => (IReadOnlyList<string>)new[]
            {
                s.Email,
                s.Source ?? string.Empty,
                s.UtmSource ?? string.Empty,
                s.CreatedUtc.ToString("u"),
                s.Notified ? "yes" : "no",
                s.Unsubscribed ? "yes" : "no",
            })
            .ToList();

        var sheet = new Export.TabularSheet(
            "LaunchList",
            new[] { "Email", "Source", "UtmSource", "SignedUpUtc", "Notified", "Unsubscribed" },
            rows);
        return System.Text.Encoding.UTF8.GetBytes(Export.CsvWriter.Write(sheet));
    }

    /// <summary>Maps a signup entity to its list DTO.</summary>
    private static LaunchSignupDto ToDto(LaunchSignup s) =>
        new(s.Id, s.Email, s.Source, s.UtmSource, s.CreatedUtc, s.Notified, s.NotifiedUtc);
}
