using System.Security.Cryptography;

namespace Tedwren.Application.CompliancePacks;

/// <summary>
/// Generates the opaque, non-guessable access token embedded in a pack's share link (R9). The token is the
/// only handle to the pack — there are no permanent, guessable public URLs — so it is drawn from a
/// cryptographic RNG and URL-safe encoded.
/// </summary>
public static class PackToken
{
    /// <summary>Returns a new 256-bit URL-safe token.</summary>
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
