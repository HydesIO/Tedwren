namespace Tedwren.Client.Services;

/// <summary>
/// Client-side admin-area configuration, bound from the <c>Admin</c> section of
/// <c>wwwroot/appsettings.json</c>. When <see cref="Enabled"/> is true the console offers the Tedwren
/// platform admin area. This flag never grants access on its own: the admin gate additionally requires the
/// signed-in user to be a platform administrator (<see cref="AuthState.IsPlatformAdmin"/>, computed
/// server-side). It only decides whether the capability is available in this deployment.
/// </summary>
public sealed class AdminAreaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Admin";

    /// <summary>Whether the admin area is enabled for this deployment.</summary>
    public bool Enabled { get; set; }
}
