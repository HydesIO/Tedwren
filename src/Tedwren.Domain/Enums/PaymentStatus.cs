namespace Tedwren.Domain.Enums;

/// <summary>
/// Lifecycle of a direct-debit payment, mirroring the GoCardless payment states. A returned payment lands in
/// <see cref="Failed"/> or <see cref="ChargedBack"/> and may be re-taken (a fresh retry), which is the
/// "take again after a return" flow the admin area supports.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Created, not yet submitted to the banks.</summary>
    PendingSubmission = 0,

    /// <summary>Submitted to the banks, awaiting confirmation.</summary>
    Submitted = 1,

    /// <summary>Confirmed by the banks (funds on the way).</summary>
    Confirmed = 2,

    /// <summary>Paid out to the Tedwren creditor account (settled).</summary>
    PaidOut = 3,

    /// <summary>Failed / returned by the bank (e.g. insufficient funds) — eligible to be re-taken.</summary>
    Failed = 4,

    /// <summary>Cancelled before collection.</summary>
    Cancelled = 5,

    /// <summary>Charged back after payout (indemnity claim) — eligible to be re-taken.</summary>
    ChargedBack = 6,
}
