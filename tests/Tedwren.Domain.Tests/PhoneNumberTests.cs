using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Tests for the <see cref="PhoneNumber"/> normaliser — the SF-1 identity key. Getting normalisation
/// wrong quietly creates duplicate people, so the varied-format cases are the point of this suite.
/// </summary>
public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("07700900123", "+447700900123")]
    [InlineData("+44 7700 900123", "+447700900123")]
    [InlineData("(07700) 900-123", "+447700900123")]
    [InlineData("0044 7700 900123", "+447700900123")]
    [InlineData("+1 202 555 0173", "+12025550173")]
    public void Parse_NormalisesToCanonicalForm(string input, string expected)
    {
        var number = PhoneNumber.Parse(input);

        Assert.Equal(expected, number.Value);
    }

    [Fact]
    public void DifferentFormats_OfSameNumber_AreEqual()
    {
        var a = PhoneNumber.Parse("07700 900123");
        var b = PhoneNumber.Parse("+44 (0)7700-900123".Replace("(0)", string.Empty));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void DifferentNumbers_AreNotEqual()
    {
        Assert.NotEqual(PhoneNumber.Parse("07700900123"), PhoneNumber.Parse("07700900124"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-number")]
    [InlineData("12345")]          // no country indication and not a national 0-number
    [InlineData("012")]            // too short after normalisation
    public void TryParse_ReturnsFalse_ForUnusableInput(string? input)
    {
        Assert.False(PhoneNumber.TryParse(input, out var number));
        Assert.Null(number);
    }

    [Fact]
    public void Parse_Throws_ForUnusableInput()
    {
        Assert.Throws<FormatException>(() => PhoneNumber.Parse("nonsense"));
    }
}
