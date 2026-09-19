using System.Text.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Sync;

/// <summary>
/// Drains a queued completed-form submission (M6): the whole <see cref="CreateFormSubmissionRequest"/> (answers +
/// base64 files + signature-inline) rides in the outbox item's payload, so the handler simply POSTs it as one
/// idempotent call — no per-file upload checkpoint (unlike the single-photo evidence/hazard handlers).
/// </summary>
public sealed class FormsOutboxHandler : IOutboxItemHandler
{
    /// <summary>The <see cref="OutboxItem.Kind"/> this handler processes.</summary>
    public const string ItemKind = "form-submission";

    private readonly FormsApiClient _api;

    /// <summary>Creates the handler over the forms client.</summary>
    public FormsOutboxHandler(FormsApiClient api) => _api = api;

    /// <inheritdoc />
    public string Kind => ItemKind;

    /// <inheritdoc />
    public async Task ExecuteAsync(OutboxItem item, IOutboxStore store, CancellationToken cancellationToken = default)
    {
        var request = JsonSerializer.Deserialize<CreateFormSubmissionRequest>(item.PayloadJson)!;
        await _api.SubmitAsync(request, cancellationToken);
    }
}
