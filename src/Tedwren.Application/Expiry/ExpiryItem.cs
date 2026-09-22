using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Expiry;

/// <summary>
/// A source-neutral expiry event fed to the SF-9 warning engine, the SUB-5 weekly digest and the upcoming-expiries
/// read. Each expiry source (cards, company documents, inductions) projects its records into these, emitting one
/// item per (record × responsible company) and resolving that item's own recipients — the operative's mobile for a
/// text (null when the source has no operative, e.g. a company document) and the responsible company's email — so
/// the engine, digest and read stay source-agnostic. A card held by an operative engaged by two companies therefore
/// yields two items (one per company); the warning engine's per-recipient idempotency collapses the shared worker
/// text back to one send.
/// </summary>
/// <param name="Source">Which register the item comes from — the idempotency-log discriminator.</param>
/// <param name="SubjectId">The underlying record id (card / document / induction session) — the log's subject key.</param>
/// <param name="PersonId">The operative the item concerns (card / induction), or null for a company-level item.</param>
/// <param name="CompanyId">The responsible / engaging company — digest grouping and tenant read scope (R15).</param>
/// <param name="ExpiresOn">The normalised expiry date (SF-9 schedule input; an induction's <c>ExpiresUtc</c> is date-folded).</param>
/// <param name="Label">Human label for the message and register row (e.g. "CSCS Card", "Employer's Liability Insurance").</param>
/// <param name="PersonName">The operative's recorded name (from the engagement), or null for a company-level item.</param>
/// <param name="WorkerNumber">The operative's mobile for the SF-9 text, or null when the source has no operative.</param>
/// <param name="AdminEmail">The responsible company's email for the SF-9 email / SUB-5 digest, or null when none is on file.</param>
public sealed record ExpiryItem(
    ExpirySource Source,
    Guid SubjectId,
    Guid? PersonId,
    Guid CompanyId,
    DateOnly ExpiresOn,
    string Label,
    string? PersonName,
    string? WorkerNumber,
    string? AdminEmail);
