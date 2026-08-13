using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="FormSubmission"/> and its files (the PRD-Phase 2 Forms Library). Tenant-agnostic — the service scopes by company (R15).</summary>
public interface IFormSubmissionRepository
{
    /// <summary>Returns a company's submissions, newest first.</summary>
    Task<IReadOnlyList<FormSubmission>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single submission by id, or null.</summary>
    Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new submission.</summary>
    Task AddAsync(FormSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>Persists a review-state change (approve/reject with a note).</summary>
    Task UpdateAsync(FormSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>Persists a captured file for a submission.</summary>
    Task AddFileAsync(FormSubmissionFile file, CancellationToken cancellationToken = default);

    /// <summary>Returns the file metadata (no bytes) captured for a submission.</summary>
    Task<IReadOnlyList<FormSubmissionFile>> GetFilesBySubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single file including its bytes, or null.</summary>
    Task<FormSubmissionFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default);
}
