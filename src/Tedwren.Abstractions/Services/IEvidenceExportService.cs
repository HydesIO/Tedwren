using Tedwren.Abstractions.Contracts.Evidence;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Unified compliance evidence export (PRD §8.2): assembles a company's compliance evidence — permits, RAMS, plant
/// register, safety events, HAVs and document acknowledgements — into a single downloadable pack (a multi-CSV ZIP)
/// for an ISO 45001 / project audit. Scoped to the company (R15); part of the paid HSE module.
/// </summary>
public interface IEvidenceExportService
{
    /// <summary>Returns the sections the export will contain and their record counts, without generating the file.</summary>
    Task<EvidenceSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Generates the evidence pack (a ZIP of one CSV per non-empty section plus a manifest).</summary>
    Task<EvidenceExportFileDto> BuildZipAsync(Guid companyId, CancellationToken cancellationToken = default);
}
