using Tedwren.Mobile.Core.Identity;

namespace Tedwren.Mobile.Core.Tests.Identity;

/// <summary>Verifies client-side mobile-number normalisation reuses the domain E.164 rules (SF-1, Q9).</summary>
public class MobilePhoneNumberTests
{
    [Theory]
    [InlineData("07700 900123", "+447700900123")]
    [InlineData("+44 7700 900123", "+447700900123")]
    [InlineData("00447700900123", "+447700900123")]
    public void Normalise_canonicalises_uk_numbers(string input, string expected)
        => Assert.Equal(expected, MobilePhoneNumber.Normalise(input));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notaphone")]
    [InlineData("12345")]
    public void Normalise_returns_null_for_unusable_input(string input)
        => Assert.Null(MobilePhoneNumber.Normalise(input));

    [Fact]
    public void IsValid_reflects_normalisation()
    {
        Assert.True(MobilePhoneNumber.IsValid("07700900123"));
        Assert.False(MobilePhoneNumber.IsValid("nope"));
    }
}
