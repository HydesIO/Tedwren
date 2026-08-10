using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// Deprecated in-proc <see cref="IAttendanceService"/> for the retained (test-only) client <c>Mock</c> mode.
/// It returns empty results and neutral outcomes so the Attendance console renders without a backend; the
/// real behaviour lives behind <see cref="ApiAttendanceService"/> in the standard <c>Api</c> mode.
/// </summary>
public sealed class ClientMockAttendanceService : IAttendanceService
{
    /// <summary>No-op sign-in (mock mode is deprecated and holds no attendance state).</summary>
    public Task<SignInResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SignInResult(false, "Refused", "Sign-in is unavailable in mock mode.", Guid.Empty, null));

    /// <summary>No-op sign-out.</summary>
    public Task<SignOutResult> SignOutAsync(SignOutRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SignOutResult(false, "Refused", "Sign-out is unavailable in mock mode.", null, Guid.Empty));

    /// <summary>No workers on site in mock mode.</summary>
    public Task<IReadOnlyList<OnSiteWorkerDto>> GetOnSiteAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OnSiteWorkerDto>>(Array.Empty<OnSiteWorkerDto>());

    /// <summary>Empty log in mock mode.</summary>
    public Task<IReadOnlyList<AttendanceRecordDto>> GetSiteRecordsAsync(Guid siteId, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AttendanceRecordDto>>(Array.Empty<AttendanceRecordDto>());
}
