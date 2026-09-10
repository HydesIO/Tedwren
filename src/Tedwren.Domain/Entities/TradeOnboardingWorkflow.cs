using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// The pure state machine for a trade self-service onboarding submission (UAT-023). Kept separate and static so
/// it is trivially testable and reused by the service, mirroring <see cref="TimesheetWorkflow"/>. A trade submits
/// from <c>Invited</c> or <c>Returned</c>; a manager then approves, rejects or returns a <c>Submitted</c> one.
/// </summary>
public static class TradeOnboardingWorkflow
{
    /// <summary>The trade can submit for review from a fresh invite or one returned for changes.</summary>
    public static bool CanSubmit(TradeOnboardingStatus status) =>
        status is TradeOnboardingStatus.Invited or TradeOnboardingStatus.Returned;

    /// <summary>A manager can approve only a submission awaiting review.</summary>
    public static bool CanApprove(TradeOnboardingStatus status) => status is TradeOnboardingStatus.Submitted;

    /// <summary>A manager can reject only a submission awaiting review.</summary>
    public static bool CanReject(TradeOnboardingStatus status) => status is TradeOnboardingStatus.Submitted;

    /// <summary>A manager can return (for changes) only a submission awaiting review.</summary>
    public static bool CanReturn(TradeOnboardingStatus status) => status is TradeOnboardingStatus.Submitted;
}
