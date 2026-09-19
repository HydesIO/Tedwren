using System.Text.Json;
using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Havs;

/// <summary>
/// The store-agnostic HAVs exposure service (PRD §8.2). A record captures a person's tool usages for a day; the
/// daily A(8), exposure points and band are derived by <see cref="HavsCalculator"/> (HSE methodology), never
/// stored. The tool usages are persisted as JSON. Everything is scoped to the company (R15); records are
/// append-only evidence.
/// </summary>
public sealed class HavsExposureService : IHavsExposureService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHavsExposureRepository _records;

    /// <summary>Creates the service over the HAVs repository.</summary>
    public HavsExposureService(IHavsExposureRepository records) => _records = records;

    /// <summary>Records a person's daily HAVs exposure and returns it with its derived A(8)/points/band.</summary>
    public async Task<HavsExposureRecordDto> RecordAsync(Guid companyId, string recordedBy, CreateHavsExposureRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.PersonName))
        {
            throw new ArgumentException("A person name is required.", nameof(request));
        }

        var usages = (request.ToolUsages ?? new List<HavsToolUsageDto>())
            .Where(u => !string.IsNullOrWhiteSpace(u.ToolName) && u.MagnitudeMs2 > 0 && u.TriggerMinutes > 0)
            .Select(u => new HavsToolUsageDto(u.ToolName.Trim(), u.MagnitudeMs2, u.TriggerMinutes))
            .ToList();
        if (usages.Count == 0)
        {
            throw new ArgumentException("At least one tool usage (with a magnitude and trigger time) is required.", nameof(request));
        }

        var record = new HavsExposureRecord
        {
            CompanyId = companyId,
            PersonName = request.PersonName.Trim(),
            ExposureDate = request.ExposureDate,
            ToolUsagesJson = JsonSerializer.Serialize(usages, JsonOptions),
            RecordedBy = string.IsNullOrWhiteSpace(recordedBy) ? "System" : recordedBy.Trim(),
        };
        await _records.AddAsync(record, cancellationToken);
        return ToDto(record);
    }

    /// <summary>Returns a company's HAVs exposure records, newest first.</summary>
    public async Task<IReadOnlyList<HavsExposureRecordDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        (await _records.GetByCompanyAsync(companyId, cancellationToken)).Select(ToDto).ToList();

    /// <summary>Returns a single HAVs exposure record, or null when missing/cross-tenant (R15).</summary>
    public async Task<HavsExposureRecordDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _records.GetAsync(id, cancellationToken);
        return record is null || record.CompanyId != companyId ? null : ToDto(record);
    }

    /// <summary>Maps a record to its DTO, deserialising the tool usages and deriving the A(8)/points/band.</summary>
    private static HavsExposureRecordDto ToDto(HavsExposureRecord r)
    {
        var usages = JsonSerializer.Deserialize<List<HavsToolUsageDto>>(r.ToolUsagesJson, JsonOptions) ?? new List<HavsToolUsageDto>();
        var result = HavsCalculator.Compute(usages);
        return new HavsExposureRecordDto(
            r.Id, r.PersonName, r.ExposureDate, usages,
            result.DailyExposureA8, result.ExposurePoints, result.Band.ToString(), r.RecordedBy, r.RecordedUtc);
    }
}
