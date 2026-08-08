namespace Tedwren.Domain.Notifications;

/// <summary>The channel an expiry warning is delivered on (SF-9): the worker by text, the administrator by email.</summary>
public enum NotificationChannel
{
    /// <summary>SMS to the worker's mobile.</summary>
    Sms = 0,

    /// <summary>Email to the responsible administrator/manager.</summary>
    Email = 1,
}
