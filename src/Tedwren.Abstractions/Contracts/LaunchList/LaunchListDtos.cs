namespace Tedwren.Abstractions.Contracts.LaunchList;

/// <summary>A visitor's request to join the launch list (anonymous, from the marketing landing page).</summary>
public sealed record CreateLaunchSignupRequest(
    string Email,
    string? Source = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null);

/// <summary>
/// The outcome of a launch-list signup. <see cref="Accepted"/> is true whether the email was newly added or
/// already present — the visitor sees the same friendly confirmation either way, revealing nothing about who is
/// already on the list.
/// </summary>
public sealed record LaunchSignupResultDto(bool Accepted, bool AlreadyOnList);

/// <summary>A launch-list subscriber as the admin console lists them.</summary>
public sealed record LaunchSignupDto(
    Guid Id,
    string Email,
    string? Source,
    string? UtmSource,
    DateTimeOffset CreatedUtc,
    bool Notified,
    DateTimeOffset? NotifiedUtc);

/// <summary>Admin request to send the launch announcement. When <see cref="OnlyUnnotified"/> is true (default), addresses already notified are skipped.</summary>
public sealed record NotifyLaunchRequest(bool OnlyUnnotified = true);

/// <summary>The result of a bulk launch-announcement send: how many were targeted, sent and failed.</summary>
public sealed record NotifyLaunchResultDto(int Targeted, int Sent, int Failed);
