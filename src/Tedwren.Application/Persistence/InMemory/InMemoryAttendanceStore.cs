using System.Collections.Concurrent;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock attendance repository (API <c>DataSource=Mock</c>). Registered as
/// a singleton so attendance persists across requests within the host. Seeded with a couple of open sign-ins
/// on the main contractor's site so the live muster / "who is on site now" view (MC-12/MC-14) has data.
/// </summary>
public sealed class InMemoryAttendanceStore
{
    /// <summary>Attendance records by id (append-only, R4).</summary>
    public ConcurrentDictionary<Guid, AttendanceRecord> Records { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemoryAttendanceStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryAttendanceStore(bool seed)
    {
        if (!seed)
        {
            return;
        }

        // Two Meridian operatives currently on site (open sign-ins, no sign-out) at Meridian Tower, a few hours
        // ago so they don't read as overnight (SF-19). This is what the main contractor's muster shows.
        var earlier = DateTimeOffset.UtcNow.AddHours(-3);
        SeedOpenSignIn(DemoSeed.FletcherPersonId, earlier);
        SeedOpenSignIn(DemoSeed.MarshPersonId, earlier.AddMinutes(-20));
    }

    /// <summary>Adds an accepted, in-boundary open sign-in at Meridian Tower for the muster demo (MC-12).</summary>
    private void SeedOpenSignIn(Guid personId, DateTimeOffset occurredUtc)
    {
        var record = new AttendanceRecord
        {
            PersonId = personId,
            SiteId = DemoSeed.MeridianTowerSiteId,
            Type = AttendanceEventType.SignIn,
            Outcome = AttendanceOutcome.Accepted,
            Method = SignInMethod.QrScan,
            Latitude = 51.5074,
            Longitude = -0.1278,
            WithinBoundary = true,
            OccurredUtc = occurredUtc,
        };
        Records[record.Id] = record;
    }
}
