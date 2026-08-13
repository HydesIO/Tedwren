namespace Tedwren.Domain.Entities;

/// <summary>
/// A single captured answer within a form submission, keyed by the field id it answers. <see cref="Value"/>
/// carries single-valued answers (text, number, date, RAG, yes/no, a signature data URL); <see cref="Values"/>
/// carries multi-select answers. A value record so a submission's answers serialise cleanly to JSON.
/// </summary>
/// <param name="FieldId">The field this answers (matches a <see cref="FormField"/> id in the template version).</param>
/// <param name="Value">The single-valued answer, when applicable.</param>
/// <param name="Values">The chosen values for a multi-select answer, when applicable.</param>
public sealed record FormAnswer(
    string FieldId,
    string? Value,
    IReadOnlyList<string> Values);
