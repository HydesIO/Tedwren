namespace Tedwren.DataAccess;

/// <summary>
/// Stable type anchor for the <c>Tedwren.DataAccess</c> assembly. It carries no behaviour; it
/// exists so the API host and integration tests can reference this assembly without depending on
/// a concrete repository type. The Dapper connection factory, the shared repository base and the
/// SQL Server / PostgreSQL dialect abstractions are introduced on the first data-access slice in
/// Phase 8, per the development plan.
/// </summary>
public static class DataAccessAssemblyReference
{
}
