namespace Tedwren.Domain.Enums;

/// <summary>The lifecycle state of an accident / incident record (PRD §8.2): report → investigate → close.</summary>
public enum IncidentStatus
{
    /// <summary>Reported and awaiting investigation.</summary>
    Reported = 0,

    /// <summary>Under investigation (causes/actions being recorded).</summary>
    UnderInvestigation = 1,

    /// <summary>Investigation closed out.</summary>
    Closed = 2,
}
