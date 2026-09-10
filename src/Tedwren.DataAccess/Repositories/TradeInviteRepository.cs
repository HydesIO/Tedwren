using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ITradeInviteRepository"/> for trade self-service onboarding invitations (UAT-023, SUB-4/MC-27).</summary>
public sealed class TradeInviteRepository : RepositoryBase, ITradeInviteRepository
{
    private const string Columns =
        "Id, Token, PasscodeHash, CompanyId, InviterCompanyId, ContactName, ContactEmail, Status, ExpiresUtc, " +
        "SubmittedUtc, DecidedBy, DecidedUtc, ReviewNote, CreatedByUserId, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public TradeInviteRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Inserts a new trade invite.</summary>
    public Task AddAsync(TradeInvite invite, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO TradeInvites (Id, Token, PasscodeHash, CompanyId, InviterCompanyId, ContactName, ContactEmail, " +
            "Status, ExpiresUtc, SubmittedUtc, DecidedBy, DecidedUtc, ReviewNote, CreatedByUserId, CreatedUtc) VALUES " +
            "(@Id, @Token, @PasscodeHash, @CompanyId, @InviterCompanyId, @ContactName, @ContactEmail, " +
            "@Status, @ExpiresUtc, @SubmittedUtc, @DecidedBy, @DecidedUtc, @ReviewNote, @CreatedByUserId, @CreatedUtc)",
            ToParameters(invite), cancellationToken);

    /// <summary>Returns the invite for a token, or null.</summary>
    public async Task<TradeInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM TradeInvites WHERE Token = @Token", new { Token = token }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the invite by id, or null.</summary>
    public async Task<TradeInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM TradeInvites WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the invites created by an inviting company, newest activity first.</summary>
    public async Task<IReadOnlyList<TradeInvite>> GetByInviterCompanyAsync(Guid inviterCompanyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM TradeInvites WHERE InviterCompanyId = @InviterCompanyId ORDER BY CreatedUtc DESC",
            new { InviterCompanyId = inviterCompanyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Updates an invite's mutable fields (status, submission, decision).</summary>
    public Task UpdateAsync(TradeInvite invite, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE TradeInvites SET ContactName = @ContactName, ContactEmail = @ContactEmail, Status = @Status, " +
            "SubmittedUtc = @SubmittedUtc, DecidedBy = @DecidedBy, DecidedUtc = @DecidedUtc, ReviewNote = @ReviewNote " +
            "WHERE Id = @Id",
            ToParameters(invite), cancellationToken);

    /// <summary>Flattens an invite to Dapper parameters (enum as int).</summary>
    private static object ToParameters(TradeInvite i) => new
    {
        i.Id,
        i.Token,
        i.PasscodeHash,
        i.CompanyId,
        i.InviterCompanyId,
        i.ContactName,
        i.ContactEmail,
        Status = (int)i.Status,
        i.ExpiresUtc,
        i.SubmittedUtc,
        i.DecidedBy,
        i.DecidedUtc,
        i.ReviewNote,
        i.CreatedByUserId,
        i.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static TradeInvite ToEntity(Row r) => new()
    {
        Id = r.Id,
        Token = r.Token,
        PasscodeHash = r.PasscodeHash,
        CompanyId = r.CompanyId,
        InviterCompanyId = r.InviterCompanyId,
        ContactName = r.ContactName,
        ContactEmail = r.ContactEmail,
        Status = (TradeOnboardingStatus)r.Status,
        ExpiresUtc = r.ExpiresUtc,
        SubmittedUtc = r.SubmittedUtc,
        DecidedBy = r.DecidedBy,
        DecidedUtc = r.DecidedUtc,
        ReviewNote = r.ReviewNote,
        CreatedByUserId = r.CreatedByUserId,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string Token, string? PasscodeHash, Guid CompanyId, Guid InviterCompanyId, string? ContactName,
        string? ContactEmail, int Status, DateTimeOffset ExpiresUtc, DateTimeOffset? SubmittedUtc, string? DecidedBy,
        DateTimeOffset? DecidedUtc, string? ReviewNote, Guid? CreatedByUserId, DateTimeOffset CreatedUtc);
}
