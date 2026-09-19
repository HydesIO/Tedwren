using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Onboarding;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Mobile;

namespace Tedwren.Api.Tests;

/// <summary>
/// Exercises the anonymous operative auth endpoints (M2) end-to-end over the in-memory host, with the OTP code
/// generator and SMS sender replaced by test doubles so the code is known and nothing is actually texted.
/// </summary>
public sealed class MobileAuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Code = "123456";
    private readonly WebApplicationFactory<Program> _factory;

    public MobileAuthApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IOtpCodeGenerator>(new FixedOtpCodeGenerator(Code));
            services.AddSingleton<ISmsSender>(new NoopSmsSender());
        })).CreateClient();

    [Fact]
    public async Task Enrol_then_refresh_succeeds()
    {
        var client = CreateClient();
        await SeedOperativeAsync(client, "07700900321");

        var requested = await client.PostAsJsonAsync("/api/mobile/auth/request-otp", new RequestOtpRequest("07700900321"));
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);

        var verify = await client.PostAsJsonAsync("/api/mobile/auth/verify-otp",
            new VerifyOtpRequest("07700900321", Code, "device-a", "Pixel"));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        var result = (await verify.Content.ReadFromJsonAsync<MobileAuthResultDto>())!;
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));

        var refresh = await client.PostAsJsonAsync("/api/mobile/auth/refresh",
            new RefreshTokenRequest(result.RefreshToken, "device-a"));
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }

    [Fact]
    public async Task Verify_with_wrong_code_is_unauthorized()
    {
        var client = CreateClient();
        await SeedOperativeAsync(client, "07700900654");
        await client.PostAsJsonAsync("/api/mobile/auth/request-otp", new RequestOtpRequest("07700900654"));

        var verify = await client.PostAsJsonAsync("/api/mobile/auth/verify-otp",
            new VerifyOtpRequest("07700900654", "000000", "device-b", null));
        Assert.Equal(HttpStatusCode.Unauthorized, verify.StatusCode);
    }

    [Fact]
    public async Task RequestOtp_for_unknown_number_is_ok_without_enumeration()
    {
        var client = CreateClient();
        var requested = await client.PostAsJsonAsync("/api/mobile/auth/request-otp", new RequestOtpRequest("07700900000"));
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
    }

    /// <summary>Seeds an actively-engaged operative for the bypass admin's company via the anonymous onboarding flow.</summary>
    private static async Task SeedOperativeAsync(HttpClient client, string mobileNumber)
    {
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;

        var create = await client.PostAsJsonAsync("/api/onboarding",
            new CreateOnboardingLinkRequest(companyId, "Alex Operative", "Groundworks", RequirePasscode: false));
        create.EnsureSuccessStatusCode();
        var link = (await create.Content.ReadFromJsonAsync<OnboardingLinkDto>())!;

        var submit = await client.PostAsJsonAsync(
            $"/api/onboarding/submit?token={link.Token}",
            new SubmitOnboardingDetailsRequest("Alex Operative", mobileNumber, "Groundworks"));
        submit.EnsureSuccessStatusCode();
    }

    private sealed class NoopSmsSender : ISmsSender
    {
        public Task SendAsync(string toNumber, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedOtpCodeGenerator : IOtpCodeGenerator
    {
        private readonly string _code;
        public FixedOtpCodeGenerator(string code) => _code = code;
        public string Generate() => _code;
    }
}
