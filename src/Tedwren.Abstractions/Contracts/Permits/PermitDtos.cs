namespace Tedwren.Abstractions.Contracts.Permits;

/// <summary>An issued or draft permit to work, for the permits list.</summary>
public sealed record PermitDto(
    Guid Id,
    string PermitType,
    string? SiteName,
    string? ResponsiblePerson,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? Description,
    bool HighRisk,
    bool RamsAttached,
    string Status,
    DateTimeOffset CreatedUtc);

/// <summary>Request to raise a permit. <see cref="Issue"/> distinguishes a draft from an issued permit.</summary>
public sealed record CreatePermitRequest(
    Guid CompanyId,
    string PermitType,
    string? SiteName,
    string? ResponsiblePerson,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? Description,
    bool HighRisk,
    bool RamsAttached,
    bool Issue);
