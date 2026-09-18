namespace Tedwren.Abstractions.Common;

/// <summary>
/// Provider-neutral verification state carried on qualification-card DTOs (SF-7), mirroring the domain
/// enum without the contracts depending on the domain. The client renders the three states distinctly.
/// </summary>
// Serialized by name on the API↔client wire (F21); persistence uses the separate Domain enums (ints).
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum CardVerificationState
{
    /// <summary>Read from a photo/upload but not yet checked by a person.</summary>
    ReadUnchecked = 0,

    /// <summary>Confirmed by a named person at the customer (SF-6).</summary>
    CustomerChecked = 1,

    /// <summary>Verified live against CSCS (PRD-Phase 1).</summary>
    CscsVerified = 2,
}
