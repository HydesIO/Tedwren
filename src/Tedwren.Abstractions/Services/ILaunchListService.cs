using Tedwren.Abstractions.Contracts.LaunchList;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The pre-launch interest list (Web Content Spec §6.9): visitors add their email from the marketing landing
/// page, and the product team later sends everyone a branded launch announcement. Signup is anonymous and
/// deduplicated; listing and notifying are platform-admin only.
/// </summary>
public interface ILaunchListService
{
    /// <summary>Adds an email to the launch list (idempotent on the address). Never throws for a duplicate.</summary>
    Task<LaunchSignupResultDto> SignUpAsync(CreateLaunchSignupRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns every launch-list subscriber, newest first.</summary>
    Task<IReadOnlyList<LaunchSignupDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the branded launch announcement to each subscriber individually (one email per address, never a
    /// shared recipient list), skipping opted-out addresses and marking each sent one as notified. Returns how
    /// many were targeted, sent and failed.
    /// </summary>
    Task<NotifyLaunchResultDto> NotifyAsync(NotifyLaunchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Opts a subscriber out via their unsubscribe token (PECR/GDPR). Returns true when a matching subscriber was found.</summary>
    Task<bool> UnsubscribeAsync(string token, CancellationToken cancellationToken = default);
}
