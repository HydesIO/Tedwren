using Tedwren.Abstractions.Services;

namespace Tedwren.Application.MasterData;

/// <summary>
/// The shared (global) compliance master-list values that ship with the product (Subcontractor Onboarding spec
/// §5–§8) so a new customer is productive immediately (mirrors SF-12). These are duplicated in the raw migration
/// script <c>038_master_list_items.sql</c>, which seeds the same rows into the database; the in-memory test
/// double seeds from here so the (test-only) InMemory data source behaves like a migrated database. If a value
/// is added here, add it to the migration too (and vice-versa).
/// </summary>
public static class MasterListSeed
{
    /// <summary>Global values grouped by list key, in display order.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Values { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [MasterListKeys.DocumentHeadings] = new[]
            {
                "Employer's Liability Insurance",
                "Public Liability Insurance",
                "Professional Indemnity Insurance",
                "SSIP Membership",
                "Risk Assessments & Method Statements (RAMS)",
                "COSHH Assessment",
                "Company Health & Safety Policy",
                "Construction Phase Plan",
                "Lifting Plan (LOLER)",
                "Temporary Works Design / Register",
                "Permit to Work / Safe System of Work",
                "Plant & Equipment Inspection Records",
                "Manual Handling Assessment",
                "Noise / HAVS Assessment",
                "Environmental / Waste Management Plan",
                "Toolbox Talk Records",
                "Trade Accreditations",
            },
            [MasterListKeys.SsipSchemes] = new[]
            {
                "CHAS",
                "SMAS Worksafe",
                "Alcumus SafeContractor",
                "Constructionline",
                "Acclaim Accreditation",
                "Avetta",
                "Eurosafe CDM Competent",
                "Fortius CDM Comply",
                "Greenlight Safety Assessment Scheme",
                "CQMS Safety-Scheme",
                "Safe-T-Cert",
                "NHBC Safemark",
                "Exor",
                "Vantify Supply Chain",
                "Achilles Building Confidence",
                "HAE SafeHire Certification",
                "Other (specify)",
            },
            [MasterListKeys.OtherRequirements] = new[]
            {
                "Asbestos Awareness (UKATA / IATP)",
                "Face Fit Testing (RPE)",
                "First Aid at Work",
                "Manual Handling",
                "Working at Height",
                "Fire Marshal / Warden",
                "Abrasive Wheels",
                "Drug & Alcohol Policy / Test",
                "COSHH Awareness",
                "Safety-critical Medical",
            },
        };
}
