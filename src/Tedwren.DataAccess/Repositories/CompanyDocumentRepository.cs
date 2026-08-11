using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ICompanyDocumentRepository"/> (SUB-4). SQL is ANSI-portable across SQL Server and PostgreSQL.</summary>
public sealed class CompanyDocumentRepository : RepositoryBase, ICompanyDocumentRepository
{
    private const string Columns = "Id, CompanyId, Name, Type, ExpiresOn, Reference, CreatedUtc";

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

    /// <summary>Inserts a new company document.</summary>
    public Task AddAsync(CompanyDocument document, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO CompanyDocuments (Id, CompanyId, Name, Type, ExpiresOn, Reference, CreatedUtc) " +
            "VALUES (@Id, @CompanyId, @Name, @Type, @ExpiresOn, @Reference, @CreatedUtc)",
            new
            {
                document.Id, document.CompanyId, document.Name, document.Type,
                document.ExpiresOn, document.Reference, document.CreatedUtc,
            },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static CompanyDocument ToEntity(CompanyDocumentRow r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Name = r.Name,
        Type = r.Type,
        ExpiresOn = r.ExpiresOn,
        Reference = r.Reference,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record CompanyDocumentRow(
        Guid Id, Guid CompanyId, string Name, string Type, DateOnly? ExpiresOn, string? Reference, DateTimeOffset CreatedUtc);
}
