using Tedwren.DataAccess;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Scaffolding-level sanity test for the data-access assembly. The LocalDB integration harness
/// (transaction-per-test, purpose-created data, never mutating existing records) is introduced
/// with the first repository slice in Phase 8.
/// </summary>
public sealed class DataAccessAssemblyReferenceTests
{
    /// <summary>Verifies the data-access assembly is referenced and loadable from the test project.</summary>
    [Fact]
    public void DataAccessAssembly_IsReferenced()
    {
        Assert.NotNull(typeof(DataAccessAssemblyReference).Assembly);
    }
}
