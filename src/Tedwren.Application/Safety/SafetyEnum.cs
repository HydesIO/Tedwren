namespace Tedwren.Application.Safety;

/// <summary>Lenient enum parsing for the safety DTOs, which carry enum values as strings on the wire (PRD §8.2).</summary>
internal static class SafetyEnum
{
    /// <summary>Parses an enum name case-insensitively, falling back to <paramref name="fallback"/> when blank/unknown.</summary>
    public static TEnum Parse<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
}
