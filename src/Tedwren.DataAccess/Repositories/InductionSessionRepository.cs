using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IInductionSessionRepository"/> (MC-1–MC-7). The completed-step ids are stored as JSON; the
/// remaining workflow fields are scalar columns. SQL is ANSI-portable across both engines; the status enum is
/// stored as its numeric value.
/// </summary>
public sealed class InductionSessionRepository : RepositoryBase, IInductionSessionRepository
{
    private const string Columns =
        "Id, TemplateId, CompanyId, PersonId, PersonName, Status, StartedUtc, CompletedUtc, CompletionReference, " +
        "ExpiresUtc, AttemptCount, LastScore, CompletedStepsJson, SignatureName, ConsentGiven, ResetReason, SupersededBySessionId";

    /// <summary>Creates the repository over the connection factory.</summary>
    public InductionSessionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a session by id, or null.</summary>
    public async Task<InductionSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM InductionSessions WHERE Id = @Id", new { Id = sessionId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns a company's sessions, newest first.</summary>
    public async Task<IReadOnlyList<InductionSession>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM InductionSessions WHERE CompanyId = @CompanyId ORDER BY StartedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the operative's latest non-superseded session for a template, or null (MC-7). Status 3 = Superseded.</summary>
    public async Task<InductionSession?> GetLatestForPersonAsync(Guid companyId, Guid templateId, Guid personId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM InductionSessions " +
            "WHERE CompanyId = @CompanyId AND TemplateId = @TemplateId AND PersonId = @PersonId AND Status <> 3 " +
            "ORDER BY StartedUtc DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY",
            new { CompanyId = companyId, TemplateId = templateId, PersonId = personId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the operative's latest passed induction for the company across any template, or null (MC-8). Status 2 = Passed.</summary>
    public async Task<InductionSession?> GetLatestPassedForPersonAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM InductionSessions " +
            "WHERE CompanyId = @CompanyId AND PersonId = @PersonId AND Status = 2 " +
            "ORDER BY CompletedUtc DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY",
            new { CompanyId = companyId, PersonId = personId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Persists a new session.</summary>
    public Task AddAsync(InductionSession session, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO InductionSessions ({Columns}) VALUES " +
            "(@Id, @TemplateId, @CompanyId, @PersonId, @PersonName, @Status, @StartedUtc, @CompletedUtc, @CompletionReference, " +
            "@ExpiresUtc, @AttemptCount, @LastScore, @CompletedStepsJson, @SignatureName, @ConsentGiven, @ResetReason, @SupersededBySessionId)",
            ToParameters(session), cancellationToken);

    /// <summary>Persists workflow changes to a session.</summary>
    public Task UpdateAsync(InductionSession session, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE InductionSessions SET Status = @Status, CompletedUtc = @CompletedUtc, CompletionReference = @CompletionReference, " +
            "ExpiresUtc = @ExpiresUtc, AttemptCount = @AttemptCount, LastScore = @LastScore, CompletedStepsJson = @CompletedStepsJson, " +
            "SignatureName = @SignatureName, ConsentGiven = @ConsentGiven, ResetReason = @ResetReason, SupersededBySessionId = @SupersededBySessionId " +
            "WHERE Id = @Id",
            ToParameters(session), cancellationToken);

    /// <summary>Flattens a session to Dapper parameters (enum as int, completed steps as JSON).</summary>
    private static object ToParameters(InductionSession s) => new
    {
        s.Id,
        s.TemplateId,
        s.CompanyId,
        s.PersonId,
        s.PersonName,
        Status = (int)s.Status,
        s.StartedUtc,
        s.CompletedUtc,
        s.CompletionReference,
        s.ExpiresUtc,
        s.AttemptCount,
        s.LastScore,
        CompletedStepsJson = JsonSerializer.Serialize(s.CompletedStepIds),
        s.SignatureName,
        s.ConsentGiven,
        s.ResetReason,
        s.SupersededBySessionId,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static InductionSession ToEntity(Row r) => new()
    {
        Id = r.Id,
        TemplateId = r.TemplateId,
        CompanyId = r.CompanyId,
        PersonId = r.PersonId,
        PersonName = r.PersonName,
        Status = (InductionStatus)r.Status,
        StartedUtc = r.StartedUtc,
        CompletedUtc = r.CompletedUtc,
        CompletionReference = r.CompletionReference,
        ExpiresUtc = r.ExpiresUtc,
        AttemptCount = r.AttemptCount,
        LastScore = r.LastScore,
        CompletedStepIds = string.IsNullOrWhiteSpace(r.CompletedStepsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(r.CompletedStepsJson) ?? new List<string>(),
        SignatureName = r.SignatureName,
        ConsentGiven = r.ConsentGiven,
        ResetReason = r.ResetReason,
        SupersededBySessionId = r.SupersededBySessionId,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid TemplateId, Guid CompanyId, Guid PersonId, string PersonName, int Status,
        DateTimeOffset StartedUtc, DateTimeOffset? CompletedUtc, string? CompletionReference, DateTimeOffset? ExpiresUtc,
        int AttemptCount, int? LastScore, string? CompletedStepsJson, string? SignatureName, bool ConsentGiven,
        string? ResetReason, Guid? SupersededBySessionId);
}
