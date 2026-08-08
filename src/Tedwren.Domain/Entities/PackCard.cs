using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A single qualification card as captured into a compliance pack — a <b>snapshot fixed at send</b> (R7): the
/// name, computed currency, expiry and verification wording are frozen at the moment the pack is created, so
/// the pack reads the same later even if the underlying card changes or is renewed.
/// </summary>
public sealed record PackCard(
    string QualificationName,
    string StatusLabel,
    CardStatus Status,
    DateOnly? ExpiresOn,
    string VerificationLabel);
