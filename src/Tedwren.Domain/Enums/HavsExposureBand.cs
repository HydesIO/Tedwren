namespace Tedwren.Domain.Enums;

/// <summary>
/// A daily hand-arm vibration exposure band against the Control of Vibration at Work Regulations 2005 (PRD §8.2).
/// The thresholds are the HSE's daily A(8) values: the Exposure Action Value (EAV) is 2.5 m/s² A(8) and the
/// Exposure Limit Value (ELV) is 5.0 m/s² A(8).
/// </summary>
public enum HavsExposureBand
{
    /// <summary>Below the Exposure Action Value (A(8) &lt; 2.5 m/s²) — within routine control.</summary>
    BelowActionValue = 0,

    /// <summary>At or above the EAV but below the ELV (2.5 ≤ A(8) &lt; 5.0) — action required to reduce exposure.</summary>
    AboveActionValue = 1,

    /// <summary>At or above the Exposure Limit Value (A(8) ≥ 5.0 m/s²) — the legal limit is exceeded.</summary>
    AboveLimitValue = 2,
}
