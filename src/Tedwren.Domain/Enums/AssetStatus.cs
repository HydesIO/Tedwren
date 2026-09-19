namespace Tedwren.Domain.Enums;

/// <summary>The lifecycle state of a plant/equipment asset (PRD §8.2 — the plant &amp; equipment register).</summary>
public enum AssetStatus
{
    /// <summary>In service and tracked.</summary>
    Active = 0,

    /// <summary>Retired/disposed — kept for history but no longer in service (module-off never deletes it, §9).</summary>
    Retired = 1,
}
