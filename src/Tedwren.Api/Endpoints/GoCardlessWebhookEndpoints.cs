using System.Text.Json;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Billing;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the inbound GoCardless webhook endpoint (<c>/api/webhooks/gocardless</c>). It is <b>anonymous</b>
/// (webhooks are not JWT-authenticated) but authenticated by the <c>Webhook-Signature</c> HMAC, which is
/// verified against the configured secret <b>before</b> any processing — an unverifiable request is rejected
/// with 401 and never touches the billing state. This is a server-to-server POST; the read-only write-blocker
/// only applies to authenticated users, so an anonymous webhook is not affected.
/// </summary>
public static class GoCardlessWebhookEndpoints
{
    /// <summary>Registers the anonymous, signature-verified GoCardless webhook endpoint.</summary>
    public static IEndpointRouteBuilder MapGoCardlessWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/gocardless", async (
                HttpRequest request,
                GoCardlessOptions options,
                GoCardlessWebhookProcessor processor,
                CancellationToken cancellationToken) =>
            {
                using var reader = new StreamReader(request.Body);
                var body = await reader.ReadToEndAsync(cancellationToken);

                var signature = request.Headers["Webhook-Signature"].FirstOrDefault();
                if (!GoCardlessSignatureVerifier.IsValid(body, signature, options.WebhookSecret))
                {
                    // Fails closed: bad or unverifiable signature (including when no secret is configured).
                    return Results.Unauthorized();
                }

                try
                {
                    var result = await processor.ProcessAsync(body, cancellationToken);
                    // A 2xx acknowledges receipt so GoCardless stops re-delivering.
                    return Results.Ok(new { result.Applied, result.Duplicates, result.Ignored, result.Failed });
                }
                catch (JsonException)
                {
                    return Results.BadRequest(new { error = "Malformed webhook body." });
                }
            })
            .AllowAnonymous()
            .WithName("GoCardlessWebhook")
            .WithTags("Webhooks");

        return app;
    }
}
