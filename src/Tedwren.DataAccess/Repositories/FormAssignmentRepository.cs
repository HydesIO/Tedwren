using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IFormAssignmentRepository"/> (the PRD-Phase 2 Forms Library). Assignments are flat rows —
/// no JSON — so the mapping is direct. SQL is ANSI-portable across both engines.
/// </summary>
public sealed class FormAssignmentRepository : RepositoryBase, IFormAssignmentRepository
{
    private const string Columns =
        "Id, CompanyId, FormTemplateFamilyId, FormName, Scope, SiteId, PersonId, InductionTemplateId, Schedule, FailureAlertEmail, CreatedUtc, LastReminderUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public FormAssignmentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a company's assignments, newest first.</summary>
    public async Task<IReadOnlyList<FormAssignment>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM FormAssignments WHERE CompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns an assignment by id, or null.</summary>
    public async Task<FormAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM FormAssignments WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the assignments of a form family within a company.</summary>
    public async Task<IReadOnlyList<FormAssignment>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM FormAssignments WHERE CompanyId = @CompanyId AND FormTemplateFamilyId = @FamilyId",
            new { CompanyId = companyId, FamilyId = familyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns every assignment on a recurring schedule (Schedule &lt;&gt; 0 = AdHoc), across all companies (R12).</summary>
    public async Task<IReadOnlyList<FormAssignment>> GetRecurringAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM FormAssignments WHERE Schedule <> 0 ORDER BY CompanyId, CreatedUtc",
            new { }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Persists a new assignment.</summary>
    public Task AddAsync(FormAssignment assignment, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO FormAssignments ({Columns}) VALUES " +
            "(@Id, @CompanyId, @FormTemplateFamilyId, @FormName, @Scope, @SiteId, @PersonId, @InductionTemplateId, @Schedule, @FailureAlertEmail, @CreatedUtc, @LastReminderUtc)",
            ToParameters(assignment), cancellationToken);

    /// <summary>Records the last recurring-reminder time for an assignment (per-period idempotency).</summary>
    public Task UpdateLastReminderAsync(Guid id, DateTimeOffset sentUtc, CancellationToken cancellationToken = default) =>
        ExecuteAsync("UPDATE FormAssignments SET LastReminderUtc = @SentUtc WHERE Id = @Id",
            new { Id = id, SentUtc = sentUtc }, cancellationToken);

    /// <summary>Removes an assignment.</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM FormAssignments WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Flattens an assignment to Dapper parameters (enums as int).</summary>
    private static object ToParameters(FormAssignment a) => new
    {
        a.Id,
        a.CompanyId,
        a.FormTemplateFamilyId,
        a.FormName,
        Scope = (int)a.Scope,
        a.SiteId,
        a.PersonId,
        a.InductionTemplateId,
        Schedule = (int)a.Schedule,
        a.FailureAlertEmail,
        a.CreatedUtc,
        a.LastReminderUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static FormAssignment ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        FormTemplateFamilyId = r.FormTemplateFamilyId,
        FormName = r.FormName,
        Scope = (FormScope)r.Scope,
        SiteId = r.SiteId,
        PersonId = r.PersonId,
        InductionTemplateId = r.InductionTemplateId,
        Schedule = (FormSchedule)r.Schedule,
        FailureAlertEmail = r.FailureAlertEmail,
        CreatedUtc = r.CreatedUtc,
        LastReminderUtc = r.LastReminderUtc,
    };

    /// <summary>Flat row shape Dapper maps into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, Guid FormTemplateFamilyId, string FormName, int Scope, Guid? SiteId,
        Guid? PersonId, Guid? InductionTemplateId, int Schedule, string? FailureAlertEmail, DateTimeOffset CreatedUtc,
        DateTimeOffset? LastReminderUtc);
}
