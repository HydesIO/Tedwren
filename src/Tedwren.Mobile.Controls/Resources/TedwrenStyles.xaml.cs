namespace Tedwren.Mobile.Controls.Resources;

/// <summary>
/// Typed resource dictionary holding the Tedwren spacing/radius tokens and semantic control styles. It merges
/// <see cref="TedwrenColors"/> so its StaticResource colour lookups resolve; the app head merges only this
/// dictionary to pull in the whole design system.
/// </summary>
public partial class TedwrenStyles : ResourceDictionary
{
    /// <summary>Loads the XAML-defined styles.</summary>
    public TedwrenStyles() => InitializeComponent();
}
