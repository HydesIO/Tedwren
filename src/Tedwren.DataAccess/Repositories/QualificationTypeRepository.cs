using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IQualificationTypeRepository"/>. SQL is ANSI-portable across both engines.</summary>
public sealed class QualificationTypeRepository : RepositoryBase, IQualificationTypeRepository
{
    private const string Columns =
        "Id, Name, Category, Issuer, DefaultValidityMonths, IsCscsVerifiable, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public QualificationTypeRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns all qualification types ordered by name.</summary>
    public async Task<IReadOnlyList<QualificationType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM QualificationTypes ORDER BY Name", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a type by id, or null.</summary>
    public async Task<QualificationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM QualificationTypes WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new qualification type.</summary>
    public Task AddAsync(QualificationType type, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO QualificationTypes (Id, Name, Category, Issuer, DefaultValidityMonths, IsCscsVerifiable, CreatedUtc) " +
            "VALUES (@Id, @Name, @Category, @Issuer, @DefaultValidityMonths, @IsCscsVerifiable, @CreatedUtc)",
            new
            {
                type.Id, type.Name, type.Category, type.Issuer,
                type.DefaultValidityMonths, type.IsCscsVerifiable, type.CreatedUtc,
            },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static QualificationType ToEntity(Row r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Category = r.Category,
        Issuer = r.Issuer,
        DefaultValidityMonths = r.DefaultValidityMonths,
        IsCscsVerifiable = r.IsCscsVerifiable,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string Name, string? Category, string? Issuer,
        int DefaultValidityMonths, bool IsCscsVerifiable, DateTimeOffset CreatedUtc);
}
