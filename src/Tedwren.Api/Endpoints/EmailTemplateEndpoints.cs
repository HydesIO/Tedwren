using System.Reflection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Notifications.Email;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Hosts the branded email template inside the API: the anonymous logo asset that emails reference by absolute
/// URL, a Development-only preview that renders the layout and every optional component to the browser, and an
/// admin-only test-send that delivers a real branded sample so live delivery can be verified. These are the
/// "templates hosted within the API".
/// </summary>
public static class EmailTemplateEndpoints
{
    /// <summary>Manifest name of the embedded PNG logo (see <c>Tedwren.Api.csproj</c>).</summary>
    private const string LogoResourceName = "Tedwren.Api.Assets.logo.png";

    /// <summary>Registers the email-asset and email-template preview endpoints.</summary>
    public static IEndpointRouteBuilder MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        // Public logo asset: email clients load it by absolute URL, so it must be anonymous.
        app.MapGet("/api/email-assets/logo.png", () =>
            {
                var bytes = ReadLogoBytes();
                return bytes is null ? Results.NotFound() : Results.File(bytes, "image/png");
            })
            .WithName("EmailLogo")
            .WithTags("Email")
            .AllowAnonymous();

        // Dev-only visual preview of the template and component kit. Never exposed outside Development.
        app.MapGet("/api/email-templates/preview/{template?}", (
                string? template,
                IEmailTemplateRenderer renderer,
                IHostEnvironment environment) =>
            {
                if (!environment.IsDevelopment())
                {
                    return Results.NotFound();
                }

                var (subject, html) = BuildSample(renderer, template);
                return Results.Content(html, "text/html");
            })
            .WithName("EmailTemplatePreview")
            .WithTags("Email")
            .AllowAnonymous();

        // Admin-only: actually send a branded sample to verify end-to-end delivery. With Provider=Resend this
        // is a real email; with the default Outbox stub it is captured (see GET /api/jobs/outbox). It sends, so
        // unlike the anonymous preview it is protected.
        app.MapPost("/api/email-templates/test-send", async (
                TestSendRequest request,
                IEmailSender email,
                EmailOptions options,
                CancellationToken cancellationToken) =>
            {
                var toEmail = (request.ToEmail ?? string.Empty).Trim();
                if (toEmail.Length == 0 || !toEmail.Contains('@') || !toEmail.Contains('.'))
                {
                    return Results.BadRequest(new { error = "A valid recipient email address is required." });
                }

                var content = new EmailContentBuilder()
                    .Heading("Test email from Tedwren")
                    .Paragraph("If you're reading this, the Tedwren email pipeline is configured correctly and can deliver branded notifications.")
                    .Callout($"Delivered via the '{options.Provider}' provider.", "Delivery")
                    .Build();

                await email.SendHtmlAsync(toEmail, "Tedwren test email", content, cancellationToken);
                return Results.Ok(new { sentTo = toEmail, provider = options.Provider.ToString() });
            })
            .WithName("EmailTemplateTestSend")
            .WithTags("Email")
            .RequireAuthorization("AdminOnly");

        return app;
    }

    /// <summary>Request body for the admin test-send endpoint.</summary>
    private sealed record TestSendRequest(string ToEmail);

    /// <summary>Reads the embedded logo PNG into memory (small, so read per request is fine).</summary>
    private static byte[]? ReadLogoBytes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(LogoResourceName);
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>Builds a named sample email so each template/component can be eyeballed in the browser.</summary>
    private static (string Subject, string Html) BuildSample(IEmailTemplateRenderer renderer, string? template)
    {
        switch ((template ?? string.Empty).ToLowerInvariant())
        {
            case "verification":
            {
                const string subject = "Your Tedwren verification code";
                var content = new EmailContentBuilder()
                    .Heading(subject)
                    .Paragraph("Use the code below to finish signing in. It expires in 10 minutes and is for your use only.")
                    .VerificationCode("482913", "Verification code")
                    .Paragraph("If you didn't request this, you can safely ignore this email.")
                    .Build();
                return (subject, renderer.Render(subject, content));
            }

            case "welcome":
            {
                const string subject = "You've been invited to Tedwren";
                var content = new EmailContentBuilder()
                    .Heading(subject)
                    .Paragraph("You've been invited to join your company's Tedwren workspace. Click below to set up your account.")
                    .Button("Accept your invitation", "https://app.tedwren.co.uk/invite/accept?token=sample")
                    .Callout("This invitation link is unique to you and expires in 7 days.", "Keep it private")
                    .Build();
                return (subject, renderer.Render(subject, content));
            }

            case "digest":
            {
                const string subject = "Weekly expiry digest — 3 item(s)";
                var content = new EmailContentBuilder()
                    .Heading(subject)
                    .Paragraph("The following items expire within the next 60 days:")
                    .Table(
                        new[] { "Worker / Item", "Expires", "In" },
                        new IReadOnlyList<string>[]
                        {
                            new[] { "Joe Smith — CSCS Card", "24 Aug 2026", "12 days" },
                            new[] { "Amara Okafor — First Aid", "02 Sep 2026", "21 days" },
                            new[] { "Site insurance — Elm Court", "18 Sep 2026", "37 days" },
                        })
                    .Button("Open the console", "https://app.tedwren.co.uk/expiry")
                    .Build();
                return (subject, renderer.Render(subject, content));
            }

            case "plaintext":
            {
                // Demonstrates how the existing jobs' plain-text bodies are auto-wrapped.
                const string subject = "Qualification expiry warning";
                const string body =
                    "A qualification is due to expire soon.\n\n" +
                    "CSCS Card expires on 24 Aug 2026 (in 12 days). Please arrange renewal to keep site access uninterrupted.";
                return (subject, renderer.RenderPlainText(subject, body));
            }

            default:
            {
                // Showcase: every component in one email.
                const string subject = "Tedwren email component showcase";
                var content = new EmailContentBuilder()
                    .Heading("Component showcase", 1)
                    .Paragraph("This preview renders every optional component in the branded shell.")
                    .Heading("Paragraph & highlight")
                    .Raw("<p style=\"margin:0 0 16px 0;font-family:" + "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif" +
                         ";font-size:15px;line-height:1.6;color:#212529;\">Normal text with an inline " +
                         EmailComponents.Highlight("highlighted") + " phrase.</p>")
                    .Heading("Verification code")
                    .VerificationCode("482913", "Your 2FA code")
                    .Heading("Callout")
                    .Callout("This is a highlighted callout used for important notices.", "Please note")
                    .Heading("Key / value rows")
                    .KeyValueRows(new[]
                    {
                        new KeyValuePair<string, string>("Reference", "TW-10482"),
                        new KeyValuePair<string, string>("Site", "Elm Court"),
                        new KeyValuePair<string, string>("Date", "12 Aug 2026"),
                    })
                    .Heading("Table")
                    .Table(
                        new[] { "Item", "Status" },
                        new IReadOnlyList<string>[]
                        {
                            new[] { "CSCS Card", "Valid" },
                            new[] { "First Aid", "Expiring" },
                        })
                    .Heading("Bullet list")
                    .BulletList(new[] { "First point", "Second point", "Third point" })
                    .Divider()
                    .Heading("Button")
                    .Button("Primary action", "https://app.tedwren.co.uk")
                    .Build();
                return (subject, renderer.Render(subject, content));
            }
        }
    }
}
