using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IFormSubmissionRepository"/> (the PRD-Phase 2 Forms Library). Answers are stored as JSON;
/// captured files are stored as blobs in a companion table. Append-only evidence (R4/R10): rows are inserted
/// and only their review status is later updated. SQL is ANSI-portable across both engines.
/// </summary>
public sealed class FormSubmissionRepository : RepositoryBase, IFormSubmissionRepository
{
    private const string Columns =
        "Id, CompanyId, FormTemplateId, FormTemplateVersion, FormName, Scope, SiteId, PersonId, AnswersJson, Status, SubmittedUtc, SubmittedBy, ReviewNote";

    private const string FileColumns =
        "Id, CompanyId, SubmissionId, FieldId, FileName, ContentType, Content, UploadedUtc";

    private const string FileMetaColumns =
        "Id, CompanyId, SubmissionId, FieldId, FileName, ContentType, UploadedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public FormSubmissionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a company's submissions, newest first.</summary>
    public async Task<IReadOnlyList<FormSubmission>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM FormSubmissions WHERE CompanyId = @CompanyId ORDER BY SubmittedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a submission by id, or null.</summary>
    public async Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM FormSubmissions WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Persists a new submission.</summary>
    public Task AddAsync(FormSubmission submission, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO FormSubmissions ({Columns}) VALUES " +
            "(@Id, @CompanyId, @FormTemplateId, @FormTemplateVersion, @FormName, @Scope, @SiteId, @PersonId, @AnswersJson, @Status, @SubmittedUtc, @SubmittedBy, @ReviewNote)",
            ToParameters(submission), cancellationToken);

    /// <summary>Persists a review-state change.</summary>
    public Task UpdateAsync(FormSubmission submission, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE FormSubmissions SET Status = @Status, ReviewNote = @ReviewNote WHERE Id = @Id",
            ToParameters(submission), cancellationToken);

    /// <summary>Persists a captured file.</summary>
    public Task AddFileAsync(FormSubmissionFile file, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO FormSubmissionFiles ({FileColumns}) VALUES " +
            "(@Id, @CompanyId, @SubmissionId, @FieldId, @FileName, @ContentType, @Content, @UploadedUtc)",
            new
            {
                file.Id, file.CompanyId, file.SubmissionId, file.FieldId, file.FileName,
                file.ContentType, file.Content, file.UploadedUtc,
            }, cancellationToken);

    /// <summary>Returns file metadata (no bytes) for a submission.</summary>
    public async Task<IReadOnlyList<FormSubmissionFile>> GetFilesBySubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<FileMetaRow>(
            $"SELECT {FileMetaColumns} FROM FormSubmissionFiles WHERE SubmissionId = @SubmissionId ORDER BY UploadedUtc",
            new { SubmissionId = submissionId }, cancellationToken);
        return rows.Select(r => new FormSubmissionFile
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            SubmissionId = r.SubmissionId,
            FieldId = r.FieldId,
            FileName = r.FileName,
            ContentType = r.ContentType,
            Content = Array.Empty<byte>(),
            UploadedUtc = r.UploadedUtc,
        }).ToList();
    }

    /// <summary>Returns a single file including its bytes, or null.</summary>
    public async Task<FormSubmissionFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<FileRow>(
            $"SELECT {FileColumns} FROM FormSubmissionFiles WHERE Id = @Id", new { Id = fileId }, cancellationToken);
        return row is null ? null : new FormSubmissionFile
        {
            Id = row.Id,
            CompanyId = row.CompanyId,
            SubmissionId = row.SubmissionId,
            FieldId = row.FieldId,
            FileName = row.FileName,
            ContentType = row.ContentType,
            Content = row.Content,
            UploadedUtc = row.UploadedUtc,
        };
    }

    /// <summary>Flattens a submission to Dapper parameters (answers as JSON, enums as int).</summary>
    private static object ToParameters(FormSubmission s) => new
    {
        s.Id,
        s.CompanyId,
        s.FormTemplateId,
        s.FormTemplateVersion,
        s.FormName,
        Scope = (int)s.Scope,
        s.SiteId,
        s.PersonId,
        AnswersJson = JsonSerializer.Serialize(s.Answers),
        Status = (int)s.Status,
        s.SubmittedUtc,
        s.SubmittedBy,
        s.ReviewNote,
    };

    /// <summary>Maps a queried row to the domain entity, deserialising the answers.</summary>
    private static FormSubmission ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        FormTemplateId = r.FormTemplateId,
        FormTemplateVersion = r.FormTemplateVersion,
        FormName = r.FormName,
        Scope = (FormScope)r.Scope,
        SiteId = r.SiteId,
        PersonId = r.PersonId,
        Answers = JsonSerializer.Deserialize<List<FormAnswer>>(r.AnswersJson) ?? new List<FormAnswer>(),
        Status = (FormSubmissionStatus)r.Status,
        SubmittedUtc = r.SubmittedUtc,
        SubmittedBy = r.SubmittedBy,
        ReviewNote = r.ReviewNote,
    };

    /// <summary>Flat submission row shape Dapper maps into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, Guid FormTemplateId, int FormTemplateVersion, string FormName, int Scope,
        Guid? SiteId, Guid? PersonId, string AnswersJson, int Status, DateTimeOffset SubmittedUtc,
        string SubmittedBy, string? ReviewNote);

    /// <summary>Flat file row (with bytes) shape Dapper maps into.</summary>
    private sealed record FileRow(
        Guid Id, Guid CompanyId, Guid SubmissionId, string FieldId, string FileName, string ContentType,
        byte[] Content, DateTimeOffset UploadedUtc);

    /// <summary>Flat file metadata row (no bytes) shape Dapper maps into.</summary>
    private sealed record FileMetaRow(
        Guid Id, Guid CompanyId, Guid SubmissionId, string FieldId, string FileName, string ContentType,
        DateTimeOffset UploadedUtc);
}
