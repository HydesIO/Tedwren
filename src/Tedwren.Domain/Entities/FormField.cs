using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A single field descriptor within a form template (the PRD-Phase 2 checklist/inspection engine). It
/// is a value record — a template is an ordered list of these — so it serialises cleanly to JSON and is
/// safe to share. Per-kind extensible config is carried as JSON: <paramref name="ValidationJson"/> for
/// validators (min/max, length, regex, decimal places) and <paramref name="OptionsJson"/> for the option
/// lists of dropdown/multi-select/RAG fields.
/// </summary>
/// <param name="Id">Stable field identifier within the template (used as the answer key).</param>
/// <param name="Kind">The field kind the author chose.</param>
/// <param name="Label">The field's display label.</param>
/// <param name="HelpText">Optional helper text shown under the label.</param>
/// <param name="Required">Whether an answer is mandatory (required-validated by default).</param>
/// <param name="ValidationJson">Optional per-kind validators as JSON.</param>
/// <param name="OptionsJson">Optional option list (dropdown/multi-select/RAG) as JSON.</param>
/// <param name="Order">The field's position within its section.</param>
public sealed record FormField(
    string Id,
    FormFieldKind Kind,
    string Label,
    string? HelpText,
    bool Required,
    string? ValidationJson,
    string? OptionsJson,
    int Order);
