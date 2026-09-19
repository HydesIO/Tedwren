using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A plant/equipment asset in a company's register (PRD §8.2). It carries the asset's identity, custody and the
/// certification/inspection dates that drive the same expiry-warning schedule as a qualification card, so a
/// certificate cannot silently lapse. Scoped to the owning company (R15). The PRD asked for this entity from the
/// MVP so later Health, Safety &amp; Compliance work is an addition, not a rewrite.
/// </summary>
public sealed class Asset
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that owns the asset record.</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Display name (e.g. "Tower Crane TC-1").</summary>
    public required string Name { get; set; }

    /// <summary>The asset type/category (e.g. Crane, Excavator, MEWP) — from the reference list.</summary>
    public string? AssetType { get; set; }

    /// <summary>Manufacturer serial or fleet number.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Where the asset currently is (site or yard).</summary>
    public string? Location { get; set; }

    /// <summary>The person or company responsible for the asset.</summary>
    public string? OwnerName { get; set; }

    /// <summary>When the asset's statutory certification expires (drives the expiry warning schedule, SF-9).</summary>
    public DateOnly? CertificationExpiry { get; set; }

    /// <summary>When the asset's next inspection is due.</summary>
    public DateOnly? NextInspectionDue { get; set; }

    /// <summary>Free-text notes.</summary>
    public string? Notes { get; set; }

    /// <summary>The asset's lifecycle state.</summary>
    public AssetStatus Status { get; set; } = AssetStatus.Active;

    /// <summary>When the record was created (UTC; displayed in UK local time, R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
