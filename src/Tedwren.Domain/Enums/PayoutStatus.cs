namespace Tedwren.Domain.Enums;

/// <summary>
/// State of a BACS payout — GoCardless settling collected funds into the Tedwren creditor bank account —
/// mirroring the GoCardless payout states.
/// </summary>
public enum PayoutStatus
{
    /// <summary>Created, not yet sent to the bank.</summary>
    Pending = 0,

    /// <summary>Paid out to the creditor bank account (settled).</summary>
    Paid = 1,
}
