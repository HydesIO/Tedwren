using Tedwren.Domain.ValueObjects;

namespace Tedwren.Mobile.Core.Identity;

/// <summary>
/// Client-side mobile-number handling for operative sign-in (SF-1). Reuses the domain <see cref="PhoneNumber"/>
/// normaliser so the app sends the API the same canonical E.164 form the server keys identity on (PRD Q9):
/// normalising differently on the device would either fail to match an existing operative or seed a duplicate.
/// </summary>
public static class MobilePhoneNumber
{
    /// <summary>Attempts to normalise raw user input to canonical E.164 form (e.g. "+447700900123"); returns null when unusable.</summary>
    public static string? Normalise(string? input) =>
        PhoneNumber.TryParse(input, out var number) ? number!.Value : null;

    /// <summary>Whether the raw input is a usable mobile number by the domain rules.</summary>
    public static bool IsValid(string? input) => PhoneNumber.TryParse(input, out _);
}
