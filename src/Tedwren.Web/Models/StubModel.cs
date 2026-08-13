using Tedwren.Web.Configuration;

namespace Tedwren.Web.Models;

/// <summary>
/// View model for the shared W1 stub body (<c>Views/Shared/_Stub.cshtml</c>). Each page supplies its
/// heading, a short lede, the W-phase that delivers the real content, and an optional canonical CTA.
/// These stubs are replaced page-by-page in W3–W7.
/// </summary>
/// <param name="Heading">Page H1.</param>
/// <param name="Lede">Short descriptive line.</param>
/// <param name="Phase">The W-phase that delivers the real content (e.g. "W3").</param>
/// <param name="Cta">Optional canonical CTA to render.</param>
public sealed record StubModel(string Heading, string Lede, string Phase, CtaAction? Cta = null);
