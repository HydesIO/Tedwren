using Microsoft.Extensions.Options;

namespace Tedwren.Web.Configuration;

/// <summary>
/// Pre-launch gate (Web redesign): when <see cref="SiteConfig.IsLanding"/> is true, the whole site is
/// replaced by the single landing page. Any GET that isn't already the landing page — or an
/// always-allowed endpoint (the landing form POST, cookie consent, health, robots) — is internally
/// rewritten to the landing route, so every URL serves the landing page without a redirect loop. A
/// non-GET to a gated path is redirected to the landing page. Static assets are served by the
/// static-file middleware registered ahead of this one, so CSS/JS/images always load. When the flag is
/// off this middleware does nothing.
/// </summary>
public sealed class LandingGateMiddleware
{
    /// <summary>The route that renders the landing page.</summary>
    public const string LandingPath = "/landing";

    private readonly RequestDelegate _next;

    /// <summary>Stores the next middleware in the pipeline.</summary>
    /// <param name="next">The next request delegate.</param>
    public LandingGateMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Gates the request when landing mode is on; otherwise passes it straight through.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="siteOptions">The bound site options carrying the landing flag.</param>
    public async Task InvokeAsync(HttpContext context, IOptions<SiteConfig> siteOptions)
    {
        if (!siteOptions.Value.IsLanding)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (IsAlwaysAllowed(path))
        {
            await _next(context);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method))
        {
            // Render the landing page in place, keeping the visitor's URL (no redirect, no loop).
            context.Request.Path = LandingPath;
            await _next(context);
            return;
        }

        // A gated write (e.g. a form on a hidden page) has nowhere to go while pre-launch — send it home.
        context.Response.Redirect(LandingPath);
    }

    /// <summary>Paths that must keep working while the gate is on: the landing page itself, its form
    /// POST, cookie consent, health and robots.</summary>
    /// <param name="path">The request path.</param>
    private static bool IsAlwaysAllowed(PathString path) =>
        path.StartsWithSegments(LandingPath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/consent", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/error", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase);
}
