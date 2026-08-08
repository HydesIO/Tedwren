using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Timesheets;

/// <summary>
/// Rolls a person's append-only attendance log up into per-day, per-site worked hours (SUB-7). Sign-ins that
/// count for presence are paired with the next counting sign-out at the same site; refusals and unmatched
/// sign-ins contribute nothing. Kept pure (no I/O) so the roll-up is deterministic and unit-testable. Days are
/// bucketed by the sign-in instant; UK-local display is applied at the edge (R11).
/// </summary>
public static class TimesheetHoursCalculator
{
    /// <summary>One rolled-up bucket of hours for a day at a site.</summary>
    public sealed record RolledUpLine(DateOnly WorkDate, Guid SiteId, decimal Hours);

    /// <summary>Pairs sign-ins with sign-outs per site and sums the durations per (day, site).</summary>
    public static IReadOnlyList<RolledUpLine> Roll(IEnumerable<AttendanceRecord> records)
    {
        var totals = new Dictionary<(DateOnly Date, Guid SiteId), decimal>();

        foreach (var bySite in records
                     .Where(r => r.CountsForPresence)
                     .GroupBy(r => r.SiteId))
        {
            AttendanceRecord? openSignIn = null;
            foreach (var record in bySite.OrderBy(r => r.OccurredUtc))
            {
                switch (record.Type)
                {
                    case AttendanceEventType.SignIn:
                        openSignIn = record;
                        break;
                    case AttendanceEventType.SignOut when openSignIn is not null:
                        var hours = (decimal)(record.OccurredUtc - openSignIn.OccurredUtc).TotalHours;
                        if (hours > 0)
                        {
                            var key = (DateOnly.FromDateTime(openSignIn.OccurredUtc.UtcDateTime), bySite.Key);
                            totals[key] = totals.TryGetValue(key, out var existing) ? existing + hours : hours;
                        }

                        openSignIn = null;
                        break;
                }
            }
        }

        return totals
            .Select(kvp => new RolledUpLine(kvp.Key.Date, kvp.Key.SiteId, Math.Round(kvp.Value, 2)))
            .OrderBy(l => l.WorkDate)
            .ToList();
    }
}
