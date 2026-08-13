namespace Tedwren.Abstractions.Contracts.Forms;

/// <summary>A single captured answer, keyed by field id. <c>Value</c> for single-valued answers, <c>Values</c> for multi-select.</summary>
public sealed record FormAnswerDto(string FieldId, string? Value, IReadOnlyList<string> Values);

/// <summary>A file captured against a submission, sent from the client as base64 content.</summary>
public sealed record FormSubmissionFileInput(string FieldId, string FileName, string ContentType, string ContentBase64);

/// <summary>Metadata for a file captured against a submission (no bytes).</summary>
public sealed record FormSubmissionFileDto(Guid Id, string FieldId, string FileName, string ContentType, DateTimeOffset UploadedUtc);

/// <summary>Request to submit a completed form (requirement 7: at organisation or site level). <c>Scope</c> is the <c>FormScope</c> name.</summary>
public sealed record CreateFormSubmissionRequest(
    Guid FormTemplateId,
    string Scope,
    Guid? SiteId,
    Guid? PersonId,
    IReadOnlyList<FormAnswerDto> Answers,
    IReadOnlyList<FormSubmissionFileInput> Files);

/// <summary>A submission row for the submissions list.</summary>
public sealed record FormSubmissionSummaryDto(
    Guid Id,
    Guid FormTemplateId,
    string FormName,
    int FormTemplateVersion,
    string Scope,
    string Status,
    DateTimeOffset SubmittedUtc,
    string SubmittedBy);

/// <summary>The full submission — answers and captured-file metadata — for viewing / PDF / email.</summary>
public sealed record FormSubmissionDetailDto(
    Guid Id,
    Guid FormTemplateId,
    string FormName,
    int FormTemplateVersion,
    string Scope,
    Guid? SiteId,
    Guid? PersonId,
    string Status,
    DateTimeOffset SubmittedUtc,
    string SubmittedBy,
    string? ReviewNote,
    IReadOnlyList<FormAnswerDto> Answers,
    IReadOnlyList<FormSubmissionFileDto> Files);

/// <summary>Request to review a submission — an optional note (a rejection should carry a reason).</summary>
public sealed record ReviewFormSubmissionRequest(string? Note);
