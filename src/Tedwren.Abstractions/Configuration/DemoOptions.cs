namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Toggles Development/demo-only conveniences that must NEVER be enabled in Production. It currently gates the
/// operative demo sign-in (<c>operative@tedwren.com</c>) used by the browser emulator, which mints a real
/// operative token without an SMS one-time code. Bound from the <c>Demo</c> section; it defaults OFF so the
/// surface is fail-closed unless explicitly enabled, and startup validation refuses to boot Production while it
/// is on (mirroring <c>Auth:TestBypass</c>).
/// </summary>
public sealed class DemoOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Demo";

    /// <summary>
    /// Whether demo-only conveniences (the operative demo sign-in) are enabled. Must be false in Production —
    /// <see cref="Tedwren.Abstractions.Configuration.DemoOptions"/> is validated at startup to enforce this.
    /// </summary>
    public bool Enabled { get; set; }
}
