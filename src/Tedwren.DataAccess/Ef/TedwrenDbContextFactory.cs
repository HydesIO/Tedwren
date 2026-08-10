using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tedwren.DataAccess.Ef;

/// <summary>
/// Design-time factory used by the <c>dotnet ef</c> tools to build a <see cref="TedwrenDbContext"/> without
/// starting the application. The engine and connection string are read from environment variables so the
/// same commands target either database:
/// <list type="bullet">
///   <item><c>TEDWREN_EF_PROVIDER</c> — <c>SqlServer</c> (default) or <c>PostgreSql</c>.</item>
///   <item><c>TEDWREN_EF_CONNECTION</c> — the connection string (a placeholder is used for <c>migrations add</c>,
///   which does not connect; supply a real one for <c>database update</c>).</item>
/// </list>
/// See docs/ef-migrations.md.
/// </summary>
public sealed class TedwrenDbContextFactory : IDesignTimeDbContextFactory<TedwrenDbContext>
{
    /// <summary>Builds the context for the configured design-time engine and connection string.</summary>
    public TedwrenDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("TEDWREN_EF_PROVIDER") ?? "SqlServer";
        var connection = Environment.GetEnvironmentVariable("TEDWREN_EF_CONNECTION");
        var options = new DbContextOptionsBuilder<TedwrenDbContext>();

        if (IsPostgres(provider))
        {
            options.UseNpgsql(
                connection ?? "Host=localhost;Database=tedwren;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly(typeof(TedwrenDbContext).Assembly.FullName));
        }
        else
        {
            options.UseSqlServer(
                connection ?? "Server=localhost;Database=Tedwren;Trusted_Connection=True;TrustServerCertificate=True",
                b => b.MigrationsAssembly(typeof(TedwrenDbContext).Assembly.FullName));
        }

        return new TedwrenDbContext(options.Options);
    }

    /// <summary>Whether the provider name selects PostgreSQL.</summary>
    private static bool IsPostgres(string provider) =>
        provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
        || provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase);
}
