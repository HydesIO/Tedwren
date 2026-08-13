using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Application.Forms;

/// <summary>
/// The shipped starter form library (PRD §6 "ship a template library, not an empty builder"). A few ready-made,
/// professional forms a customer can use immediately or adapt — so the Forms Library is useful on day one rather
/// than an empty builder. Field ids are stable within each form.
/// </summary>
public static class DefaultFormTemplates
{
    /// <summary>A starter form: a name, an optional description and its sections.</summary>
    public sealed record Starter(string Name, string? Description, IReadOnlyList<FormSectionDto> Sections);

    /// <summary>The shipped starter forms.</summary>
    public static IReadOnlyList<Starter> Starters { get; } = new List<Starter>
    {
        new("Daily Site Diary", "A daily record of site conditions and activity.", new List<FormSectionDto>
        {
            new("conditions", "Site conditions", new List<FormFieldDto>
            {
                new("date", "Date", "Date", null, true, null, null, 0),
                new("weather", "Weather", "Dropdown", null, true, null, "[\"Dry\",\"Wet\",\"Frost\",\"High winds\"]", 1),
                new("temperature", "Temperature (°C)", "Number", null, false, null, null, 2),
            }, 0),
            new("activity", "Activity", new List<FormFieldDto>
            {
                new("operatives", "Operatives on site", "Number", null, true, null, null, 0),
                new("work", "Work carried out", "LongText", null, true, null, null, 1),
                new("delays", "Delays or issues", "LongText", "Anything to flag", false, null, null, 2),
            }, 1),
        }),

        new("Plant & Equipment Checklist", "A pre-use inspection of plant and equipment.", new List<FormSectionDto>
        {
            new("checks", "Inspection", new List<FormFieldDto>
            {
                new("plant", "Plant / equipment", "ShortText", null, true, null, null, 0),
                new("guards", "Guards and covers fitted", "RagStatus", null, true, null, null, 1),
                new("warning", "Warning devices working", "RagStatus", null, true, null, null, 2),
                new("hydraulics", "No hydraulic / fluid leaks", "RagStatus", null, true, null, null, 3),
                new("tyres", "Tyres / tracks sound", "RagStatus", null, true, null, null, 4),
                new("certified", "Operator holds a valid ticket", "YesNo", null, true, null, null, 5),
            }, 0),
            new("signoff", "Sign-off", new List<FormFieldDto>
            {
                new("notes", "Notes", "LongText", null, false, null, null, 0),
                new("signature", "Inspector signature", "Signature", null, true, null, null, 1),
            }, 1),
        }),

        new("Welfare Check", "A check of welfare facilities on site.", new List<FormSectionDto>
        {
            new("welfare", "Welfare facilities", new List<FormFieldDto>
            {
                new("toilets", "Toilets clean and stocked", "RagStatus", null, true, null, null, 0),
                new("water", "Drinking water available", "RagStatus", null, true, null, null, 1),
                new("rest", "Rest area clean and heated", "RagStatus", null, true, null, null, 2),
                new("firstaid", "First-aid kit stocked", "YesNo", null, true, null, null, 3),
                new("notes", "Notes", "LongText", "Anything to flag", false, null, null, 4),
            }, 0),
        }),
    };
}
