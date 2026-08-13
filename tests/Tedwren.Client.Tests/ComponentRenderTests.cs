using Bunit;
using Tedwren.UiComponents.DataDisplay;
using Tedwren.UiComponents.Feedback;
using Tedwren.UiComponents.Forms;
using Tedwren.UiComponents.Models;

namespace Tedwren.Client.Tests;

/// <summary>
/// bUnit smoke tests that assert the shared UI components actually render in a WebAssembly-equivalent Razor
/// pipeline — closing the previously-tracked gap of "WASM in-browser render never asserted". These render
/// components in-process and check their emitted markup, so a rendering regression fails the build.
/// </summary>
public sealed class ComponentRenderTests : TestContext
{
    [Fact]
    public void StatusPill_RendersLabelAndStatusClass()
    {
        var cut = RenderComponent<StatusPill>(parameters => parameters
            .Add(p => p.Label, "Active")
            .Add(p => p.Status, StatusKind.Success));

        Assert.Contains("Active", cut.Markup);
        Assert.Contains("status-pill--success", cut.Markup);
        Assert.Contains("role=\"status\"", cut.Markup);
    }

    [Theory]
    [InlineData(StatusKind.Danger, "status-pill--danger")]
    [InlineData(StatusKind.Warning, "status-pill--warning")]
    [InlineData(StatusKind.Neutral, "status-pill--neutral")]
    public void StatusPill_MapsEachStatusToItsClass(StatusKind status, string expectedClass)
    {
        var cut = RenderComponent<StatusPill>(parameters => parameters
            .Add(p => p.Label, "X")
            .Add(p => p.Status, status));

        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void EmptyState_RendersTitleAndDescription()
    {
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, "icon")
            .Add(p => p.Title, "No users yet")
            .Add(p => p.Description, "Invite a colleague to give them console access."));

        Assert.Contains("No users yet", cut.Markup);
        Assert.Contains("Invite a colleague", cut.Markup);
    }

    [Fact]
    public void BannerAlert_RendersMessageWithSeverityRole()
    {
        var cut = RenderComponent<BannerAlert>(parameters => parameters
            .Add(p => p.Message, "Sending is restricted")
            .Add(p => p.Severity, StatusKind.Danger));

        Assert.Contains("Sending is restricted", cut.Markup);
        Assert.Contains("role=\"alert\"", cut.Markup);   // danger is announced assertively
    }

    [Fact]
    public void RagInput_RendersThreeOptionsAndMarksTheSelected()
    {
        var cut = RenderComponent<TedwrenRagInput>(parameters => parameters
            .Add(p => p.Label, "Housekeeping")
            .Add(p => p.Value, "Green"));

        Assert.Contains("Housekeeping", cut.Markup);
        Assert.Contains("rag-input__option--red", cut.Markup);
        Assert.Contains("rag-input__option--amber", cut.Markup);
        Assert.Contains("rag-input__option--green", cut.Markup);
        // The chosen value is marked selected and announced.
        Assert.Contains("rag-input__option--selected", cut.Markup);
        Assert.Contains("aria-checked=\"true\"", cut.Markup);
    }

    [Fact]
    public void RagInput_ClickingAnOption_RaisesValueChanged()
    {
        string? chosen = null;
        var cut = RenderComponent<TedwrenRagInput>(parameters => parameters
            .Add(p => p.Label, "Housekeeping")
            .Add(p => p.ValueChanged, v => chosen = v));

        cut.Find(".rag-input__option--amber").Click();

        Assert.Equal("Amber", chosen);
    }
}
