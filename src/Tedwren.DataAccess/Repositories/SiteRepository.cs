using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ISiteRepository"/>. The boundary is stored as three nullable columns.</summary>
public sealed class SiteRepository : RepositoryBase, ISiteRepository
{
    private const string Columns =
        "Id, CompanyId, Name, Client, Region, Address, HasCompound, IsDispersed, " +
        "BoundaryLatitude, BoundaryLongitude, BoundaryRadiusMetres, LocationPolicy, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public SiteRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Deletes a site by id (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM Sites WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Returns all sites ordered by name.</summary>
    public async Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM Sites ORDER BY Name", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a site by id, or null.</summary>
    public async Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Sites WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new site.</summary>
    public Task AddAsync(Site site, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Sites (Id, CompanyId, Name, Client, Region, Address, HasCompound, IsDispersed, " +
            "BoundaryLatitude, BoundaryLongitude, BoundaryRadiusMetres, LocationPolicy, CreatedUtc) " +
            "VALUES (@Id, @CompanyId, @Name, @Client, @Region, @Address, @HasCompound, @IsDispersed, " +
            "@BoundaryLatitude, @BoundaryLongitude, @BoundaryRadiusMetres, @LocationPolicy, @CreatedUtc)",
            ToParameters(site), cancellationToken);

    /// <summary>Updates an existing site's mutable fields.</summary>
    public Task UpdateAsync(Site site, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Sites SET Name = @Name, Client = @Client, Region = @Region, Address = @Address, " +
            "HasCompound = @HasCompound, IsDispersed = @IsDispersed, BoundaryLatitude = @BoundaryLatitude, " +
            "BoundaryLongitude = @BoundaryLongitude, BoundaryRadiusMetres = @BoundaryRadiusMetres, " +
            "LocationPolicy = @LocationPolicy WHERE Id = @Id",
            ToParameters(site), cancellationToken);

    /// <summary>Flattens a site (and its optional boundary) to Dapper parameters.</summary>
    private static object ToParameters(Site s) => new
    {
        s.Id,
        s.CompanyId,
        s.Name,
        s.Client,
        s.Region,
        s.Address,
        s.HasCompound,
        s.IsDispersed,
        BoundaryLatitude = s.Boundary?.CentreLatitude,
        BoundaryLongitude = s.Boundary?.CentreLongitude,
        BoundaryRadiusMetres = s.Boundary?.RadiusMetres,
        LocationPolicy = (int)s.LocationPolicy,
        s.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity, reconstructing the boundary when present.</summary>
    private static Site ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Name = r.Name,
        Client = r.Client,
        Region = r.Region,
        Address = r.Address,
        HasCompound = r.HasCompound,
        IsDispersed = r.IsDispersed,
        Boundary = r.BoundaryLatitude is { } lat && r.BoundaryLongitude is { } lng && r.BoundaryRadiusMetres is { } radius
            ? new Geofence(lat, lng, radius)
            : null,
        LocationPolicy = (Tedwren.Domain.Enums.LocationPolicy)r.LocationPolicy,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, string Name, string? Client, string? Region, string? Address,
        bool HasCompound, bool IsDispersed, double? BoundaryLatitude, double? BoundaryLongitude,
        double? BoundaryRadiusMetres, int LocationPolicy, DateTimeOffset CreatedUtc);
}
