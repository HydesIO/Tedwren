namespace Tedwren.Domain.Enums;

/// <summary>
/// How far a qualification card's authenticity has been established (SF-7). The interface must show
/// these three states distinctly — they must not look the same — because each carries a different
/// level of assurance.
/// </summary>
public enum CardVerificationState
{
    /// <summary>Read from a photograph/upload but not yet checked by a person.</summary>
    ReadUnchecked = 0,

    /// <summary>Checked and confirmed by a named person at the customer (SF-6).</summary>
    CustomerChecked = 1,

    /// <summary>Verified live against CSCS (PRD-Phase 1). Modelled now; not produced until that phase.</summary>
    CscsVerified = 2,
}
