using Tedwren.Domain.Enums;

namespace Tedwren.Application.Billing;

/// <summary>
/// Maps GoCardless status strings to the local domain enums (Single Responsibility). Kept in one place so the
/// billing service, webhooks (Phase C) and reconciliation all agree on the mapping. Unknown values fall back
/// to the nearest safe state rather than throwing, so a new GoCardless status never breaks processing.
/// </summary>
public static class GoCardlessStatusMap
{
    /// <summary>
    /// Maps a GoCardless mandate status <b>or</b> webhook action to <see cref="MandateStatus"/>. The resource
    /// status strings and the webhook action names overlap; both are handled here so processing and
    /// reconciliation share one mapping. Returns null for a value that carries no status change (e.g. an
    /// informational action), so the caller can ignore it rather than forcing a wrong state.
    /// </summary>
    public static MandateStatus? ToMandateStatus(string? statusOrAction) => statusOrAction switch
    {
        "pending_customer_approval" => MandateStatus.PendingCustomerApproval,
        "created" => MandateStatus.PendingSubmission,
        "pending_submission" => MandateStatus.PendingSubmission,
        "customer_approval_granted" => MandateStatus.PendingSubmission,
        "submitted" => MandateStatus.Submitted,
        "active" => MandateStatus.Active,
        "failed" => MandateStatus.Failed,
        "cancelled" => MandateStatus.Cancelled,
        "expired" => MandateStatus.Expired,
        _ => null,
    };

    /// <summary>
    /// Maps a GoCardless payment status <b>or</b> webhook action to <see cref="PaymentStatus"/>. Returns null
    /// for a value that carries no status change, so the caller can ignore it.
    /// </summary>
    public static PaymentStatus? ToPaymentStatus(string? statusOrAction) => statusOrAction switch
    {
        "created" => PaymentStatus.PendingSubmission,
        "pending_submission" => PaymentStatus.PendingSubmission,
        "pending_customer_approval" => PaymentStatus.PendingSubmission,
        "resubmission_requested" => PaymentStatus.PendingSubmission,
        "submitted" => PaymentStatus.Submitted,
        "confirmed" => PaymentStatus.Confirmed,
        "paid_out" => PaymentStatus.PaidOut,
        "failed" => PaymentStatus.Failed,
        "late_failure_settled" => PaymentStatus.Failed,
        "cancelled" => PaymentStatus.Cancelled,
        "charged_back" => PaymentStatus.ChargedBack,
        _ => null,
    };

    /// <summary>Whether a payment in this state may be re-taken (returned/charged back).</summary>
    public static bool IsRetryable(PaymentStatus status) =>
        status is PaymentStatus.Failed or PaymentStatus.ChargedBack;

    /// <summary>Maps a GoCardless payout status to <see cref="PayoutStatus"/>.</summary>
    public static PayoutStatus ToPayoutStatus(string? status) => status switch
    {
        "paid" => PayoutStatus.Paid,
        _ => PayoutStatus.Pending,
    };
}
