using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IAssetRepository"/> for the plant &amp; equipment register, scoped by company (R15).</summary>
public sealed class AssetRepository : RepositoryBase, IAssetRepository
{
    private const string SelectColumns =
        "SELECT Id, CompanyId, Name, AssetType, SerialNumber, Location, OwnerName, CertificationExpiry, " +
        "NextInspectionDue, Notes, Status, CreatedUtc FROM Assets";

    /// <summary>Creates the repository over the connection factory.</summary>
    public AssetRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new asset.</summary>
    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "INSERT INTO Assets (Id, CompanyId, Name, AssetType, SerialNumber, Location, OwnerName, " +
            "CertificationExpiry, NextInspectionDue, Notes, Status, CreatedUtc) VALUES " +
            "(@Id, @CompanyId, @Name, @AssetType, @SerialNumber, @Location, @OwnerName, " +
            "@CertificationExpiry, @NextInspectionDue, @Notes, @Status, @CreatedUtc)",
            ToParams(asset), cancellationToken);

    /// <summary>Returns a company's assets, newest first.</summary>
    public async Task<IReadOnlyList<Asset>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            SelectColumns + " WHERE CompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <summary>Returns a single asset by id, or null if none exists.</summary>
    public async Task<Asset?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(SelectColumns + " WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    /// <summary>Updates an asset's mutable fields.</summary>
    public async Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "UPDATE Assets SET Name = @Name, AssetType = @AssetType, SerialNumber = @SerialNumber, " +
            "Location = @Location, OwnerName = @OwnerName, CertificationExpiry = @CertificationExpiry, " +
            "NextInspectionDue = @NextInspectionDue, Notes = @Notes, Status = @Status WHERE Id = @Id",
            ToParams(asset), cancellationToken);

    /// <summary>The parameter set shared by insert and update.</summary>
    private static object ToParams(Asset a) => new
    {
        a.Id,
        a.CompanyId,
        a.Name,
        a.AssetType,
        a.SerialNumber,
        a.Location,
        a.OwnerName,
        a.CertificationExpiry,
        a.NextInspectionDue,
        a.Notes,
        Status = (int)a.Status,
        a.CreatedUtc,
    };

    /// <summary>Maps a flat row to an asset entity.</summary>
    private static Asset Map(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Name = r.Name,
        AssetType = r.AssetType,
        SerialNumber = r.SerialNumber,
        Location = r.Location,
        OwnerName = r.OwnerName,
        CertificationExpiry = r.CertificationExpiry,
        NextInspectionDue = r.NextInspectionDue,
        Notes = r.Notes,
        Status = (AssetStatus)r.Status,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id,
        Guid CompanyId,
        string Name,
        string? AssetType,
        string? SerialNumber,
        string? Location,
        string? OwnerName,
        DateOnly? CertificationExpiry,
        DateOnly? NextInspectionDue,
        string? Notes,
        int Status,
        DateTimeOffset CreatedUtc);
}
