using Dapper;
using Tedwren.DataAccess.TypeHandlers;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Guards the Dapper DateOnly/TimeOnly handlers. SQL Server returns date/time columns as
/// DateTime/TimeSpan; without these handlers Dapper cannot bind those values to record constructor
/// parameters typed DateOnly/TimeOnly and throws "one matching signature is required" at runtime
/// (this was the QualificationCard expiry-query failure). These tests need no database.
/// </summary>
public sealed class DapperTypeHandlerTests
{
    /// <summary>Registration is idempotent and marks DateOnly/TimeOnly as handled types for Dapper.</summary>
    [Fact]
    public void EnsureRegistered_MakesDateOnlyAndTimeOnlyHandledTypes()
    {
        DapperTypeHandlers.EnsureRegistered();
        DapperTypeHandlers.EnsureRegistered(); // second call must be a harmless no-op

        Assert.True(SqlMapper.HasTypeHandler(typeof(DateOnly)));
        Assert.True(SqlMapper.HasTypeHandler(typeof(TimeOnly)));
    }

    /// <summary>The handler reconstructs a DateOnly from the DateTime the SQL Server driver returns.</summary>
    [Fact]
    public void DateOnlyHandler_ParsesDateTimeFromDriver()
    {
        var handler = new DateOnlyTypeHandler();

        Assert.Equal(new DateOnly(2026, 8, 16), handler.Parse(new DateTime(2026, 8, 16, 13, 45, 0)));
        Assert.Equal(new DateOnly(2026, 8, 16), handler.Parse(new DateOnly(2026, 8, 16)));
    }

    /// <summary>The handler reconstructs a TimeOnly from the TimeSpan the SQL Server driver returns.</summary>
    [Fact]
    public void TimeOnlyHandler_ParsesTimeSpanFromDriver()
    {
        var handler = new TimeOnlyTypeHandler();

        Assert.Equal(new TimeOnly(13, 45, 0), handler.Parse(new TimeSpan(13, 45, 0)));
        Assert.Equal(new TimeOnly(13, 45, 0), handler.Parse(new TimeOnly(13, 45, 0)));
    }
}
