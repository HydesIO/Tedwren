namespace Tedwren.Domain.Enums;

/// <summary>
/// A configurable induction capture step (MC-3). The set is data-driven — a template chooses which steps it
/// includes and in what order — so this enumerates the known kinds without fixing any one induction's shape.
/// </summary>
public enum InductionStepKind
{
    /// <summary>Confirm identity details (MC-2).</summary>
    Identity = 0,

    /// <summary>Present/confirm qualification cards (MC-2).</summary>
    Cards = 1,

    /// <summary>Capture an emergency contact (MC-2).</summary>
    EmergencyContact = 2,

    /// <summary>Acknowledge a declaration (MC-2).</summary>
    Declaration = 3,

    /// <summary>Watch a video or read a document (MC-4).</summary>
    Media = 4,

    /// <summary>Capture a signature (MC-5).</summary>
    Signature = 5,
}
