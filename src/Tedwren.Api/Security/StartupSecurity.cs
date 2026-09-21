using System.Text;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Auth;

namespace Tedwren.Api.Security;

/// <summary>
/// Fail-closed startup validation of security-sensitive configuration. In Production the application refuses to
/// start when a committed development default is still in effect (the JWT signing key or the seed admin
/// password), when the authentication test-bypass is enabled, or when a required database secret is missing.
/// This turns a silent, dangerous misconfiguration into an immediate, obvious boot failure rather than a live
/// exposure. Non-production environments (Development, and the test host) keep the convenient defaults so local
/// runs and the end-to-end suite work unchanged. Single responsibility: validate config, throw if unsafe.
/// </summary>
public static class StartupSecurity
{
    /// <summary>Minimum acceptable length, in bytes, for the symmetric JWT signing key (256-bit, for HMAC-SHA256).</summary>
    private const int MinimumSigningKeyBytes = 32;

    /// <summary>
    /// Validates the supplied security configuration for the given environment, throwing
    /// <see cref="InvalidOperationException"/> when it is unsafe to run. The full set of production checks is only
    /// enforced when <paramref name="environment"/> is Production; the test-bypass check applies everywhere because
    /// authenticating every request as an Administrator must never be possible in a real deployment.
    /// </summary>
    /// <param name="environment">The host environment (drives which checks are enforced).</param>
    /// <param name="jwt">The bound JWT options.</param>
    /// <param name="seed">The bound bootstrap-admin seed options.</param>
    /// <param name="testBypass">Whether the auth test-bypass scheme is enabled (<c>Auth:TestBypass</c>).</param>
    /// <param name="demo">The bound demo options (gates the operative demo sign-in — must be off in Production).</param>
    /// <param name="backend">The resolved data-source options (mode + provider).</param>
    /// <param name="productConnectionString">The product database connection string in Database mode, if any.</param>
    public static void Validate(
        IHostEnvironment environment,
        JwtOptions jwt,
        SeedAdminOptions seed,
        bool testBypass,
        DemoOptions demo,
        BackendOptions backend,
        string? productConnectionString)
    {
        // The test-bypass authenticates every request as an Administrator; that is only ever acceptable outside
        // Production. Refuse to boot Production with it on, regardless of anything else.
        if (testBypass && environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Auth:TestBypass must never be enabled in Production — it authenticates every request as an Administrator.");
        }

        // The operative demo sign-in mints a real operative token from a known email without an SMS code — a
        // browser-emulator convenience that must never exist in Production. Refuse to boot Production with it on.
        if (demo.Enabled && environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Demo:Enabled must never be enabled in Production — it exposes an operative token minter without a one-time code.");
        }

        if (!environment.IsProduction())
        {
            return;
        }

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey == JwtOptions.DevelopmentSigningKey)
        {
            problems.Add("Jwt:SigningKey is unset or the committed development default — set a unique secret value.");
        }
        else if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < MinimumSigningKeyBytes)
        {
            problems.Add($"Jwt:SigningKey must be at least {MinimumSigningKeyBytes} bytes (256-bit) for HMAC-SHA256.");
        }

        if (string.IsNullOrWhiteSpace(seed.Password) || seed.Password == SeedAdminOptions.DevelopmentPassword)
        {
            problems.Add("Seed:Password is unset or the committed development default — set a strong password.");
        }

        if (backend.Mode != DataSourceMode.InMemory && string.IsNullOrWhiteSpace(productConnectionString))
        {
            var name = backend.Provider == DatabaseProvider.PostgreSql ? "PostgreSql" : "SqlServer";
            problems.Add(
                $"ConnectionStrings:{name} is not configured — supply it from the environment or a secret store.");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "Refusing to start: insecure production configuration. " + string.Join(" ", problems) +
                " See docs/operations.md for the required environment settings.");
        }
    }
}
