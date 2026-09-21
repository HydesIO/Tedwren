using Tedwren.Abstractions.Contracts.MasterData;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.MasterData;

/// <summary>
/// Store-agnostic compliance master-data service (Subcontractor Onboarding spec §5–§8). Serves the
/// SSIP/document-heading/"other requirements" lists and maintains them under the spec's ownership model: the
/// platform administrator owns the shared (global) rows every tenant inherits, while a main contractor sees
/// those plus its own org-scoped custom entries and may add more — but may never edit the shared national list
/// (R15). Soft-delete keeps a referenced value out of pickers without destroying it.
/// </summary>
public sealed class MasterDataService : IMasterDataService
{
    private readonly IMasterListItemRepository _repository;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// Creates the service over the repository. <paramref name="currentUser"/> is optional (supplied by DI) so
    /// read scope and platform-admin write rights are resolved from the signed-in identity (R15); when it is
    /// absent (direct construction in unit tests) the caller is treated as an unscoped platform administrator.
    /// </summary>
    public MasterDataService(IMasterListItemRepository repository, ICurrentUserService? currentUser = null)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>Returns the active values for a list visible to the caller (global + own-org), in display order.</summary>
    public async Task<IReadOnlyList<MasterListItemDto>> GetListAsync(string listKey, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetByKeyAsync(listKey, await CallerCompanyAsync(cancellationToken), includeInactive: false, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    /// <summary>Returns the values for a list visible to the caller, including soft-deleted rows, for management screens.</summary>
    public async Task<IReadOnlyList<MasterListItemDto>> GetForManagementAsync(string listKey, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetByKeyAsync(listKey, await CallerCompanyAsync(cancellationToken), includeInactive: true, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    /// <summary>Adds a value; a platform administrator may add a shared (global) value, a tenant an org-scoped custom one.</summary>
    public async Task<Guid> CreateAsync(CreateMasterListItemRequest request, CancellationToken cancellationToken = default)
    {
        var value = (request.Value ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            throw new ArgumentException("A value is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ListKey))
        {
            throw new ArgumentException("A list key is required.", nameof(request));
        }

        var caller = await CallerAsync(cancellationToken);
        Guid? companyId;
        if (request.Global)
        {
            // Only the platform administrator owns the shared national list; letting a tenant write to it is
            // exactly the fragmentation the spec forbids (§9 ownership note).
            if (!caller.IsPlatformAdmin)
            {
                throw new InvalidOperationException("Only a platform administrator can add to the shared list.");
            }

            companyId = null;
        }
        else
        {
            companyId = caller.CompanyId
                ?? throw new InvalidOperationException("A signed-in company is required to add a custom entry.");
        }

        var item = new MasterListItem
        {
            Id = Guid.NewGuid(),
            ListKey = request.ListKey.Trim(),
            CompanyId = companyId,
            Value = value,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedUtc = DateTimeOffset.UtcNow,
        };
        await _repository.AddAsync(item, cancellationToken);
        return item.Id;
    }

    /// <summary>Updates a value the caller owns (text/order/active). A global row is platform-admin only (R15).</summary>
    public async Task UpdateAsync(Guid id, UpdateMasterListItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await AuthorizeMutationAsync(id, cancellationToken);
        var value = (request.Value ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            throw new ArgumentException("A value is required.", nameof(request));
        }

        item.Value = value;
        item.SortOrder = request.SortOrder;
        item.IsActive = request.IsActive;
        await _repository.UpdateAsync(item, cancellationToken);
    }

    /// <summary>Soft-deletes a value the caller owns (clears IsActive). A global row is platform-admin only (R15).</summary>
    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await AuthorizeMutationAsync(id, cancellationToken);
        item.IsActive = false;
        await _repository.UpdateAsync(item, cancellationToken);
    }

    /// <summary>Loads the target row and checks the caller may mutate it: a global row is platform-admin only; an org row only its owner (R15).</summary>
    private async Task<MasterListItem> AuthorizeMutationAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The list value was not found.");

        if (_currentUser is null)
        {
            return item;   // unscoped (unit tests) — no tenant restriction
        }

        var caller = await _currentUser.GetCurrentAsync(cancellationToken);
        if (caller.IsPlatformAdmin)
        {
            return item;
        }

        if (item.CompanyId is null)
        {
            throw new InvalidOperationException("Only a platform administrator can change the shared list.");
        }

        if (item.CompanyId != caller.CompanyId)
        {
            throw new InvalidOperationException("This value belongs to another company.");
        }

        return item;
    }

    /// <summary>The signed-in caller's rights, or an unscoped platform admin for direct construction (tests).</summary>
    private async Task<CallerContext> CallerAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return new CallerContext(IsPlatformAdmin: true, CompanyId: null);
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return new CallerContext(user.IsPlatformAdmin, user.CompanyId);
    }

    /// <summary>The caller's company for read-scoping (null for platform admin / unscoped tests → global rows only).</summary>
    private async Task<Guid?> CallerCompanyAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return null;
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return user.CompanyId;
    }

    /// <summary>Maps a domain value to its DTO (IsGlobal is derived from a null company).</summary>
    private static MasterListItemDto ToDto(MasterListItem i) =>
        new(i.Id, i.ListKey, i.CompanyId, i.Value, i.SortOrder, i.IsActive, i.CompanyId is null);

    /// <summary>The subset of the caller identity this service needs.</summary>
    private readonly record struct CallerContext(bool IsPlatformAdmin, Guid? CompanyId);
}
