using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IInductionLinkRepository"/> for shareable, tokenised induction links (UAT-018, MC-1/MC-2).</summary>
public sealed class InductionLinkRepository : RepositoryBase, IInductionLinkRepository
{
    private const string Columns =
        "Id, Token, PasscodeHash, CompanyId, TemplateId, Name, ExpiresUtc, Status, SessionId, CreatedByUserId, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public InductionLinkRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Inserts a new induction link.</summary>
    public Task AddAsync(InductionLink link, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO InductionLinks (Id, Token, PasscodeHash, CompanyId, TemplateId, Name, ExpiresUtc, Status, " +
            "SessionId, CreatedByUserId, CreatedUtc) VALUES " +
            "(@Id, @Token, @PasscodeHash, @CompanyId, @TemplateId, @Name, @ExpiresUtc, @Status, " +
            "@SessionId, @CreatedByUserId, @CreatedUtc)",
            ToParameters(link), cancellationToken);

    /// <summary>Returns the link for a token, or null.</summary>
    public async Task<InductionLink?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM InductionLinks WHERE Token = @Token", new { Token = token }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Updates a link's mutable fields (status, started session).</summary>
    public Task UpdateAsync(InductionLink link, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE InductionLinks SET Name = @Name, Status = @Status, SessionId = @SessionId WHERE Id = @Id",
            ToParameters(link), cancellationToken);

    /// <summary>Flattens a link to Dapper parameters (enum as int).</summary>
    private static object ToParameters(InductionLink l) => new
    {
        l.Id,
        l.Token,
        l.PasscodeHash,
        l.CompanyId,
        l.TemplateId,
        l.Name,
        l.ExpiresUtc,
        Status = (int)l.Status,
        l.SessionId,
        l.CreatedByUserId,
        l.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static InductionLink ToEntity(Row r) => new()
    {
        Id = r.Id,
        Token = r.Token,
        PasscodeHash = r.PasscodeHash,
        CompanyId = r.CompanyId,
        TemplateId = r.TemplateId,
        Name = r.Name,
        ExpiresUtc = r.ExpiresUtc,
        Status = (OnboardingLinkStatus)r.Status,
        SessionId = r.SessionId,
        CreatedByUserId = r.CreatedByUserId,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string Token, string? PasscodeHash, Guid CompanyId, Guid TemplateId, string? Name,
        DateTimeOffset ExpiresUtc, int Status, Guid? SessionId, Guid? CreatedByUserId, DateTimeOffset CreatedUtc);
}
