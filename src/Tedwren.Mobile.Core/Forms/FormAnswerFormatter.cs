using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Mobile.Core.Forms;

/// <summary>
/// Formats a submitted form answer for read-only review display (M7), mirroring the console renderer: multi-select
/// values are comma-joined, a data-URL value (a captured signature or photo) is flagged as an image, "true"/"false"
/// reads as Yes/No, and anything else is shown verbatim. Pure so the native review renderer and its tests share it.
/// </summary>
public static class FormAnswerFormatter
{
    /// <summary>Whether the answer's value is an inline image data URL (a captured signature or photo).</summary>
    public static bool IsImage(FormAnswerDto answer) =>
        answer.Value is { } value && value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase);

    /// <summary>The human-readable text for an answer (empty when it is an image or has no value).</summary>
    public static string Format(FormAnswerDto answer)
    {
        if (answer.Values is { Count: > 0 })
        {
            return string.Join(", ", answer.Values);
        }

        if (string.IsNullOrWhiteSpace(answer.Value) || IsImage(answer))
        {
            return string.Empty;
        }

        return answer.Value switch
        {
            "true" => "Yes",
            "false" => "No",
            _ => answer.Value,
        };
    }
}
