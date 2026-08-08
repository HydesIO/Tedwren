using Tedwren.Abstractions.Common;
using Tedwren.UiComponents.Models;

namespace Tedwren.Client.Services;

/// <summary>
/// Maps the provider-neutral <see cref="RiskState"/> carried on site DTOs to the UI <see cref="RiskSeverity"/>
/// that drives <c>RiskChip</c> colour, keeping the shared contracts free of any UI dependency.
/// </summary>
public static class RiskStateView
{
    /// <summary>Maps a risk state to its display severity.</summary>
    public static RiskSeverity ToSeverity(RiskState risk) => risk switch
    {
        RiskState.High => RiskSeverity.High,
        RiskState.Medium => RiskSeverity.Medium,
        _ => RiskSeverity.Low,
    };
}
