namespace Tedwren.Domain.Enums;

/// <summary>
/// Which Tedwren product a lead is for — the two models are sold separately (PRD §2). Drives the revenue
/// basis: a main contractor is billed per active site, a subcontractor per operative band (PRD §9).
/// </summary>
public enum LeadModel
{
    /// <summary>Undecided / not yet qualified.</summary>
    Unknown = 0,

    /// <summary>Principal/main contractor — runs sites, billed per active site (a dispersed scheme is one site, SF-25/26).</summary>
    MainContractor = 1,

    /// <summary>Subcontractor — sends operatives to others' sites, billed per operative band.</summary>
    Subcontractor = 2,
}
