using System.Text.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.UiComponents.Forms;

namespace Tedwren.Client.Pages.Forms;

/// <summary>
/// Client-side editable model for the form builder (PRD-Phase 2). Mirrors the <see cref="FormSectionDto"/> /
/// <see cref="FormFieldDto"/> contracts as mutable classes the UI can bind to, with mapping to/from the DTOs and
/// the small set of authorable field kinds. Options for choice fields are edited as <see cref="ChipOption"/>
/// chips and persisted into <c>OptionsJson</c> as <c>{id,text}</c> objects (older plain-string arrays are read
/// back for compatibility).
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
                    Options = ParseOptions(f.OptionsJson),
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
                UsesOptions(f.Kind) ? OptionsToJson(f.Options) : null,
                fi)).ToList(),
            si)).ToList();

    /// <summary>
    /// Reads a field's <c>OptionsJson</c> into editable chips. Accepts both the current <c>[{id,text}]</c> shape
    /// and the legacy plain-string array <c>["Pass","Fail"]</c> (each legacy value gets a freshly-assigned id).
    /// </summary>
    public static List<ChipOption> ParseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new List<ChipOption>();
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return new List<ChipOption>();

            var chips = new List<ChipOption>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    var text = element.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) chips.Add(ChipOption.Create(text));
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    var text = element.TryGetProperty("text", out var t) ? t.GetString() : null;
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var id = element.TryGetProperty("id", out var i) ? i.GetString() : null;
                    chips.Add(new ChipOption { Id = string.IsNullOrWhiteSpace(id) ? ChipOption.NewId() : id!, Text = text.Trim() });
                }
            }

            return chips;
        }
        catch (JsonException)
        {
            return new List<ChipOption>();
        }
    }

    /// <summary>Serialises option chips to the persisted <c>[{id,text}]</c> JSON array (assigning ids where missing).</summary>
    public static string OptionsToJson(IReadOnlyList<ChipOption> options)
    {
        var payload = options
            .Where(o => !string.IsNullOrWhiteSpace(o.Text))
            .Select(o => new OptionJson(string.IsNullOrWhiteSpace(o.Id) ? ChipOption.NewId() : o.Id, o.Text.Trim()))
            .ToList();
        return JsonSerializer.Serialize(payload);
    }

    /// <summary>Reads a field's option <em>labels</em> (for the renderer / preview) from its OptionsJson, both shapes.</summary>
    public static IReadOnlyList<string> ReadOptions(string? optionsJson) =>
        ParseOptions(optionsJson).Select(o => o.Text).ToList();

    /// <summary>The persisted option shape: a stable id plus its display text.</summary>
    private sealed record OptionJson(string id, string text);

    /// <summary>An editable section.</summary>
    public sealed class SectionEdit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public List<FieldEdit> Fields { get; set; } = new();

        /// <summary>UI-only collapse state for the builder panel (not persisted).</summary>
        public bool Collapsed { get; set; }
    }

    /// <summary>An editable field.</summary>
    public sealed class FieldEdit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Kind { get; set; } = "ShortText";
        public string Label { get; set; } = string.Empty;
        public string? HelpText { get; set; }
        public bool Required { get; set; } = true;
        public List<ChipOption> Options { get; set; } = new();
    }
}
