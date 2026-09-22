using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Subcontractors;

/// <summary>The outcome of one RAMS review-cycle scan: how many configured subcontractors were evaluated and how many reminders were sent.</summary>
public sealed record RamsReviewReminderScanResult(int ConfigsEvaluated, int RemindersSent);

/// <summary>
/// The RAMS re-review reminder engine (Subcontractor Onboarding spec §4). This is <b>beyond PRD v6.4</b> — §8.2
/// RAMS review is per-submission — so it is gated behind the paid <c>subcontractor-onboarding</c> module and
/// fails closed when that is off (Q2). For each configured subcontractor with a review cycle and an approved live
/// RAMS, it works out the next re-review date (approval time + cycle months) and, once that date passes, emails
/// the main-contractor administrator a prompt to re-review. It is <b>reminder-only</b>: it never expires or
/// revokes an approval (append-only, R4/R16). Each configuration records when it was last reminded
/// (<see cref="SubcontractorOnboardingConfig.LastRamsReviewReminderUtc"/>), so re-running the scan within the same
/// due window never reminds twice (the same idempotency guarantee as SF-9). Data-store agnostic.
/// </summary>
public sealed class RamsReviewCycleReminderJob
{
    /// <summary>The paid module this beyond-PRD reminder sits behind (default off; the scan is a no-op for tenants that do not hold it).</summary>
    private const string ModuleKey = "subcontractor-onboarding";

    private readonly ISubcontractorOnboardingConfigRepository _configs;
    private readonly IRamsRepository _rams;
    private readonly ICompanyRepository _companies;
    private readonly IEntitlementService _entitlements;
    private readonly IEmailSender _email;

    /// <summary>Creates the job over the config, RAMS and company repositories, the entitlement gate and the email sender.</summary>
    public RamsReviewCycleReminderJob(
        ISubcontractorOnboardingConfigRepository configs,
        IRamsRepository rams,
        ICompanyRepository companies,
        IEntitlementService entitlements,
        IEmailSender email)
    {
        _configs = configs;
        _rams = rams;
        _companies = companies;
        _entitlements = entitlements;
        _email = email;
    }

    /// <summary>Evaluates every review-cycle configuration as of <paramref name="asOf"/> and sends any due reminders (idempotently per due window).</summary>
    public async Task<RamsReviewReminderScanResult> RunAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var configs = await _configs.GetWithReviewCycleAsync(cancellationToken);
        var sent = 0;

        // Group by inviting main contractor so each tenant's entitlement + administrator email are resolved once (R15).
        foreach (var byTenant in configs.GroupBy(c => c.InviterCompanyId))
        {
            var inviterCompanyId = byTenant.Key;

            // Beyond-PRD: no reminders unless the tenant holds the subcontractor-onboarding module (fail closed, Q2).
            if (!await _entitlements.IsEnabledAsync(inviterCompanyId, ModuleKey, cancellationToken))
            {
                continue;
            }

            var company = await _companies.GetByIdAsync(inviterCompanyId, cancellationToken);
            var recipient = company?.ContactEmail;

            foreach (var config in byTenant)
            {
                if (config.RamsFamilyId is not { } familyId || config.RamsReviewCycleMonths is not { } months)
                {
                    continue;
                }

                var family = await _rams.GetByFamilyAsync(inviterCompanyId, familyId, cancellationToken);
                var live = family.FirstOrDefault(r => r.IsLive);
                if (live?.ReviewedUtc is not { } approvedUtc)
                {
                    continue;   // no approved live version yet — nothing to re-review
                }

                if (RamsReviewCycle.DueUtc(months, approvedUtc) is not { } dueUtc || asOf < dueUtc)
                {
                    continue;   // not due yet
                }

                // Already reminded for this due window? (marker on/after the current due date.)
                if (config.LastRamsReviewReminderUtc is { } last && last >= dueUtc)
                {
                    continue;
                }

                // No administrator email configured yet: leave the marker unset so a reminder fires once one is
                // set (matching the recurring-form reminder). A missing contact is a configuration gap, not a send.
                if (string.IsNullOrWhiteSpace(recipient))
                {
                    continue;
                }

                await _email.SendAsync(recipient!.Trim(), ReminderSubject(live), ReminderMessage(live, months, dueUtc), cancellationToken);
                sent++;
                await _configs.UpdateReviewReminderAsync(config.Id, asOf, cancellationToken);
            }
        }

        return new RamsReviewReminderScanResult(configs.Count, sent);
    }

    /// <summary>The reminder email subject.</summary>
    private static string ReminderSubject(RamsSubmission live) => $"RAMS review due: {live.ContractorName}";

    /// <summary>The reminder email wording, naming the cycle and the due date.</summary>
    private static string ReminderMessage(RamsSubmission live, int months, DateTimeOffset dueUtc) =>
        $"The RAMS \"{live.Title}\" for {live.ContractorName} was approved on {live.ReviewedUtc:dd MMM yyyy} and is due " +
        $"for its {months}-monthly re-review (due {dueUtc:dd MMM yyyy}). Please re-review the current live version.";
}
