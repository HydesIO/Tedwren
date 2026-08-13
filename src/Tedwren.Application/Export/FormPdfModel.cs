namespace Tedwren.Application.Export;

/// <summary>
/// The data a rendered form-submission PDF needs, assembled by the submission service from the submission, its
/// template version and its captured files. Kept separate from the renderer so the layout has no knowledge of
/// the domain or DTOs.
/// </summary>
public sealed record FormPdfModel(
    string FormName,
    int Version,
    string Scope,
    string? SiteName,
    string SubmittedBy,
    DateTimeOffset SubmittedUtc,
    string Status,
    string? ReviewNote,
    IReadOnlyList<FormPdfSection> Sections);

/// <summary>A titled group of answered items in the PDF.</summary>
public sealed record FormPdfSection(string Title, IReadOnlyList<FormPdfItem> Items);

/// <summary>
/// A single answered item. <see cref="ValueText"/> carries a textual answer; <see cref="Image"/> carries an
/// embedded picture (a photo or a signature). Exactly one is typically set.
/// </summary>
public sealed record FormPdfItem(string Label, string? ValueText, byte[]? Image);
