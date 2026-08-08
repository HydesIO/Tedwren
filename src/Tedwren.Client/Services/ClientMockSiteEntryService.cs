using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ISiteEntryService"/> for client <c>DataSource=Mock</c>. It fabricates three demo operatives — one
/// clear, one with an expired card, one without a valid induction — so the five-check fail-closed decision
/// (R2), actionable block reasons (MC-9), the day-only manager override (MC-11) and the muster with competency
/// cover (MC-13/MC-14) are all demonstrable in-proc without a backend.
/// </summary>
public sealed class ClientMockSiteEntryService : ISiteEntryService
{
    /// <summary>The demo site the gate operates on.</summary>
    public static readonly Guid DemoSiteId = Guid.Parse("77777777-7777-4777-8777-000000000001");

    /// <summary>The demo operatives offered at the gate (id, name, profile).</summary>
    public static readonly IReadOnlyList<(Guid Id, string Name)> DemoOperatives = new[]
    {
        (Guid.Parse("77777777-7777-4777-8777-000000000010"), "M. Adeyemi (clear)"),
        (Guid.Parse("77777777-7777-4777-8777-000000000011"), "J. Fletcher (expired card)"),
        (Guid.Parse("77777777-7777-4777-8777-000000000012"), "R. Okafor (no induction)"),
    };

    private static readonly Guid Clear = DemoOperatives[0].Id;
    private static readonly Guid ExpiredCard = DemoOperatives[1].Id;
    private static readonly Guid NoInduction = DemoOperatives[2].Id;

    /// <summary>Runs the mock five-check decision for the demo operative (MC-8/MC-9/MC-11/R2/R10).</summary>
    public Task<EntryDecisionResultDto> DecideAsync(DecideEntryRequest request, CancellationToken cancellationToken = default)
    {
        var checks = new List<DecisionCheckResultDto>
        {
            new("Registered", "Passed", "Registered and active with this company"),
            new("Not signed in elsewhere", "Passed", "Not signed in at any other site"),
            new("Induction valid", request.PersonId == NoInduction ? "Failed" : "Passed",
                request.PersonId == NoInduction ? "No completed induction on record" : "Inducted (reference IND-DEMO)"),
            new("Cards in date & confirmed", request.PersonId == ExpiredCard ? "Failed" : "Passed",
                request.PersonId == ExpiredCard ? "Expired card(s): CSCS — Skilled Worker" : "All cards in date and confirmed"),
            new("RAMS", "NotRun", "RAMS module not held — check does not apply"),
        };

        var admittedByChecks = checks.All(c => c.Outcome != "Failed");
        var wasOverridden = false;
        if (!admittedByChecks && request.Override is { } ov)
        {
            checks.Add(new DecisionCheckResultDto("Manager override", "Passed", $"Overridden by {ov.By} (day only): {ov.Reason}"));
            wasOverridden = true;
        }

        var admitted = admittedByChecks || wasOverridden;
        var blockReason = admitted
            ? null
            : string.Join("; ", checks.Where(c => c.Outcome == "Failed").Select(c => c.Detail));

        var result = new EntryDecisionResultDto(admitted, blockReason, wasOverridden, Guid.NewGuid(), ElapsedMs: 8, checks);
        return Task.FromResult(result);
    }

    /// <summary>Returns a fabricated muster with a first-aider present (MC-12–MC-14).</summary>
    public Task<MusterDto> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var people = new[]
        {
            new MusterPersonDto(Clear, "M. Adeyemi", null, null, DateTimeOffset.UtcNow.AddHours(-2)),
            new MusterPersonDto(NoInduction, "R. Okafor", null, null, DateTimeOffset.UtcNow.AddHours(-1)),
        };
        var competencies = new[] { new CompetencyCoverDto("First Aid", true, 1) };
        return Task.FromResult(new MusterDto(siteId, DateTimeOffset.UtcNow, people, competencies));
    }
}
