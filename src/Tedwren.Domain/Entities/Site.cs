using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A site a company records and manages. A subcontractor records the sites its people attend — sites
/// belonging to other companies — and recording a site costs nothing and is unlimited (SF-6). A site
/// carries a boundary used to verify sign-in (SF-14). It may have no fixed point of presence — no compound,
/// gate or cabin to attach a code to (SF-25) — and it may be a dispersed scheme of many properties, each
/// with its own boundary, grouped under this one site for management, reporting and billing (SF-26).
/// </summary>
public sealed class Site
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that recorded/manages this site (owner; reads are scoped to it, R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Site name.</summary>
    public required string Name { get; set; }

    /// <summary>The client the site belongs to (free text — for a subcontractor-recorded external site, SF-6).</summary>
    public string? Client { get; set; }

    /// <summary>Region (free text).</summary>
    public string? Region { get; set; }

    /// <summary>Address (free text; the overall scheme address for a dispersed site).</summary>
    public string? Address { get; set; }

    /// <summary>
    /// Whether the site has a fixed point of presence (a compound/gate/cabin something can be attached to).
    /// When false, sign-in cannot depend on anything physically present — the SF-25 phone route is used.
    /// </summary>
    public bool HasCompound { get; set; } = true;

    /// <summary>Whether this site is a dispersed scheme of individual properties (SF-26).</summary>
    public bool IsDispersed { get; set; }

    /// <summary>The site-level boundary (SF-14). Null for a dispersed scheme, whose boundaries live on its properties.</summary>
    public Geofence? Boundary { get; set; }

    /// <summary>What happens at sign-in when a worker's location is unavailable (SF-15). Defaults to record-and-flag.</summary>
    public LocationPolicy LocationPolicy { get; set; } = LocationPolicy.RecordAndFlag;

    /// <summary>When the site record was created (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
