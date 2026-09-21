using Tedwren.Abstractions.Common;
using Tedwren.Web.App.Components;
using Tedwren.Web.App.Shared;

namespace Tedwren.Web.App.Tests;

/// <summary>Tests for the shared manager UI helpers — status→pill mapping and the image data-URL sniffing.</summary>
public class WebManagerUiTests
{
    [Theory]
    [InlineData(ComplianceState.Compliant, TwStatusKind.Success)]
    [InlineData(ComplianceState.AtRisk, TwStatusKind.Warning)]
    [InlineData(ComplianceState.NonCompliant, TwStatusKind.Danger)]
    [InlineData(ComplianceState.Pending, TwStatusKind.Neutral)]
    public void ForCompliance_maps_state_to_pill_kind(ComplianceState state, TwStatusKind expected)
    {
        Assert.Equal(expected, WebManagerUi.ForCompliance(state));
    }

    [Theory]
    [InlineData("Approved", TwStatusKind.Success)]
    [InlineData("Rejected", TwStatusKind.Danger)]
    [InlineData("Submitted", TwStatusKind.Warning)]
    [InlineData("Draft", TwStatusKind.Neutral)]
    public void ForFormStatus_maps_status_to_pill_kind(string status, TwStatusKind expected)
    {
        Assert.Equal(expected, WebManagerUi.ForFormStatus(status));
    }

    [Fact]
    public void DataUrl_sniffs_png_magic_bytes()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01, 0x02 };
        Assert.StartsWith("data:image/png;base64,", WebManagerUi.DataUrl(png));
    }

    [Fact]
    public void DataUrl_defaults_to_jpeg()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01 };
        Assert.StartsWith("data:image/jpeg;base64,", WebManagerUi.DataUrl(jpeg));
    }

    [Fact]
    public void DataUrl_is_null_for_empty()
    {
        Assert.Null(WebManagerUi.DataUrl(null));
        Assert.Null(WebManagerUi.DataUrl(Array.Empty<byte>()));
    }
}
