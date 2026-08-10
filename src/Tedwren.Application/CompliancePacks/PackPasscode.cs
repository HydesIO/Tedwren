using System.Security.Cryptography;
using System.Text;

namespace Tedwren.Application.CompliancePacks;

/// <summary>
/// Hashes and verifies a pack passcode (SUB-18). The plaintext passcode is never stored — only a per-pack
/// salted <b>PBKDF2</b> hash (a deliberately slow KDF, so a leaked hash resists offline brute-force of the
/// short passcode) — and a short random passcode is generated when the sender does not supply one. Verify
/// still accepts the legacy salted-SHA-256 format so older packs keep working. Runs server-side only.
/// </summary>
public static class PackPasscode
{
    // PBKDF2 work factor. Raise over time as hardware improves.
    private const int Pbkdf2Iterations = 210_000;
    private const int KeyBytes = 32;
    private const int SaltBytes = 16;
    private const string Pbkdf2Prefix = "pbkdf2";

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

    /// <summary>Hashes a passcode with PBKDF2, returning <c>pbkdf2:iterations:salt:hash</c> (salt/hash base64).</summary>
    public static string Hash(string passcode)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passcode), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeyBytes);
        return $"{Pbkdf2Prefix}:{Pbkdf2Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>Verifies a passcode against a stored hash (PBKDF2 or legacy salted SHA-256) in constant time.</summary>
    public static bool Verify(string passcode, string stored)
    {
        if (stored.StartsWith(Pbkdf2Prefix + ":", StringComparison.Ordinal))
        {
            var parts = stored.Split(':', 4);
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(passcode), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        return VerifyLegacySha256(passcode, stored);
    }

    /// <summary>Verifies against the legacy salted SHA-256 <c>salt:hash</c> format.</summary>
    private static bool VerifyLegacySha256(string passcode, string stored)
    {
        var parts = stored.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var input = new byte[salt.Length + Encoding.UTF8.GetByteCount(passcode)];
        salt.CopyTo(input, 0);
        Encoding.UTF8.GetBytes(passcode, 0, passcode.Length, input, salt.Length);
        var actual = SHA256.HashData(input);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
