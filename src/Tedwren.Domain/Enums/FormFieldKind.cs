namespace Tedwren.Domain.Enums;

/// <summary>
/// The kind of a single field in a customer-built form (the PRD-Phase 2 checklist/inspection engine,
/// PRD §8.2 "checklist and inspection engine"). The set is the full spectrum a template author can
/// choose from; it is data-driven — a template picks which kinds it uses and in what order — so new
/// kinds are added as a case here, never a rewrite of the renderer or persistence.
/// </summary>
public enum FormFieldKind
{
    /// <summary>A single line of free text.</summary>
    ShortText = 0,

    /// <summary>A multi-line block of free text.</summary>
    LongText = 1,

    /// <summary>A numeric value (validators may bound it, min/max/decimals).</summary>
    Number = 2,

    /// <summary>A calendar date.</summary>
    Date = 3,

    /// <summary>A time of day.</summary>
    Time = 4,

    /// <summary>A single choice from a fixed option list.</summary>
    Dropdown = 5,

    /// <summary>Zero or more choices from a fixed option list.</summary>
    MultiSelect = 6,

    /// <summary>A yes/no answer (rendered as a switch, house rule — never a checkbox).</summary>
    YesNo = 7,

    /// <summary>A red/amber/green status item (site/plant inspections).</summary>
    RagStatus = 8,

    /// <summary>A captured photograph.</summary>
    Photo = 9,

    /// <summary>An uploaded file/attachment.</summary>
    FileUpload = 10,

    /// <summary>A captured signature.</summary>
    Signature = 11,

    /// <summary>A display-only section/heading (no captured value).</summary>
    Heading = 12,

    /// <summary>Display-only instruction/guidance text (no captured value).</summary>
    Instruction = 13,
}
