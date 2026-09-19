namespace Tedwren.Mobile.Core.Forms;

/// <summary>
/// A partially-filled form saved locally (M6) so the operative can leave and resume, and survive an app restart —
/// kept in the encrypted store. Keyed by the assignment id (one in-progress draft per assignment). The
/// <see cref="PayloadJson"/> is the serialized in-progress capture (answers + any captured files, base64).
/// </summary>
public sealed class FormDraft
{
    /// <summary>The assignment this draft is for (the draft key).</summary>
    public Guid Key { get; set; }

    /// <summary>The template version being filled (so a superseded template can be detected on resume).</summary>
    public Guid TemplateVersionId { get; set; }

    /// <summary>The serialized in-progress capture (answers + captured files).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>When the draft was last autosaved (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Stores in-progress form drafts (M6 autosave/resume). Implemented by the encrypted store in the MAUI head; an
/// in-memory double backs the unit tests.
/// </summary>
public interface IFormDraftStore
{
    /// <summary>Saves (inserts or replaces) a draft.</summary>
    Task SaveAsync(FormDraft draft, CancellationToken cancellationToken = default);

    /// <summary>Loads the draft for an assignment, or null when none is saved.</summary>
    Task<FormDraft?> GetAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>Deletes a draft (after a successful enqueue, or when discarded).</summary>
    Task DeleteAsync(Guid key, CancellationToken cancellationToken = default);
}
