using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// One configurable step in an induction template (MC-3). Steps are data-driven so a customer can shape the
/// induction to their site; a required step must be demonstrably completed before the induction can finish
/// (MC-4). <paramref name="Reference"/> carries an optional linked resource — for a <see cref="InductionStepKind.Form"/>
/// step it is the assigned form's family id (Forms Library integration, requirement 5); null for every other kind.
/// </summary>
public sealed record InductionStep(string Id, InductionStepKind Kind, string Label, bool Required, string? Reference = null);
