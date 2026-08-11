namespace Tedwren.Domain.Enums;

/// <summary>The state of a self-service operative onboarding link (SF-4, SUB-2).</summary>
public enum OnboardingLinkStatus
{
    /// <summary>Sent but not yet completed by the operative.</summary>
    Pending = 0,

    /// <summary>The operative has submitted their details (and any cards); awaiting admin confirmation.</summary>
    Submitted = 1,

    /// <summary>Withdrawn by the sender.</summary>
    Revoked = 2,
}
