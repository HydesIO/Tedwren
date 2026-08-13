using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Attendance;
using Tedwren.Application.Expiry;
using Tedwren.Application.Forms;
using Tedwren.Application.Jobs;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the expiry-engine job endpoints (SF-9, SUB-5, SF-21/R12) and the expiry read endpoint. The job
/// triggers let ops/tests run a scan on demand and observe the outbox and run history; the scheduler runs
/// them automatically in a real deployment.
/// </summary>
public static class JobEndpoints
{
    /// <summary>Registers the <c>/api/jobs</c> and <c>/api/expiry</c> endpoint groups.</summary>
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/api/jobs").WithTags("Jobs");

        // Runs the expiry-warning scan now (SF-9), recorded as a job run (SF-21).
        jobs.MapPost("/expiry-scan", async (JobRunner runner, ExpiryWarningJob job, CancellationToken cancellationToken) =>
            {
                var today = Today();
                ExpiryScanResultDto result = new(0, 0);
                await runner.RunAsync(JobNames.ExpiryScan, async token =>
                {
                    result = await job.RunAsync(today, token);
                    return (result.CardsEvaluated, result.NotificationsSent);
                }, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("RunExpiryScan");

        // Runs the weekly digest now (SUB-5), recorded as a job run (SF-21).
        jobs.MapPost("/weekly-digest", async (JobRunner runner, WeeklyDigestJob job, CancellationToken cancellationToken) =>
            {
                var today = Today();
                DigestResultDto result = new(0, 0);
                await runner.RunAsync(JobNames.WeeklyDigest, async token =>
                {
                    result = await job.RunAsync(today, token);
                    return (result.CompaniesProcessed, result.EmailsSent);
                }, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("RunWeeklyDigest");

        // Flags workers left signed in overnight and alerts their manager (SF-19).
        jobs.MapPost("/overnight-check", async (JobRunner runner, OvernightSignInJob job, CancellationToken cancellationToken) =>
            {
                var cutoff = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
                var flagged = 0;
                var notifications = 0;
                await runner.RunAsync(JobNames.OvernightCheck, async token =>
                {
                    (flagged, notifications) = await job.RunAsync(cutoff, token);
                    return (flagged, notifications);
                }, cancellationToken);
                return Results.Ok(new { flagged, notifications });
            })
            .WithName("RunOvernightCheck");

        // Reminds admins of recurring forms not completed this period (PRD-Phase 2, R12), recorded as a job run.
        jobs.MapPost("/form-reminders", async (JobRunner runner, RecurringFormReminderJob job, CancellationToken cancellationToken) =>
            {
                FormReminderScanResult result = new(0, 0);
                await runner.RunAsync(JobNames.FormReminder, async token =>
                {
                    result = await job.RunAsync(DateTimeOffset.UtcNow, token);
                    return (result.AssignmentsEvaluated, result.RemindersSent);
                }, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("RunFormReminders");

        // Checks each job's heartbeat and alerts ops on a silent stop (R12).
        jobs.MapPost("/heartbeat-check", async (JobHeartbeatMonitor monitor, CancellationToken cancellationToken) =>
                Results.Ok(new HeartbeatResultDto(await monitor.CheckAsync(DateTimeOffset.UtcNow, cancellationToken))))
            .WithName("RunHeartbeatCheck");

        // Recent job runs (SF-21 visibility).
        jobs.MapGet("/runs", async (IExpiryQueryService query, CancellationToken cancellationToken) =>
                Results.Ok(await query.GetRecentJobRunsAsync(20, cancellationToken)))
            .WithName("GetJobRuns");

        // What the stub senders "sent" (dev/mock observability).
        jobs.MapGet("/outbox", (INotificationOutbox outbox) => Results.Ok(outbox.Messages))
            .WithName("GetOutbox");

        var expiry = app.MapGroup("/api/expiry").WithTags("Expiry");
        expiry.MapGet("/upcoming", async (int? withinDays, IExpiryQueryService query, CancellationToken cancellationToken) =>
                Results.Ok(await query.GetUpcomingAsync(withinDays ?? 60, cancellationToken)))
            .WithName("GetUpcomingExpiries");

        return app;
    }

    /// <summary>Today's date (UTC; card expiry is date-only).</summary>
    private static DateOnly Today() => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
}
