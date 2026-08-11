namespace Tedwren.Abstractions.Services;

/// <summary>
/// Serves the fixed option lists (trades, company types, permit types, regions) that populate form
/// dropdowns, from the database rather than in-proc sample data. Values are keyed by a well-known list key
/// (see <see cref="ReferenceListKeys"/>) and returned in display order.
/// </summary>
public interface IReferenceDataService
{
    /// <summary>Returns the ordered values for a reference list, or an empty list for an unknown key.</summary>
    Task<IReadOnlyList<string>> GetListAsync(string listKey, CancellationToken cancellationToken = default);
}

/// <summary>The well-known reference-list keys shared by the API, client and seed data.</summary>
public static class ReferenceListKeys
{
    /// <summary>Company/organisation types (e.g. Main Contractor, Subcontractor).</summary>
    public const string CompanyTypes = "company-types";

    /// <summary>Construction trades.</summary>
    public const string Trades = "trades";

    /// <summary>Permit-to-work types.</summary>
    public const string PermitTypes = "permit-types";

    /// <summary>UK regions.</summary>
    public const string Regions = "regions";
}
