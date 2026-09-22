namespace Tedwren.Domain.Entities;

/// <summary>
/// A main contractor's configuration for onboarding one subcontractor (Subcontractor Onboarding spec Stage 1 /
/// §4). Created alongside the <see cref="TradeInvite"/> that anchors the relationship (InviterCompanyId → the
/// subcontractor's CompanyId), it records what the subcontractor must satisfy: the access period, the required
/// document headings (each flagged "required before work" — the Gate 1 set), whether SSSTS/SMSTS are required,
/// the induction settings applied to the sub's operatives, and the RAMS review cycle.
/// <para>
/// Scoping (see <c>docs/subcontractor-onboarding-plan.md</c>): access period and RAMS review cycle are beyond
/// PRD v6.4 — captured and persisted here but not yet enforced. The induction the operative completes is the
/// main contractor's own template (§6.1), so these are settings the MC applies, not a sub-issued induction.
/// </para>
/// </summary>
public sealed class SubcontractorOnboardingConfig
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The inviting main contractor's company (the owning tenant, R15).</summary>
    public Guid InviterCompanyId { get; set; }

    /// <summary>The subcontractor company this configuration is for.</summary>
    public Guid SubcontractorCompanyId { get; set; }

    /// <summary>The trade invite that carries the onboarding link for this subcontractor.</summary>
    public Guid TradeInviteId { get; set; }

    /// <summary>How long the subcontractor's access lasts, in months (spec §4, default 12). Captured; not yet enforced.</summary>
    public int AccessPeriodMonths { get; set; } = 12;

    /// <summary>The configured required-document headings and their "required before work" flags (the Gate 1 set).</summary>
    public IReadOnlyList<RequiredDocumentHeading> RequiredDocuments { get; set; } = Array.Empty<RequiredDocumentHeading>();

    /// <summary>Whether SSSTS is required for the subcontractor's supervisor (spec §4). Captured; enforced in a later phase.</summary>
    public bool SsstsRequired { get; set; }

    /// <summary>Whether SMSTS is required for the subcontractor's supervisor (spec §4). Captured; enforced in a later phase.</summary>
    public bool SmstsRequired { get; set; }

    /// <summary>How long a completed induction stays valid, in days (spec §4; default 365, MC-7).</summary>
    public int InductionValidityDays { get; set; } = 365;

    /// <summary>Number of correct quiz answers required to pass the induction (spec §4, MC-4).</summary>
    public int InductionPassMark { get; set; }

    /// <summary>Maximum induction quiz attempts before a manager reset is needed (spec §4; default 3, MC-6).</summary>
    public int InductionAttemptLimit { get; set; } = 3;

    /// <summary>RAMS re-review cycle in months (6/9/12; spec §4). Null when not set. Captured; not yet enforced.</summary>
    public int? RamsReviewCycleMonths { get; set; }

    /// <summary>When the configuration was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>
/// One configured required-document heading and whether it must be present and valid before the subcontractor's
/// operatives can start work (the Gate 1 blocking set; spec §5 "required before work" flag).
/// </summary>
public sealed record RequiredDocumentHeading(string Heading, bool RequiredBeforeWork);
