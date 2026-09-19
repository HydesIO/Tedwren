using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IIncidentReportRepository"/>.</summary>
public sealed class InMemoryIncidentReportRepository : IIncidentReportRepository
{
    private readonly ConcurrentDictionary<Guid, IncidentReport> _reports = new();

    /// <summary>Persists a new incident record.</summary>
    public Task AddAsync(IncidentReport report, CancellationToken cancellationToken = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }

    /// <summary>Returns a company's incident records, newest first.</summary>
    public Task<IReadOnlyList<IncidentReport>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IncidentReport> rows = _reports.Values
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.ReportedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a single incident record by id, or null if none exists.</summary>
    public Task<IncidentReport?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reports.TryGetValue(id, out var report) ? report : null);

    /// <summary>Updates an incident record's investigation/close-out state.</summary>
    public Task UpdateAsync(IncidentReport report, CancellationToken cancellationToken = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }
}
