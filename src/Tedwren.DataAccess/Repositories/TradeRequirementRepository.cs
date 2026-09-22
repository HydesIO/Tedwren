using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ITradeRequirementRepository"/>. SQL is ANSI-portable across both engines.</summary>
public sealed class TradeRequirementRepository : RepositoryBase, ITradeRequirementRepository
{
    private const string Columns = "Id, Trade, QualificationTypeId, LegalMandatory, ClientRequired, CompanyId";

    /// <summary>Creates the repository over the connection factory.</summary>
    public TradeRequirementRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the requirements for a trade (case-insensitive): global rows plus those owned by the company. A null company id matches global rows only.</summary>
    public async Task<IReadOnlyList<TradeQualificationRequirement>> GetByTradeAsync(string trade, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM TradeQualificationRequirements WHERE LOWER(Trade) = LOWER(@Trade) AND (CompanyId IS NULL OR CompanyId = @CompanyId)",
            new { Trade = trade, CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns every trade requirement.</summary>
    public async Task<IReadOnlyList<TradeQualificationRequirement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM TradeQualificationRequirements", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the requirements a caller may manage: global rows plus those owned by the company (null = platform admin, global only).</summary>
    public async Task<IReadOnlyList<TradeQualificationRequirement>> GetForManagementAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM TradeQualificationRequirements WHERE CompanyId IS NULL OR CompanyId = @CompanyId",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a requirement by id, or null.</summary>
    public async Task<TradeQualificationRequirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM TradeQualificationRequirements WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new trade requirement.</summary>
    public Task AddAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO TradeQualificationRequirements (Id, Trade, QualificationTypeId, LegalMandatory, ClientRequired, CompanyId) " +
            "VALUES (@Id, @Trade, @QualificationTypeId, @LegalMandatory, @ClientRequired, @CompanyId)",
            new { requirement.Id, requirement.Trade, requirement.QualificationTypeId, requirement.LegalMandatory, requirement.ClientRequired, requirement.CompanyId },
            cancellationToken);

    /// <summary>Updates a requirement's editable flags (the trade/type/owner are immutable).</summary>
    public Task UpdateAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE TradeQualificationRequirements SET LegalMandatory = @LegalMandatory, ClientRequired = @ClientRequired WHERE Id = @Id",
            new { requirement.Id, requirement.LegalMandatory, requirement.ClientRequired }, cancellationToken);

    /// <summary>Removes a requirement (the trade→accreditation mapping row).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM TradeQualificationRequirements WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static TradeQualificationRequirement ToEntity(Row r) => new()
    {
        Id = r.Id,
        Trade = r.Trade,
        QualificationTypeId = r.QualificationTypeId,
        LegalMandatory = r.LegalMandatory,
        ClientRequired = r.ClientRequired,
        CompanyId = r.CompanyId,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(Guid Id, string Trade, Guid QualificationTypeId, bool LegalMandatory, bool ClientRequired, Guid? CompanyId);
}
