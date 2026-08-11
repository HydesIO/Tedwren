using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Workforce;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the org-wide workforce endpoints (<c>/api/workforce</c>): an operative added
/// to a company appears in the register and resolves to a profile by slug.
/// </summary>
public sealed class WorkforceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public WorkforceApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AddedOperative_AppearsInRegister_AndResolvesBySlug()
    {
        var client = _factory.CreateClient();
        // R15: the register is tenant-scoped, so add the operative to the signed-in caller's own company.
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;
        var name = "Workforce Tester " + Guid.NewGuid().ToString("N")[..6];

        var add = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, name, "07700900742", "Scaffolding", null));
        Assert.True(add.IsSuccessStatusCode);

        var register = await client.GetFromJsonAsync<List<OperativeListItemDto>>("/api/workforce");
        var row = Assert.Single(register!, o => o.Name == name);
        Assert.Equal("Scaffolding", row.Trade);
        Assert.Equal(ComplianceState.Pending, row.State);   // no cards yet — pending, never invented (SF-8)

        var detail = await client.GetFromJsonAsync<OperativeDetailDto>($"/api/workforce/{row.Slug}");
        Assert.NotNull(detail);
        Assert.Equal(name, detail!.Name);
        Assert.Empty(detail.Qualifications);
    }

    [Fact]
    public async Task UnknownSlug_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workforce/no-such-operative");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
