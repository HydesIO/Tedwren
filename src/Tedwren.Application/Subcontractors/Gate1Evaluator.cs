using Tedwren.Abstractions.Contracts.Subcontractors;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Subcontractors;

/// <summary>
/// Evaluates Gate 1 (Subcontractor Onboarding spec §2/§5): every document heading flagged "required before work"
/// must be present (a file uploaded) and valid (not expired) before the subcontractor's operatives can start.
/// Pure and shared by the main-contractor read (<see cref="SubcontractorOnboardingService"/>) and the
/// subcontractor's link flow (<see cref="Tedwren.Application.Trades.TradeOnboardingService"/>), so both decide the
/// gate identically against current data (R2/R3). "Valid" follows the spec's recommended rule: a matching
/// document is present and its expiry, if any, is in the future.
/// </summary>
public static class Gate1Evaluator
{
    /// <summary>Evaluates Gate 1 for a configuration against a company's documents as at <paramref name="today"/>.</summary>
    public static Gate1StatusDto Evaluate(
        SubcontractorOnboardingConfig config, IReadOnlyList<CompanyDocument> documents, DateOnly today)
    {
        var requirements = config.RequiredDocuments
            .Where(d => d.RequiredBeforeWork)
            .Select(d =>
            {
                var match = documents.FirstOrDefault(doc => Matches(doc.Type, d.Heading) || Matches(doc.Name, d.Heading));
                var present = match is not null && !string.IsNullOrWhiteSpace(match.FileReference);
                var valid = present && (match!.ExpiresOn is null || match.ExpiresOn.Value >= today);
                var issue = present ? (valid ? null : "Expired") : "Not uploaded";
                return new Gate1RequirementDto(d.Heading, valid, issue);
            })
            .ToList();

        return new Gate1StatusDto(requirements.All(r => r.Satisfied), requirements);
    }

    /// <summary>Case-insensitive heading match on a document's type or name.</summary>
    private static bool Matches(string? value, string heading) =>
        !string.IsNullOrWhiteSpace(value) && string.Equals(value.Trim(), heading.Trim(), StringComparison.OrdinalIgnoreCase);
}
