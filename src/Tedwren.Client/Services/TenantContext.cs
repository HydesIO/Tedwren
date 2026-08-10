namespace Tedwren.Client.Services;

/// <summary>
/// The current customer (company) the console is acting for. Until multi-tenant sign-in exists, the app is
/// single-tenant and this is a well-known id shared with the API's seed data so entitlement and scoped
/// reads line up (R15). Centralised here so there is one place to replace when real tenancy arrives.
/// </summary>
public static class TenantContext
{
    /// <summary>The active company id (matches the API's default seed company).</summary>
    public static readonly Guid CurrentCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");
}
