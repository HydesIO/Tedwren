using Tedwren.Abstractions.Contracts.Evidence;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Reads operatives' field evidence captures (M5) for the manager review surface (M7). All reads are scoped to
/// the reviewing manager's company (R15); the M5 write path lives on the operative plane (<c>/api/mobile/evidence</c>).
/// Photos are referenced only by their image-store id and served through the authorised image route (R9).
/// </summary>
public interface IEvidenceCaptureQueryService
{
    /// <summary>Returns a company's evidence captures, newest first; optionally filtered to one operative.</summary>
    Task<IReadOnlyList<EvidenceCaptureDto>> GetForCompanyAsync(Guid companyId, Guid? personId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns a single capture the company owns, or null when it is missing or belongs to another company (R15).</summary>
    Task<EvidenceCaptureDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
}
