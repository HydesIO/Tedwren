namespace Tedwren.Abstractions.Contracts.DemoData;

/// <summary>
/// A snapshot of whether the Tedwren demonstration dataset is currently present, with per-area counts so the
/// Product Admin portal can show the operator what exists before they recreate or delete it. Counts are drawn
/// from the two fixed demo companies only (nothing else is ever reported or touched).
/// </summary>
/// <param name="Present">True when the demo companies exist in the database.</param>
/// <param name="Companies">Number of demo companies present (0 or 2).</param>
/// <param name="Users">Number of demo administrator/user accounts present.</param>
/// <param name="Sites">Number of demo sites present across both companies.</param>
/// <param name="Operatives">Number of demo operative engagements present across both companies.</param>
/// <param name="QualificationCards">Number of demo qualification cards present.</param>
/// <param name="AttendanceRecords">Number of demo attendance records present.</param>
/// <param name="Payments">Number of demo commercial payments present (reporting history).</param>
public sealed record DemoDataStatus(
    bool Present,
    int Companies,
    int Users,
    int Sites,
    int Operatives,
    int QualificationCards,
    int AttendanceRecords,
    int Payments);

/// <summary>
/// A live progress snapshot for a running seed/delete operation, polled by the Product Admin dialog to drive a
/// determinate progress indicator. <see cref="Percent"/> is 0–100. When <see cref="Running"/> is false and
/// <see cref="Percent"/> is 100 the last operation finished; <see cref="Error"/> is set when it failed.
/// </summary>
/// <param name="Running">True while an operation is in progress.</param>
/// <param name="Operation">The operation label ("Seeding" / "Deleting"), or null when idle.</param>
/// <param name="Stage">The current stage description (e.g. "Sites", "Payment history").</param>
/// <param name="Completed">Number of stages completed.</param>
/// <param name="Total">Total number of stages.</param>
/// <param name="Percent">Overall completion 0–100.</param>
/// <param name="Error">A failure message when the last operation failed, otherwise null.</param>
public sealed record DemoDataProgress(
    bool Running,
    string? Operation,
    string? Stage,
    int Completed,
    int Total,
    int Percent,
    string? Error);
