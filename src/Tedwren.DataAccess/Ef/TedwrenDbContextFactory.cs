using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Tedwren.Abstractions.Configuration;

namespace Tedwren.DataAccess.Ef;

/// <summary>
/// Design-time factory used by the <c>dotnet ef</c> tools to build a <see cref="TedwrenDbContext"/> without
/// starting the application. The engine and connection string are read from the API's
/// <c>appsettings.json</c> (the same file the running app uses), so <c>dotnet ef database update</c> targets
/// the configured database with no environment variables required:
/// <list type="bullet">
///   <item><c>DataSource:Provider</c> — <c>SqlServer</c> (default) or <c>PostgreSql</c>.</item>
///   <item><c>ConnectionStrings:SqlServer</c> / <c>ConnectionStrings:PostgreSql</c> — the connection string
///   for the selected provider.</item>
/// </list>
/// Optional environment variables (<c>TEDWREN_EF_PROVIDER</c> / <c>TEDWREN_EF_CONNECTION</c>) still override
/// the file when set, but are no longer required. See docs/ef-migrations.md.
/// </summary>
public sealed class TedwrenDbContextFactory : IDesignTimeDbContextFactory<TedwrenDbContext>
{
    /// <summary>Builds the context for the configured design-time engine and connection string.</summary>
    public TedwrenDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        // Provider: env var override wins, otherwise DataSource:Provider from appsettings.json.
        var provider = Environment.GetEnvironmentVariable("TEDWREN_EF_PROVIDER")
            ?? configuration[$"{BackendOptions.SectionName}:Provider"]
            ?? nameof(DatabaseProvider.SqlServer);

        var isPostgres = IsPostgres(provider);

        // Connection string: env var override wins, otherwise the ConnectionStrings entry for the provider.
        var connectionName = isPostgres ? "PostgreSql" : "SqlServer";
        var connection = Environment.GetEnvironmentVariable("TEDWREN_EF_CONNECTION")
            ?? configuration.GetConnectionString(connectionName);

        var options = new DbContextOptionsBuilder<TedwrenDbContext>();

        if (isPostgres)
        {
            options.UseNpgsql(
                RequireConnection(connection, connectionName),
                b => b.MigrationsAssembly(typeof(TedwrenDbContext).Assembly.FullName));
        }
        else
        {
            options.UseSqlServer(
                RequireConnection(connection, connectionName),
                b => b.MigrationsAssembly(typeof(TedwrenDbContext).Assembly.FullName));
        }

        return new TedwrenDbContext(options.Options);
    }

    /// <summary>
    /// Loads configuration from the API's <c>appsettings.json</c> (plus its environment-specific overlay and
    /// process environment variables). The API project is located by walking up from the factory assembly to
    /// the repository root, so the command works regardless of the current working directory.
    /// </summary>
    private static IConfigurationRoot BuildConfiguration()
    {
        var apiDirectory = LocateApiDirectory();
        var builder = new ConfigurationBuilder();

        if (apiDirectory is not null)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            builder.SetBasePath(apiDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            if (!string.IsNullOrWhiteSpace(environment))
            {
                builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
            }
        }

        // Environment variables remain available (e.g. ConnectionStrings__SqlServer) but are not required.
        builder.AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// Finds the <c>src/Tedwren.Api</c> directory by walking up from the running assembly's location until the
    /// repository root (the folder containing <c>Tedwren.sln</c>) is found. Returns <c>null</c> if not located.
    /// </summary>
    private static string? LocateApiDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (current.GetFiles("Tedwren.sln").Length > 0)
            {
                var apiDirectory = Path.Combine(current.FullName, "src", "Tedwren.Api");
                return Directory.Exists(apiDirectory) ? apiDirectory : null;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>Ensures a connection string was resolved, failing with a clear message otherwise.</summary>
    private static string RequireConnection(string? connection, string connectionName) =>
        string.IsNullOrWhiteSpace(connection)
            ? throw new InvalidOperationException(
                $"No connection string was found for the '{connectionName}' provider. Set " +
                $"'ConnectionStrings:{connectionName}' in src/Tedwren.Api/appsettings.json (or the " +
                "TEDWREN_EF_CONNECTION environment variable) before running 'dotnet ef database update'.")
            : connection;

    /// <summary>Whether the provider name selects PostgreSQL.</summary>
    private static bool IsPostgres(string provider) =>
        provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
        || provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase);
}
