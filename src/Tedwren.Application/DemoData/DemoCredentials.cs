namespace Tedwren.Application.DemoData;

/// <summary>
/// The published, well-known sign-in passwords for the demonstration accounts. They are deliberately fixed and
/// held in one place so the demo dataset (<see cref="DemoDataPlanBuilder"/>) and the startup demo-login seed
/// (<see cref="DemoLoginSeeder"/>) can never drift apart. These are demo-only credentials: the seed that applies
/// them is gated on <c>Demo:Enabled</c>, which <c>StartupSecurity</c> refuses to allow in Production.
/// </summary>
public static class DemoCredentials
{
    /// <summary>Password for the two demo-tenant administrators (<c>contractor@tedwren.com</c> / <c>subcontractor@tedwren.com</c>).</summary>
    public const string TenantPassword = "Demo123!";

    /// <summary>Password for the named Tedwren platform administrators on a demonstration/staging deployment.</summary>
    public const string AdminPassword = "Admin123!";
}
