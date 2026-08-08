using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ITimesheetRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryTimesheetRepository : ITimesheetRepository
{
    private readonly InMemoryTimesheetStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryTimesheetRepository(InMemoryTimesheetStore store) => _store = store;

    /// <summary>Returns a timesheet header by id, or null.</summary>
    public Task<Timesheet?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Timesheets.GetValueOrDefault(id));

    /// <summary>Returns an operative's timesheet header for a company and week, or null.</summary>
    public Task<Timesheet?> GetByWeekAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Timesheets.Values.FirstOrDefault(t =>
            t.CompanyId == companyId && t.PersonId == personId && t.WeekStart == weekStart));

    /// <summary>Returns a company's timesheet headers for a week (MC-24).</summary>
    public Task<IReadOnlyList<Timesheet>> GetByCompanyWeekAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Timesheet> results = _store.Timesheets.Values
            .Where(t => t.CompanyId == companyId && t.WeekStart == weekStart)
            .OrderBy(t => t.OperativeName)
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Returns all lines of a timesheet, oldest first (retained superseded lines included, R16).</summary>
    public Task<IReadOnlyList<TimesheetEntry>> GetEntriesAsync(Guid timesheetId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TimesheetEntry> results = _store.Entries.Values
            .Where(e => e.TimesheetId == timesheetId)
            .OrderBy(e => e.CreatedUtc)
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Persists a new timesheet with its initial rolled-up lines.</summary>
    public Task AddAsync(Timesheet timesheet, IReadOnlyList<TimesheetEntry> entries, CancellationToken cancellationToken = default)
    {
        _store.Timesheets[timesheet.Id] = timesheet;
        foreach (var entry in entries)
        {
            _store.Entries[entry.Id] = entry;
        }

        return Task.CompletedTask;
    }

    /// <summary>Appends a correction line (R16).</summary>
    public Task AddEntryAsync(TimesheetEntry entry, CancellationToken cancellationToken = default)
    {
        _store.Entries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    /// <summary>Persists the header's workflow changes (the header instance is shared, so this is a no-op guard).</summary>
    public Task UpdateAsync(Timesheet timesheet, CancellationToken cancellationToken = default)
    {
        _store.Timesheets[timesheet.Id] = timesheet;
        return Task.CompletedTask;
    }
}
