using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock attendance repository (API <c>DataSource=Mock</c>). Registered as
/// a singleton so attendance persists across requests within the host. Starts empty (attendance accrues from
/// sign-ins).
/// </summary>
public sealed class InMemoryAttendanceStore
{
    /// <summary>Attendance records by id (append-only, R4).</summary>
    public ConcurrentDictionary<Guid, AttendanceRecord> Records { get; } = new();
}
