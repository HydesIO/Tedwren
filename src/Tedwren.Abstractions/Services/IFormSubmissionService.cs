using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The Forms Library submission service (PRD-Phase 2 checklist/inspection engine). Captures completed forms as
/// append-only evidence (R4/R10), validates that required fields were answered (required-by-default), stores any
/// captured files, and drives the review flow. Tenant-scoped — a caller only ever sees its own company's
/// submissions (R15).
/// </summary>
public interface IFormSubmissionService
{
    /// <summary>Lists the caller's submissions, newest first.</summary>
    Task<IReadOnlyList<FormSubmissionSummaryDto>> GetSubmissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a submission with its answers and file metadata, or null when not found / not the caller's.</summary>
    Task<FormSubmissionDetailDto?> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Submits a completed form and returns its id. Throws <see cref="System.ArgumentException"/> when required fields are unanswered or the template is not published.</summary>
    Task<Guid> SubmitAsync(CreateFormSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Approves a submission. Returns the updated submission, or null when not found / not the caller's.</summary>
    Task<FormSubmissionDetailDto?> ApproveAsync(Guid id, ReviewFormSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Rejects a submission (a written reason is required). Returns the updated submission, or null when not found / not the caller's.</summary>
    Task<FormSubmissionDetailDto?> RejectAsync(Guid id, ReviewFormSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a captured file (name, type, bytes), or null when not found / not the caller's.</summary>
    Task<FormFileContent?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default);
}

/// <summary>A captured file's bytes and metadata, for download/streaming.</summary>
public sealed record FormFileContent(string FileName, string ContentType, byte[] Content);
