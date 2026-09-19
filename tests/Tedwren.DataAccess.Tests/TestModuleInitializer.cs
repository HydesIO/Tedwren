using System.Runtime.CompilerServices;
using Tedwren.DataAccess.TypeHandlers;
using Xunit;

// The repository integration tests each run the schema migrations against the one shared TEDWREN_TEST_SQLSERVER
// database. Running test classes in parallel makes them race to apply the same migrations, producing spurious
// "there is already an object named ..." failures (the product applies migrations once at startup and the runner
// is idempotent on restart). Serialising the assembly removes the race; the DB tests are few and fast.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Registers the Dapper type handlers (DateOnly / TimeOnly / DateTimeOffset) once before any test in this
/// assembly runs, mirroring what <c>AddSqlDataAccess</c> does at application startup. The repository integration
/// tests construct repositories directly (not through DI), so without this they would implicitly depend on some
/// other test having called <see cref="DapperTypeHandlers.EnsureRegistered"/> first — leaving them order- and
/// parallelism-dependent (a <see cref="DateOnly"/> / <see cref="DateTimeOffset"/> parameter otherwise throws
/// "cannot be used as a parameter value"). Registration is process-wide and idempotent.
/// </summary>
internal static class TestModuleInitializer
{
    /// <summary>Runs once when the test assembly loads.</summary>
    [ModuleInitializer]
    internal static void Init() => DapperTypeHandlers.EnsureRegistered();
}
