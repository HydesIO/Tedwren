using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IFormTemplateRepository"/> (the PRD-Phase 2 Forms Library). The form's sections and
/// fields are stored as JSON in a single column, matching the "extensible config as JSON" convention used
/// by the induction template. SQL is ANSI-portable so the one class serves both SQL Server and PostgreSQL.
/// </summary>
public sealed class FormTemplateRepository : RepositoryBase, IFormTemplateRepository
{
    private const string Columns =
        "Id, CompanyId, FamilyId, Name, Description, Version, Status, SectionsJson, CreatedUtc, UpdatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public FormTemplateRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a company's form template versions, newest change first.</summary>
    public async Task<IReadOnlyList<FormTemplate>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM FormTemplates WHERE CompanyId = @CompanyId ORDER BY UpdatedUtc DESC, Name",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a template version by id, or null.</summary>
    public async Task<FormTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM FormTemplates WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns all versions of a family owned by a company, highest version first.</summary>
    public async Task<IReadOnlyList<FormTemplate>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM FormTemplates WHERE CompanyId = @CompanyId AND FamilyId = @FamilyId ORDER BY Version DESC",
            new { CompanyId = companyId, FamilyId = familyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Persists a new template version.</summary>
    public Task AddAsync(FormTemplate template, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO FormTemplates ({Columns}) VALUES " +
            "(@Id, @CompanyId, @FamilyId, @Name, @Description, @Version, @Status, @SectionsJson, @CreatedUtc, @UpdatedUtc)",
            ToParameters(template), cancellationToken);

    /// <summary>Updates an existing template version's editable content and status.</summary>
    public Task UpdateAsync(FormTemplate template, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE FormTemplates SET Name = @Name, Description = @Description, Status = @Status, " +
            "SectionsJson = @SectionsJson, UpdatedUtc = @UpdatedUtc WHERE Id = @Id",
            ToParameters(template), cancellationToken);

    /// <summary>Flattens a template to Dapper parameters (sections as JSON, status as int).</summary>
    private static object ToParameters(FormTemplate t) => new
    {
        t.Id,
        t.CompanyId,
        t.FamilyId,
        t.Name,
        t.Description,
        t.Version,
        Status = (int)t.Status,
        SectionsJson = JsonSerializer.Serialize(t.Sections),
        t.CreatedUtc,
        t.UpdatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity, deserialising the sections.</summary>
    private static FormTemplate ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        FamilyId = r.FamilyId,
        Name = r.Name,
        Description = r.Description,
        Version = r.Version,
        Status = (FormTemplateStatus)r.Status,
        Sections = JsonSerializer.Deserialize<List<FormSectionDef>>(r.SectionsJson) ?? new List<FormSectionDef>(),
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, Guid FamilyId, string Name, string? Description, int Version, int Status,
        string SectionsJson, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);
}
