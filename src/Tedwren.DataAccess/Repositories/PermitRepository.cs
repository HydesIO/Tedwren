using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IPermitRepository"/> for permits to work, scoped by company (R15).</summary>
public sealed class PermitRepository : RepositoryBase, IPermitRepository
{
    /// <summary>Creates the repository over the connection factory.</summary>
    public PermitRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new permit.</summary>
    public async Task AddAsync(Permit permit, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "INSERT INTO Permits (Id, CompanyId, PermitType, SiteName, ResponsiblePerson, ValidFrom, ValidTo, " +
            "Description, HighRisk, RamsAttached, Status, CreatedUtc) VALUES " +
            "(@Id, @CompanyId, @PermitType, @SiteName, @ResponsiblePerson, @ValidFrom, @ValidTo, " +
            "@Description, @HighRisk, @RamsAttached, @Status, @CreatedUtc)",
            new
            {
                permit.Id,
                permit.CompanyId,
                permit.PermitType,
                permit.SiteName,
                permit.ResponsiblePerson,
                permit.ValidFrom,
                permit.ValidTo,
                permit.Description,
                permit.HighRisk,
                permit.RamsAttached,
                Status = (int)permit.Status,
                permit.CreatedUtc,
            }, cancellationToken);

    /// <summary>Returns a company's permits, newest first.</summary>
    public async Task<IReadOnlyList<Permit>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            "SELECT Id, CompanyId, PermitType, SiteName, ResponsiblePerson, ValidFrom, ValidTo, " +
            "Description, HighRisk, RamsAttached, Status, CreatedUtc FROM Permits " +
            "WHERE CompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);

        return rows.Select(Map).ToList();
    }

    /// <summary>Returns a single permit by id, or null if none exists.</summary>
    public async Task<Permit?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            "SELECT Id, CompanyId, PermitType, SiteName, ResponsiblePerson, ValidFrom, ValidTo, " +
            "Description, HighRisk, RamsAttached, Status, CreatedUtc FROM Permits WHERE Id = @Id",
            new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    /// <summary>Updates a permit's lifecycle status.</summary>
    public async Task UpdateStatusAsync(Guid id, PermitStatus status, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "UPDATE Permits SET Status = @Status WHERE Id = @Id",
            new { Id = id, Status = (int)status }, cancellationToken);

    /// <summary>Maps a flat row to a permit entity.</summary>
    private static Permit Map(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        PermitType = r.PermitType,
        SiteName = r.SiteName,
        ResponsiblePerson = r.ResponsiblePerson,
        ValidFrom = r.ValidFrom,
        ValidTo = r.ValidTo,
        Description = r.Description,
        HighRisk = r.HighRisk,
        RamsAttached = r.RamsAttached,
        Status = (PermitStatus)r.Status,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id,
        Guid CompanyId,
        string PermitType,
        string? SiteName,
        string? ResponsiblePerson,
        DateOnly? ValidFrom,
        DateOnly? ValidTo,
        string? Description,
        bool HighRisk,
        bool RamsAttached,
        int Status,
        DateTimeOffset CreatedUtc);
}
