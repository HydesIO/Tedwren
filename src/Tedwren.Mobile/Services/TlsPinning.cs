using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Tedwren.Mobile.Services;

/// <summary>
/// TLS certificate pinning for the API HttpClients (M8). Validates the server certificate's public key
/// (SubjectPublicKeyInfo SHA-256) against a pin set, so a mis-issued or intercepting certificate is rejected even
/// if it chains to a trusted root. The pin set is a deployment artifact populated before store submission (see
/// <c>docs/mobile-store-readiness.md</c>); until it is set, standard chain validation applies so the app still works
/// in dev/staging. In DEBUG the local ASP.NET dev certificate is trusted so on-device debugging works.
/// </summary>
public static class TlsPinning
{
    // Base64 SHA-256 of the Tedwren API certificate SubjectPublicKeyInfo. Populate for production before submission
    // (pin the leaf and a backup/intermediate so a routine renewal doesn't brick the app).
    private static readonly HashSet<string> PinnedSpkiSha256 = new(StringComparer.Ordinal)
    {
        // "…base64 SPKI pin…",
    };

    /// <summary>Creates a pinning-enabled primary HTTP handler for a typed client.</summary>
    public static HttpMessageHandler CreateHandler() =>
        new HttpClientHandler { ServerCertificateCustomValidationCallback = Validate };

    /// <summary>Validates the server certificate: the chain must be clean and, when pins are configured, the SPKI must match one.</summary>
    public static bool Validate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
#if DEBUG
        // Local dev/staging over the ASP.NET dev certificate — do not pin while debugging.
        _ = (request, certificate, chain, errors);
        return true;
#else
        // Fail closed on any chain/name/date error, then require a pin match once pins are configured.
        if (errors != SslPolicyErrors.None || certificate is null)
        {
            return false;
        }

        if (PinnedSpkiSha256.Count == 0)
        {
            return true; // no pins configured yet — rely on the validated chain (flagged for pre-submission)
        }

        var spki = SHA256.HashData(certificate.PublicKey.ExportSubjectPublicKeyInfo());
        return PinnedSpkiSha256.Contains(Convert.ToBase64String(spki));
#endif
    }
}
