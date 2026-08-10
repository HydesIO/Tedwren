namespace Tedwren.Domain.Enums;

/// <summary>
/// How a qualification card was captured (SF-5). Cards are captured by photograph from a phone or by
/// desktop upload; a manual entry is recorded too so the provenance of every record is known.
/// </summary>
public enum CardCaptureSource
{
    /// <summary>Photographed on a phone.</summary>
    Photo = 0,

    /// <summary>Uploaded from a desktop.</summary>
    Upload = 1,

    /// <summary>Keyed in by a person (no image).</summary>
    Manual = 2,
}
