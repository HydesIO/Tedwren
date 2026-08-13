using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Forms;

/// <summary>
/// The single implementation of the Forms Library submission rules (PRD-Phase 2 checklist/inspection engine).
/// Validates a completed form against its published template (required-by-default, requirement 2), snapshots the
/// template name/version so the record can always be reconstructed (R16), stores captured files as blobs
/// (requirement 4), and records the submission as append-only evidence (R4/R10). Tenant-scoped — every read and
/// write is confined to the caller's company (R15).
/// </summary>
public sealed class FormSubmissionService : IFormSubmissionService
{
    private readonly IFormSubmissionRepository _submissions;
    private readonly IFormTemplateRepository _templates;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>Field kinds that capture no value, so they are never required-validated.</summary>
    private static readonly HashSet<FormFieldKind> DisplayOnly = new() { FormFieldKind.Heading, FormFieldKind.Instruction };

    /// <summary>Field kinds whose answer is a captured file rather than an inline value.</summary>
    private static readonly HashSet<FormFieldKind> FileKinds = new() { FormFieldKind.Photo, FormFieldKind.FileUpload };

    /// <summary>Creates the service over its repositories and the optional current-user (tenant + submitter).</summary>
    public FormSubmissionService(
        IFormSubmissionRepository submissions,
        IFormTemplateRepository templates,
        ICurrentUserService? currentUser = null)
    {
        _submissions = submissions;
        _templates = templates;
        _currentUser = currentUser;
    }

