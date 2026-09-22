using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="RamsSubmission"/> (RAMS review, PRD §8.2). Reads are scoped by company (R15).</summary>
public interface IRamsRepository
{
    /// <summary>Persists a new RAMS submission (append-only — never modifies an earlier version).</summary>
    Task AddAsync(RamsSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's RAMS submissions, newest first.</summary>
    Task<IReadOnlyList<RamsSubmission>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns every version in a family for the company, newest version first (live-version management, spec Stage 3).</summary>
    Task<IReadOnlyList<RamsSubmission>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the current live (approved / approved-with-comments) version of a family, or null when none is live (Gate 5).</summary>
    Task<RamsSubmission?> GetLiveForFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single submission by id, or null if none exists.</summary>
    Task<RamsSubmission?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates a submission's review state (status, note, reviewer).</summary>
    Task UpdateAsync(RamsSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>Returns the highest version number in a family for the company (0 when the family is unknown).</summary>
    Task<int> GetMaxVersionAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default);
}
