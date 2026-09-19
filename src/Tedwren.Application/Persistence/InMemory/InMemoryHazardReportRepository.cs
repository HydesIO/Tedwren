using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IHazardReportRepository"/>.</summary>
public sealed class InMemoryHazardReportRepository : IHazardReportRepository
{
    private readonly ConcurrentDictionary<Guid, HazardReport> _reports = new();

    /// <summary>Persists a new hazard report.</summary>
    public Task AddAsync(HazardReport report, CancellationToken cancellationToken = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }

    /// <summary>Returns a company's hazard reports, newest first.</summary>
    public Task<IReadOnlyList<HazardReport>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HazardReport> rows = _reports.Values
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.ReportedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a single hazard report by id, or null if none exists.</summary>
    public Task<HazardReport?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reports.TryGetValue(id, out var report) ? report : null);

    /// <summary>Updates a hazard report's triage/close-out state.</summary>
    public Task UpdateAsync(HazardReport report, CancellationToken cancellationToken = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }
}
