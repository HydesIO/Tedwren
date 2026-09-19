namespace Tedwren.Abstractions.Contracts.Documents;

/// <summary>A distributed document for the list, with its completion counts (PRD §8.2).</summary>
public sealed record DocumentDistributionDto(
    Guid Id,
    string Title,
    string? Category,
    string? Audience,
    bool HasFile,
    string SentBy,
    DateTimeOffset SentUtc,
    int SignedCount,
    int TotalCount);

/// <summary>One recipient's line in the completion matrix.</summary>
public sealed record DocumentAcknowledgementDto(
    Guid Id,
    string RecipientName,
    bool Acknowledged,
    DateTimeOffset? AcknowledgedUtc);

/// <summary>A distribution with its completion matrix (the acknowledgement rows).</summary>
public sealed record DocumentDistributionDetailDto(
    DocumentDistributionDto Distribution,
    IReadOnlyList<DocumentAcknowledgementDto> Acknowledgements);

/// <summary>Request to distribute a document to a set of recipients for acknowledgement.</summary>
public sealed record CreateDistributionRequest(
    string Title,
    string? Category,
    string? Audience,
    string? FileBase64,
    string? FileContentType,
    IReadOnlyList<string> Recipients);
