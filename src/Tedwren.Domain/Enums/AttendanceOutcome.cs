namespace Tedwren.Domain.Enums;

/// <summary>
/// The outcome of an attendance attempt. Every attempt is recorded, including refusals (SF-16). Only
/// <see cref="Accepted"/> and <see cref="Flagged"/> events change whether a worker is present.
/// </summary>
public enum AttendanceOutcome
{
    /// <summary>Verified inside the boundary with a location (SF-14).</summary>
    Accepted = 0,

    /// <summary>Recorded but flagged — location was unavailable and the customer setting records-and-flags (SF-15).</summary>
    Flagged = 1,

    /// <summary>Refused — outside the boundary, location required by policy, or already signed in elsewhere (SF-18).</summary>
    Refused = 2,
}
