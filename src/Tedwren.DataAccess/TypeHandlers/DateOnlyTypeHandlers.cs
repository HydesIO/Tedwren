using System.Data;
using Dapper;

namespace Tedwren.DataAccess.TypeHandlers;

/// <summary>
/// Dapper handler mapping <see cref="DateOnly"/> to a SQL <c>date</c>. Microsoft.Data.SqlClient
/// surfaces <c>date</c> columns as <see cref="DateTime"/>, which Dapper cannot reconcile with a
/// record constructor parameter typed <see cref="DateOnly"/> (it fails constructor matching with
/// "one matching signature is required"). Registering this handler makes both directions explicit
/// and lets <see cref="SqlMapper.HasTypeHandler"/> satisfy constructor matching for
/// <see cref="DateOnly"/> and its nullable form. Npgsql maps <c>date</c> to <see cref="DateOnly"/>
/// natively, so the handler is a harmless pass-through there.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    /// <summary>Writes a <see cref="DateOnly"/> as a <c>date</c> parameter.</summary>
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    /// <summary>Reads a stored value (usually <see cref="DateTime"/>) back into a <see cref="DateOnly"/>.</summary>
    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        string s => DateOnly.Parse(s),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value)),
    };
}

/// <summary>
/// Dapper handler mapping <see cref="TimeOnly"/> to a SQL <c>time</c>, mirroring
/// <see cref="DateOnlyTypeHandler"/> so time-of-day columns round-trip through the same path.
/// </summary>
public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    /// <summary>Writes a <see cref="TimeOnly"/> as a <c>time</c> parameter.</summary>
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    /// <summary>Reads a stored value (usually <see cref="TimeSpan"/>) back into a <see cref="TimeOnly"/>.</summary>
    public override TimeOnly Parse(object value) => value switch
    {
        TimeOnly t => t,
        TimeSpan ts => TimeOnly.FromTimeSpan(ts),
        DateTime dt => TimeOnly.FromDateTime(dt),
        string s => TimeOnly.Parse(s),
        _ => TimeOnly.FromTimeSpan((TimeSpan)value),
    };
}

/// <summary>
/// Dapper handler mapping <see cref="DateTimeOffset"/> across both engines. Microsoft.Data.SqlClient surfaces
/// <c>datetimeoffset</c> columns as <see cref="DateTimeOffset"/> directly, but Npgsql surfaces <c>timestamptz</c>
/// columns as a UTC <see cref="DateTime"/>, which Dapper cannot reconcile with a record constructor parameter
/// typed <see cref="DateTimeOffset"/> (it fails constructor matching with "one matching signature is required" —
/// the same failure the <see cref="DateOnlyTypeHandler"/> exists to prevent). Registering this handler normalises
/// the read on PostgreSQL — a stored UTC instant becomes a zero-offset <see cref="DateTimeOffset"/> — and is a
/// harmless pass-through on SQL Server. The write side only sets the parameter value, leaving the provider's
/// native type inference intact (<see cref="DateTimeOffset"/> maps to <c>datetimeoffset</c> on SQL Server and
/// <c>timestamptz</c> on PostgreSQL), so it matches the prior, working insert behaviour on both engines.
/// </summary>
public sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    /// <summary>Writes a <see cref="DateTimeOffset"/> parameter, letting the provider infer its native column type.</summary>
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) => parameter.Value = value;

    /// <summary>Reads a stored value back into a <see cref="DateTimeOffset"/>, treating a bare DB timestamp as UTC.</summary>
    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dto => dto,
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
        string s => DateTimeOffset.Parse(s, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal),
        _ => new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(value), DateTimeKind.Utc)),
    };
}

/// <summary>Registers the Dapper type handlers once for the whole process.</summary>
public static class DapperTypeHandlers
{
    private static readonly object Gate = new();
    private static bool _registered;

    /// <summary>
    /// Idempotently registers the <see cref="DateOnly"/>/<see cref="TimeOnly"/> handlers. Dapper's
    /// registry is a static, process-wide store, so this must run once before any query executes.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        lock (Gate)
        {
            if (_registered)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
            _registered = true;
        }
    }
}
