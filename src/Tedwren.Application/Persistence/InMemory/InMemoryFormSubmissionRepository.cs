using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IFormSubmissionRepository"/> over the shared store (test-only mock mode).</summary>
public sealed class InMemoryFormSubmissionRepository : IFormSubmissionRepository
{
    private readonly InMemoryFormStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryFormSubmissionRepository(InMemoryFormStore store) => _store = store;

    /// <summary>Returns a company's submissions, newest first.</summary>
    public Task<IReadOnlyList<FormSubmission>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FormSubmission> submissions = _store.Submissions.Values
            .Where(s => s.CompanyId == companyId)
            .OrderByDescending(s => s.SubmittedUtc)
            .ToList();
        return Task.FromResult(submissions);
    }

    /// <summary>Returns a submission by id, or null.</summary>
    public Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Submissions.GetValueOrDefault(id));

    /// <summary>Adds a submission to the store.</summary>
    public Task AddAsync(FormSubmission submission, CancellationToken cancellationToken = default)
    {
        _store.Submissions[submission.Id] = submission;
        return Task.CompletedTask;
    }

    /// <summary>Updates a submission in the store (review status).</summary>
    public Task UpdateAsync(FormSubmission submission, CancellationToken cancellationToken = default)
    {
        _store.Submissions[submission.Id] = submission;
        return Task.CompletedTask;
    }

    /// <summary>Adds a captured file to the store.</summary>
    public Task AddFileAsync(FormSubmissionFile file, CancellationToken cancellationToken = default)
    {
        _store.Files[file.Id] = file;
        return Task.CompletedTask;
    }

    /// <summary>Returns file metadata (no bytes) for a submission.</summary>
    public Task<IReadOnlyList<FormSubmissionFile>> GetFilesBySubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FormSubmissionFile> files = _store.Files.Values
            .Where(f => f.SubmissionId == submissionId)
            .OrderBy(f => f.UploadedUtc)
            .Select(f => new FormSubmissionFile
            {
                Id = f.Id,
                CompanyId = f.CompanyId,
                SubmissionId = f.SubmissionId,
                FieldId = f.FieldId,
                FileName = f.FileName,
                ContentType = f.ContentType,
                Content = Array.Empty<byte>(),
                UploadedUtc = f.UploadedUtc,
            })
            .ToList();
        return Task.FromResult(files);
    }

    /// <summary>Returns a single file including its bytes, or null.</summary>
    public Task<FormSubmissionFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Files.GetValueOrDefault(fileId));
}
