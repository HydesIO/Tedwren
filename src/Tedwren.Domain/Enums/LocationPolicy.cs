namespace Tedwren.Domain.Enums;

/// <summary>
/// What happens when a worker's location is unavailable or refused at sign-in — a customer setting, not a
/// hard rule (SF-15, PRD Q5). Whichever the customer chooses is stated in the interface.
/// </summary>
public enum LocationPolicy
{
    /// <summary>Record the sign-in as location-unverified and flag it to the manager (the default).</summary>
    RecordAndFlag = 0,

    /// <summary>Refuse the sign-in when a location cannot be established.</summary>
    Refuse = 1,
}
