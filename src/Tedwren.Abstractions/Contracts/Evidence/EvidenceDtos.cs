namespace Tedwren.Abstractions.Contracts.Evidence;

/// <summary>One section of the unified compliance evidence export, with how many records it holds (PRD §8.2).</summary>
public sealed record EvidenceSectionDto(string Name, int Count);

/// <summary>
/// A summary of what the unified compliance evidence export will contain (PRD §8.2) — the sections spanning
/// permits, RAMS, plant, safety events, HAVs and document acknowledgements, and their record counts. Lets the UI
/// show the auditor exactly what the downloadable pack covers before generating it.
/// </summary>
public sealed record EvidenceSummaryDto(
    IReadOnlyList<EvidenceSectionDto> Sections,
    int TotalRecords,
    DateTimeOffset GeneratedUtc);

/// <summary>A generated evidence-export file (the multi-CSV ZIP) ready to download (PRD §8.2).</summary>
public sealed record EvidenceExportFileDto(string FileName, string ContentType, byte[] Content);
