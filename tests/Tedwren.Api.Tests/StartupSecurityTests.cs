using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tedwren.Abstractions.Configuration;
using Tedwren.Api.Security;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// Unit tests for <see cref="StartupSecurity"/> — the fail-closed check that refuses to boot Production with a
/// committed development default, the auth test-bypass, or a missing database secret, while leaving
/// non-production environments (including the test host) free to use the convenient defaults.
/// </summary>
public class StartupSecurityTests
{
    /// <summary>Minimal <see cref="IHostEnvironment"/> double with a settable environment name.</summary>
    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tedwren.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static JwtOptions StrongJwt() => new() { SigningKey = new string('k', 40) };
    private static SeedAdminOptions StrongSeed() => new() { Password = "S0me-Str0ng-Passw0rd!" };

    private static void Validate(
        IHostEnvironment env, JwtOptions jwt, SeedAdminOptions seed, bool testBypass, BackendOptions backend, string? conn)
        => StartupSecurity.Validate(env, jwt, seed, testBypass, backend, conn);

    [Fact]
    public void Development_with_all_defaults_does_not_throw()
    {
        var env = new FakeEnvironment { EnvironmentName = Environments.Development };
        // Defaults: dev JWT key, dev seed password, test-bypass on, InMemory — exactly the test host's shape.
        var ex = Record.Exception(() =>
            Validate(env, new JwtOptions(), new SeedAdminOptions(), testBypass: true,
                new BackendOptions { Mode = DataSourceMode.InMemory }, conn: null));
        Assert.Null(ex);
    }

    [Fact]
    public void Production_with_default_jwt_key_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Validate(new FakeEnvironment(), new JwtOptions(), StrongSeed(), testBypass: false,
                new BackendOptions { Mode = DataSourceMode.InMemory }, conn: null));
        Assert.Contains("Jwt:SigningKey", ex.Message);
    }

    [Fact]
    public void Production_with_short_jwt_key_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Validate(new FakeEnvironment(), new JwtOptions { SigningKey = "short" }, StrongSeed(), testBypass: false,
                new BackendOptions { Mode = DataSourceMode.InMemory }, conn: null));
        Assert.Contains("256-bit", ex.Message);
    }

    [Fact]
    public void Production_with_default_seed_password_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Validate(new FakeEnvironment(), StrongJwt(), new SeedAdminOptions(), testBypass: false,
                new BackendOptions { Mode = DataSourceMode.InMemory }, conn: null));
        Assert.Contains("Seed:Password", ex.Message);
    }

    [Fact]
    public void Production_with_test_bypass_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Validate(new FakeEnvironment(), StrongJwt(), StrongSeed(), testBypass: true,
                new BackendOptions { Mode = DataSourceMode.InMemory }, conn: null));
        Assert.Contains("Auth:TestBypass", ex.Message);
    }

    [Fact]
    public void Production_database_mode_without_connection_string_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Validate(new FakeEnvironment(), StrongJwt(), StrongSeed(), testBypass: false,
                new BackendOptions { Mode = DataSourceMode.Database, Provider = DatabaseProvider.SqlServer }, conn: ""));
        Assert.Contains("ConnectionStrings:SqlServer", ex.Message);
    }

    [Fact]
    public void Production_with_strong_configuration_does_not_throw()
    {
        var ex = Record.Exception(() =>
            Validate(new FakeEnvironment(), StrongJwt(), StrongSeed(), testBypass: false,
                new BackendOptions { Mode = DataSourceMode.Database, Provider = DatabaseProvider.SqlServer },
                conn: "Server=db;Database=Tedwren;"));
        Assert.Null(ex);
    }
}
