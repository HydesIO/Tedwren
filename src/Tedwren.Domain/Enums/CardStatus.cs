namespace Tedwren.Domain.Enums;

/// <summary>
/// A qualification card's currency, always <b>calculated from the expiry date, never typed in</b> (SF-8).
/// Visible to the manager and to the worker.
/// </summary>
public enum CardStatus
{
    /// <summary>In date (or holds no expiry).</summary>
    Valid = 0,

    /// <summary>In date but within the expiry-warning window.</summary>
    ExpiringSoon = 1,

    /// <summary>Past its expiry date.</summary>
    Expired = 2,
}
