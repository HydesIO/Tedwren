namespace Tedwren.Domain.Enums;

/// <summary>
/// A console access role. A read-only <see cref="Auditor"/> role exists for auditors, insurers or a client
/// given temporary visibility (SF-23) — it can see but never change data.
/// </summary>
public enum AccessRole
{
    /// <summary>Full administrative access.</summary>
    Administrator = 0,

    /// <summary>Manages compliance (workforce, cards, packs).</summary>
    ComplianceManager = 1,

    /// <summary>Manages their own sites.</summary>
    SiteManager = 2,

    /// <summary>Read-only visibility for an auditor, insurer or client (SF-23).</summary>
    Auditor = 3,
}

/// <summary>Permission helpers for <see cref="AccessRole"/>.</summary>
public static class RolePermissions
{
    /// <summary>Whether a role may change data. The <see cref="AccessRole.Auditor"/> role is read-only (SF-23).</summary>
    public static bool CanWrite(AccessRole role) => role != AccessRole.Auditor;
}
