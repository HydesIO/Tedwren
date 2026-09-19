using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Sync;

/// <summary>
/// Shared upload-then-checkpoint step for the capture handlers (M5): uploads the item's photo once, records the
/// returned reference on the item and clears the bytes, persisting that checkpoint <b>before</b> the submit — so a
/// submit failure retries only the submit, never re-uploading the photo (avoids duplicate images).
/// </summary>
internal static class UploadCheckpoint
{
    /// <summary>Uploads the photo and checkpoints its reference, if there is a photo not yet uploaded.</summary>
    public static async Task EnsurePhotoUploadedAsync(OutboxItem item, CaptureApiClient api, IOutboxStore store, CancellationToken cancellationToken)
    {
        if (item.UploadedImageReference is not null || item.PhotoBytes is not { Length: > 0 })
        {
            return;
        }

        item.UploadedImageReference = await api.UploadPhotoAsync(item.PhotoBytes, item.PhotoContentType ?? "image/jpeg", cancellationToken);
        item.PhotoBytes = null; // the reference is now the checkpoint; free the blob.
        await store.UpdateAsync(item, cancellationToken);
    }
}
