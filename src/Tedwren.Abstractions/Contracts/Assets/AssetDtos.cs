namespace Tedwren.Abstractions.Contracts.Assets;

/// <summary>
/// A plant/equipment asset for the register list (PRD §8.2). <paramref name="CertificationStatus"/> is derived
/// from <paramref name="CertificationExpiry"/> (Valid / ExpiringSoon / Expired / None) so a lapsing certificate
/// is visible without opening the record.
/// </summary>
public sealed record AssetDto(
    Guid Id,
    string Name,
    string? AssetType,
    string? SerialNumber,
    string? Location,
    string? OwnerName,
    DateOnly? CertificationExpiry,
    DateOnly? NextInspectionDue,
    string? Notes,
    string Status,
    string CertificationStatus,
    DateTimeOffset CreatedUtc);

/// <summary>Request to add an asset to the register.</summary>
public sealed record CreateAssetRequest(
    string Name,
    string? AssetType,
    string? SerialNumber,
    string? Location,
    string? OwnerName,
    DateOnly? CertificationExpiry,
    DateOnly? NextInspectionDue,
    string? Notes);

/// <summary>Request to update an asset's details.</summary>
public sealed record UpdateAssetRequest(
    string Name,
    string? AssetType,
    string? SerialNumber,
    string? Location,
    string? OwnerName,
    DateOnly? CertificationExpiry,
    DateOnly? NextInspectionDue,
    string? Notes);
