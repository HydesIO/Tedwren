namespace Tedwren.Domain.Notifications;

/// <summary>
/// A point in the expiry-warning schedule (SF-9): warnings go out at 60, 30 and 7 days before expiry,
/// on the day, and the day after. The enum value <b>is</b> the day offset relative to the expiry date, so
/// <c>(int)stage</c> gives the threshold directly.
/// </summary>
public enum ExpiryWarningStage
{
    /// <summary>The day after expiry.</summary>
    DayAfter = -1,

    /// <summary>On the expiry date.</summary>
    OnExpiry = 0,

    /// <summary>Seven days before expiry.</summary>
    SevenDays = 7,

    /// <summary>Thirty days before expiry.</summary>
    ThirtyDays = 30,

    /// <summary>Sixty days before expiry.</summary>
    SixtyDays = 60,
}
