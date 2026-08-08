using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Attendance;

/// <summary>
/// Finds workers left signed in overnight and alerts the responsible manager, flagging the record for
/// correction rather than silently truncating it (SF-19). The flag is a new appended record referencing the
/// open sign-in (R4 — never edited). Idempotent: a sign-in is only flagged once.
/// </summary>
public sealed class OvernightSignInJob
{
    private readonly IAttendanceRepository _attendance;
    private readonly ISiteRepository _sites;
    private readonly ICompanyRepository _companies;
    private readonly IEmailSender _email;

    /// <summary>Creates the job over its repositories and the email sender.</summary>
    public OvernightSignInJob(IAttendanceRepository attendance, ISiteRepository sites, ICompanyRepository companies, IEmailSender email)
    {
        _attendance = attendance;
        _sites = sites;
        _companies = companies;
        _email = email;
    }

    /// <summary>Flags and alerts every open sign-in older than <paramref name="cutoffUtc"/>. Returns (flagged, alerts sent).</summary>
    public async Task<(int Flagged, int Notifications)> RunAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
    {
        var overnight = await _attendance.GetUnflaggedOvernightSignInsAsync(cutoffUtc, cancellationToken);
        var flagged = 0;
        var notifications = 0;

        foreach (var signIn in overnight)
        {
            // R4: append a flag referencing the sign-in; the sign-in itself is never truncated.
            await _attendance.AddAsync(new AttendanceRecord
            {
                PersonId = signIn.PersonId,
                SiteId = signIn.SiteId,
                PropertyId = signIn.PropertyId,
                Type = AttendanceEventType.OvernightFlag,
                Outcome = AttendanceOutcome.Flagged,
                CorrectsRecordId = signIn.Id,
                Reason = "Left signed in overnight — flagged for correction.",
            }, cancellationToken);
            flagged++;

            var site = await _sites.GetByIdAsync(signIn.SiteId, cancellationToken);
            if (site is null)
            {
                continue;
            }

            var company = await _companies.GetByIdAsync(site.CompanyId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(company?.ContactEmail))
            {
                await _email.SendAsync(
                    company!.ContactEmail!,
                    "Worker left signed in overnight",
                    $"A worker was left signed in at {site.Name} overnight and has been flagged for correction.",
                    cancellationToken);
                notifications++;
            }
        }

        return (flagged, notifications);
    }
}
