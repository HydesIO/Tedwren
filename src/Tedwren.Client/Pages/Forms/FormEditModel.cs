using System.Text.Json;
using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Client.Pages.Forms;

/// <summary>
/// Client-side editable model for the form builder (PRD-Phase 2). Mirrors the <see cref="FormSectionDto"/> /
/// <see cref="FormFieldDto"/> contracts as mutable classes the UI can bind to, with mapping to/from the DTOs and
/// the small set of authorable field kinds. Options for choice fields are edited as a comma-separated string and
/// stored as a JSON array in <c>OptionsJson</c>.
/// </summary>
public static class FormEditModel
{
    /// <summary>The field kinds an author can choose, with display labels.</summary>
    public static IReadOnlyList<(string Kind, string Label)> Kinds { get; } = new[]
    {
        ("ShortText", "Short text"),
        ("LongText", "Long text"),
        ("Number", "Number"),
        ("Date", "Date"),
        ("Time", "Time"),
        ("Dropdown", "Dropdown (single choice)"),
        ("MultiSelect", "Multi-select"),
        ("YesNo", "Yes / No"),
        ("RagStatus", "Red / Amber / Green"),
        ("Photo", "Photo"),
        ("FileUpload", "File upload"),
        ("Signature", "Signature"),
        ("Heading", "Heading (display only)"),
        ("Instruction", "Instruction (display only)"),
    };

    /// <summary>Kinds whose options the author supplies.</summary>
    public static bool UsesOptions(string kind) => kind is "Dropdown" or "MultiSelect";

    /// <summary>Builds the editable section list from a loaded template (or a single empty section for a new form).</summary>
    public static List<SectionEdit> FromDto(IReadOnlyList<FormSectionDto>? sections)
    {
        if (sections is null || sections.Count == 0)
        {
            return new List<SectionEdit> { new() { Title = "Section 1" } };
        }

        return sections
            .OrderBy(s => s.Order)
            .Select(s => new SectionEdit
            {
                Id = s.Id,
                Title = s.Title,
                Fields = s.Fields.OrderBy(f => f.Order).Select(f => new FieldEdit
                {
                    Id = f.Id,
                    Kind = f.Kind,
                    Label = f.Label,
                    HelpText = f.HelpText,
                    Required = f.Required,
                    OptionsCsv = OptionsToCsv(f.OptionsJson),
                }).ToList(),
            })
            .ToList();
    }

    /// <summary>Maps the editable section list back to the DTO the API expects, assigning order from position.</summary>
    public static List<FormSectionDto> ToDto(IReadOnlyList<SectionEdit> sections) =>
        sections.Select((s, si) => new FormSectionDto(
            string.IsNullOrWhiteSpace(s.Id) ? Guid.NewGuid().ToString("N") : s.Id,
            string.IsNullOrWhiteSpace(s.Title) ? $"Section {si + 1}" : s.Title.Trim(),
            s.Fields.Select((f, fi) => new FormFieldDto(
                string.IsNullOrWhiteSpace(f.Id) ? Guid.NewGuid().ToString("N") : f.Id,
                f.Kind,
                string.IsNullOrWhiteSpace(f.Label) ? "Untitled field" : f.Label.Trim(),
                string.IsNullOrWhiteSpace(f.HelpText) ? null : f.HelpText.Trim(),
                f.Required,
                null,
                UsesOptions(f.Kind) ? CsvToOptionsJson(f.OptionsCsv) : null,
                fi)).ToList(),
            si)).ToList();

    /// <summary>Reads a JSON option array into a comma-separated string for editing.</summary>
    private static string OptionsToCsv(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return string.Empty;
        try
        {
            var options = JsonSerializer.Deserialize<List<string>>(optionsJson);
            return options is null ? string.Empty : string.Join(", ", options);
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    /// <summary>Serialises a comma-separated option string to a JSON array.</summary>
    private static string CsvToOptionsJson(string? csv)
    {
        var options = (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        return JsonSerializer.Serialize(options);
    }

    /// <summary>Reads a field's options (for the renderer / preview) from its OptionsJson.</summary>
    public static IReadOnlyList<string> ReadOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsJson) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>An editable section.</summary>
    public sealed class SectionEdit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public List<FieldEdit> Fields { get; set; } = new();
    }

    /// <summary>An editable field.</summary>
    public sealed class FieldEdit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Kind { get; set; } = "ShortText";
        public string Label { get; set; } = string.Empty;
        public string? HelpText { get; set; }
        public bool Required { get; set; } = true;
        public string OptionsCsv { get; set; } = string.Empty;
    }
}
