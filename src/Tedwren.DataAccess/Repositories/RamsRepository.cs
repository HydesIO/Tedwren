using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IRamsRepository"/> for RAMS review (PRD §8.2), scoped by company (R15). Append-only.</summary>
public sealed class RamsRepository : RepositoryBase, IRamsRepository
{
    private const string SelectColumns =
        "SELECT Id, CompanyId, FamilyId, Version, Reference, ContractorName, Title, SiteId, SiteName, FileReference, " +
        "Status, ReviewNote, ReviewedBy, ReviewedUtc, SubmittedUtc, IsLive FROM RamsSubmissions";

    /// <summary>Creates the repository over the connection factory.</summary>
    public RamsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new RAMS submission.</summary>
    public async Task AddAsync(RamsSubmission submission, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "INSERT INTO RamsSubmissions (Id, CompanyId, FamilyId, Version, Reference, ContractorName, Title, " +
            "SiteId, SiteName, FileReference, Status, ReviewNote, ReviewedBy, ReviewedUtc, SubmittedUtc, IsLive) VALUES " +
            "(@Id, @CompanyId, @FamilyId, @Version, @Reference, @ContractorName, @Title, @SiteId, @SiteName, " +
            "@FileReference, @Status, @ReviewNote, @ReviewedBy, @ReviewedUtc, @SubmittedUtc, @IsLive)",
            ToParams(submission), cancellationToken);

    /// <summary>Returns a company's RAMS submissions, newest first.</summary>
    public async Task<IReadOnlyList<RamsSubmission>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            SelectColumns + " WHERE CompanyId = @CompanyId ORDER BY SubmittedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <summary>Returns a single submission by id, or null if none exists.</summary>
    public async Task<RamsSubmission?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(SelectColumns + " WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    /// <summary>Updates a submission's review state.</summary>
    public async Task UpdateAsync(RamsSubmission submission, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "UPDATE RamsSubmissions SET Status = @Status, ReviewNote = @ReviewNote, ReviewedBy = @ReviewedBy, " +
            "ReviewedUtc = @ReviewedUtc, IsLive = @IsLive WHERE Id = @Id",
            ToParams(submission), cancellationToken);

    /// <summary>Returns every version in a family for the company, newest version first (spec Stage 3 live-version management).</summary>
    public async Task<IReadOnlyList<RamsSubmission>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            SelectColumns + " WHERE CompanyId = @CompanyId AND FamilyId = @FamilyId ORDER BY Version DESC",
            new { CompanyId = companyId, FamilyId = familyId }, cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <summary>Returns the highest version number in a family for the company (0 when unknown).</summary>
    public async Task<int> GetMaxVersionAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var versions = await QueryAsync<int>(
            "SELECT Version FROM RamsSubmissions WHERE CompanyId = @CompanyId AND FamilyId = @FamilyId",
            new { CompanyId = companyId, FamilyId = familyId }, cancellationToken);
        return versions.DefaultIfEmpty(0).Max();
    }

    /// <summary>The parameter set shared by insert and update.</summary>
    private static object ToParams(RamsSubmission s) => new
    {
        s.Id,
        s.CompanyId,
        s.FamilyId,
        s.Version,
        s.Reference,
        s.ContractorName,
        s.Title,
        s.SiteId,
        s.SiteName,
        s.FileReference,
        Status = (int)s.Status,
        s.ReviewNote,
        s.ReviewedBy,
        s.ReviewedUtc,
        s.SubmittedUtc,
        s.IsLive,
    };

    /// <summary>Maps a flat row to a RAMS submission entity.</summary>
    private static RamsSubmission Map(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        FamilyId = r.FamilyId,
        Version = r.Version,
        Reference = r.Reference,
        ContractorName = r.ContractorName,
        Title = r.Title,
        SiteId = r.SiteId,
        SiteName = r.SiteName,
        FileReference = r.FileReference,
        Status = (RamsStatus)r.Status,
        ReviewNote = r.ReviewNote,
        ReviewedBy = r.ReviewedBy,
        ReviewedUtc = r.ReviewedUtc,
        SubmittedUtc = r.SubmittedUtc,
        IsLive = r.IsLive,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id,
        Guid CompanyId,
        Guid FamilyId,
        int Version,
        string Reference,
        string ContractorName,
        string Title,
        Guid? SiteId,
        string? SiteName,
        string? FileReference,
        int Status,
        string? ReviewNote,
        string? ReviewedBy,
        DateTimeOffset? ReviewedUtc,
        DateTimeOffset SubmittedUtc,
        bool IsLive);
}
