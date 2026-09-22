using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Qualifications;

/// <summary>
/// Evaluates Gate 3 (Subcontractor Onboarding spec §2/§5, SF-11): every legally-mandatory accreditation a trade
/// requires must be present on a current (non-superseded, in-date) qualification card before the operative can
/// work. Pure and side-effect-free — it takes already-loaded entities plus today's date and returns the decision —
/// mirroring <see cref="Tedwren.Application.Subcontractors.Gate1Evaluator"/> so the same logic backs the operative
/// "what you still need" view now and the site-entry check later (Phase 7). Advisory (non-mandatory) requirements
/// are reported but never block the gate.
/// </summary>
public static class Gate3Evaluator
{
    /// <summary>Evaluates Gate 3 for a set of trade requirements against a person's cards and the type library as at <paramref name="today"/>.</summary>
    public static Gate3StatusDto Evaluate(
        IReadOnlyList<TradeQualificationRequirement> requirements,
        IReadOnlyList<QualificationCard> cards,
        IReadOnlyList<QualificationType> types,
        DateOnly today)
    {
        var current = cards.Where(c => !c.IsSuperseded).ToList();
        var heldTypeIds = current
            .Where(c => c.GetStatus(today) != CardStatus.Expired)
            .Select(c => c.QualificationTypeId)
            .ToHashSet();
        var expiredTypeIds = current
            .Where(c => c.GetStatus(today) == CardStatus.Expired)
            .Select(c => c.QualificationTypeId)
            .ToHashSet();
        var typesById = types.ToDictionary(t => t.Id);

        var items = requirements
            .Select(r =>
            {
                var satisfied = heldTypeIds.Contains(r.QualificationTypeId);
                var issue = satisfied ? null : (expiredTypeIds.Contains(r.QualificationTypeId) ? "Expired" : "Not held");
                var name = typesById.TryGetValue(r.QualificationTypeId, out var t) ? t.Name : r.QualificationTypeId.ToString();
                return new Gate3RequirementDto(name, r.LegalMandatory, satisfied, issue);
            })
            .ToList();

        // Only legally-mandatory requirements block the gate; advisory ones are reported but do not fail it.
        var cleared = items.Where(i => i.LegalMandatory).All(i => i.Satisfied);
        return new Gate3StatusDto(cleared, items);
    }
}
