namespace Tedwren.Domain.Notifications;

/// <summary>
/// The kind of record an expiry warning is about (SF-9 / SUB-4). The idempotency log keys on this together with
/// the subject id, so a card, a company document and an induction whose ids are unrelated never collide in the
/// log. The value is stable: <see cref="Card"/> is 0 so pre-existing card-only log rows read back correctly.
/// </summary>
public enum ExpirySource
{
    /// <summary>An operative's qualification card / accreditation (SF-8/SF-9).</summary>
    Card = 0,

    /// <summary>A company-level document — insurance, accreditation or policy (SUB-4).</summary>
    CompanyDocument = 1,

    /// <summary>An operative's completed induction, expiring per its template validity (MC-7).</summary>
    Induction = 2,
}
