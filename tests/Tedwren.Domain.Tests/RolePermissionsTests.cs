using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the read-only auditor rule (SF-23): the <see cref="AccessRole.Auditor"/> role may never change
/// data, while every other role may.
/// </summary>
public sealed class RolePermissionsTests
{
    [Theory]
    [InlineData(AccessRole.Administrator)]
    [InlineData(AccessRole.ComplianceManager)]
    [InlineData(AccessRole.SiteManager)]
    public void NonAuditorRoles_CanWrite(AccessRole role) =>
        Assert.True(RolePermissions.CanWrite(role));

    [Fact]
    public void AuditorRole_IsReadOnly() =>
        Assert.False(RolePermissions.CanWrite(AccessRole.Auditor));
}
