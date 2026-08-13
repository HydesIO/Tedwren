using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Forms;

/// <summary>The outcome of one recurring-reminder scan: how many recurring assignments were evaluated and how many reminders were sent.</summary>
public sealed record FormReminderScanResult(int AssignmentsEvaluated, int RemindersSent);

/// <summary>
/// The recurring-form reminder engine (PRD-Phase 2 checklist scheduling, R12). For every assignment on a Daily,
/// Weekly or Monthly cadence it works out the current period, and — if no submission of that form family has been
/// completed within the period — emails a "form due" reminder to the company administrator (and the assignment's
/// alert address, when set). Each assignment records when it was last reminded (<see cref="FormAssignment.LastReminderUtc"/>),
/// so running the scan more than once in a period never reminds twice (the same idempotency guarantee as SF-9).
/// Data-store agnostic: the same logic runs over the in-memory and Dapper repositories.
/// </summary>
public sealed class RecurringFormReminderJob
{
    private readonly IFormAssignmentRepository _assignments;
    private readonly IFormSubmissionRepository _submissions;
    private readonly IFormTemplateRepository _templates;
    private readonly ICompanyRepository _companies;
    private readonly IEmailSender _email;

    /// <summary>Creates the job over its repositories and the email provider.</summary>
    public RecurringFormReminderJob(
        IFormAssignmentRepository assignments,
        IFormSubmissionRepository submissions,
        IFormTemplateRepository templates,
        ICompanyRepository companies,
        IEmailSender email)
    {
        _assignments = assignments;
        _submissions = submissions;
        _templates = templates;
        _companies = companies;
        _email = email;
    }

    /// <summary>Evaluates every recurring assignment as of <paramref name="asOf"/> and sends any due reminders (idempotently per period).</summary>
    public async Task<FormReminderScanResult> RunAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var recurring = await _assignments.GetRecurringAsync(cancellationToken);
        var sent = 0;

        // Group by company so the submissions and administrator email for each tenant are fetched once (R15).
        foreach (var byCompany in recurring.GroupBy(a => a.CompanyId))
        {
            var companyId = byCompany.Key;
            var submissions = await _submissions.GetByCompanyAsync(companyId, cancellationToken);
            var company = await _companies.GetByIdAsync(companyId, cancellationToken);

            foreach (var assignment in byCompany)
            {
                var periodStart = PeriodStart(assignment.Schedule, asOf);

                // Already reminded for this period? (LastReminderUtc falls on/after the current period's start.)
                if (assignment.LastReminderUtc is { } last && last >= periodStart)
                {
                    continue;
                }

                // Resolve every version id of the assigned family, so a submission against any version counts.
                var versions = await _templates.GetByFamilyAsync(companyId, assignment.FormTemplateFamilyId, cancellationToken);
                var versionIds = versions.Select(v => v.Id).ToHashSet();

                var completedThisPeriod = submissions.Any(s => versionIds.Contains(s.FormTemplateId) && s.SubmittedUtc >= periodStart);
                if (completedThisPeriod)
                {
                    continue;
                }

                var recipients = Recipients(assignment, company);
                if (recipients.Count == 0)
                {
                    // Nothing to send to yet; leave the marker unset so a reminder fires once a contact is configured.
                    continue;
                }

                foreach (var recipient in recipients)
                {
                    await _email.SendAsync(recipient, ReminderSubject(assignment), ReminderMessage(assignment, asOf), cancellationToken);
                    sent++;
                }

                assignment.LastReminderUtc = asOf;
                await _assignments.UpdateLastReminderAsync(assignment.Id, asOf, cancellationToken);
            }
        }

        return new FormReminderScanResult(recurring.Count, sent);
    }

    /// <summary>The start (UTC midnight) of the cadence's current period: the day, the ISO week (Monday), or the calendar month.</summary>
    private static DateTimeOffset PeriodStart(FormSchedule schedule, DateTimeOffset asOf)
    {
        var today = asOf.UtcDateTime.Date;
        var start = schedule switch
        {
            FormSchedule.Daily => today,
            FormSchedule.Weekly => today.AddDays(-((int)today.DayOfWeek + 6) % 7),   // back to Monday
            FormSchedule.Monthly => new DateTime(today.Year, today.Month, 1),
            _ => today,
        };
        return new DateTimeOffset(start, TimeSpan.Zero);
    }

    /// <summary>The distinct reminder recipients: the assignment's alert address (when set) and the company administrator.</summary>
    private static IReadOnlyList<string> Recipients(FormAssignment assignment, Company? company)
    {
        var recipients = new List<string>();
        if (!string.IsNullOrWhiteSpace(assignment.FailureAlertEmail))
        {
            recipients.Add(assignment.FailureAlertEmail!.Trim());
        }

        if (!string.IsNullOrWhiteSpace(company?.ContactEmail))
        {
            recipients.Add(company!.ContactEmail!.Trim());
        }

        return recipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The reminder email subject.</summary>
    private static string ReminderSubject(FormAssignment assignment) => $"Form due: {assignment.FormName}";

    /// <summary>The reminder email wording, naming the cadence and the current period.</summary>
    private static string ReminderMessage(FormAssignment assignment, DateTimeOffset asOf)
    {
        var cadence = assignment.Schedule switch
        {
            FormSchedule.Daily => "today",
            FormSchedule.Weekly => "this week",
            FormSchedule.Monthly => "this month",
            _ => "for this period",
        };
        return $"The {assignment.Schedule.ToString().ToLowerInvariant()} form \"{assignment.FormName}\" has not been completed {cadence} " +
               $"(as of {asOf:dd MMM yyyy}). Please complete it to stay compliant.";
    }
}
