using Tedwren.Application.Timesheets;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the attendance-to-hours roll-up (SUB-7): paired sign-in/sign-out durations are summed per day and
/// site, refusals and unmatched sign-ins contribute nothing.
/// </summary>
public sealed class TimesheetHoursCalculatorTests
{
    private static readonly Guid Person = Guid.NewGuid();
    private static readonly Guid SiteA = Guid.NewGuid();
    private static readonly Guid SiteB = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 8, 3);

    private static AttendanceRecord Event(AttendanceEventType type, Guid siteId, int hour, AttendanceOutcome outcome = AttendanceOutcome.Accepted) =>
        new()
        {
            PersonId = Person,
            SiteId = siteId,
            Type = type,
            Outcome = outcome,
            OccurredUtc = new DateTimeOffset(Day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero),
        };

    [Fact]
    public void PairsSignInAndSignOut_IntoDailyHours()
    {
        var records = new[]
        {
            Event(AttendanceEventType.SignIn, SiteA, 8),
            Event(AttendanceEventType.SignOut, SiteA, 16),   // 8 hours
        };

        var line = Assert.Single(TimesheetHoursCalculator.Roll(records));
        Assert.Equal(Day, line.WorkDate);
        Assert.Equal(SiteA, line.SiteId);
        Assert.Equal(8m, line.Hours);
    }

    [Fact]
    public void SplitsAcrossSites()
    {
        var records = new[]
        {
            Event(AttendanceEventType.SignIn, SiteA, 7),
            Event(AttendanceEventType.SignOut, SiteA, 11),   // 4h at A
            Event(AttendanceEventType.SignIn, SiteB, 12),
            Event(AttendanceEventType.SignOut, SiteB, 16),   // 4h at B
        };

        var lines = TimesheetHoursCalculator.Roll(records);
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal(4m, l.Hours));
    }

    [Fact]
    public void RefusalsAndUnmatchedSignIns_ContributeNothing()
    {
        var records = new[]
        {
            Event(AttendanceEventType.SignIn, SiteA, 9, AttendanceOutcome.Refused),  // refusal — ignored
            Event(AttendanceEventType.SignIn, SiteB, 8),                              // no sign-out — no hours
        };

        Assert.Empty(TimesheetHoursCalculator.Roll(records));
    }
}
