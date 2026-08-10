using Tedwren.Abstractions.Common;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Organisation;

/// <summary>
/// Rolls a set of current qualification cards up into a single compliance state and percentage (SF-8). Pure
/// and deterministic: the state is derived from the cards' computed statuses (never invented), and reports
/// <see cref="ComplianceState.Pending"/> when there is nothing to assess. Shared by the operative and company
/// roll-ups so both use the same rule.
/// </summary>
public static class ComplianceRollup
{
    /// <summary>
    /// Computes (state, percent) from a person's current (non-superseded) cards as of a date. Percent is the
    /// share of in-date-and-not-expiring cards; the state is non-compliant if anything is expired, at risk if
    /// anything is expiring soon, otherwise compliant. No cards → (Pending, null).
    /// </summary>
    public static (ComplianceState State, double? Percent) FromCards(IReadOnlyList<QualificationCard> currentCards, DateOnly asOf)
    {
        if (currentCards.Count == 0)
        {
            return (ComplianceState.Pending, null);
        }

        var valid = 0;
        var expiringSoon = 0;
        var expired = 0;
        foreach (var card in currentCards)
        {
            switch (card.GetStatus(asOf))
            {
                case CardStatus.Valid: valid++; break;
                case CardStatus.ExpiringSoon: expiringSoon++; break;
                case CardStatus.Expired: expired++; break;
            }
        }

        var percent = Math.Round(100.0 * valid / currentCards.Count);
        var state = expired > 0
            ? ComplianceState.NonCompliant
            : expiringSoon > 0 ? ComplianceState.AtRisk : ComplianceState.Compliant;
        return (state, percent);
    }

    /// <summary>The display label for a compliance state (matches the UI wording).</summary>
    public static string Label(ComplianceState state) => state switch
    {
        ComplianceState.Compliant => "Compliant",
        ComplianceState.AtRisk => "At risk",
        ComplianceState.NonCompliant => "Non-compliant",
        _ => "Pending",
    };
}
