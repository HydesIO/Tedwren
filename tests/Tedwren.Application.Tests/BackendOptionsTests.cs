using Tedwren.Abstractions.Configuration;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the defaults of the mock/database switch. The backend must default to mock data so
/// that, until a database is configured, behaviour is identical to the current sample-data app.
/// </summary>
public sealed class BackendOptionsTests
{
    /// <summary>A freshly-constructed <see cref="BackendOptions"/> defaults to mock data.</summary>
    [Fact]
    public void Default_Mode_IsMock()
    {
        var options = new BackendOptions();

        Assert.Equal(DataSourceMode.Mock, options.Mode);
        Assert.Equal(DatabaseProvider.SqlServer, options.Provider);
    }

    /// <summary>The bound section name matches the configuration key used across the solution.</summary>
    [Fact]
    public void SectionName_IsDataSource()
    {
        Assert.Equal("DataSource", BackendOptions.SectionName);
    }
}
