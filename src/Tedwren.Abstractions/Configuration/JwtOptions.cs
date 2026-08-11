namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// JWT bearer settings bound from the <c>Jwt</c> configuration section. The signing key must be supplied by
/// configuration in any real deployment; the development default is for local runs and tests only.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The symmetric signing key (HMAC-SHA256). Must be set from config/secret in real deployments.</summary>
    public string SigningKey { get; set; } = "tedwren-dev-signing-key-change-me-in-production-0123456789";

    /// <summary>Token issuer.</summary>
    public string Issuer { get; set; } = "tedwren";

    /// <summary>Token audience.</summary>
    public string Audience { get; set; } = "tedwren-console";

    /// <summary>Token lifetime in minutes.</summary>
    public int LifetimeMinutes { get; set; } = 480;
}
