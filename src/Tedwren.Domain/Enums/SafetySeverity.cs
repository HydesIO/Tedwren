namespace Tedwren.Domain.Enums;

/// <summary>A shared severity rating for safety observations and incidents (PRD §8.2).</summary>
public enum SafetySeverity
{
    /// <summary>Low severity / potential.</summary>
    Low = 0,

    /// <summary>Medium severity / potential.</summary>
    Medium = 1,

    /// <summary>High severity / potential.</summary>
    High = 2,
}
