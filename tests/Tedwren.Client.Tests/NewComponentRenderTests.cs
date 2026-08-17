using Bunit;
using Microsoft.AspNetCore.Components;
using Tedwren.UiComponents.Feedback;
using Tedwren.UiComponents.Forms;

namespace Tedwren.Client.Tests;

/// <summary>
/// bUnit render tests for the components added in the Site Gate / Forms redesign: the AsyncContent loading /
/// error / empty / content switch (Workstream 1) and the editable ChipInput (Workstream 6).
/// </summary>
public sealed class NewComponentRenderTests : TestContext
{
    [Fact]
    public void AsyncContent_WhenLoading_ShowsSkeleton()
    {
        var cut = RenderComponent<AsyncContent>(p => p
            .Add(a => a.Loading, true)
            .AddChildContent("<span>loaded</span>"));

        Assert.Contains("skeleton", cut.Markup);
        Assert.DoesNotContain("loaded", cut.Markup);
    }

    [Fact]
    public void AsyncContent_WhenError_ShowsBannerAndRetry()
    {
        var retried = false;
        var cut = RenderComponent<AsyncContent>(p => p
            .Add(a => a.Loading, false)
            .Add(a => a.Error, "Boom")
            .Add(a => a.OnRetry, EventCallback.Factory.Create(this, () => retried = true))
            .AddChildContent("<span>loaded</span>"));

        Assert.Contains("Boom", cut.Markup);
        Assert.DoesNotContain("loaded", cut.Markup);

        cut.Find("button").Click();
        Assert.True(retried);
    }

    [Fact]
    public void AsyncContent_WhenEmpty_ShowsEmptyState()
    {
        var cut = RenderComponent<AsyncContent>(p => p
            .Add(a => a.Loading, false)
            .Add(a => a.IsEmpty, true)
            .Add(a => a.EmptyTitle, "Nothing here")
            .AddChildContent("<span>loaded</span>"));

        Assert.Contains("Nothing here", cut.Markup);
        Assert.DoesNotContain("loaded", cut.Markup);
    }

    [Fact]
    public void AsyncContent_WhenReady_ShowsChildContent()
    {
        var cut = RenderComponent<AsyncContent>(p => p
            .Add(a => a.Loading, false)
            .AddChildContent("<span>loaded</span>"));

        Assert.Contains("loaded", cut.Markup);
        Assert.DoesNotContain("skeleton", cut.Markup);
    }

    [Fact]
    public void ChipInput_RendersEachOptionWithTextAndId()
    {
        var value = new List<ChipOption> { new() { Id = "abc123", Text = "Pass" }, new() { Id = "def456", Text = "Fail" } };
        var cut = RenderComponent<ChipInput>(p => p.Add(c => c.Value, value));

        Assert.Contains("Pass", cut.Markup);
        Assert.Contains("Fail", cut.Markup);
        Assert.Contains("abc123", cut.Markup);
        Assert.Contains("def456", cut.Markup);
    }

    [Fact]
    public void ChipInput_RemovingAChip_RaisesValueChanged()
    {
        var value = new List<ChipOption> { new() { Id = "id1", Text = "Only" } };
        List<ChipOption>? updated = null;

        var cut = RenderComponent<ChipInput>(p => p
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<List<ChipOption>>(this, v => updated = v)));

        cut.Find(".chip-input__remove").Click();

        Assert.NotNull(updated);
        Assert.Empty(updated!);
    }
}
