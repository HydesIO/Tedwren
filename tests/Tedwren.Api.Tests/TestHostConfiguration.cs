using System.Runtime.CompilerServices;

namespace Tedwren.Api.Tests;

/// <summary>
/// Forces the API test host onto the in-memory data doubles. The product default is
/// <c>DataSource:Mode=Database</c>; the end-to-end API tests deliberately run against the in-memory
/// repositories for speed and isolation — no SQL Server required — by selecting the test-only
/// <c>InMemory</c> mode. Setting the value as an environment variable makes it win over
/// <c>appsettings.json</c> for every <c>WebApplicationFactory&lt;Program&gt;</c> created in this assembly,
/// without editing each test class.
/// </summary>
internal static class TestHostConfiguration
{
    /// <summary>Runs once when the test assembly loads, before any test executes.</summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("DataSource__Mode", "InMemory");
        Environment.SetEnvironmentVariable("Jobs__SchedulerEnabled", "false");
        // Authenticate every request as an Administrator so existing endpoint tests run unchanged. Auth-specific
        // tests turn this off per-client (UseSetting("Auth:TestBypass","false")) to exercise the real JWT path.
        Environment.SetEnvironmentVariable("Auth__TestBypass", "true");
    }
}
