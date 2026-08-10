using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// One configurable step in an induction template (MC-3). Steps are data-driven so a customer can shape the
/// induction to their site; a required step must be demonstrably completed before the induction can finish
/// (MC-4).
/// </summary>
public sealed record InductionStep(string Id, InductionStepKind Kind, string Label, bool Required);
