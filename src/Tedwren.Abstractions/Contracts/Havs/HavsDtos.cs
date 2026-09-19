namespace Tedwren.Abstractions.Contracts.Havs;

/// <summary>One tool's contribution to a day's hand-arm vibration exposure (PRD §8.2).</summary>
public sealed record HavsToolUsageDto(
    string ToolName,
    double MagnitudeMs2,
    int TriggerMinutes);

/// <summary>
/// A day's HAVs exposure for one person, with the derived daily A(8), exposure points and band (PRD §8.2). The
/// derived values come from the tool usages via the HSE methodology; <see cref="Band"/> is the enum name.
/// </summary>
public sealed record HavsExposureRecordDto(
    Guid Id,
    string PersonName,
    DateOnly ExposureDate,
    IReadOnlyList<HavsToolUsageDto> ToolUsages,
    double DailyExposureA8,
    int ExposurePoints,
    string Band,
    string RecordedBy,
    DateTimeOffset RecordedUtc);

/// <summary>Request to record a person's HAVs exposure for a day from its tool usages.</summary>
public sealed record CreateHavsExposureRequest(
    string PersonName,
    DateOnly ExposureDate,
    IReadOnlyList<HavsToolUsageDto> ToolUsages);
