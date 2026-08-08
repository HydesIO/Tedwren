namespace Tedwren.Domain.Entities;

/// <summary>
/// One operative included in a compliance pack, with their compliance snapshot fixed at send (R7). The name
/// is the one recorded by the sending company (names live on the engagement, not the person).
/// </summary>
public sealed record PackSubject(
    Guid PersonId,
    string Name,
    IReadOnlyList<PackCard> Cards);
