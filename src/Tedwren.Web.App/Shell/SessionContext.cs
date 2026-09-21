using Tedwren.Mobile.Core.Session;

namespace Tedwren.Web.App.Shell;

/// <summary>
/// Holds the emulator's current signed-in session(s) so screens can show the signed-in identity and the shell can
/// gate unauthenticated screens. This is only the in-memory "who is signed in right now" for the UI — the tokens
/// and refresh live in the Core session managers. Operative and manager are tracked separately, matching the
/// app's two auth planes.
/// </summary>
public sealed class SessionContext
{
    /// <summary>The current operative (field) session, or null when not signed in as an operative.</summary>
    public MobileSession? Operative { get; private set; }

    /// <summary>The current manager (console) session, or null when not signed in as a manager.</summary>
    public MobileSession? Manager { get; private set; }

    /// <summary>Raised when either session changes, so subscribers re-render.</summary>
    public event Action? Changed;

    /// <summary>Sets (or clears) the operative session and notifies subscribers.</summary>
    public void SetOperative(MobileSession? session)
    {
        Operative = session;
        Changed?.Invoke();
    }

    /// <summary>Sets (or clears) the manager session and notifies subscribers.</summary>
    public void SetManager(MobileSession? session)
    {
        Manager = session;
        Changed?.Invoke();
    }
}
