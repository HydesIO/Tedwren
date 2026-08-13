namespace Tedwren.Domain.Enums;

/// <summary>
/// Where a form is completed / assigned. A form can be filled at organisation or site level (requirement 7),
/// and later assigned to a site operator or the induction wizard (assignment lands in a later phase).
/// </summary>
public enum FormScope
{
    /// <summary>Completed for the organisation (company) as a whole.</summary>
    Organisation = 0,

    /// <summary>Completed against a specific site.</summary>
    Site = 1,

    /// <summary>Completed by/against a specific operator (person).</summary>
    Operator = 2,

    /// <summary>Completed as part of an induction.</summary>
    Induction = 3,
}
