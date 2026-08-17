using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Tedwren.Abstractions.Configuration;

namespace Tedwren.DataAccess.Commercial.Ef;

/// <summary>
/// Design-time factory used by the <c>dotnet ef</c> tools to build a <see cref="CommercialDbContext"/> without
/// starting the application. Mirrors the product <c>TedwrenDbContextFactory</c> but targets the <b>Commercial</b>
/// database: the engine comes from <c>DataSource:Provider</c> and the connection string from
/// <c>ConnectionStrings:SqlServerCommercial</c> / <c>ConnectionStrings:PostgreSqlCommercial</c> in the API's
/// <c>appsettings.json</c>. When the commercial string is empty it falls back to the product connection string
/// (<c>ConnectionStrings:SqlServer</c> / <c>PostgreSql</c>), exactly as the running app does for a single-database
/// dev setup. Optional overrides:
/// <list type="bullet">
///   <item><c>TEDWREN_EF_PROVIDER</c> — <c>SqlServer</c> (default) or <c>PostgreSql</c>.</item>
///   <item><c>TEDWREN_EF_COMMERCIAL_CONNECTION</c> — the commercial connection string.</item>
/// </list>
/// See docs/ef-migrations.md §8.
/// </summary>
public sealed class CommercialDbContextFactory : IDesignTimeDbContextFactory<CommercialDbContext>
{
    /// <summary>Builds the context for the configured design-time engine and commercial connection string.</summary>
    public CommercialDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        // Provider: env var override wins, otherwise DataSource:Provider from appsettings.json.
        var provider = Environment.GetEnvironmentVariable("TEDWREN_EF_PROVIDER")
            ?? configuration[$"{BackendOptions.SectionName}:Provider"]
            ?? nameof(DatabaseProvider.SqlServer);

        var isPostgres = IsPostgres(provider);

        // Connection string: env var override wins, otherwise the commercial ConnectionStrings entry, falling
        // back to the product connection string when the commercial one is empty (single-database dev setup).
        var connectionName = isPostgres ? "PostgreSql" : "SqlServer";
        var commercial = configuration.GetConnectionString(connectionName + "Commercial");
        var connection = Environment.GetEnvironmentVariable("TEDWREN_EF_COMMERCIAL_CONNECTION")
            ?? (string.IsNullOrWhiteSpace(commercial) ? configuration.GetConnectionString(connectionName) : commercial);

        var options = new DbContextOptionsBuilder<CommercialDbContext>();

        if (isPostgres)
        {
            options.UseNpgsql(
                RequireConnection(connection, connectionName),
                b => b.MigrationsAssembly(typeof(CommercialDbContext).Assembly.FullName));
        }
        else
        {
            options.UseSqlServer(
                RequireConnection(connection, connectionName),
                b => b.MigrationsAssembly(typeof(CommercialDbContext).Assembly.FullName));
        }

        return new CommercialDbContext(options.Options);
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

        // Environment variables remain available (e.g. ConnectionStrings__SqlServerCommercial) but are not required.
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
                $"No connection string was found for the commercial database. Set " +
                $"'ConnectionStrings:{connectionName}Commercial' (or '{connectionName}') in " +
                "src/Tedwren.Api/appsettings.json (or the TEDWREN_EF_COMMERCIAL_CONNECTION environment " +
                "variable) before running 'dotnet ef database update'.")
            : connection;

    /// <summary>Whether the provider name selects PostgreSQL.</summary>
    private static bool IsPostgres(string provider) =>
        provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
        || provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase);
}
