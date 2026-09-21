namespace Tedwren.Domain.Entities;

/// <summary>
/// An admin-maintained option in one of the compliance master lists the subcontractor-onboarding wizard and
/// gates draw on — configurable document headings, SSIP membership schemes and per-operative "other
/// requirements" (Subcontractor Onboarding spec §5–§8). The canonical rows are owned by the platform
/// administrator (<see cref="CompanyId"/> null = global, inherited by every tenant); a main contractor may add
/// an org-scoped custom entry (<see cref="CompanyId"/> set) when something niche is missing, but never edits
/// the shared national list, which would fragment it (R15). A value is soft-deleted via <see cref="IsActive"/>
/// rather than removed, so a value already referenced by a subcontractor's configuration is never orphaned.
/// </summary>
public sealed class MasterListItem
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The list this value belongs to (see <c>MasterListKeys</c>), e.g. <c>ssip-schemes</c>.</summary>
    public string ListKey { get; set; } = string.Empty;

    /// <summary>Owning company for a custom entry, or null for a shared (global) platform-owned value.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>The display value (e.g. "CHAS", "Asbestos Awareness").</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Display order within the list (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether the value is live; a soft delete clears this rather than removing the row.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the row was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; set; }
}
