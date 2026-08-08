namespace Tedwren.Domain;

/// <summary>
/// Stable type anchor for the <c>Tedwren.Domain</c> assembly. It carries no behaviour; it exists
/// so other layers and tests can reference this assembly (for example for dependency-injection
/// assembly scanning) without taking a dependency on a concrete domain type whose name may
/// change. Domain entities and value objects (person, company, engagement, …) are introduced
/// from Phase 8 onward, per the development plan.
/// </summary>
public static class DomainAssemblyReference
{
}
