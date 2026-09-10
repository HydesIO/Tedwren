using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.UiComponents.Models;

namespace Tedwren.Client.Pages.SiteGate;

/// <summary>How a site-entry result is presented, which differs by product (PRD §2). The subcontractor
/// product never presents a site-access decision (R18, SUB-12): its result only ever reads
/// "recorded / site-ready" or "recorded — action needed", never "permitted/denied/blocked", and it offers no
/// manager override (there is nothing to override). The main contractor product presents the actual five-check
/// entry decision (MC-8/9) with a day-only manager override (MC-11).</summary>
public sealed record SiteGateResultView(StatusKind Severity, string Title, string Message, bool OffersOverride);

/// <summary>Builds the <see cref="SiteGateResultView"/> for a decision result and the caller's product. Pure and
/// UI-free so the R18 wording rule can be asserted directly.</summary>
public static class SiteGateResultPresenter
{
    /// <summary>Presents a decision result for the given product. A null (unknown) product keeps the main
    /// contractor decision presentation.</summary>
    public static SiteGateResultView For(OrgType? product, EntryDecisionResultDto result)
    {
        if (product == OrgType.Subcontractor)
        {
            // R18: attendance is recorded either way; outstanding cards are flagged as items to resolve, never a
            // refusal of access. No override — a subcontractor sign-in never blocks.
            return new SiteGateResultView(
                result.Admitted ? StatusKind.Success : StatusKind.Warning,
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
            result.Admitted ? StatusKind.Success : StatusKind.Danger,
            title,
            message,
            OffersOverride: !result.Admitted);
    }
}
