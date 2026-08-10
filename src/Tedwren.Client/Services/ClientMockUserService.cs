using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IUserService"/> for client <c>DataSource=Mock</c>. Unlike the read-only mock services, this
/// keeps a live in-memory list so the Users screen is fully interactive without a backend — inviting,
/// editing, suspending and reactivating all take effect immediately and persist for the browser session.
/// The seed mirrors the API mock store so both modes look the same. Access is withdrawn by suspension,
/// never by deletion (SF-20/SF-23).
/// </summary>
public sealed class ClientMockUserService : IUserService
{
    // Static so the list survives component re-renders and navigation within the session.
    private static readonly List<UserDto> Users = CreateSeed();
    private static readonly object Gate = new();

    private static readonly IReadOnlyList<RoleOption> Roles = new List<RoleOption>
    {
        new("Administrator", "Administrator", true),
        new("ComplianceManager", "Compliance Manager", true),
        new("SiteManager", "Site Manager", true),
        new("Auditor", "Auditor (read-only)", false),
    };

    /// <summary>Returns the current users, ordered by name.</summary>
    public Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        lock (Gate)
        {
            IReadOnlyList<UserDto> snapshot = Users.OrderBy(u => u.Name).ToList();
            return Task.FromResult(snapshot);
        }
    }

    /// <summary>Returns a user by id, or null.</summary>
    public Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (Gate)
        {
            return Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
        }
    }

    /// <summary>Returns the selectable roles.</summary>
    public Task<IReadOnlyList<RoleOption>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Roles);

    /// <summary>Invites a user, adding them to the live list. Rejects duplicate emails.</summary>
    public Task<Guid> InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
    {
        lock (Gate)
        {
            if (Users.Any(u => string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");
            }

            var role = Roles.FirstOrDefault(r => r.Value == request.Role) ?? Roles[^1];
            var user = new UserDto(
                Guid.NewGuid(), request.Name.Trim(), request.Email.Trim(),
                role.Value, role.Label, "Invited", "Invited", role.CanWrite,
                DateTimeOffset.UtcNow, LastActiveUtc: null);
            Users.Add(user);
            return Task.FromResult(user.Id);
        }
    }

    /// <summary>Updates a user's name and role. Null when not found.</summary>
    public Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        lock (Gate)
        {
            var index = Users.FindIndex(u => u.Id == id);
            if (index < 0)
            {
                return Task.FromResult<UserDto?>(null);
            }

            var role = Roles.FirstOrDefault(r => r.Value == request.Role) ?? Roles[^1];
            var updated = Users[index] with
            {
                Name = request.Name.Trim(),
                Role = role.Value,
                RoleLabel = role.Label,
                CanWrite = role.CanWrite,
            };
            Users[index] = updated;
            return Task.FromResult<UserDto?>(updated);
        }
    }

    /// <summary>Suspends a user. Null when not found.</summary>
    public Task<UserDto?> SuspendUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetStatus(id, "Suspended");

    /// <summary>Reactivates a user. Null when not found.</summary>
    public Task<UserDto?> ReactivateUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetStatus(id, "Active");

    /// <summary>Sets a user's status label and returns the updated row, or null when not found.</summary>
    private static Task<UserDto?> SetStatus(Guid id, string status)
    {
        lock (Gate)
        {
            var index = Users.FindIndex(u => u.Id == id);
            if (index < 0)
            {
                return Task.FromResult<UserDto?>(null);
            }

            var updated = Users[index] with { Status = status, StatusLabel = status };
            Users[index] = updated;
            return Task.FromResult<UserDto?>(updated);
        }
    }

    /// <summary>Builds the deterministic seed shown when mock mode first loads.</summary>
    private static List<UserDto> CreateSeed() => new()
    {
        Seed("Alex Morgan", "alex.morgan@meridian.example", "Administrator", "Administrator", true, "Active", 0),
        Seed("Priya Shah", "priya.shah@meridian.example", "ComplianceManager", "Compliance Manager", true, "Active", 1),
        Seed("Danny Cole", "danny.cole@meridian.example", "SiteManager", "Site Manager", true, "Active", 3),
        Seed("Ruth Bevan", "ruth.bevan@insurer.example", "Auditor", "Auditor (read-only)", false, "Active", 12),
        Seed("Sam Whitfield", "sam.whitfield@meridian.example", "SiteManager", "Site Manager", true, "Invited", null),
        Seed("Jordan Vale", "jordan.vale@meridian.example", "ComplianceManager", "Compliance Manager", true, "Suspended", 96),
    };

    /// <summary>Creates one seed row.</summary>
    private static UserDto Seed(string name, string email, string role, string roleLabel, bool canWrite, string status, int? lastActiveDaysAgo) =>
        new(Guid.NewGuid(), name, email, role, roleLabel, status, status, canWrite,
            DateTimeOffset.UtcNow.AddDays(-30),
            lastActiveDaysAgo is { } days ? DateTimeOffset.UtcNow.AddDays(-days) : null);
}
