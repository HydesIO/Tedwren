namespace Tedwren.Abstractions.Common;

/// <summary>
/// Which of the two products a company is on (PRD §2 — "two MVP products, one platform, sold separately"),
/// as carried across the API/DTO boundary and branched on by the client. Mirrors the domain
/// <c>Tedwren.Domain.Enums.OrgType</c>; the Application layer maps between the two (the client references
/// only Abstractions, the entity only Domain). Drives the default module bundle (SF-22), the console shape
/// (SUB-24 vs MC-23) and the sign-in semantics (R18).
/// </summary>
public enum OrgType
{
    /// <summary>A specialist subcontractor: time &amp; attendance + the compliance pack (§5.2).</summary>
    Subcontractor,

    /// <summary>A principal/regional main contractor: workforce management + the site-entry decision (§5.3).</summary>
    MainContractor,
}
