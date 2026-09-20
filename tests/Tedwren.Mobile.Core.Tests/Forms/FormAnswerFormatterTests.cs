using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Core.Forms;

namespace Tedwren.Mobile.Core.Tests.Forms;

/// <summary>Verifies the read-only answer formatter mirrors the console renderer (multi-select join, Yes/No, image flag).</summary>
public class FormAnswerFormatterTests
{
    private static FormAnswerDto Answer(string? value = null, params string[] values) =>
        new("field", value, values);

    [Fact]
    public void Multi_select_values_are_comma_joined()
    {
        Assert.Equal("Boots, Helmet, Gloves", FormAnswerFormatter.Format(Answer(null, "Boots", "Helmet", "Gloves")));
    }

    [Fact]
    public void Boolean_values_read_as_yes_or_no()
    {
        Assert.Equal("Yes", FormAnswerFormatter.Format(Answer("true")));
        Assert.Equal("No", FormAnswerFormatter.Format(Answer("false")));
    }

    [Fact]
    public void An_image_data_url_is_flagged_and_not_shown_as_text()
    {
        var answer = Answer("data:image/png;base64,AAAA");

        Assert.True(FormAnswerFormatter.IsImage(answer));
        Assert.Equal(string.Empty, FormAnswerFormatter.Format(answer));
    }

    [Fact]
    public void Plain_text_is_shown_verbatim_and_blank_is_empty()
    {
        Assert.Equal("Level 3 north stair", FormAnswerFormatter.Format(Answer("Level 3 north stair")));
        Assert.Equal(string.Empty, FormAnswerFormatter.Format(Answer("   ")));
        Assert.False(FormAnswerFormatter.IsImage(Answer("Level 3")));
    }
}
