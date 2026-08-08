namespace Tedwren.Abstractions.Common;

/// <summary>
/// A provider-neutral risk level carried on site DTOs, so the shared contracts do not depend on any UI
/// enum. The client maps it to its risk-chip severity.
/// </summary>
public enum RiskState
{
    /// <summary>Low risk.</summary>
    Low = 0,

    /// <summary>Medium risk.</summary>
    Medium = 1,

    /// <summary>High risk.</summary>
    High = 2,
}
