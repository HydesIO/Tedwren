namespace Tedwren.Application.DemoData;

/// <summary>
/// Identity of the seeded demo operative used by the browser emulator's Development-only demo sign-in
/// (<c>operative@tedwren.com</c>). Its mobile number sits deliberately outside every workforce range in
/// <see cref="DemoDataPlanBuilder"/> (main <c>+447700900100…</c>, sub <c>+447700900600…</c>, company contacts
/// <c>…900001/2</c>), and its ids are derived like all other demo records so it seeds and tears down with the
/// rest of the demo dataset. This is NOT a real auth path — the demo sign-in that consumes it is fail-closed
/// (see <c>DemoOptions</c>) and never exists in Production.
/// </summary>
public static class DemoOperatives
{
    /// <summary>The demo operative's email — the credential the emulator's demo sign-in accepts.</summary>
    public const string Email = "operative@tedwren.com";

    /// <summary>The demo operative's mobile number (deliberately free of the seeded workforce ranges).</summary>
    public const string Phone = "+447700900000";

    /// <summary>Deterministic person id (matches the seeded <c>Person</c>).</summary>
    public static readonly Guid PersonId = DemoDataIds.Derive("person:demo-operative");

    /// <summary>Deterministic engagement id (matches the seeded <c>Engagement</c>).</summary>
    public static readonly Guid EngagementId = DemoDataIds.Derive("engagement:demo-operative");

    /// <summary>Returns the demo operative's phone for a matching email (case-insensitive, trimmed), else null.</summary>
    public static string? PhoneFor(string? email) =>
        string.Equals(email?.Trim(), Email, StringComparison.OrdinalIgnoreCase) ? Phone : null;
}
