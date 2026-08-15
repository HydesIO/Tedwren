using Tedwren.Domain.Enums;

namespace Tedwren.Application.Billing;

/// <summary>
/// Maps GoCardless status strings to the local domain enums (Single Responsibility). Kept in one place so the
/// billing service, webhooks (Phase C) and reconciliation all agree on the mapping. Unknown values fall back
/// to the nearest safe state rather than throwing, so a new GoCardless status never breaks processing.
/// </summary>
public static class GoCardlessStatusMap
{
    /// <summary>Maps a GoCardless mandate status to <see cref="MandateStatus"/>.</summary>
    public static MandateStatus ToMandateStatus(string? status) => status switch
    {
        "pending_customer_approval" => MandateStatus.PendingCustomerApproval,
        "pending_submission" => MandateStatus.PendingSubmission,
        "submitted" => MandateStatus.Submitted,
        "active" => MandateStatus.Active,
        "failed" => MandateStatus.Failed,
        "cancelled" => MandateStatus.Cancelled,
        "expired" => MandateStatus.Expired,
        _ => MandateStatus.PendingCustomerApproval,
    };

    /// <summary>Maps a GoCardless payment status to <see cref="PaymentStatus"/>.</summary>
    public static PaymentStatus ToPaymentStatus(string? status) => status switch
    {
        "pending_submission" => PaymentStatus.PendingSubmission,
        "pending_customer_approval" => PaymentStatus.PendingSubmission,
        "submitted" => PaymentStatus.Submitted,
        "confirmed" => PaymentStatus.Confirmed,
        "paid_out" => PaymentStatus.PaidOut,
        "failed" => PaymentStatus.Failed,
        "cancelled" => PaymentStatus.Cancelled,
        "charged_back" => PaymentStatus.ChargedBack,
        _ => PaymentStatus.PendingSubmission,
    };

    /// <summary>Whether a payment in this state may be re-taken (returned/charged back).</summary>
    public static bool IsRetryable(PaymentStatus status) =>
        status is PaymentStatus.Failed or PaymentStatus.ChargedBack;
}
