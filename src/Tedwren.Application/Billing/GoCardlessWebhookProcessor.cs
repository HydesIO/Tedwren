using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Billing;

/// <summary>
/// Processes a verified GoCardless webhook body: parses its events, stores each once (deduped by GoCardless
/// event id so re-delivery never applies twice), and updates the local mandate/payment to the event's status.
/// A returned payment (failed/charged back) is recorded with its reason so the admin can re-take it. Each
/// event's outcome is stored for the admin events view. One failing event never aborts the batch.
/// </summary>
public sealed class GoCardlessWebhookProcessor
{
    private readonly IWebhookEventRepository _events;
    private readonly IMandateRepository _mandates;
    private readonly IPaymentRepository _payments;

    /// <summary>Creates the processor over the webhook-event, mandate and payment repositories.</summary>
    public GoCardlessWebhookProcessor(IWebhookEventRepository events, IMandateRepository mandates, IPaymentRepository payments)
    {
        _events = events;
        _mandates = mandates;
        _payments = payments;
    }

    /// <summary>Parses and applies every event in the webhook body, returning a tally of what happened.</summary>
    public async Task<WebhookProcessResult> ProcessAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(rawBody);
        if (!document.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
        {
            return new WebhookProcessResult(0, 0, 0, 0);
        }

        int applied = 0, duplicates = 0, ignored = 0, failed = 0;
        foreach (var element in events.EnumerateArray())
        {
            var id = GetString(element, "id");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (await _events.GetByGoCardlessEventIdAsync(id, cancellationToken) is not null)
            {
                duplicates++;
                continue;
            }

            var resourceType = GetString(element, "resource_type") ?? "unknown";
            var action = GetString(element, "action") ?? "unknown";
            var (paymentId, mandateId) = ReadLinks(element);

            var stored = new WebhookEvent
            {
                GoCardlessEventId = id,
                ResourceType = resourceType,
                Action = action,
                ResourceId = paymentId ?? mandateId,
                RawJson = element.GetRawText(),
            };
            await _events.AddAsync(stored, cancellationToken);

            try
            {
                var outcome = await ApplyAsync(resourceType, action, paymentId, mandateId, ReadDetail(element), cancellationToken);
                stored.Outcome = outcome.Outcome;
                stored.Detail = outcome.Detail;
                switch (outcome.Outcome)
                {
                    case WebhookProcessingOutcome.Applied: applied++; break;
                    default: ignored++; break;
                }
            }
            catch (Exception ex)
            {
                stored.Outcome = WebhookProcessingOutcome.Failed;
                stored.Detail = ex.Message;
                failed++;
            }

            stored.ProcessedUtc = DateTimeOffset.UtcNow;
            await _events.UpdateAsync(stored, cancellationToken);
        }

        return new WebhookProcessResult(applied, duplicates, ignored, failed);
    }

    /// <summary>Applies one event to the referenced mandate/payment, returning the outcome and a note.</summary>
    private async Task<(WebhookProcessingOutcome Outcome, string? Detail)> ApplyAsync(
        string resourceType, string action, string? paymentId, string? mandateId, string? detail, CancellationToken cancellationToken)
    {
        if (resourceType == "payments" && !string.IsNullOrEmpty(paymentId))
        {
            var payment = await _payments.GetByGoCardlessPaymentIdAsync(paymentId, cancellationToken);
            if (payment is null)
            {
                return (WebhookProcessingOutcome.Ignored, "Unknown payment.");
            }

            if (GoCardlessStatusMap.ToPaymentStatus(action) is not { } status)
            {
                return (WebhookProcessingOutcome.Ignored, $"No status change for action '{action}'.");
            }

            payment.Status = status;
            payment.FailureReason = status is PaymentStatus.Failed or PaymentStatus.ChargedBack ? detail : null;
            payment.UpdatedUtc = DateTimeOffset.UtcNow;
            await _payments.UpdateAsync(payment, cancellationToken);
            return (WebhookProcessingOutcome.Applied, status.ToString());
        }

        if (resourceType == "mandates" && !string.IsNullOrEmpty(mandateId))
        {
            var mandate = await _mandates.GetByGoCardlessMandateIdAsync(mandateId, cancellationToken);
            if (mandate is null)
            {
                return (WebhookProcessingOutcome.Ignored, "Unknown mandate.");
            }

            if (GoCardlessStatusMap.ToMandateStatus(action) is not { } status)
            {
                return (WebhookProcessingOutcome.Ignored, $"No status change for action '{action}'.");
            }

            mandate.Status = status;
            mandate.UpdatedUtc = DateTimeOffset.UtcNow;
            await _mandates.UpdateAsync(mandate, cancellationToken);
            return (WebhookProcessingOutcome.Applied, status.ToString());
        }

        return (WebhookProcessingOutcome.Ignored, $"Unhandled resource type '{resourceType}'.");
    }

    /// <summary>Reads the linked payment and mandate ids from an event's <c>links</c> object.</summary>
    private static (string? PaymentId, string? MandateId) ReadLinks(JsonElement element)
    {
        if (!element.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        return (GetString(links, "payment"), GetString(links, "mandate"));
    }

    /// <summary>Reads a human-readable detail (description then cause) from an event's <c>details</c> object.</summary>
    private static string? ReadDetail(JsonElement element)
    {
        if (!element.TryGetProperty("details", out var details) || details.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(details, "description") ?? GetString(details, "cause");
    }

    /// <summary>Reads a string property, or null when absent/non-string.</summary>
    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

/// <summary>Tally of a webhook batch: events applied, duplicates skipped, ignored, and failed.</summary>
public sealed record WebhookProcessResult(int Applied, int Duplicates, int Ignored, int Failed);
