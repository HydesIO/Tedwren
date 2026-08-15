namespace Tedwren.Domain.Enums;

/// <summary>
/// Lifecycle of a direct-debit mandate, mirroring the GoCardless mandate states so the platform can track a
/// company's authorisation to collect. A mandate must be <see cref="Active"/> before a payment can be taken.
/// </summary>
public enum MandateStatus
{
    /// <summary>The customer has not yet completed the hosted authorisation (billing request pending).</summary>
    PendingCustomerApproval = 0,

    /// <summary>Authorised but not yet submitted to the banks.</summary>
    PendingSubmission = 1,

    /// <summary>Submitted to the banks, awaiting activation.</summary>
    Submitted = 2,

    /// <summary>Active — payments can be collected against it.</summary>
    Active = 3,

    /// <summary>Set-up failed (e.g. bank rejected the mandate).</summary>
    Failed = 4,

    /// <summary>Cancelled — no further payments can be collected.</summary>
    Cancelled = 5,

    /// <summary>Expired through inactivity.</summary>
    Expired = 6,
}
