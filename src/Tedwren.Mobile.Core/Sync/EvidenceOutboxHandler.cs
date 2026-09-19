using System.Text.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Sync;

/// <summary>Drains a queued generic evidence capture (M5): upload the photo (checkpointed), then submit the item.</summary>
public sealed class EvidenceOutboxHandler : IOutboxItemHandler
{
    /// <summary>The <see cref="OutboxItem.Kind"/> this handler processes.</summary>
    public const string ItemKind = "evidence";

    private readonly CaptureApiClient _api;

    /// <summary>Creates the handler over the capture client.</summary>
    public EvidenceOutboxHandler(CaptureApiClient api) => _api = api;

    /// <inheritdoc />
    public string Kind => ItemKind;

    /// <inheritdoc />
    public async Task ExecuteAsync(OutboxItem item, IOutboxStore store, CancellationToken cancellationToken = default)
    {
        await UploadCheckpoint.EnsurePhotoUploadedAsync(item, _api, store, cancellationToken);
        var request = JsonSerializer.Deserialize<MobileReportEvidenceRequest>(item.PayloadJson)!
            with { PhotoReference = item.UploadedImageReference };
        await _api.ReportEvidenceAsync(request, cancellationToken);
    }
}
