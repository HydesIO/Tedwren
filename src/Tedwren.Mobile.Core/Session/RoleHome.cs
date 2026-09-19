namespace Tedwren.Mobile.Core.Session;

/// <summary>
/// Which of the two role-specific home experiences a signed-in user lands on. The app is one binary with a
/// role-switched shell (never both at once): each home has its own interactive card menu and its own overview
/// dashboard.
/// </summary>
public enum RoleHome
{
    /// <summary>The field operative experience — sign in/out, forms due, capture evidence, my hours, my record.</summary>
    Operative,

    /// <summary>The site manager / administrator experience — muster, site-entry decisions, forms review, reports.</summary>
    Manager,
}
