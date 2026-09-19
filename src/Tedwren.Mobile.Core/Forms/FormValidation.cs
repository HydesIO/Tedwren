using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Mobile.Core.Forms;

/// <summary>
/// Client-side required-by-default validation (M6), mirroring the server rule in <c>FormSubmissionService</c>: every
/// required, value-capturing field must have a non-empty answer, or a file for Photo/FileUpload. It lets the fill
/// page block and point at what's missing before enqueueing; the server re-validates authoritatively on sync.
/// </summary>
public static class FormValidation
{
    private static readonly HashSet<string> DisplayOnly = new(StringComparer.OrdinalIgnoreCase) { "Heading", "Instruction" };
    private static readonly HashSet<string> FileKinds = new(StringComparer.OrdinalIgnoreCase) { "Photo", "FileUpload" };

    /// <summary>Returns the labels of required fields with no answer/file — empty when the form is complete.</summary>
    public static IReadOnlyList<string> MissingRequired(
        FormTemplateDto template,
        IReadOnlyList<FormAnswerDto> answers,
        IReadOnlyList<FormSubmissionFileInput> files)
    {
        var answered = answers.Where(HasValue).Select(a => a.FieldId).ToHashSet(StringComparer.Ordinal);
        var withFile = files.Select(f => f.FieldId).ToHashSet(StringComparer.Ordinal);

        var missing = new List<string>();
        foreach (var field in template.Sections.SelectMany(s => s.Fields))
        {
            if (!field.Required || DisplayOnly.Contains(field.Kind))
            {
                continue;
            }

            var satisfied = FileKinds.Contains(field.Kind) ? withFile.Contains(field.Id) : answered.Contains(field.Id);
            if (!satisfied)
            {
                missing.Add(field.Label);
            }
        }

        return missing;
    }

    private static bool HasValue(FormAnswerDto answer) =>
        !string.IsNullOrWhiteSpace(answer.Value) || answer.Values is { Count: > 0 };
}
