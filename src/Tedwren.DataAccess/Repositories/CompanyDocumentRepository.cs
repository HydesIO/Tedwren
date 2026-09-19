using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ICompanyDocumentRepository"/> (SUB-4). SQL is ANSI-portable across SQL Server and PostgreSQL.</summary>
public sealed class CompanyDocumentRepository : RepositoryBase, ICompanyDocumentRepository
{
    private const string Columns =
        "Id, CompanyId, Name, Type, ExpiresOn, Reference, FileReference, Version, SupersedesDocumentId, SupersededByDocumentId, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public CompanyDocumentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a company's documents, most-recently-created first.</summary>
    public async Task<IReadOnlyList<CompanyDocument>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<CompanyDocumentRow>(
            $"SELECT {Columns} FROM CompanyDocuments WHERE CompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a single document by id, or null if none exists (MC-27 versioning).</summary>
    public async Task<CompanyDocument?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<CompanyDocumentRow>(
            $"SELECT {Columns} FROM CompanyDocuments WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new company document.</summary>
    public Task AddAsync(CompanyDocument document, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO CompanyDocuments (Id, CompanyId, Name, Type, ExpiresOn, Reference, FileReference, Version, " +
            "SupersedesDocumentId, SupersededByDocumentId, CreatedUtc) " +
            "VALUES (@Id, @CompanyId, @Name, @Type, @ExpiresOn, @Reference, @FileReference, @Version, " +
            "@SupersedesDocumentId, @SupersededByDocumentId, @CreatedUtc)",
            ToParams(document), cancellationToken);

    /// <summary>Updates a document's mutable/versioning fields (MC-27: marking it superseded).</summary>
    public Task UpdateAsync(CompanyDocument document, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE CompanyDocuments SET Name = @Name, Type = @Type, ExpiresOn = @ExpiresOn, Reference = @Reference, " +
            "FileReference = @FileReference, SupersededByDocumentId = @SupersededByDocumentId WHERE Id = @Id",
            ToParams(document), cancellationToken);

    /// <summary>The parameter set shared by insert and update.</summary>
    private static object ToParams(CompanyDocument d) => new
    {
        d.Id, d.CompanyId, d.Name, d.Type, d.ExpiresOn, d.Reference, d.FileReference,
        d.Version, d.SupersedesDocumentId, d.SupersededByDocumentId, d.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static CompanyDocument ToEntity(CompanyDocumentRow r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Name = r.Name,
        Type = r.Type,
        ExpiresOn = r.ExpiresOn,
        Reference = r.Reference,
        FileReference = r.FileReference,
        Version = r.Version,
        SupersedesDocumentId = r.SupersedesDocumentId,
        SupersededByDocumentId = r.SupersededByDocumentId,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record CompanyDocumentRow(
        Guid Id, Guid CompanyId, string Name, string Type, DateOnly? ExpiresOn, string? Reference, string? FileReference,
        int Version, Guid? SupersedesDocumentId, Guid? SupersededByDocumentId, DateTimeOffset CreatedUtc);
}
