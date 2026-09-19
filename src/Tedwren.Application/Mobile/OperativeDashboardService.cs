using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;

namespace Tedwren.Application.Mobile;

/// <summary>
/// Builds the operative home dashboard (M3) from the workforce profile (name, compliance, next card expiry) and
/// the current week's hours (SUB-27). The signed-in state / current site (M4, attendance) and the forms-due
/// count (M6, forms engine) are placeholders until those phases land.
/// </summary>
public sealed class OperativeDashboardService : IOperativeDashboardService
{
    private readonly IMobileSurfaceService _surface;
    private readonly ITimesheetService _timesheets;

    /// <summary>Creates the service over the mobile surface (profile) and the timesheet service.</summary>
    public OperativeDashboardService(IMobileSurfaceService surface, ITimesheetService timesheets)
    {
        _surface = surface;
        _timesheets = timesheets;
    }

    /// <summary>Composes the dashboard for one operative, or null when they have no engagement in the company.</summary>
    public async Task<OperativeDashboardDto?> GetAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default)
    {
        var profile = await _surface.GetProfileAsync(companyId, personId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var hours = await _timesheets.GetOperativeHoursAsync(companyId, personId, CurrentWeekStart(), cancellationToken);

        // Soonest card expiry (earliest overall — a past date reads as overdue), mirroring the register roll-up.
        var nextExpiry = profile.Qualifications
            .Where(q => q.ExpiresOn is not null)
            .Select(q => q.ExpiresOn!.Value)
            .DefaultIfEmpty()
            .Min();

        return new OperativeDashboardDto(
            profile.Name,
            profile.State,
            profile.StatusLabel,
            hours.TotalHours,
            nextExpiry == default ? null : nextExpiry,
            FormsDue: 0,
            SignedIn: false,
            CurrentSiteId: null,
            CurrentSiteName: null);
    }

    /// <summary>The Monday (UTC date) of the current week — the timesheet week boundary (SUB-8).</summary>
    private static DateOnly CurrentWeekStart()
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysSinceMonday);
    }
}
