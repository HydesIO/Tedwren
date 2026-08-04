using Tedwren.UiComponents.Models;
using Tedwren.UiComponents.Navigation;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// Supplies the presentation-layer data the application shell needs (navigation,
/// platform list, environment info, signed-in user). Deliberately an interface so a
/// real API-backed implementation can replace it later without touching any component.
/// </summary>
public interface IShellSampleDataService
{
    IReadOnlyList<NavItem> GetNavItems();
    IReadOnlyList<string> GetPlatforms();
    string GetSelectedPlatform();
    ShellUser GetCurrentUser();
    ShellEnvironment GetEnvironment();
    int GetNotificationCount();
    IReadOnlyList<NotificationEntry> GetNotifications();
}

/// <summary>Signed-in user, for the top-bar profile menu.</summary>
public sealed record ShellUser(string Name, string Role, string? AvatarUrl = null);

/// <summary>Environment/version info, for the sidebar environment panel.</summary>
public sealed record ShellEnvironment(string Name, string Version, string Build, bool IsHealthy);
