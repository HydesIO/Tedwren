namespace Tedwren.Domain.Enums;

/// <summary>The kind of safety observation a worker reports from site (PRD §8.2 — near-miss / hazard reporting).</summary>
public enum HazardKind
{
    /// <summary>A near-miss: an event that could have caused harm but did not.</summary>
    NearMiss = 0,

    /// <summary>A hazard: a condition with the potential to cause harm.</summary>
    Hazard = 1,

    /// <summary>An unsafe act: a behaviour that could cause harm.</summary>
    UnsafeAct = 2,

    /// <summary>An unsafe condition: an environmental/plant condition that could cause harm.</summary>
    UnsafeCondition = 3,
}
