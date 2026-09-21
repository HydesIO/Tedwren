namespace Tedwren.Abstractions.Contracts.MasterData;

/// <summary>A single option in a compliance master list (Subcontractor Onboarding spec §5–§8).</summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="ListKey">The list this value belongs to (see <c>MasterListKeys</c>).</param>
/// <param name="CompanyId">Owning company for a custom entry, or null for a shared (global) value.</param>
/// <param name="Value">The display value.</param>
/// <param name="SortOrder">Display order within the list.</param>
/// <param name="IsActive">Whether the value is live (not soft-deleted).</param>
/// <param name="IsGlobal">True when the value is a shared platform-owned row (i.e. <paramref name="CompanyId"/> is null).</param>
public sealed record MasterListItemDto(
    Guid Id, string ListKey, Guid? CompanyId, string Value, int SortOrder, bool IsActive, bool IsGlobal);

/// <summary>
/// Request to add a master-list value. <paramref name="Global"/> is honoured only for a platform administrator
/// (who owns the shared national list); a tenant's entry is always scoped to its own company (R15).
/// </summary>
public sealed record CreateMasterListItemRequest(string ListKey, string Value, int SortOrder, bool Global);

/// <summary>Request to update a master-list value (display text, order, active flag — the latter enables soft delete / restore).</summary>
public sealed record UpdateMasterListItemRequest(string Value, int SortOrder, bool IsActive);
