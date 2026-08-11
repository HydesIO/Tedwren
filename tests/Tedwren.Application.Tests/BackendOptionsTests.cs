using Tedwren.Abstractions.Configuration;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the defaults of the data-source options. The backend defaults to the database — the only
/// supported runtime mode; the in-memory mode is a test-only double selected explicitly by the test host.
/// </summary>
public sealed class BackendOptionsTests
{
    /// <summary>A freshly-constructed <see cref="BackendOptions"/> defaults to the database.</summary>
    [Fact]
    public void Default_Mode_IsDatabase()
    {
        var options = new BackendOptions();

        Assert.Equal(DataSourceMode.Database, options.Mode);
        Assert.Equal(DatabaseProvider.SqlServer, options.Provider);
    }

    /// <summary>The bound section name matches the configuration key used across the solution.</summary>
    [Fact]
    public void SectionName_IsDataSource()
    {
        Assert.Equal("DataSource", BackendOptions.SectionName);
    }
}
