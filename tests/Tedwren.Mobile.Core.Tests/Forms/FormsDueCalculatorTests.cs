using Tedwren.Mobile.Core.Forms;

namespace Tedwren.Mobile.Core.Tests.Forms;

/// <summary>Verifies the schedule-aware "forms due" calculation (M6) for each cadence, weekday-independently.</summary>
public class FormsDueCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AdHoc_is_due_until_completed()
    {
        Assert.Equal(FormDueState.Due, FormsDueCalculator.Evaluate("AdHoc", null, Now));
        Assert.Equal(FormDueState.NotDue, FormsDueCalculator.Evaluate("AdHoc", Now.AddHours(-1), Now));
    }

    [Fact]
    public void Daily_tracks_the_current_day()
    {
        Assert.Equal(FormDueState.NotDue, FormsDueCalculator.Evaluate("Daily", Now.AddHours(-2), Now));  // earlier today
        Assert.Equal(FormDueState.Due, FormsDueCalculator.Evaluate("Daily", Now.AddDays(-1), Now));      // yesterday
        Assert.Equal(FormDueState.Overdue, FormsDueCalculator.Evaluate("Daily", null, Now));             // never
        Assert.Equal(FormDueState.Overdue, FormsDueCalculator.Evaluate("Daily", Now.AddDays(-3), Now));  // missed prior days
    }

    [Fact]
    public void Weekly_tracks_the_current_week()
    {
        Assert.Equal(FormDueState.NotDue, FormsDueCalculator.Evaluate("Weekly", Now, Now));               // this week
        Assert.Equal(FormDueState.Overdue, FormsDueCalculator.Evaluate("Weekly", null, Now));             // never
        Assert.Equal(FormDueState.Overdue, FormsDueCalculator.Evaluate("Weekly", Now.AddDays(-14), Now)); // two weeks ago
    }

    [Fact]
    public void Monthly_tracks_the_current_month()
    {
        Assert.Equal(FormDueState.NotDue, FormsDueCalculator.Evaluate("Monthly", new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero), Now));
        Assert.Equal(FormDueState.Due, FormsDueCalculator.Evaluate("Monthly", new DateTimeOffset(2026, 5, 20, 9, 0, 0, TimeSpan.Zero), Now)); // last month
        Assert.Equal(FormDueState.Overdue, FormsDueCalculator.Evaluate("Monthly", new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.Zero), Now)); // months ago
    }
}
