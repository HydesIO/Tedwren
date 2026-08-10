using System.Runtime.CompilerServices;

namespace Tedwren.Api.Tests;

/// <summary>
/// Forces the API test host onto the in-memory data doubles. The product default is now
/// <c>DataSource:Mode=Database</c> (Mock is deprecated for runtime use), but the end-to-end API tests
/// deliberately run against the in-memory repositories for speed and isolation — no SQL Server required.
/// Setting the value as an environment variable makes it win over <c>appsettings.json</c> for every
/// <c>WebApplicationFactory&lt;Program&gt;</c> created in this assembly, without editing each test class.
/// </summary>
internal static class TestHostConfiguration
{
    /// <summary>Runs once when the test assembly loads, before any test executes.</summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("DataSource__Mode", "Mock");
        Environment.SetEnvironmentVariable("Jobs__SchedulerEnabled", "false");
    }
}
