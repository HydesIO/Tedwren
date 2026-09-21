using Bunit;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Web.App.Components;

namespace Tedwren.Web.App.Tests;

/// <summary>bUnit render tests for the emulator's faithful Tw* kit + the dynamic form's answer collection.</summary>
public class ComponentRenderTests : TestContext
{
    [Fact]
    public void TwStatusPill_renders_kind_class_and_text()
    {
        var cut = RenderComponent<TwStatusPill>(p => p
            .Add(x => x.Kind, TwStatusKind.Success)
            .Add(x => x.Text, "Compliant"));

        Assert.Contains("tw-pill--success", cut.Markup);
        Assert.Contains("Compliant", cut.Markup);
    }

    [Fact]
    public void TwKpiCard_renders_value_and_label()
    {
        var cut = RenderComponent<TwKpiCard>(p => p.Add(x => x.Value, "128").Add(x => x.Label, "Operatives"));
        Assert.Contains("128", cut.Markup);
        Assert.Contains("Operatives", cut.Markup);
    }

    [Fact]
    public void TwMenuTile_with_href_renders_an_anchor()
    {
        var cut = RenderComponent<TwMenuTile>(p => p
            .Add(x => x.Title, "My hours")
            .Add(x => x.Href, "/operative/hours"));

        var anchor = cut.Find("a.tw-tile");
        Assert.Equal("/operative/hours", anchor.GetAttribute("href"));
        Assert.Contains("My hours", cut.Markup);
    }

    [Fact]
    public void TwEmptyState_renders_title_and_message()
    {
        var cut = RenderComponent<TwEmptyState>(p => p
            .Add(x => x.Title, "No evidence")
            .Add(x => x.Message, "Captured photos will appear here."));

        Assert.Contains("No evidence", cut.Markup);
        Assert.Contains("Captured photos will appear here.", cut.Markup);
    }

    [Fact]
    public void DynamicForm_collects_a_short_text_answer()
    {
        // The signature field's OnAfterRender interop isn't exercised here, but keep JS loose for safety.
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sections = new List<FormSectionDto>
        {
            new("s1", "Section", new List<FormFieldDto>
            {
                new("f1", "ShortText", "Your name", null, true, null, null, 0),
            }, 0),
        };

        var cut = RenderComponent<DynamicForm>(p => p.Add(x => x.Sections, sections));
        cut.Find("input").Change("Alex");

        var answers = cut.Instance.GetAnswers();
        Assert.Contains(answers, a => a.FieldId == "f1" && a.Value == "Alex");
    }
}
