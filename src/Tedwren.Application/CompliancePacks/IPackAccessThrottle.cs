namespace Tedwren.Application.CompliancePacks;

/// <summary>
/// Rate-limits recipient passcode attempts per pack token, so a leaked link cannot have its short passcode
/// brute-forced online (a hardening action from the pack-link security review). Keyed by token; a run of
/// failures locks that token out for a cool-off window.
/// </summary>
public interface IPackAccessThrottle
{
    /// <summary>Whether the token is currently locked out after too many failed passcode attempts.</summary>
    bool IsLockedOut(string token);

    /// <summary>Records a failed passcode attempt for the token.</summary>
    void RegisterFailure(string token);

    /// <summary>Clears the token's failure count after a successful access.</summary>
    void Reset(string token);
}
