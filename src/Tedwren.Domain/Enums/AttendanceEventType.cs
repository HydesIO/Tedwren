namespace Tedwren.Domain.Enums;

/// <summary>The kind of attendance event recorded (SF-13–SF-19). The log is append-only (R4).</summary>
public enum AttendanceEventType
{
    /// <summary>A worker signing in / confirming arrival.</summary>
    SignIn = 0,

    /// <summary>A worker signing out / confirming departure.</summary>
    SignOut = 1,

    /// <summary>A flag appended when a sign-in was left open overnight (SF-19) — the record is flagged, not truncated.</summary>
    OvernightFlag = 2,
}
