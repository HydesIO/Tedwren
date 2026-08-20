using System.Collections.Concurrent;
using Tedwren.Application.Auth;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock timesheet repository (API <c>DataSource=Mock</c>). Registered as a
/// singleton so timesheets persist across requests within the host. The weekly timesheet is the subcontractor
/// product's feature (SUB-8), so the demo week belongs to the subcontractor demo tenant (Apex Groundworks).
/// </summary>
public sealed class InMemoryTimesheetStore
{
    /// <summary>The demo tenant the seed timesheets belong to — the subcontractor Apex Groundworks (SUB-8).</summary>
    public static readonly Guid DemoCompanyId = AdminUserSeeder.SubcontractorSeedCompanyId;

    /// <summary>The demo site used by the seed lines — Apex Yard (shared with the site store seed).</summary>
    public static readonly Guid DemoSiteId = DemoSeed.ApexYardSiteId;

    /// <summary>The Monday of the demo timesheet week.</summary>
    public static readonly DateOnly DemoWeekStart = new(2026, 8, 3);

    /// <summary>Timesheet headers by id.</summary>
    public ConcurrentDictionary<Guid, Timesheet> Timesheets { get; } = new();

    /// <summary>Timesheet lines by id (append-only, R16).</summary>
    public ConcurrentDictionary<Guid, TimesheetEntry> Entries { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemoryTimesheetStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryTimesheetStore(bool seed)
    {
        if (!seed)
        {
            return;
        }

        // Apex operatives' weekly timesheets, one at each stage of the SUB-9 approval lifecycle.
        Seed("Samuel Okafor", TimesheetStatus.Submitted, new[] { 8m, 8m, 7.5m, 8m, 6m });
        Seed("Carlos Reyes", TimesheetStatus.Approved, new[] { 8m, 8m, 8m, 8m, 8m });
        Seed("Leon Whitaker", TimesheetStatus.Draft, new[] { 7m, 8m, 8m });
    }

    /// <summary>Adds a seed timesheet with one line per supplied day, starting on the demo Monday.</summary>
    private void Seed(string operative, TimesheetStatus status, IReadOnlyList<decimal> dailyHours)
    {
        var timesheet = new Timesheet
        {
            CompanyId = DemoCompanyId,
            PersonId = Guid.NewGuid(),
            OperativeName = operative,
            WeekStart = DemoWeekStart,
            Status = status,
            SubmittedUtc = status is TimesheetStatus.Submitted or TimesheetStatus.Approved
                ? new DateTimeOffset(DemoWeekStart.AddDays(5).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : null,
            DecidedBy = status is TimesheetStatus.Approved ? "Grace Bello" : null,
            DecidedUtc = status is TimesheetStatus.Approved
                ? new DateTimeOffset(DemoWeekStart.AddDays(6).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : null,
            DecisionScope = status is TimesheetStatus.Approved ? ApprovalScope.All : null,
        };
        Timesheets[timesheet.Id] = timesheet;

        for (var day = 0; day < dailyHours.Count; day++)
        {
            var entry = new TimesheetEntry
            {
                TimesheetId = timesheet.Id,
                WorkDate = DemoWeekStart.AddDays(day),
                SiteId = DemoSiteId,
                SiteName = "Apex Yard",
                Hours = dailyHours[day],
            };
            Entries[entry.Id] = entry;
        }
    }
}
