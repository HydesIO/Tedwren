using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IUserRefreshTokenRepository"/> for console user refresh tokens (M8).</summary>
public sealed class UserRefreshTokenRepository : RepositoryBase, IUserRefreshTokenRepository
{
    private const string Columns = "Id, UserId, CompanyId, TokenHash, ExpiresUtc, CreatedUtc, LastUsedUtc, RevokedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public UserRefreshTokenRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<UserRefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM UserRefreshTokens WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    public Task AddAsync(UserRefreshToken token, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO UserRefreshTokens (Id, UserId, CompanyId, TokenHash, ExpiresUtc, CreatedUtc, LastUsedUtc, RevokedUtc) " +
            "VALUES (@Id, @UserId, @CompanyId, @TokenHash, @ExpiresUtc, @CreatedUtc, @LastUsedUtc, @RevokedUtc)",
            ToParameters(token), cancellationToken);

    public Task UpdateAsync(UserRefreshToken token, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE UserRefreshTokens SET TokenHash = @TokenHash, ExpiresUtc = @ExpiresUtc, LastUsedUtc = @LastUsedUtc, " +
            "RevokedUtc = @RevokedUtc WHERE Id = @Id",
            ToParameters(token), cancellationToken);

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedUtc, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE UserRefreshTokens SET RevokedUtc = @RevokedUtc WHERE UserId = @UserId AND RevokedUtc IS NULL",
            new { UserId = userId, RevokedUtc = revokedUtc }, cancellationToken);

    /// <summary>Flattens a token to Dapper parameters.</summary>
    private static object ToParameters(UserRefreshToken t) => new
    {
        t.Id,
        t.UserId,
        t.CompanyId,
        t.TokenHash,
        t.ExpiresUtc,
        t.CreatedUtc,
        t.LastUsedUtc,
        t.RevokedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static UserRefreshToken ToEntity(Row r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        CompanyId = r.CompanyId,
        TokenHash = r.TokenHash,
        ExpiresUtc = r.ExpiresUtc,
        CreatedUtc = r.CreatedUtc,
        LastUsedUtc = r.LastUsedUtc,
        RevokedUtc = r.RevokedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid UserId, Guid CompanyId, string TokenHash, DateTimeOffset ExpiresUtc,
        DateTimeOffset CreatedUtc, DateTimeOffset LastUsedUtc, DateTimeOffset? RevokedUtc);
}
