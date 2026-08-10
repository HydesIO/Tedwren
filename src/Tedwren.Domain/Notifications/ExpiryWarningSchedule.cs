namespace Tedwren.Domain.Notifications;

/// <summary>
/// The expiry-warning schedule (SF-9). Pure and deterministic so it can be unit-tested in isolation:
/// given a card's expiry date and "today", it returns which warning stages are <b>due</b>. A stage is due
/// once its threshold has been reached (<c>daysUntilExpiry &lt;= offset</c>), which makes the schedule
/// catch-up-safe — a stage missed because a job did not run one day still fires on the next run. Combined
/// with a per-(card, stage) notification log, each stage is nonetheless delivered only once.
/// </summary>
public static class ExpiryWarningSchedule
{
    /// <summary>All stages, from earliest (60 days out) to latest (the day after expiry).</summary>
    public static IReadOnlyList<ExpiryWarningStage> Stages { get; } = new[]
    {
        ExpiryWarningStage.SixtyDays,
        ExpiryWarningStage.ThirtyDays,
        ExpiryWarningStage.SevenDays,
        ExpiryWarningStage.OnExpiry,
        ExpiryWarningStage.DayAfter,
    };

    /// <summary>
    /// Returns the stages that are due for a card expiring on <paramref name="expiresOn"/> as of
    /// <paramref name="asOf"/> — every stage whose threshold has been reached.
    /// </summary>
    public static IReadOnlyList<ExpiryWarningStage> DueStages(DateOnly expiresOn, DateOnly asOf)
    {
        var daysUntilExpiry = expiresOn.DayNumber - asOf.DayNumber;
        return Stages.Where(stage => daysUntilExpiry <= (int)stage).ToList();
    }
}
