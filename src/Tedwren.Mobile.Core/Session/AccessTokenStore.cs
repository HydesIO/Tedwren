namespace Tedwren.Mobile.Core.Session;

/// <summary>
/// Holds the current operative access token in memory (never persisted — only the long-lived refresh token
/// lives in secure storage). Set from the session on enrol/resume and rotated by the auth message handler on a
/// silent refresh. Thread-safe so the handler and UI can touch it concurrently.
/// </summary>
public sealed class AccessTokenStore
{
    private readonly object _gate = new();
    private string? _token;

    /// <summary>The current bearer access token, or null when signed out / locked.</summary>
    public string? AccessToken
    {
        get { lock (_gate) { return _token; } }
        set { lock (_gate) { _token = value; } }
    }
}
