using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ITradeRequirementRepository"/>. SQL is ANSI-portable across both engines.</summary>
public sealed class TradeRequirementRepository : RepositoryBase, ITradeRequirementRepository
{
    private const string Columns = "Id, Trade, QualificationTypeId";

    /// <summary>Creates the repository over the connection factory.</summary>
    public TradeRequirementRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the requirements for a trade (case-insensitive via LOWER on both sides).</summary>
    public async Task<IReadOnlyList<TradeQualificationRequirement>> GetByTradeAsync(string trade, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM TradeQualificationRequirements WHERE LOWER(Trade) = LOWER(@Trade)",
            new { Trade = trade }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns every trade requirement.</summary>
    public async Task<IReadOnlyList<TradeQualificationRequirement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM TradeQualificationRequirements", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Inserts a new trade requirement.</summary>
    public Task AddAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO TradeQualificationRequirements (Id, Trade, QualificationTypeId) " +
            "VALUES (@Id, @Trade, @QualificationTypeId)",
            new { requirement.Id, requirement.Trade, requirement.QualificationTypeId },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static TradeQualificationRequirement ToEntity(Row r) => new()
    {
        Id = r.Id,
        Trade = r.Trade,
        QualificationTypeId = r.QualificationTypeId,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(Guid Id, string Trade, Guid QualificationTypeId);
}
