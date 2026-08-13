namespace Tedwren.Web.Content;

/// <summary>
/// Options for the JSON content layer, bound from the "Content" configuration section. Points the
/// provider at the directory of JSON content files, resolved relative to the app content root.
/// </summary>
public sealed class ContentOptions
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "Content";

    /// <summary>Content directory, relative to the app content root. Defaults to "Content".</summary>
    public string Directory { get; set; } = "Content";
}
