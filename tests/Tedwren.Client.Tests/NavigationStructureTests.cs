using Tedwren.Client.Services;
using Tedwren.UiComponents.Navigation;

namespace Tedwren.Client.Tests;

/// <summary>
/// Guards the sidebar navigation structure after UAT-009 / UAT-021: Users and Inductions (config) moved
/// under a "System Configuration" group, and the flatten helper that keeps nested destinations reachable by
/// the title matcher and command palette.
/// </summary>
public sealed class NavigationStructureTests
{
    [Fact]
    public void Leaves_OfLeafItem_IsItself()
    {
        var item = new NavItem("Dashboard", "icon", "/");

        var leaves = item.Leaves().ToList();

        Assert.Single(leaves);
        Assert.Same(item, leaves[0]);
    }

    [Fact]
    public void Leaves_OfGroup_AreItsChildren_NotTheHeader()
    {
        var users = new NavItem("Users", "icon", "/users");
        var group = new NavItem("System Configuration", "icon", string.Empty, new[] { users });

        var leaves = group.Leaves().ToList();

        Assert.Single(leaves);
        Assert.Same(users, leaves[0]);   // the non-navigable header is not a destination
    }

    [Fact]
    public void SystemConfiguration_IsAGroup_ContainingUsersAndInductions()
    {
        var group = ShellChrome.NavItems.Single(n => n.Label == "System Configuration");

        Assert.True(group.HasChildren);
        Assert.Equal(string.Empty, group.Href);   // header carries no route of its own

        var childHrefs = group.Children!.Select(c => c.Href).ToList();
        Assert.Contains("/users", childHrefs);
        Assert.Contains("/inductions", childHrefs);
        Assert.Contains("/system-configuration", childHrefs);   // former settings page, now "General Settings"
    }

    [Fact]
    public void UsersAndInductions_AreNoLongerTopLevel()
    {
        var topLevelHrefs = ShellChrome.NavItems.Select(n => n.Href).ToList();

        Assert.DoesNotContain("/users", topLevelHrefs);
        Assert.DoesNotContain("/inductions", topLevelHrefs);

        // Induction *records* stay operational (top level); only the induction *builder* moved.
        Assert.Contains("/induction-records", topLevelHrefs);
    }
}
