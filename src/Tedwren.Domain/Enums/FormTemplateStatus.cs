namespace Tedwren.Domain.Enums;

/// <summary>
/// The lifecycle state of a form template version. Templates are versioned and append-only (R4/R10/R16):
/// a draft is edited freely, publishing freezes that version, and archiving retires a family from the
/// library without deleting its history.
/// </summary>
public enum FormTemplateStatus
{
    /// <summary>Being authored; editable in place.</summary>
    Draft = 0,

    /// <summary>Published and frozen; editing creates a new draft version.</summary>
    Published = 1,

    /// <summary>Retired from the library; retained for historic submissions.</summary>
    Archived = 2,
}
