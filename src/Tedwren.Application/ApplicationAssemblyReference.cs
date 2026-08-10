namespace Tedwren.Application;

/// <summary>
/// Stable type anchor for the <c>Tedwren.Application</c> assembly. It carries no behaviour; it
/// exists so the API host and tests can reference this assembly for dependency-injection
/// registration without depending on a concrete service type. Business services (each behind an
/// interface, following the Single Responsibility Principle) are introduced from Phase 8 onward.
/// </summary>
public static class ApplicationAssemblyReference
{
}
