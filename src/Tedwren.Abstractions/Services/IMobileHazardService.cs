using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Safety;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Server-only write for an operative's offline-captured hazard / near-miss report (M5), reusing the HSE hazard
/// domain (PRD §8.2) without touching the console <see cref="IHazardReportService"/> (SRP — the client implements
/// that one). Gated by the <c>hse</c> module at the endpoint. CompanyId + reporter come from the token (R15); the
/// device-generated client id makes a retried sync idempotent (R4/R16).
/// </summary>
public interface IMobileHazardService
{
    /// <summary>Records a hazard report (idempotent on the request's client id), returning the stored report.</summary>
    Task<HazardReportDto> ReportAsync(Guid companyId, string reportedBy, MobileReportHazardRequest request, CancellationToken cancellationToken = default);
}
