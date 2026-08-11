namespace Tedwren.Client.Pages.Onboarding;

/// <summary>
/// The org type an onboarding run is provisioning. Stored on the company as a free-text <c>Type</c> string,
/// but modelled here as a choice so the wizard can branch its steps (subcontractor vs main contractor).
/// </summary>
public enum OnboardingOrgType
{
    /// <summary>A specialist subcontractor: time &amp; attendance, operatives, insurances, sites they attend (§5.2).</summary>
    Subcontractor,

    /// <summary>A principal/regional main contractor: sites they run, inductions, site-entry (§5.3).</summary>
    MainContractor,
}

/// <summary>
/// The full state captured across the onboarding wizard's steps, bound as a single object so values survive
/// step changes. It is turned into service calls on finish (company → admin → sites → operatives / insurances
/// / induction template). Deliberately plain: the wizard owns validation and sequencing.
/// </summary>
public sealed class OnboardingModel
{
    /// <summary>Which product the new organisation is being set up for.</summary>
    public OnboardingOrgType OrgType { get; set; } = OnboardingOrgType.Subcontractor;

    // --- Company details ---

    /// <summary>Registered company name (required).</summary>
    public string? CompanyName { get; set; }

    /// <summary>Primary trade (free text / picklist).</summary>
    public string? Trade { get; set; }

    /// <summary>Companies House (or equivalent) registration number.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Registered address.</summary>
    public string? Address { get; set; }

    /// <summary>Primary contact name.</summary>
    public string? ContactName { get; set; }

    /// <summary>Primary contact email.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Primary contact phone.</summary>
    public string? ContactPhone { get; set; }

    // --- First admin (SF-20) ---

    /// <summary>First administrator's full name (required).</summary>
    public string? AdminName { get; set; }

    /// <summary>First administrator's email (required, sign-in identity).</summary>
    public string? AdminEmail { get; set; }

    // --- Sites ---

    /// <summary>Sites to create for the organisation (attended, for subcontractors; run, for main contractors).</summary>
    public List<OnboardingSite> Sites { get; } = new();

    // --- Operatives (subcontractor) ---

    /// <summary>Operatives to add by direct entry (subcontractor branch, SUB-1).</summary>
    public List<OnboardingOperative> Operatives { get; } = new();

    // --- Insurances / accreditations (subcontractor, SUB-4) ---

    /// <summary>Company documents to record (subcontractor branch, SUB-4).</summary>
    public List<OnboardingDocument> Documents { get; } = new();

    // --- Induction template (main contractor, MC-3) ---

    /// <summary>Name of the induction template to seed (main contractor branch).</summary>
    public string? InductionName { get; set; } = "General Site Induction";

    /// <summary>How long a completed induction stays valid, in days (MC-7; default 365).</summary>
    public int InductionValidityDays { get; set; } = 365;

    /// <summary>Number of correct quiz answers required to pass (MC-4; default 3).</summary>
    public int InductionPassMark { get; set; } = 3;

    /// <summary>The company's type string as stored (mirrors the org type choice).</summary>
    public string TypeLabel => OrgType == OnboardingOrgType.MainContractor ? "Main Contractor" : "Subcontractor";
}

/// <summary>A site captured during onboarding (SF-14/SF-6).</summary>
public sealed class OnboardingSite
{
    /// <summary>Site name (required).</summary>
    public string? Name { get; set; }

    /// <summary>The client the site is run for.</summary>
    public string? Client { get; set; }

    /// <summary>Region / area.</summary>
    public string? Region { get; set; }

    /// <summary>Site address.</summary>
    public string? Address { get; set; }

    // Optional geofence (mainly for main-contractor sites, SF-14).

    /// <summary>Geofence centre latitude, if a boundary is set.</summary>
    public double? Latitude { get; set; }

    /// <summary>Geofence centre longitude, if a boundary is set.</summary>
    public double? Longitude { get; set; }

    /// <summary>Geofence radius in metres, if a boundary is set.</summary>
    public double? RadiusMetres { get; set; }
}

/// <summary>An operative captured by direct entry during onboarding (SUB-1).</summary>
public sealed class OnboardingOperative
{
    /// <summary>Operative name recorded for this company (SF-2).</summary>
    public string? Name { get; set; }

    /// <summary>Mobile number identifying the person (SF-1).</summary>
    public string? MobileNumber { get; set; }

    /// <summary>Trade (free text).</summary>
    public string? Trade { get; set; }
}

/// <summary>A company document (insurance/accreditation/policy) captured during onboarding (SUB-4).</summary>
public sealed class OnboardingDocument
{
    /// <summary>Creates a document row for a default type.</summary>
    public OnboardingDocument(string name, string type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>Whether the user has opted to record this document.</summary>
    public bool Selected { get; set; }

    /// <summary>Document display name.</summary>
    public string Name { get; set; }

    /// <summary>Document category (Insurance / Accreditation / Policy).</summary>
    public string Type { get; set; }

    /// <summary>Optional expiry date.</summary>
    public DateTime? ExpiresOn { get; set; }

    /// <summary>Optional reference (policy/certificate number).</summary>
    public string? Reference { get; set; }
}
