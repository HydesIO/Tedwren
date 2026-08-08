using System.Text;

namespace Tedwren.Domain.ValueObjects;

/// <summary>
/// A mobile number normalised to a single canonical form so the same person entered by different
/// companies in different formats resolves to one identity (SF-1, PRD Q9). Normalisation is settled
/// once here and applied everywhere: getting it wrong quietly creates duplicate people. Equality is
/// by the normalised value. Names are deliberately NOT normalised — see PRD §5.1.
/// </summary>
public sealed class PhoneNumber : IEquatable<PhoneNumber>
{
    /// <summary>The canonical, normalised number in E.164 style (e.g. "+447700900123").</summary>
    public string Value { get; }

    /// <summary>Private: instances are only created through <see cref="Parse"/> / <see cref="TryParse"/>.</summary>
    private PhoneNumber(string value) => Value = value;

    /// <summary>Normalises and validates <paramref name="input"/>, throwing when it is unusable.</summary>
    public static PhoneNumber Parse(string? input)
    {
        if (!TryParse(input, out var number))
        {
            throw new FormatException($"'{input}' is not a usable mobile number.");
        }

        return number!;
    }

    /// <summary>
    /// Attempts to normalise <paramref name="input"/> to canonical form. Returns false (rather than
    /// throwing) when the input cannot be resolved to a plausible number. A UK national number
    /// (leading '0') is treated as +44; a '00' prefix is treated as the international '+'.
    /// </summary>
    public static bool TryParse(string? input, out PhoneNumber? number)
    {
        number = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Keep digits, remembering a single leading '+'. Spaces, hyphens, brackets and dots are dropped.
        var hasPlus = false;
        var digits = new StringBuilder(input.Length);
        foreach (var ch in input.Trim())
        {
            if (ch == '+' && digits.Length == 0 && !hasPlus)
            {
                hasPlus = true;
            }
            else if (char.IsDigit(ch))
            {
                digits.Append(ch);
            }
        }

        var national = digits.ToString();
        if (national.Length == 0)
        {
            return false;
        }

        string e164;
        if (hasPlus)
        {
            e164 = "+" + national;
        }
        else if (national.StartsWith("00", StringComparison.Ordinal))
        {
            e164 = "+" + national[2..];
        }
        else if (national.StartsWith('0'))
        {
            // UK national format → +44.
            e164 = "+44" + national[1..];
        }
        else
        {
            // No country indication and not a national '0' number — cannot safely assume a country.
            return false;
        }

        // E.164 permits up to 15 digits after '+'; a real number needs at least a handful.
        var e164Digits = e164.Length - 1;
        if (e164Digits is < 8 or > 15)
        {
            return false;
        }

        number = new PhoneNumber(e164);
        return true;
    }

    /// <summary>Value equality on the normalised number.</summary>
    public bool Equals(PhoneNumber? other) => other is not null && Value == other.Value;

    /// <summary>Value equality on the normalised number.</summary>
    public override bool Equals(object? obj) => Equals(obj as PhoneNumber);

    /// <summary>Hash of the normalised number, consistent with <see cref="Equals(PhoneNumber?)"/>.</summary>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <summary>Returns the canonical normalised number.</summary>
    public override string ToString() => Value;
}
