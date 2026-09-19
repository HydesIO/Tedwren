namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// JWT bearer settings bound from the <c>Jwt</c> configuration section. The signing key must be supplied by
/// configuration in any real deployment; the development default is for local runs and tests only.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The committed local-development signing key. Convenient for local runs and tests, but insecure for a real
    /// deployment (anyone with source access could forge tokens), so startup validation refuses to boot Production
    /// while this value — or an unset/short key — is in effect. Named here so that check has a single source.
    /// </summary>
    public const string DevelopmentSigningKey = "tedwren-dev-signing-key-change-me-in-production-0123456789";

    /// <summary>The symmetric signing key (HMAC-SHA256). Must be set from config/secret in real deployments.</summary>
    public string SigningKey { get; set; } = DevelopmentSigningKey;

    /// <summary>Token issuer.</summary>
    public string Issuer { get; set; } = "tedwren";

    /// <summary>Token audience.</summary>
    public string Audience { get; set; } = "tedwren-console";

    /// <summary>Token lifetime in minutes.</summary>
    public int LifetimeMinutes { get; set; } = 480;

    /// <summary>Audience for operative (mobile) access tokens, kept distinct from the console audience (M2).</summary>
    public string MobileAudience { get; set; } = "tedwren-mobile";

    /// <summary>Operative access-token lifetime in minutes — short, because a device-bound refresh token renews it.</summary>
    public int MobileLifetimeMinutes { get; set; } = 60;

    /// <summary>Operative refresh-token lifetime in days — long-lived, device-bound and rotated on each use.</summary>
    public int RefreshLifetimeDays { get; set; } = 30;
}
