namespace Tedwren.Domain.Enums;

/// <summary>The kind of recorded accident / incident (PRD §8.2 — the structured incident record with RIDDOR).</summary>
public enum IncidentKind
{
    /// <summary>An accident that caused injury or harm.</summary>
    Accident = 0,

    /// <summary>An incident (property/process) without personal injury.</summary>
    Incident = 1,

    /// <summary>A dangerous occurrence (a RIDDOR-defined category even without injury).</summary>
    DangerousOccurrence = 2,
}
