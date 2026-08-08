namespace Tedwren.Abstractions.Contracts.Entitlements;

/// <summary>A module and whether the customer has it enabled (SF-22, Q2).</summary>
public sealed record ModuleEntitlementDto(string Key, string Name, string Description, bool Enabled);
