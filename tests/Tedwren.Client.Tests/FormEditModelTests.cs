using System.Text.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Client.Pages.Forms;
using Tedwren.UiComponents.Forms;

namespace Tedwren.Client.Tests;

/// <summary>
/// Unit tests for the Forms builder edit model — specifically the chip ⇄ OptionsJson round-trip that replaced
/// the old comma-separated option entry, including back-compatible reads of the legacy plain-string shape
/// (Site Gate / Forms redesign, Workstream 6).
/// </summary>
public sealed class FormEditModelTests
{
    [Fact]
    public void ParseOptions_ReadsLegacyPlainStringArray_AssigningIds()
    {
        var chips = FormEditModel.ParseOptions("[\"Pass\",\"Fail\",\"N/A\"]");

        Assert.Equal(new[] { "Pass", "Fail", "N/A" }, chips.Select(c => c.Text));
        Assert.All(chips, c => Assert.False(string.IsNullOrWhiteSpace(c.Id)));
    }

    [Fact]
    public void ParseOptions_ReadsObjectShape_PreservingIds()
    {
        var json = "[{\"id\":\"abc123\",\"text\":\"Yes\"},{\"id\":\"def456\",\"text\":\"No\"}]";

        var chips = FormEditModel.ParseOptions(json);

        Assert.Collection(chips,
            c => { Assert.Equal("abc123", c.Id); Assert.Equal("Yes", c.Text); },
            c => { Assert.Equal("def456", c.Id); Assert.Equal("No", c.Text); });
    }

    [Fact]
    public void ParseOptions_HandlesNullEmptyAndMalformed()
    {
        Assert.Empty(FormEditModel.ParseOptions(null));
        Assert.Empty(FormEditModel.ParseOptions(""));
        Assert.Empty(FormEditModel.ParseOptions("not json"));
        Assert.Empty(FormEditModel.ParseOptions("{\"not\":\"an array\"}"));
    }

    [Fact]
    public void OptionsToJson_SerialisesIdAndText_AndDropsBlankText()
    {
        var options = new List<ChipOption>
        {
            new() { Id = "id1", Text = "Alpha" },
            new() { Id = "id2", Text = "  " },   // dropped
            new() { Id = "id3", Text = "Beta " }, // trimmed
        };

        var json = FormEditModel.OptionsToJson(options);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
        Assert.Equal("id1", doc.RootElement[0].GetProperty("id").GetString());
        Assert.Equal("Alpha", doc.RootElement[0].GetProperty("text").GetString());
        Assert.Equal("Beta", doc.RootElement[1].GetProperty("text").GetString());
    }

    [Fact]
    public void OptionsToJson_AssignsIdWhenMissing()
    {
        var options = new List<ChipOption> { new() { Id = "", Text = "Solo" } };

        var json = FormEditModel.OptionsToJson(options);
        using var doc = JsonDocument.Parse(json);

        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement[0].GetProperty("id").GetString()));
    }

    [Fact]
    public void ChipRoundTrip_PreservesTextAndIds_ThroughDtoMapping()
    {
        // Build a Dropdown field with chip options, map to DTO, then back — ids and text survive.
        var sections = new List<FormEditModel.SectionEdit>
        {
            new()
            {
                Title = "Checks",
                Fields = new List<FormEditModel.FieldEdit>
                {
                    new()
                    {
                        Kind = "Dropdown",
                        Label = "Outcome",
                        Options = new List<ChipOption>
                        {
                            new() { Id = "o1", Text = "Pass" },
                            new() { Id = "o2", Text = "Fail" },
                        },
                    },
                },
            },
        };

        var dtos = FormEditModel.ToDto(sections);
        var back = FormEditModel.FromDto(dtos);

        var options = back.Single().Fields.Single().Options;
        Assert.Collection(options,
            o => { Assert.Equal("o1", o.Id); Assert.Equal("Pass", o.Text); },
            o => { Assert.Equal("o2", o.Id); Assert.Equal("Fail", o.Text); });
    }

    [Fact]
    public void ToDto_OmitsOptionsForNonChoiceKinds()
    {
        var sections = new List<FormEditModel.SectionEdit>
        {
            new()
            {
                Fields = new List<FormEditModel.FieldEdit>
                {
                    new() { Kind = "ShortText", Label = "Name", Options = new List<ChipOption> { new() { Id = "x", Text = "ignored" } } },
                },
            },
        };

        var field = FormEditModel.ToDto(sections).Single().Fields.Single();
        Assert.Null(field.OptionsJson);
    }

    [Fact]
    public void ReadOptions_ReturnsLabels_ForBothShapes()
    {
        Assert.Equal(new[] { "A", "B" }, FormEditModel.ReadOptions("[\"A\",\"B\"]"));
        Assert.Equal(new[] { "A", "B" },
            FormEditModel.ReadOptions("[{\"id\":\"1\",\"text\":\"A\"},{\"id\":\"2\",\"text\":\"B\"}]"));
    }
}
