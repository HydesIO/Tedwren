using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ISitePropertyRepository"/> for dispersed-scheme properties (SF-26).</summary>
public sealed class SitePropertyRepository : RepositoryBase, ISitePropertyRepository
{
    private const string Columns =
        "Id, SiteId, Address, Units, BoundaryLatitude, BoundaryLongitude, BoundaryRadiusMetres, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public SitePropertyRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the properties of a site ordered by address.</summary>
    public async Task<IReadOnlyList<SiteProperty>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM SiteProperties WHERE SiteId = @SiteId ORDER BY Address",
            new { SiteId = siteId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Counts the properties of a site.</summary>
    public async Task<int> CountBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM SiteProperties WHERE SiteId = @SiteId", new { SiteId = siteId }, cancellationToken);
        return (int)count;
    }

    /// <summary>Inserts a new property.</summary>
    public Task AddAsync(SiteProperty property, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO SiteProperties (Id, SiteId, Address, Units, BoundaryLatitude, BoundaryLongitude, BoundaryRadiusMetres, CreatedUtc) " +
            "VALUES (@Id, @SiteId, @Address, @Units, @BoundaryLatitude, @BoundaryLongitude, @BoundaryRadiusMetres, @CreatedUtc)",
            new
            {
                property.Id,
                property.SiteId,
                property.Address,
                property.Units,
                BoundaryLatitude = property.Boundary.CentreLatitude,
                BoundaryLongitude = property.Boundary.CentreLongitude,
                BoundaryRadiusMetres = property.Boundary.RadiusMetres,
                property.CreatedUtc,
            },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static SiteProperty ToEntity(Row r) => new()
    {
        Id = r.Id,
        SiteId = r.SiteId,
        Address = r.Address,
        Units = r.Units,
        Boundary = new Geofence(r.BoundaryLatitude, r.BoundaryLongitude, r.BoundaryRadiusMetres),
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid SiteId, string Address, int Units,
        double BoundaryLatitude, double BoundaryLongitude, double BoundaryRadiusMetres, DateTimeOffset CreatedUtc);
}
