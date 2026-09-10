namespace Tedwren.Domain.Entities;

/// <summary>
/// A company-held document — an insurance, accreditation or policy (SUB-4). Each carries an optional expiry
/// date so the compliance roll-up can flag it as expiring or expired. The metadata is held here; when a file was
/// uploaded (e.g. a trade's RAMS/insurance PDF via self-service onboarding, UAT-023), <see cref="FileReference"/>
/// points at the stored bytes in the private image/blob store (R9), served only through an authorised endpoint.
/// </summary>
public sealed class CompanyDocument
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The owning company (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Display name for the document (e.g. "Employer's Liability Insurance").</summary>
    public required string Name { get; set; }

    /// <summary>The document type/category (e.g. "Insurance", "Accreditation", "Policy").</summary>
    public required string Type { get; set; }

    /// <summary>When the document expires, if it has an expiry (date-only, R11).</summary>
    public DateOnly? ExpiresOn { get; set; }

    /// <summary>An optional reference (policy/certificate number).</summary>
    public string? Reference { get; set; }

    /// <summary>The stored-file reference (image/blob store id) when a file was uploaded; null for metadata-only docs (UAT-023, R9).</summary>
    public string? FileReference { get; set; }

    /// <summary>When the record was created (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
