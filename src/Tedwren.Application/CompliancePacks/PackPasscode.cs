using System.Security.Cryptography;
using System.Text;

namespace Tedwren.Application.CompliancePacks;

/// <summary>
/// Hashes and verifies a pack passcode (SUB-18). The plaintext passcode is never stored — only a per-pack
/// salted SHA-256 hash — and a short random passcode is generated when the sender does not supply one. Runs
/// server-side only (never in the browser), so the framework hash APIs are available.
/// </summary>
public static class PackPasscode
{
    /// <summary>Generates a short, human-shareable passcode (8 unambiguous characters).</summary>
    public static string Generate()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // no I/O/0/1
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>Hashes a passcode with a random salt, returning <c>salt:hash</c> (both base64).</summary>
    public static string Hash(string passcode)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Compute(passcode, salt);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    /// <summary>Verifies a passcode against a stored <c>salt:hash</c> in constant time.</summary>
    public static bool Verify(string passcode, string stored)
    {
        var parts = stored.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Compute(passcode, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>Computes the salted SHA-256 of a passcode.</summary>
    private static byte[] Compute(string passcode, byte[] salt)
    {
        var input = new byte[salt.Length + Encoding.UTF8.GetByteCount(passcode)];
        salt.CopyTo(input, 0);
        Encoding.UTF8.GetBytes(passcode, 0, passcode.Length, input, salt.Length);
        return SHA256.HashData(input);
    }
}
