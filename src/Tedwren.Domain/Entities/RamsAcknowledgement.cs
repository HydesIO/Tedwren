namespace Tedwren.Domain.Entities;

/// <summary>
/// A record that an operative has read and signed a specific live version of a subcontractor's RAMS (Subcontractor
/// Onboarding spec Gate 5). It pins the exact <see cref="FamilyId"/> + <see cref="Version"/> signed, so a resubmission
/// (a new live version) invalidates the old signature and the operative must sign again. Append-only (R4/R16); scoped
/// to the reviewing main contractor (R15). <see cref="ExpiresUtc"/> carries the re-sign deadline when the MC runs a
/// RAMS review cycle (SO-4), so a signature can also lapse over time; null means it stays valid until the live version
/// changes.
/// </summary>
public sealed class RamsAcknowledgement
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The main contractor whose RAMS this signs (R15) — matches <see cref="RamsSubmission.CompanyId"/>.</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The operative who signed (identity is the person, SF-1).</summary>
    public required Guid PersonId { get; init; }

    /// <summary>The RAMS family signed (groups all versions).</summary>
    public required Guid FamilyId { get; init; }

    /// <summary>The exact live version signed; a later live version supersedes this signature (the operative re-signs).</summary>
    public required int Version { get; init; }

    /// <summary>The name the operative signed with.</summary>
    public required string SignatureName { get; init; }

    /// <summary>When it was signed (UTC, R11).</summary>
    public DateTimeOffset SignedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the signature lapses under the MC's RAMS review cycle (SO-4), or null when it does not time-expire.</summary>
    public DateTimeOffset? ExpiresUtc { get; init; }

    /// <summary>Whether the signature is still current at a point in time (it has not passed its review-cycle expiry).</summary>
    public bool IsValid(DateTimeOffset now) => ExpiresUtc is not { } expiry || now <= expiry;
}
