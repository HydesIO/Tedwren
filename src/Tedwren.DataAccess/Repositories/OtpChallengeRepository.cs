using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IOtpChallengeRepository"/> for pending one-time-code challenges (M2).</summary>
public sealed class OtpChallengeRepository : RepositoryBase, IOtpChallengeRepository
{
    private const string Columns = "Id, PhoneNumber, CodeHash, ExpiresUtc, Attempts, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public OtpChallengeRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<OtpChallenge?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        // Ordered in code (not TOP/LIMIT) so the SQL stays ANSI-portable across SQL Server and PostgreSQL.
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM OtpChallenges WHERE PhoneNumber = @PhoneNumber", new { PhoneNumber = phoneNumber }, cancellationToken);
        var row = rows.OrderByDescending(r => r.CreatedUtc).FirstOrDefault();
        return row is null ? null : ToEntity(row);
    }

    public Task AddAsync(OtpChallenge challenge, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO OtpChallenges (Id, PhoneNumber, CodeHash, ExpiresUtc, Attempts, CreatedUtc) VALUES " +
            "(@Id, @PhoneNumber, @CodeHash, @ExpiresUtc, @Attempts, @CreatedUtc)",
            ToParameters(challenge), cancellationToken);

    public Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE OtpChallenges SET Attempts = @Attempts WHERE Id = @Id",
            ToParameters(challenge), cancellationToken);

    public Task DeleteByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM OtpChallenges WHERE PhoneNumber = @PhoneNumber", new { PhoneNumber = phoneNumber }, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM OtpChallenges WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Flattens a challenge to Dapper parameters.</summary>
    private static object ToParameters(OtpChallenge c) => new
    {
        c.Id,
        c.PhoneNumber,
        c.CodeHash,
        c.ExpiresUtc,
        c.Attempts,
        c.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static OtpChallenge ToEntity(Row r) => new()
    {
        Id = r.Id,
        PhoneNumber = r.PhoneNumber,
        CodeHash = r.CodeHash,
        ExpiresUtc = r.ExpiresUtc,
        Attempts = r.Attempts,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string PhoneNumber, string CodeHash, DateTimeOffset ExpiresUtc, int Attempts, DateTimeOffset CreatedUtc);
}
