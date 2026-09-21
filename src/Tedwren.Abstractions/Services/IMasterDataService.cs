using Tedwren.Abstractions.Contracts.MasterData;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Serves and maintains the admin-owned compliance master lists (SSIP schemes, configurable document headings
/// and per-operative "other requirements") the subcontractor-onboarding wizard and gates draw on
/// (Subcontractor Onboarding spec §5–§8). Global rows are owned by the platform administrator and inherited by
/// every tenant; a main contractor sees the global rows plus its own org-scoped custom entries and may add
/// more, but never edits the shared list (R15). Reads return active values in display order; a management read
/// also includes soft-deleted rows for editing.
/// </summary>
public interface IMasterDataService
{
    /// <summary>Returns the active values for a list visible to the caller (global + own-org), in display order.</summary>
    Task<IReadOnlyList<MasterListItemDto>> GetListAsync(string listKey, CancellationToken cancellationToken = default);

    /// <summary>Returns the values for a list visible to the caller, including soft-deleted rows, for management screens.</summary>
    Task<IReadOnlyList<MasterListItemDto>> GetForManagementAsync(string listKey, CancellationToken cancellationToken = default);

    /// <summary>Adds a value; a platform administrator may add a shared (global) value, a tenant an org-scoped custom one. Returns the new id.</summary>
    Task<Guid> CreateAsync(CreateMasterListItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a value the caller owns (text/order/active). A global row is platform-admin only (R15).</summary>
    Task UpdateAsync(Guid id, UpdateMasterListItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a value the caller owns (clears IsActive). A global row is platform-admin only (R15).</summary>
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>The well-known compliance master-list keys shared by the API, client and seed data (spec §5–§8).</summary>
public static class MasterListKeys
{
    /// <summary>Configurable required-document headings a main contractor can demand of a subcontractor (spec §5/§7).</summary>
    public const string DocumentHeadings = "document-headings";

    /// <summary>SSIP membership schemes (spec §6).</summary>
    public const string SsipSchemes = "ssip-schemes";

    /// <summary>Per-operative "other requirements" a main contractor can make mandatory (spec §8).</summary>
    public const string OtherRequirements = "other-requirements";

    /// <summary>All known keys, for management screens that present one tab per list.</summary>
    public static IReadOnlyList<string> All { get; } = new[] { DocumentHeadings, SsipSchemes, OtherRequirements };
}
