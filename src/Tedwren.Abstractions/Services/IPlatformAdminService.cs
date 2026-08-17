using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Users;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Read surface for the Tedwren platform admin area: cross-company operational views of every company and
/// user on the platform. Distinct from the tenant-scoped services so these all-company reads are served by
/// a dedicated, platform-admin-gated API surface (<c>/api/admin</c>) rather than the tenant console
/// endpoints. Billing/mandate/payment surfaces are added to this area in later phases.
/// </summary>
public interface IPlatformAdminService
{
    /// <summary>Returns every company on the platform as a list-row summary.</summary>
    Task<IReadOnlyList<CompanySummary>> GetCompaniesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns every console user on the platform (operational fields only).</summary>
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits any console user (name, role, suspend state and an optional password reset) from the admin area.
    /// Returns the updated user, or null when no user matches the id.
    /// </summary>
    Task<UserDto?> UpdateUserAsync(Guid id, AdminUpdateUserRequest request, CancellationToken cancellationToken = default);
}
