using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the compliance master lists (spec §5–§8): global (platform-owned) and org-scoped custom values.</summary>
public interface IMasterListItemRepository
{
    /// <summary>Returns a value by id, or null.</summary>
    Task<MasterListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the values for a list visible to a caller: the global rows (CompanyId null) plus the rows owned by
    /// <paramref name="companyId"/> when supplied (a null company id returns global rows only). Inactive rows are
    /// included only when <paramref name="includeInactive"/> is set. Ordered by SortOrder then Value (R15).
    /// </summary>
    Task<IReadOnlyList<MasterListItem>> GetByKeyAsync(
        string listKey, Guid? companyId, bool includeInactive, CancellationToken cancellationToken = default);

    /// <summary>Persists a new value.</summary>
    Task AddAsync(MasterListItem item, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing value (text, order, active flag).</summary>
    Task UpdateAsync(MasterListItem item, CancellationToken cancellationToken = default);
}
