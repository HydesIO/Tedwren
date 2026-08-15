using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// An affiliate agreement (Web Plan §7): the terms an affiliate signs. Reached publicly by an unguessable
/// token (a random Guid), rendered as HTML, and signed with a drawn signature. Tedwren's countersignatory is
/// pre-filled. On signing, the affiliate's signature and a generated, immutable PDF are captured — the PDF is
/// fixed at signing so a later view always shows exactly what was agreed (mirrors the fixed-at-send principle
/// R7). No permanent public asset URL — the PDF is served only through the token-gated endpoint (R9).
/// </summary>
public sealed class AffiliateAgreement
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The affiliate this agreement is for.</summary>
    public required Guid AffiliateId { get; init; }

    /// <summary>The unguessable public token used to view/sign the agreement.</summary>
    public required string Token { get; init; }

    /// <summary>The agreement's lifecycle status.</summary>
    public AffiliateAgreementStatus Status { get; set; } = AffiliateAgreementStatus.Draft;

    /// <summary>The agreement terms as HTML (fixed at creation from the affiliate's commission plan).</summary>
    public required string TermsHtml { get; init; }

    /// <summary>Tedwren's countersignatory name (e.g. the director).</summary>
    public required string CountersignatoryName { get; init; }

    /// <summary>Tedwren's countersignatory title.</summary>
    public string? CountersignatoryTitle { get; init; }

    /// <summary>The affiliate's drawn signature as a PNG data URL, once signed.</summary>
    public string? SignatureDataUrl { get; set; }

    /// <summary>The name the affiliate typed/confirmed when signing.</summary>
    public string? SignedByName { get; set; }

    /// <summary>When the affiliate signed, if they have.</summary>
    public DateTimeOffset? SignedUtc { get; set; }

    /// <summary>The generated, immutable signed PDF bytes, captured at signing.</summary>
    public byte[]? PdfBytes { get; set; }

    /// <summary>When the agreement was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the agreement can still be signed (issued and not already signed).</summary>
    public bool IsSignable => Status != AffiliateAgreementStatus.Signed;
}
