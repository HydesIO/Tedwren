namespace Tedwren.Domain.Enums;

/// <summary>
/// Which of the two products a company is on (PRD §2 — "two MVP products, one platform, sold separately").
/// This is the durable, typed product discriminator carried beside the deliberately-open free-text
/// <see cref="Entities.Company.Type"/>: it selects the default module bundle (SF-22), the console shape
/// (SUB-24 vs MC-23) and the sign-in semantics (R18). One product per company.
/// </summary>
public enum OrgType
{
    /// <summary>A specialist subcontractor: time &amp; attendance + the compliance pack (§5.2).</summary>
    Subcontractor,

    /// <summary>A principal/regional main contractor: workforce management + the site-entry decision (§5.3).</summary>
    MainContractor,
}
