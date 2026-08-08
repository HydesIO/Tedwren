using Tedwren.Abstractions.Contracts.Attendance;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The sign-in / sign-out and attendance service contract (SF-13–SF-19), shared by the client and the API.
/// Verifies location against the site boundary, applies the customer's location policy, records every
/// attempt including refusals, prevents being present at two sites at once, and produces an append-only log.
/// </summary>
public interface IAttendanceService
{
    /// <summary>Records a sign-in attempt and returns its outcome (SF-13/14/15/16/18/25).</summary>
    Task<SignInResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default);

    /// <summary>Records a sign-out attempt and returns its outcome, with the duration on site (SF-17).</summary>
    Task<SignOutResult> SignOutAsync(SignOutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the workers currently present on a site.</summary>
    Task<IReadOnlyList<OnSiteWorkerDto>> GetOnSiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>Returns the recent attendance records for a site (the append-only log).</summary>
    Task<IReadOnlyList<AttendanceRecordDto>> GetSiteRecordsAsync(Guid siteId, int take, CancellationToken cancellationToken = default);
}
