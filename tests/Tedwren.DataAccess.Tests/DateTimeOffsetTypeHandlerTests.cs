using Tedwren.DataAccess.TypeHandlers;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Unit tests for <see cref="DateTimeOffsetTypeHandler"/> (no database required). PostgreSQL's Npgsql provider
/// surfaces a <c>timestamptz</c> column as a UTC <see cref="DateTime"/>, which Dapper cannot bind to a record
/// constructor parameter typed <see cref="DateTimeOffset"/> — the handler normalises that read so the same
/// repositories materialise on both SQL Server and PostgreSQL.
/// </summary>
public sealed class DateTimeOffsetTypeHandlerTests
{
    private readonly DateTimeOffsetTypeHandler _handler = new();

    [Fact] // The SQL Server path already yields DateTimeOffset — passed through unchanged.
    public void Parse_DateTimeOffset_IsReturnedUnchanged()
    {
        var value = new DateTimeOffset(2026, 9, 19, 13, 20, 0, TimeSpan.FromHours(2));

        Assert.Equal(value, _handler.Parse(value));
    }

    [Fact] // The PostgreSQL path yields a UTC DateTime — read back as a zero-offset instant, not shifted by local time.
    public void Parse_UtcDateTime_BecomesZeroOffset()
    {
        var utc = new DateTime(2026, 9, 19, 13, 20, 0, DateTimeKind.Utc);

        var result = _handler.Parse(utc);

        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(utc, result.UtcDateTime);
    }

    [Fact] // A bare (Kind=Unspecified) timestamp is treated as UTC rather than local, so the instant never drifts.
    public void Parse_UnspecifiedDateTime_IsTreatedAsUtc()
    {
        var unspecified = new DateTime(2026, 9, 19, 13, 20, 0, DateTimeKind.Unspecified);

        var result = _handler.Parse(unspecified);

        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(13, result.Hour);
    }

    [Fact] // A provider that returns the value as an ISO string is parsed as an absolute (UTC-assumed) instant.
    public void Parse_IsoString_IsParsed()
    {
        var result = _handler.Parse("2026-09-19T13:20:00Z");

        Assert.Equal(new DateTimeOffset(2026, 9, 19, 13, 20, 0, TimeSpan.Zero), result);
    }
}
