using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Server-only write for an operative's offline-captured field evidence (M5) — ungated (every operative). The
/// CompanyId + PersonId come from the operative token (never the body, R15); the device-generated client id makes a
/// retried sync idempotent (R4/R16). Kept separate from any console service so it stays cohesive (SRP).
/// </summary>
public interface IMobileEvidenceService
{
    /// <summary>Records an evidence item (idempotent on the request's client id), returning the stored item.</summary>
    Task<EvidenceItemDto> ReportAsync(Guid companyId, Guid personId, MobileReportEvidenceRequest request, CancellationToken cancellationToken = default);
}
