using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the reference-data (<c>/api/reference</c>) and current-operator (<c>/api/me</c>)
/// endpoints that replaced the client's sample form/shell data.
/// </summary>
public sealed class ReferenceAndIdentityApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses the shared test host (in-memory data double).</summary>
    public ReferenceAndIdentityApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Theory]
    [InlineData(ReferenceListKeys.CompanyTypes, "Main Contractor")]
    [InlineData(ReferenceListKeys.Trades, "Groundworks")]
    [InlineData(ReferenceListKeys.PermitTypes, "Hot Works")]
    [InlineData(ReferenceListKeys.Regions, "London")]
    public async Task Reference_ReturnsSeededList(string listKey, string expectedValue)
    {
        var client = CreateClient();

        var values = await client.GetFromJsonAsync<List<string>>($"/api/reference/{listKey}");

        Assert.NotNull(values);
        Assert.NotEmpty(values!);
        Assert.Contains(expectedValue, values!);
    }

    [Fact]
    public async Task Reference_UnknownKey_ReturnsEmpty()
    {
        var client = CreateClient();

        var values = await client.GetFromJsonAsync<List<string>>("/api/reference/does-not-exist");

        Assert.NotNull(values);
        Assert.Empty(values!);
    }

    [Fact]
    public async Task Me_ReturnsCurrentOperatorWithRole()
    {
        var client = CreateClient();

        var user = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");

        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.Name));
        Assert.False(string.IsNullOrWhiteSpace(user.Role));
    }
}
