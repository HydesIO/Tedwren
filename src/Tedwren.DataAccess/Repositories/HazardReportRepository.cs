using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IHazardReportRepository"/> for hazard / near-miss reports (PRD §8.2), scoped by company (R15).</summary>
public sealed class HazardReportRepository : RepositoryBase, IHazardReportRepository
{
    private const string SelectColumns =
        "SELECT Id, CompanyId, Reference, Kind, Description, Location, Latitude, Longitude, PhotoReference, " +
        "Severity, Category, Status, AssignedTo, ReportedBy, ReportedUtc, ClosedUtc, ClosureNote FROM HazardReports";

    /// <summary>Creates the repository over the connection factory.</summary>
    public HazardReportRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new hazard report.</summary>
    public async Task AddAsync(HazardReport report, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "INSERT INTO HazardReports (Id, CompanyId, Reference, Kind, Description, Location, Latitude, Longitude, " +
            "PhotoReference, Severity, Category, Status, AssignedTo, ReportedBy, ReportedUtc, ClosedUtc, ClosureNote) " +
            "VALUES (@Id, @CompanyId, @Reference, @Kind, @Description, @Location, @Latitude, @Longitude, " +
            "@PhotoReference, @Severity, @Category, @Status, @AssignedTo, @ReportedBy, @ReportedUtc, @ClosedUtc, @ClosureNote)",
            ToParams(report), cancellationToken);

    /// <summary>Returns a company's hazard reports, newest first.</summary>
    public async Task<IReadOnlyList<HazardReport>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            SelectColumns + " WHERE CompanyId = @CompanyId ORDER BY ReportedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <summary>Returns a single hazard report by id, or null if none exists.</summary>
    public async Task<HazardReport?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(SelectColumns + " WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    /// <summary>Updates a hazard report's triage/close-out state.</summary>
    public async Task UpdateAsync(HazardReport report, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "UPDATE HazardReports SET Category = @Category, Status = @Status, AssignedTo = @AssignedTo, " +
            "ClosedUtc = @ClosedUtc, ClosureNote = @ClosureNote WHERE Id = @Id",
            ToParams(report), cancellationToken);

    /// <summary>The parameter set shared by insert and update.</summary>
    private static object ToParams(HazardReport r) => new
    {
        r.Id,
        r.CompanyId,
        r.Reference,
        Kind = (int)r.Kind,
        r.Description,
        r.Location,
        r.Latitude,
        r.Longitude,
        r.PhotoReference,
        Severity = (int)r.Severity,
        r.Category,
        Status = (int)r.Status,
        r.AssignedTo,
        r.ReportedBy,
        r.ReportedUtc,
        r.ClosedUtc,
        r.ClosureNote,
    };

    /// <summary>Maps a flat row to a hazard report entity.</summary>
    private static HazardReport Map(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Reference = r.Reference,
        Kind = (HazardKind)r.Kind,
        Description = r.Description,
        Location = r.Location,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        PhotoReference = r.PhotoReference,
        Severity = (SafetySeverity)r.Severity,
        Category = r.Category,
        Status = (HazardStatus)r.Status,
        AssignedTo = r.AssignedTo,
        ReportedBy = r.ReportedBy,
        ReportedUtc = r.ReportedUtc,
        ClosedUtc = r.ClosedUtc,
        ClosureNote = r.ClosureNote,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id,
        Guid CompanyId,
        string Reference,
        int Kind,
        string Description,
        string? Location,
        double? Latitude,
        double? Longitude,
        string? PhotoReference,
        int Severity,
        string? Category,
        int Status,
        string? AssignedTo,
        string ReportedBy,
        DateTimeOffset ReportedUtc,
        DateTimeOffset? ClosedUtc,
        string? ClosureNote);
}
