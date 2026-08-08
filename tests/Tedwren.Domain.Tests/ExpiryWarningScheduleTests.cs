using Tedwren.Domain.Notifications;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the SF-9 warning schedule: stages become due as their thresholds are reached (60/30/7/0/−1
/// days), catch-up-safe so a missed day still fires. A per-(card, stage) log keeps each to one delivery.
/// </summary>
public sealed class ExpiryWarningScheduleTests
{
    private static readonly DateOnly Today = new(2026, 8, 8);

    [Fact]
    public void FarFromExpiry_NoStagesDue()
    {
        var due = ExpiryWarningSchedule.DueStages(Today.AddDays(120), Today);
        Assert.Empty(due);
    }

    [Fact]
    public void SixtyDaysOut_OnlySixtyDayStage()
    {
        var due = ExpiryWarningSchedule.DueStages(Today.AddDays(60), Today);
        Assert.Equal(new[] { ExpiryWarningStage.SixtyDays }, due);
    }

    [Fact]
    public void ThirtyDaysOut_SixtyAndThirty_CatchUp()
    {
        var due = ExpiryWarningSchedule.DueStages(Today.AddDays(30), Today);
        Assert.Equal(new[] { ExpiryWarningStage.SixtyDays, ExpiryWarningStage.ThirtyDays }, due);
    }

    [Fact]
    public void OnExpiryDay_AllButDayAfter()
    {
        var due = ExpiryWarningSchedule.DueStages(Today, Today);
        Assert.Equal(
            new[] { ExpiryWarningStage.SixtyDays, ExpiryWarningStage.ThirtyDays, ExpiryWarningStage.SevenDays, ExpiryWarningStage.OnExpiry },
            due);
    }

    [Fact]
    public void DayAfterExpiry_AllStages()
    {
        var due = ExpiryWarningSchedule.DueStages(Today.AddDays(-1), Today);
        Assert.Equal(5, due.Count);
        Assert.Contains(ExpiryWarningStage.DayAfter, due);
    }
}
