using Tedwren.Domain;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Scaffolding-level sanity tests for the domain assembly. They confirm the project is wired and
/// testable; substantive value-object and entity tests are added alongside their types in Phase 8.
/// </summary>
public sealed class DomainAssemblyReferenceTests
{
    /// <summary>Verifies the domain assembly is referenced and loadable from the test project.</summary>
    [Fact]
    public void DomainAssembly_IsReferenced()
    {
        Assert.NotNull(typeof(DomainAssemblyReference).Assembly);
    }
}
