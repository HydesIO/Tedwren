using Tedwren.Abstractions.Contracts.LaunchList;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the launch-list endpoints (<c>/api/launch-signups</c>): the marketing landing page adds an email
/// anonymously; the platform-admin console lists subscribers and sends the launch announcement. The public
/// signup is a separate, explicitly anonymous group; the admin reads/actions require the PlatformAdmin policy.
/// </summary>
public static class LaunchListEndpoints
{
    /// <summary>Registers the <c>/api/launch-signups</c> endpoint groups.</summary>
    public static IEndpointRouteBuilder MapLaunchListEndpoints(this IEndpointRouteBuilder app)
    {
        // Public signup — anonymous (the landing page has no account behind it), rate-limited against abuse.
        var publicGroup = app.MapGroup("/api/launch-signups").WithTags("LaunchList").AllowAnonymous().RequireRateLimiting("public");

        publicGroup.MapPost("/", async (CreateLaunchSignupRequest request, ILaunchListService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.SignUpAsync(request, cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateLaunchSignup");

        // One-click unsubscribe (PECR/GDPR): opens from the email link, returns a friendly confirmation page.
        publicGroup.MapGet("/unsubscribe", async (string? token, ILaunchListService service, CancellationToken cancellationToken) =>
            {
                var ok = await service.UnsubscribeAsync(token ?? string.Empty, cancellationToken);
                var message = ok
                    ? "You've been unsubscribed. You won't receive the Tedwren launch email."
                    : "This unsubscribe link is not valid. If you keep receiving emails, contact us and we'll remove you.";
                return Results.Content(UnsubscribePage(message), "text/html");
            })
            .WithName("UnsubscribeLaunchSignup");

        // Admin surface — platform-admin only.
        var admin = app.MapGroup("/api/launch-signups").WithTags("LaunchList").RequireAuthorization("PlatformAdmin");

        admin.MapGet("/", async (ILaunchListService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListAsync(cancellationToken)))
            .WithName("ListLaunchSignups");

        admin.MapPost("/notify", async (NotifyLaunchRequest? request, ILaunchListService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.NotifyAsync(request ?? new NotifyLaunchRequest(), cancellationToken)))
            .WithName("NotifyLaunchList");

        return app;
    }

    /// <summary>A minimal, self-contained HTML confirmation page for the unsubscribe link.</summary>
    private static string UnsubscribePage(string message) =>
        "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
        "<title>Unsubscribe · Tedwren</title>" +
        "<style>body{font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;background:#f7f7f8;color:#101828;" +
        "display:flex;min-height:100vh;align-items:center;justify-content:center;margin:0}" +
        ".card{background:#fff;border:1px solid #e4e7ec;border-radius:12px;padding:32px;max-width:420px;text-align:center}" +
        "h1{color:#e8590c;font-size:1.25rem;margin:0 0 12px}p{color:#475467;line-height:1.6;margin:0}</style></head>" +
        "<body><div class=\"card\"><h1>Tedwren</h1><p>" + System.Net.WebUtility.HtmlEncode(message) + "</p></div></body></html>";
}
