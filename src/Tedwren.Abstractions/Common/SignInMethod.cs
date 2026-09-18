namespace Tedwren.Abstractions.Common;

/// <summary>
/// Provider-neutral sign-in method carried on attendance DTOs (SF-13/SF-25), mirroring the domain enum
/// without the contracts depending on the domain.
/// </summary>
// Serialized by name on the API↔client wire (F21); persistence uses the separate Domain enums (ints).
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum SignInMethod
{
    /// <summary>Scanning a QR code at a fixed point of presence (SF-13).</summary>
    QrScan = 0,

    /// <summary>An assignment-tied link, for a scheme with nothing to scan (SF-25).</summary>
    AssignmentLink = 1,
}
