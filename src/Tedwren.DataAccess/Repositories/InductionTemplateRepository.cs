using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IInductionTemplateRepository"/> (MC-3). The configurable steps and the quiz (with its
/// server-side answers, R5) are stored as JSON, matching the "extensible config as JSON" convention. SQL is
/// ANSI-portable across both engines.
/// </summary>
public sealed class InductionTemplateRepository : RepositoryBase, IInductionTemplateRepository
{
    private const string Columns =
        "Id, CompanyId, Name, ValidityDays, PassMark, StepsJson, QuestionsJson, AttemptLimit, Mandatory, MediaUrl, SiteId";

    /// <summary>Creates the repository over the connection factory.</summary>
    public InductionTemplateRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a company's templates.</summary>
    public async Task<IReadOnlyList<InductionTemplate>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM InductionTemplates WHERE CompanyId = @CompanyId ORDER BY Name",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a template by id, or null.</summary>
    public async Task<InductionTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM InductionTemplates WHERE Id = @Id", new { Id = templateId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Persists a new template.</summary>
    public Task AddAsync(InductionTemplate template, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO InductionTemplates ({Columns}) VALUES " +
            "(@Id, @CompanyId, @Name, @ValidityDays, @PassMark, @StepsJson, @QuestionsJson, @AttemptLimit, @Mandatory, @MediaUrl, @SiteId)",
            ToParameters(template), cancellationToken);

    /// <summary>Updates a template's content and configuration (MC-15).</summary>
    public Task UpdateAsync(InductionTemplate template, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE InductionTemplates SET Name = @Name, ValidityDays = @ValidityDays, PassMark = @PassMark, " +
            "StepsJson = @StepsJson, QuestionsJson = @QuestionsJson, AttemptLimit = @AttemptLimit, " +
            "Mandatory = @Mandatory, MediaUrl = @MediaUrl, SiteId = @SiteId WHERE Id = @Id",
            ToParameters(template), cancellationToken);

    /// <summary>Flattens a template to Dapper parameters (steps/questions as JSON).</summary>
    private static object ToParameters(InductionTemplate t) => new
    {
        t.Id, t.CompanyId, t.Name, t.ValidityDays, t.PassMark,
        StepsJson = JsonSerializer.Serialize(t.Steps),
        QuestionsJson = JsonSerializer.Serialize(t.Questions),
        t.AttemptLimit, t.Mandatory, t.MediaUrl, t.SiteId,
    };

    /// <summary>Maps a queried row to the domain entity, deserialising steps and questions.</summary>
    private static InductionTemplate ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Name = r.Name,
        ValidityDays = r.ValidityDays,
        PassMark = r.PassMark,
        Steps = JsonSerializer.Deserialize<List<InductionStep>>(r.StepsJson) ?? new List<InductionStep>(),
        Questions = JsonSerializer.Deserialize<List<InductionQuizQuestion>>(r.QuestionsJson) ?? new List<InductionQuizQuestion>(),
        AttemptLimit = r.AttemptLimit ?? 3,
        Mandatory = r.Mandatory ?? true,
        MediaUrl = r.MediaUrl,
        SiteId = r.SiteId,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, string Name, int ValidityDays, int PassMark, string StepsJson, string QuestionsJson,
        int? AttemptLimit, bool? Mandatory, string? MediaUrl, Guid? SiteId);
}
