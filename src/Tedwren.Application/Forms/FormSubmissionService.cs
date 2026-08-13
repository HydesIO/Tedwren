using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Export;
using Tedwren.Application.Notifications.Email;
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
    private readonly IEmailSender? _email;
    private readonly ISiteRepository? _sites;
    private readonly IFormAssignmentRepository? _assignments;

    /// <summary>Field kinds that capture no value, so they are never required-validated.</summary>
    private static readonly HashSet<FormFieldKind> DisplayOnly = new() { FormFieldKind.Heading, FormFieldKind.Instruction };

    /// <summary>Field kinds whose answer is a captured file rather than an inline value.</summary>
    private static readonly HashSet<FormFieldKind> FileKinds = new() { FormFieldKind.Photo, FormFieldKind.FileUpload };

    /// <summary>
    /// Creates the service over its repositories and the optional current-user (tenant + submitter). The email
    /// sender and site repository are optional so unit tests that only exercise submission rules can omit them;
    /// they are supplied by the composition root for the PDF/email features.
    /// </summary>
    public FormSubmissionService(
        IFormSubmissionRepository submissions,
        IFormTemplateRepository templates,
        ICurrentUserService? currentUser = null,
        IEmailSender? email = null,
        ISiteRepository? sites = null,
        IFormAssignmentRepository? assignments = null)
    {
        _submissions = submissions;
        _templates = templates;
        _currentUser = currentUser;
        _email = email;
        _sites = sites;
        _assignments = assignments;
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
    public Task<Guid> SubmitAsync(CreateFormSubmissionRequest request, CancellationToken cancellationToken = default) =>
        SubmitCoreAsync(ResolveUserAsync, personIdOverride: null, request, cancellationToken);

    /// <summary>
    /// Submits a completed form on behalf of an anonymous take-flow (the induction-embedded form, requirement 5,
    /// R5) where the company, the person and the submitter's name are supplied by the caller rather than resolved
    /// from a signed-in user. The template must still belong to <paramref name="companyId"/> (R15).
    /// </summary>
    public Task<Guid> SubmitForContextAsync(Guid companyId, Guid? personId, string submittedBy, CreateFormSubmissionRequest request, CancellationToken cancellationToken = default) =>
        SubmitCoreAsync(
            _ => Task.FromResult<(Guid?, string)>((companyId, string.IsNullOrWhiteSpace(submittedBy) ? "Operative" : submittedBy.Trim())),
            personIdOverride: personId, request, cancellationToken);

    /// <summary>Shared submission pipeline: resolves the owning company + submitter, validates required fields, then stores the submission, its files and any failure alert.</summary>
    private async Task<Guid> SubmitCoreAsync(
        Func<CancellationToken, Task<(Guid? Company, string Name)>> resolve,
        Guid? personIdOverride,
        CreateFormSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var (company, name) = await resolve(cancellationToken);

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
            PersonId = personIdOverride ?? request.PersonId,
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

        await SendFailureAlertsIfNeededAsync(submission, template, answers, cancellationToken);
        return submission.Id;
    }

    /// <summary>
    /// If the submission contains a failed check — any red/amber/green field answered "Red" — emails the report
    /// (with the branded PDF attached) to every assignment of this form family that carries a failed-check alert
    /// address (PRD §8 "a failed check auto-alerts by email with the failed report attached"). Best-effort: a
    /// missing email sender or assignment repository simply skips the alert.
    /// </summary>
    private async Task SendFailureAlertsIfNeededAsync(FormSubmission submission, FormTemplate template,
        IReadOnlyDictionary<string, FormAnswerDto> answers, CancellationToken cancellationToken)
    {
        if (_assignments is null || _email is null)
        {
            return;
        }

        var ragFieldIds = template.Sections
            .SelectMany(s => s.Fields)
            .Where(f => f.Kind == FormFieldKind.RagStatus)
            .Select(f => f.Id)
            .ToHashSet(StringComparer.Ordinal);

        var failed = ragFieldIds.Any(id =>
            answers.TryGetValue(id, out var a) && string.Equals(a.Value, "Red", StringComparison.OrdinalIgnoreCase));
        if (!failed)
        {
            return;
        }

        var assignments = await _assignments.GetByFamilyAsync(submission.CompanyId, template.FamilyId, cancellationToken);
        var recipients = assignments
            .Where(a => !string.IsNullOrWhiteSpace(a.FailureAlertEmail))
            .Select(a => a.FailureAlertEmail!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        var pdf = await GeneratePdfAsync(submission.Id, cancellationToken);
        if (pdf is null)
        {
            return;
        }

        var content = FormSubmissionEmail.BuildFailureContent(
            submission.FormName, submission.SubmittedBy, submission.SubmittedUtc, submission.Scope.ToString());
        var attachment = new[] { new EmailAttachment(pdf.FileName, pdf.ContentType, pdf.Content) };
        foreach (var recipient in recipients)
        {
            await _email.SendHtmlWithAttachmentsAsync(recipient, FormSubmissionEmail.FailureSubjectFor(submission.FormName), content, attachment, cancellationToken);
        }
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

    /// <summary>Renders a submission to a branded PDF (requirement 4), scoped to the caller (R15).</summary>
    public async Task<FormFileContent?> GeneratePdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await BuildPdfModelAsync(id, cancellationToken);
        if (model is null)
        {
            return null;
        }

        var bytes = FormPdfRenderer.Render(model);
        var fileName = $"{Slugify(model.FormName)}-{model.SubmittedUtc:yyyyMMdd}.pdf";
        return new FormFileContent(fileName, "application/pdf", bytes);
    }

    /// <summary>Emails a submission's PDF to a recipient (requirement 6). False when the submission is not the caller's / not found.</summary>
    public async Task<bool> EmailAsync(Guid id, string recipientEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("A recipient email address is required.", nameof(recipientEmail));
        }

        if (_email is null)
        {
            throw new InvalidOperationException("Email delivery is not configured.");
        }

        var pdf = await GeneratePdfAsync(id, cancellationToken);
        var submission = await LoadOwnedAsync(id, cancellationToken);
        if (pdf is null || submission is null)
        {
            return false;
        }

        var content = FormSubmissionEmail.BuildContent(
            submission.FormName, submission.SubmittedBy, submission.SubmittedUtc, submission.Status.ToString(), submission.Scope.ToString());
        await _email.SendHtmlWithAttachmentsAsync(
            recipientEmail.Trim(),
            FormSubmissionEmail.SubjectFor(submission.FormName),
            content,
            new[] { new EmailAttachment(pdf.FileName, pdf.ContentType, pdf.Content) },
            cancellationToken);
        return true;
    }

    /// <summary>Assembles the PDF model from the submission, its template version (for labels) and its files.</summary>
    private async Task<FormPdfModel?> BuildPdfModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var submission = await LoadOwnedAsync(id, cancellationToken);
        if (submission is null)
        {
            return null;
        }

        var template = await _templates.GetByIdAsync(submission.FormTemplateId, cancellationToken);
        var answers = submission.Answers.ToDictionary(a => a.FieldId, a => a, StringComparer.Ordinal);

        // File bytes grouped by the field they answer (photos/attachments).
        var fileMeta = await _submissions.GetFilesBySubmissionAsync(id, cancellationToken);
        var filesByField = new Dictionary<string, List<FormSubmissionFile>>(StringComparer.Ordinal);
        foreach (var meta in fileMeta)
        {
            var full = await _submissions.GetFileAsync(meta.Id, cancellationToken);
            if (full is null) continue;
            (filesByField.TryGetValue(meta.FieldId, out var list) ? list : filesByField[meta.FieldId] = new()).Add(full);
        }

        var sections = new List<FormPdfSection>();
        if (template is not null)
        {
            foreach (var sec in template.Sections.OrderBy(s => s.Order))
            {
                var items = new List<FormPdfItem>();
                foreach (var field in sec.Fields.OrderBy(f => f.Order))
                {
                    if (field.Kind is FormFieldKind.Heading or FormFieldKind.Instruction)
                    {
                        continue;
                    }

                    if (field.Kind is FormFieldKind.Photo or FormFieldKind.FileUpload)
                    {
                        if (filesByField.TryGetValue(field.Id, out var files))
                        {
                            foreach (var file in files)
                            {
                                items.Add(file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                                    ? new FormPdfItem(field.Label, null, file.Content)
                                    : new FormPdfItem(field.Label, file.FileName, null));
                            }
                        }
                        else
                        {
                            items.Add(new FormPdfItem(field.Label, "—", null));
                        }

                        continue;
                    }

                    items.Add(new FormPdfItem(field.Label, RenderValue(field.Kind, answers.GetValueOrDefault(field.Id)), SignatureImage(field.Kind, answers.GetValueOrDefault(field.Id))));
                }

                sections.Add(new FormPdfSection(sec.Title, items));
            }
        }

        var siteName = submission.SiteId is { } siteId && _sites is not null
            ? (await _sites.GetByIdAsync(siteId, cancellationToken))?.Name
            : null;

        return new FormPdfModel(
            submission.FormName, submission.FormTemplateVersion, submission.Scope.ToString(), siteName,
            submission.SubmittedBy, submission.SubmittedUtc, submission.Status.ToString(), submission.ReviewNote, sections);
    }

    /// <summary>Formats an answer for display in the PDF (Yes/No for switches, joined multi-select, blank for a signature image).</summary>
    private static string RenderValue(FormFieldKind kind, FormAnswer? answer)
    {
        if (answer is null)
        {
            return "—";
        }

        if (kind == FormFieldKind.Signature)
        {
            return answer.Value is { Length: > 0 } ? string.Empty : "—";
        }

        if (answer.Values.Count > 0)
        {
            return string.Join(", ", answer.Values);
        }

        if (string.IsNullOrWhiteSpace(answer.Value))
        {
            return "—";
        }

        if (kind == FormFieldKind.YesNo)
        {
            return string.Equals(answer.Value, "true", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No";
        }

        return answer.Value;
    }

    /// <summary>Decodes a signature's data-URL answer to image bytes for embedding, or null.</summary>
    private static byte[]? SignatureImage(FormFieldKind kind, FormAnswer? answer)
    {
        if (kind != FormFieldKind.Signature || answer?.Value is not { Length: > 0 } value || !value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var comma = value.IndexOf(',');
            return comma >= 0 ? Convert.FromBase64String(value[(comma + 1)..]) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>Makes a filesystem-friendly slug from the form name for the PDF file name.</summary>
    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrEmpty(slug) ? "form" : slug;
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
