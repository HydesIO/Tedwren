namespace Tedwren.Web.Configuration;

/// <summary>
/// The three — and only three — canonical calls to action the marketing site is allowed to render
/// (Web Plan §5.4). Constraining CTAs to a closed set is a mechanism, not a note: the <c>Cta</c>
/// view component accepts a <see cref="CtaAction"/>, so vague copy such as "Learn more" or
/// "Get started" cannot be introduced by a view author. Button copy is the literal next action.
/// </summary>
public enum CtaAction
{
    /// <summary>"Book a demo" → the demo/pilot lead form.</summary>
    BookDemo,

    /// <summary>"Start a pilot" → the demo form with the pilot intent.</summary>
    StartPilot,

    /// <summary>"Get your Worker Passport" → the Worker Passport page.</summary>
    GetWorkerPassport,
}

/// <summary>Resolves a <see cref="CtaAction"/> to its fixed button copy and target path.</summary>
public static class CtaCatalogue
{
    /// <summary>Returns the canonical label and href for a CTA action.</summary>
    /// <param name="action">The canonical action to resolve.</param>
    /// <returns>The fixed button label and site-relative target.</returns>
    public static (string Label, string Href) Resolve(CtaAction action) => action switch
    {
        CtaAction.BookDemo => ("Book a demo", "/demo"),
        CtaAction.StartPilot => ("Start a pilot", "/demo?pilot=true"),
        CtaAction.GetWorkerPassport => ("Get your Worker Passport", "/worker-passport"),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown CTA action."),
    };
}
