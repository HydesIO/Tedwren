using System.Security.Cryptography;
using System.Text;

namespace Tedwren.Application.Billing;

/// <summary>
/// Verifies the <c>Webhook-Signature</c> header GoCardless sends on every webhook: an HMAC-SHA256 of the raw
/// request body, keyed by the webhook endpoint secret, as a lowercase hex digest. The comparison is
/// constant-time. An empty secret or a missing header fails closed (never trust an unverifiable webhook).
/// </summary>
public static class GoCardlessSignatureVerifier
{
    /// <summary>Returns true only when the header matches the HMAC-SHA256 of the body under the secret.</summary>
    public static bool IsValid(string rawBody, string? signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        // Constant-time compare over the raw bytes; length mismatch also returns false.
        var expected = Encoding.UTF8.GetBytes(computed);
        var actual = Encoding.UTF8.GetBytes(signatureHeader.Trim());
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
