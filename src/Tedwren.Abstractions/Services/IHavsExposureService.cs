using Tedwren.Abstractions.Contracts.Havs;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Hand-arm vibration (HAVs) exposure monitoring (PRD §8.2): record a person's daily tool usages and derive the
/// daily A(8) exposure, points and band against the HSE action/limit values. Scoped to the company (R15); part of
/// the paid HSE module.
/// </summary>
public interface IHavsExposureService
{
    /// <summary>Records a person's daily HAVs exposure and returns it with its derived A(8)/points/band.</summary>
    Task<HavsExposureRecordDto> RecordAsync(Guid companyId, string recordedBy, CreateHavsExposureRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's HAVs exposure records, newest first.</summary>
    Task<IReadOnlyList<HavsExposureRecordDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single HAVs exposure record, or null when missing/cross-tenant (R15).</summary>
    Task<HavsExposureRecordDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
}
