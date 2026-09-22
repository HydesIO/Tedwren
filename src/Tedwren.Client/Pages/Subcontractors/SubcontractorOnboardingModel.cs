namespace Tedwren.Client.Pages.Subcontractors;

/// <summary>
/// The full state captured across the subcontractor-onboarding wizard's steps (Subcontractor Onboarding spec
/// Stage 1 / §4), bound as a single object so values survive step changes. Turned into a
/// <c>SetupSubcontractorRequest</c> on finish.
/// </summary>
public sealed class SubcontractorOnboardingModel
{
    // --- Company + primary contact ---

    /// <summary>Subcontractor company name (required).</summary>
    public string? CompanyName { get; set; }

    /// <summary>Primary trade.</summary>
    public string? Trade { get; set; }

    /// <summary>Companies House (or equivalent) registration number.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Primary contact name.</summary>
    public string? ContactName { get; set; }

    /// <summary>Primary contact email (the onboarding link is sent here).</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Primary contact phone.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>Whether the onboarding link requires a passcode.</summary>
    public bool RequirePasscode { get; set; }

    // --- Access period (§4) ---

    /// <summary>Access period in months (default 12; captured, not yet enforced).</summary>
    public int AccessPeriodMonths { get; set; } = 12;

    // --- Required document headings (§5) ---

    /// <summary>The available document headings (from master data) and their selection/blocking flags.</summary>
    public List<RequiredDocumentRow> Documents { get; } = new();

    // --- SSSTS / SMSTS (§4) ---

    /// <summary>Whether SSSTS is required for the supervisor.</summary>
    public bool SsstsRequired { get; set; }

    /// <summary>Whether SMSTS is required for the supervisor.</summary>
    public bool SmstsRequired { get; set; }

    // --- Induction (§4) ---

    /// <summary>How long a completed induction stays valid, in days (MC-7; default 365).</summary>
    public int InductionValidityDays { get; set; } = 365;

    /// <summary>Number of correct quiz answers required to pass (MC-4; default 3).</summary>
    public int InductionPassMark { get; set; } = 3;

    /// <summary>Maximum quiz attempts before a manager reset (MC-6; default 3).</summary>
    public int InductionAttemptLimit { get; set; } = 3;

    // --- RAMS review cycle (§4) ---

    /// <summary>RAMS re-review cycle in months (6/9/12; default 12; captured, not yet enforced).</summary>
    public int RamsReviewCycleMonths { get; set; } = 12;
}

/// <summary>
/// A required-document heading choice in the wizard: whether the heading is required at all, and (when it is)
/// whether it blocks the subcontractor's operatives from starting work until valid (the Gate 1 flag; spec §5).
/// </summary>
public sealed class RequiredDocumentRow
{
    /// <summary>Creates a row for an available heading.</summary>
    public RequiredDocumentRow(string heading) => Heading = heading;

    /// <summary>The document heading (from master data).</summary>
    public string Heading { get; }

    /// <summary>Whether the main contractor requires this document from the subcontractor.</summary>
    public bool Selected { get; set; }

    /// <summary>Whether the document must be valid before operatives can start work (Gate 1).</summary>
    public bool RequiredBeforeWork { get; set; }
}
