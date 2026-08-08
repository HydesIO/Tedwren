namespace Tedwren.Domain.Enums;

/// <summary>
/// Whether a company's engagement of a person is currently active or archived. A leaver is archived
/// (SF-3): they stop appearing in the register and stop counting toward billing, everything recorded
/// about them survives, and they can be reactivated intact rather than re-created.
/// </summary>
public enum EngagementStatus
{
    /// <summary>Currently engaged — appears in the register and counts toward billing.</summary>
    Active = 0,

    /// <summary>Left/archived — hidden from the register and not billed, but fully retained.</summary>
    Archived = 1,
}
