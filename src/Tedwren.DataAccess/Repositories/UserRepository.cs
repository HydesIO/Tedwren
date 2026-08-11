using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IUserRepository"/> for console users (SF-20/SF-23). ANSI-portable across engines.</summary>
public sealed class UserRepository : RepositoryBase, IUserRepository
{
    private const string Columns =
        "Id, CompanyId, Name, Email, Role, Status, CreatedUtc, LastActiveUtc, " +
        "PasswordHash, PasswordSetUtc, InviteToken, InviteTokenExpiresUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public UserRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns all users ordered by name.</summary>
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM Users ORDER BY Name", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a user by id, or null.</summary>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Users WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns a user by email (case-insensitive via LOWER), or null.</summary>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Users WHERE LOWER(Email) = LOWER(@Email)",
            new { Email = email }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the user holding this invite token, or null.</summary>
    public async Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Users WHERE InviteToken = @InviteToken",
            new { InviteToken = inviteToken }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new user.</summary>
    public Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Users (Id, CompanyId, Name, Email, Role, Status, CreatedUtc, LastActiveUtc, " +
            "PasswordHash, PasswordSetUtc, InviteToken, InviteTokenExpiresUtc) " +
            "VALUES (@Id, @CompanyId, @Name, @Email, @Role, @Status, @CreatedUtc, @LastActiveUtc, " +
            "@PasswordHash, @PasswordSetUtc, @InviteToken, @InviteTokenExpiresUtc)",
            ToParameters(user), cancellationToken);

    /// <summary>Updates an existing user's mutable fields (including credentials/invite state).</summary>
    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Users SET Name = @Name, Email = @Email, Role = @Role, Status = @Status, " +
            "LastActiveUtc = @LastActiveUtc, PasswordHash = @PasswordHash, PasswordSetUtc = @PasswordSetUtc, " +
            "InviteToken = @InviteToken, InviteTokenExpiresUtc = @InviteTokenExpiresUtc WHERE Id = @Id",
            ToParameters(user), cancellationToken);

    /// <summary>Flattens a user to Dapper parameters (enums stored as ints).</summary>
    private static object ToParameters(User u) => new
    {
        u.Id,
        u.CompanyId,
        u.Name,
        u.Email,
        Role = (int)u.Role,
        Status = (int)u.Status,
        u.CreatedUtc,
        u.LastActiveUtc,
        u.PasswordHash,
        u.PasswordSetUtc,
        u.InviteToken,
        u.InviteTokenExpiresUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static User ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Name = r.Name,
        Email = r.Email,
        Role = (AccessRole)r.Role,
        Status = (UserStatus)r.Status,
        CreatedUtc = r.CreatedUtc,
        LastActiveUtc = r.LastActiveUtc,
        PasswordHash = r.PasswordHash,
        PasswordSetUtc = r.PasswordSetUtc,
        InviteToken = r.InviteToken,
        InviteTokenExpiresUtc = r.InviteTokenExpiresUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, string Name, string Email, int Role, int Status,
        DateTimeOffset CreatedUtc, DateTimeOffset? LastActiveUtc,
        string? PasswordHash, DateTimeOffset? PasswordSetUtc, string? InviteToken, DateTimeOffset? InviteTokenExpiresUtc);
}
