using Tedwren.Abstractions.Common;
using Tedwren.UiComponents.Models;

namespace Tedwren.Client.Services;

/// <summary>
/// Maps the provider-neutral <see cref="ComplianceState"/> carried on API/DTO contracts to the UI
/// <see cref="StatusKind"/> that drives <c>StatusPill</c> colour. Keeps the shared contracts free of
/// any UI dependency while letting the pages render the familiar traffic-light states.
/// </summary>
public static class ComplianceStatusView
{
    /// <summary>Maps a compliance state to its display status kind.</summary>
    public static StatusKind ToStatusKind(ComplianceState state) => state switch
    {
        ComplianceState.Compliant => StatusKind.Success,
        ComplianceState.AtRisk => StatusKind.Warning,
        ComplianceState.NonCompliant => StatusKind.Danger,
        _ => StatusKind.Neutral,
    };
}
