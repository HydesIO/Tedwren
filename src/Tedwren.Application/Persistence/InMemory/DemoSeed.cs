namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Fixed identifiers shared across the in-memory demo seed so the separate stores line up into one coherent
/// dataset (API <c>DataSource=Mock</c>). Attendance, decisions, induction records and qualification cards all
/// reference the same operatives and sites the organisation/site stores create, so the demo tenants show a
/// joined-up picture (a muster of real operatives, cards against real people) rather than orphaned rows.
/// Only used by the mock seed — never at runtime against a database.
/// </summary>
public static class DemoSeed
{
    // Main contractor (Meridian) operatives.
    /// <summary>James Fletcher (Meridian, Bricklayer).</summary>
    public static readonly Guid FletcherPersonId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");

    /// <summary>Daniel Marsh (Meridian, Site Supervisor).</summary>
    public static readonly Guid MarshPersonId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000002");

    // Subcontractor (Apex) operatives.
    /// <summary>Samuel Okafor (Apex, Groundworker).</summary>
    public static readonly Guid OkaforPersonId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000003");

    /// <summary>Carlos Reyes (Apex, Plant Operator).</summary>
    public static readonly Guid ReyesPersonId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000004");

    // Second subcontractor (Kingsway) operative.
    /// <summary>Owen Pearce (Kingsway, Electrician).</summary>
    public static readonly Guid PearcePersonId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000005");

    /// <summary>Meridian Tower — the main contractor's boundaried site (muster / site-entry demo).</summary>
    public static readonly Guid MeridianTowerSiteId = Guid.Parse("dddddddd-0000-4000-8000-000000000001");

    /// <summary>Apex Yard — the subcontractor's site.</summary>
    public static readonly Guid ApexYardSiteId = Guid.Parse("dddddddd-0000-4000-8000-000000000002");
}
