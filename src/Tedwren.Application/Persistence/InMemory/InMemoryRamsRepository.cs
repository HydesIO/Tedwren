using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IRamsRepository"/>.</summary>
public sealed class InMemoryRamsRepository : IRamsRepository
{
    private readonly ConcurrentDictionary<Guid, RamsSubmission> _rams = new();

    /// <summary>Persists a new RAMS submission.</summary>
    public Task AddAsync(RamsSubmission submission, CancellationToken cancellationToken = default)
    {
        _rams[submission.Id] = submission;
        return Task.CompletedTask;
    }

    /// <summary>Returns a company's RAMS submissions, newest first.</summary>
    public Task<IReadOnlyList<RamsSubmission>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RamsSubmission> rows = _rams.Values
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.SubmittedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a single submission by id, or null if none exists.</summary>
    public Task<RamsSubmission?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rams.TryGetValue(id, out var submission) ? submission : null);

    /// <summary>Updates a submission's review state.</summary>
    public Task UpdateAsync(RamsSubmission submission, CancellationToken cancellationToken = default)
    {
        _rams[submission.Id] = submission;
        return Task.CompletedTask;
    }

    /// <summary>Returns the highest version number in a family for the company (0 when unknown).</summary>
    public Task<int> GetMaxVersionAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var max = _rams.Values
            .Where(r => r.CompanyId == companyId && r.FamilyId == familyId)
            .Select(r => (int?)r.Version)
            .Max() ?? 0;
        return Task.FromResult(max);
    }
}
