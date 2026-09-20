using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.SiteEntry;

namespace Tedwren.Mobile.Core.SiteEntry;

/// <summary>The severity of a presented gate result — drives the native banner colour.</summary>
public enum GateResultSeverity
{
    /// <summary>Admitted / recorded and site-ready.</summary>
    Success,

    /// <summary>Recorded with something outstanding (subcontractor framing, R18).</summary>
    Warning,

    /// <summary>Entry blocked (main-contractor framing).</summary>
    Danger,
}

/// <summary>How a site-entry result is presented natively (mirrors the console <c>SiteGateResultView</c>).</summary>
public sealed record SiteGateResultView(GateResultSeverity Severity, string Title, string Message, bool OffersOverride);

/// <summary>
/// Builds the <see cref="SiteGateResultView"/> for a decision result and the caller's product (R18) — a direct
/// port of the console <c>SiteGateResultPresenter</c> so the wording rule is identical across web and mobile. The
/// subcontractor product never presents an access decision (R18/SUB-12): its result only ever reads
/// "recorded / site-ready" or "recorded — action needed", never "permitted/denied/blocked", and offers no override.
/// The main contractor product presents the five-check decision (MC-8/9) with a day-only manager override (MC-11).
/// A null (unknown) product keeps the main-contractor presentation: the gate is safety-critical and stays
/// fail-closed (R2), so a product-less company retains the ability to block rather than silently "record only".
/// </summary>
public static class SiteGateResultPresenter
{
    /// <summary>Presents a decision result for the given product.</summary>
    public static SiteGateResultView For(OrgType? product, EntryDecisionResultDto result)
    {
        if (product == OrgType.Subcontractor)
        {
            // R18: attendance is recorded either way; outstanding cards are items to resolve, never a refusal.
            return new SiteGateResultView(
                result.Admitted ? GateResultSeverity.Success : GateResultSeverity.Warning,
                result.Admitted ? "Recorded — site-ready" : "Recorded — action needed",
                result.Admitted
                    ? "Attendance recorded. Required cards are in date."
                    : $"Attendance recorded. Outstanding: {result.BlockReason ?? "required cards need attention"}.",
                OffersOverride: false);
        }

        var title = result.Admitted
            ? (result.WasOverridden ? "Admitted (manager override)" : "Admitted")
            : "Entry blocked";
        var message = result.Admitted ? $"Decision in {result.ElapsedMs} ms." : (result.BlockReason ?? "Blocked.");
        return new SiteGateResultView(
            result.Admitted ? GateResultSeverity.Success : GateResultSeverity.Danger,
            title,
            message,
            OffersOverride: !result.Admitted);
    }
}
