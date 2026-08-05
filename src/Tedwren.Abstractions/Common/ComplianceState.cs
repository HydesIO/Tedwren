namespace Tedwren.Abstractions.Common;

/// <summary>
/// A provider-neutral compliance state carried on DTOs, so the shared contracts do not depend on any
/// UI enum. The client maps this to its display status/colour. Until cards exist (Phase 9), records
/// served from the database report <see cref="Pending"/> rather than an invented percentage.
/// </summary>
public enum ComplianceState
{
    /// <summary>Not yet assessed (e.g. no cards captured yet).</summary>
    Pending = 0,

    /// <summary>Compliant.</summary>
    Compliant = 1,

    /// <summary>At risk (something expiring soon).</summary>
    AtRisk = 2,

    /// <summary>Non-compliant (something expired or missing).</summary>
    NonCompliant = 3,
}