    /// <summary>Resolves the signed-in caller (tenant company + display name). Company null when unauthenticated / in unit tests.</summary>
    private async Task<(Guid? Company, string Name)> ResolveUserAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return (null, "System");
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return (user.CompanyId, user.Name);
    }

    /// <summary>Lists the caller's submissions, newest first (R15). Empty for an unauthenticated caller.</summary>
    public async Task<IReadOnlyList<FormSubmissionSummaryDto>> GetSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        var (company, _) = await ResolveUserAsync(cancellationToken);
        if (company is null)
        {
            return new List<FormSubmissionSummaryDto>();
        }

        var submissions = await _submissions.GetByCompanyAsync(company.Value, cancellationToken);
        return submissions.Select(ToSummaryDto).ToList();
    }

    /// <summary>Returns a submission with answers and file metadata, scoped to the caller (R15).</summary>
    public async Task<FormSubmissionDetailDto?> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await LoadOwnedAsync(id, cancellationToken);
        if (submission is null)
        {
            return null;
        }

        var files = await _submissions.GetFilesBySubmissionAsync(id, cancellationToken);
        return ToDetailDto(submission, files);
    }

    /// <summary>Submits a completed form, validating required fields against the published template (R2/R16).</summary>
    public async Task<Guid> SubmitAsync(CreateFormSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var (company, name) = await ResolveUserAsync(cancellationToken);

        var template = await _templates.GetByIdAsync(request.FormTemplateId, cancellationToken);
        if (template is null || (company is not null && template.CompanyId != company))
        {
            throw new ArgumentException("The form could not be found.", nameof(request));
        }

        if (template.Status != FormTemplateStatus.Published)
        {
            throw new ArgumentException("Only a published form can be completed.", nameof(request));
        }

        var answers = (request.Answers ?? new List<FormAnswerDto>())
            .ToDictionary(a => a.FieldId, a => a, StringComparer.Ordinal);
        var fileFieldIds = (request.Files ?? new List<FormSubmissionFileInput>())
            .Select(f => f.FieldId).ToHashSet(StringComparer.Ordinal);

        // Required-by-default: every required, value-capturing field must have a non-empty answer or a file.
        var missing = new List<string>();
        foreach (var field in template.Sections.SelectMany(s => s.Fields))
        {
            if (!field.Required || DisplayOnly.Contains(field.Kind))
            {
                continue;
            }

            var answered = FileKinds.Contains(field.Kind)
                ? fileFieldIds.Contains(field.Id)
                : answers.TryGetValue(field.Id, out var a) && HasValue(a);

            if (!answered)
            {
                missing.Add(field.Label);
            }
        }

        if (missing.Count > 0)
        {
            throw new ArgumentException($"Please complete the required field(s): {string.Join(", ", missing)}.", nameof(request));
        }

        var companyId = company ?? template.CompanyId;
        var submission = new FormSubmission
        {
            CompanyId = companyId,
            FormTemplateId = template.Id,
            FormTemplateVersion = template.Version,
            FormName = template.Name,
            Scope = Enum.TryParse<FormScope>(request.Scope, ignoreCase: true, out var scope) ? scope : FormScope.Organisation,
            SiteId = request.SiteId,
            PersonId = request.PersonId,
            Answers = answers.Values
                .Select(a => new FormAnswer(a.FieldId, a.Value, a.Values ?? new List<string>()))
                .ToList(),
            Status = FormSubmissionStatus.Submitted,
            SubmittedBy = name,
        };

        await _submissions.AddAsync(submission, cancellationToken);

        foreach (var file in request.Files ?? new List<FormSubmissionFileInput>())
        {
            if (string.IsNullOrWhiteSpace(file.ContentBase64))
            {
                continue;
            }

            await _submissions.AddFileAsync(new FormSubmissionFile
            {
                CompanyId = companyId,
                SubmissionId = submission.Id,
                FieldId = file.FieldId,
                FileName = file.FileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                Content = DecodeBase64(file.ContentBase64),
            }, cancellationToken);
        }

        return submission.Id;
    }

    /// <summary>Approves a submission (R15). Returns the updated submission, or null when not the caller's.</summary>
    public Task<FormSubmissionDetailDto?> ApproveAsync(Guid id, ReviewFormSubmissionRequest request, CancellationToken cancellationToken = default) =>
        ReviewAsync(id, FormSubmissionStatus.Approved, request.Note, requireNote: false, cancellationToken);

    /// <summary>Rejects a submission with a required written reason (R15). Returns the updated submission, or null when not the caller's.</summary>
    public Task<FormSubmissionDetailDto?> RejectAsync(Guid id, ReviewFormSubmissionRequest request, CancellationToken cancellationToken = default) =>
        ReviewAsync(id, FormSubmissionStatus.Rejected, request.Note, requireNote: true, cancellationToken);

    /// <summary>Returns a captured file (bytes + metadata) scoped to the caller (R15).</summary>
    public async Task<FormFileContent?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var (company, _) = await ResolveUserAsync(cancellationToken);
        var file = await _submissions.GetFileAsync(fileId, cancellationToken);
        if (file is null || (company is not null && file.CompanyId != company))
        {
            return null;
        }

        return new FormFileContent(file.FileName, file.ContentType, file.Content);
    }

    /// <summary>Applies a review status transition, recording the note (a rejection reason is mandatory).</summary>
    private async Task<FormSubmissionDetailDto?> ReviewAsync(Guid id, FormSubmissionStatus status, string? note, bool requireNote, CancellationToken cancellationToken)
    {
        var submission = await LoadOwnedAsync(id, cancellationToken);
        if (submission is null)
        {
            return null;
        }

        if (requireNote && string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("A reason is required to reject a submission.", nameof(note));
        }

        submission.Status = status;
        submission.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await _submissions.UpdateAsync(submission, cancellationToken);

        var files = await _submissions.GetFilesBySubmissionAsync(id, cancellationToken);
        return ToDetailDto(submission, files);
    }

    /// <summary>Loads a submission and enforces tenant ownership (R15): another company's record is treated as not found.</summary>
    private async Task<FormSubmission?> LoadOwnedAsync(Guid id, CancellationToken cancellationToken)
    {
        var (company, _) = await ResolveUserAsync(cancellationToken);
        var submission = await _submissions.GetByIdAsync(id, cancellationToken);
        if (submission is null || (company is not null && submission.CompanyId != company))
        {
            return null;
        }

        return submission;
    }

    /// <summary>Whether an answer carries a non-empty value.</summary>
    private static bool HasValue(FormAnswerDto a) =>
        !string.IsNullOrWhiteSpace(a.Value) || (a.Values is { Count: > 0 } && a.Values.Any(v => !string.IsNullOrWhiteSpace(v)));

    /// <summary>Decodes a base64 payload, tolerating a "data:...;base64," prefix.</summary>
    private static byte[] DecodeBase64(string value)
    {
        var comma = value.IndexOf(',');
        var payload = value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0 ? value[(comma + 1)..] : value;
        return Convert.FromBase64String(payload);
    }

    /// <summary>Maps a submission to the list summary DTO.</summary>
    private static FormSubmissionSummaryDto ToSummaryDto(FormSubmission s) => new(
        s.Id, s.FormTemplateId, s.FormName, s.FormTemplateVersion, s.Scope.ToString(), s.Status.ToString(), s.SubmittedUtc, s.SubmittedBy);

    /// <summary>Maps a submission and its files to the detail DTO.</summary>
    private static FormSubmissionDetailDto ToDetailDto(FormSubmission s, IReadOnlyList<FormSubmissionFile> files) => new(
        s.Id, s.FormTemplateId, s.FormName, s.FormTemplateVersion, s.Scope.ToString(), s.SiteId, s.PersonId,
        s.Status.ToString(), s.SubmittedUtc, s.SubmittedBy, s.ReviewNote,
        s.Answers.Select(a => new FormAnswerDto(a.FieldId, a.Value, a.Values)).ToList(),
        files.Select(f => new FormSubmissionFileDto(f.Id, f.FieldId, f.FileName, f.ContentType, f.UploadedUtc)).ToList());
}
