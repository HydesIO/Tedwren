using Tedwren.Mobile.Core.Forms;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="IFormDraftStore"/> for the emulator: an in-memory form-draft store keyed by assignment id,
/// mirroring the device's autosave/resume behaviour for the app session. WA6 adds <c>localStorage</c> persistence
/// so an in-progress form survives a page reload.
/// </summary>
public sealed class WebFormDraftStore : IFormDraftStore
{
    private readonly Dictionary<Guid, FormDraft> _drafts = new();
    private readonly object _gate = new();

    /// <summary>Saves (inserts or replaces) a draft.</summary>
    public Task SaveAsync(FormDraft draft, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _drafts[draft.Key] = draft;
        }

        return Task.CompletedTask;
    }

    /// <summary>Loads the draft for an assignment, or null when none is saved.</summary>
    public Task<FormDraft?> GetAsync(Guid key, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _drafts.TryGetValue(key, out var draft);
            return Task.FromResult(draft);
        }
    }

    /// <summary>Deletes a draft (after a successful enqueue, or when discarded).</summary>
    public Task DeleteAsync(Guid key, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _drafts.Remove(key);
        }

        return Task.CompletedTask;
    }
}
