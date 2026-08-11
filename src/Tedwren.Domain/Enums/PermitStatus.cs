namespace Tedwren.Domain.Enums;

/// <summary>The lifecycle state of a permit to work.</summary>
public enum PermitStatus
{
    /// <summary>Saved but not yet issued.</summary>
    Draft = 0,

    /// <summary>Issued and in force for its valid period.</summary>
    Issued = 1,
}
