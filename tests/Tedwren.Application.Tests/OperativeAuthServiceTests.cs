using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Auth;
using Tedwren.Application.Mobile;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Tests;

/// <summary>Verifies operative mobile auth: OTP issue (no enumeration), device binding, and refresh rotation (M2).</summary>
public sealed class OperativeAuthServiceTests
{
    private const string Phone = "+447700900123";
    private const string PhoneNational = "07700900123";
    private const string Code = "123456";
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public async Task RequestOtp_texts_the_code_to_an_engaged_operative()
    {
        var sut = await CreateAsync();
        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));

        Assert.Single(sut.Sms.Sent);
        Assert.Equal(Phone, sut.Sms.Sent[0].To);
        Assert.Contains(Code, sut.Sms.Sent[0].Message);
        Assert.NotNull(await sut.Challenges.GetByPhoneAsync(Phone));
    }

    [Fact]
    public async Task RequestOtp_is_silent_for_an_unknown_number()
    {
        var sut = await CreateAsync(seedOperative: false);
        await sut.Service.RequestOtpAsync(new RequestOtpRequest("07700900999"));

        Assert.Empty(sut.Sms.Sent);
        Assert.Null(await sut.Challenges.GetByPhoneAsync("+447700900999"));
    }

    [Fact]
    public async Task VerifyOtp_binds_the_device_and_returns_tokens()
    {
        var sut = await CreateAsync();
        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));

        var outcome = await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, Code, "device-1", "Pixel"));

        Assert.Equal(OperativeAuthStatus.Success, outcome.Status);
        Assert.Equal(sut.PersonId, outcome.Result!.PersonId);
        Assert.Equal(CompanyId, outcome.Result.CompanyId);
        Assert.False(string.IsNullOrEmpty(outcome.Result.RefreshToken));
        var device = await sut.Devices.GetByDeviceIdAsync("device-1");
        Assert.Equal(OperativeDeviceStatus.Active, device!.Status);
        Assert.Null(await sut.Challenges.GetByPhoneAsync(Phone)); // consumed
    }

    [Fact]
    public async Task VerifyOtp_wrong_code_is_invalid_and_counts_the_attempt()
    {
        var sut = await CreateAsync();
        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));

        var outcome = await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, "000000", "device-1", null));

        Assert.Equal(OperativeAuthStatus.Invalid, outcome.Status);
        Assert.Equal(1, (await sut.Challenges.GetByPhoneAsync(Phone))!.Attempts);
    }

    [Fact]
    public async Task VerifyOtp_expired_challenge_is_invalid()
    {
        var sut = await CreateAsync();
        await sut.Challenges.AddAsync(new OtpChallenge
        {
            PhoneNumber = Phone,
            CodeHash = PasswordHasher.Hash(Code),
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        });

        var outcome = await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, Code, "device-1", null));

        Assert.Equal(OperativeAuthStatus.Invalid, outcome.Status);
    }

    [Fact]
    public async Task VerifyOtp_conflicts_when_the_operative_is_already_on_another_device()
    {
        var sut = await CreateAsync();
        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));
        await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, Code, "device-1", null));

        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));
        var outcome = await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, Code, "device-2", null));

        Assert.Equal(OperativeAuthStatus.DeviceConflict, outcome.Status);
    }

    [Fact]
    public async Task VerifyOtp_conflicts_when_the_device_belongs_to_another_operative()
    {
        var sut = await CreateAsync();

        // A different operative binds device-1 first.
        var other = new Person { PhoneNumber = PhoneNumber.Parse("+447700900222") };
        await sut.People.AddAsync(other);
        await sut.Engagements.AddAsync(new Engagement { CompanyId = CompanyId, PersonId = other.Id, Name = "Bob Other" });
        await sut.Service.RequestOtpAsync(new RequestOtpRequest("+447700900222"));
        await sut.Service.VerifyOtpAsync(new VerifyOtpRequest("+447700900222", Code, "device-1", null));

        // Our operative tries to use the same device.
        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));
        var outcome = await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, Code, "device-1", null));

        Assert.Equal(OperativeAuthStatus.DeviceConflict, outcome.Status);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_rejects_the_old_one()
    {
        var sut = await CreateAsync();
        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));
        var enrol = await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, Code, "device-1", null));
        var firstRefresh = enrol.Result!.RefreshToken;

        var refreshed = await sut.Service.RefreshAsync(new RefreshTokenRequest(firstRefresh, "device-1"));
        Assert.Equal(OperativeAuthStatus.Success, refreshed.Status);
        Assert.NotEqual(firstRefresh, refreshed.Result!.RefreshToken);

        // The rotated-away token no longer works.
        var reuse = await sut.Service.RefreshAsync(new RefreshTokenRequest(firstRefresh, "device-1"));
        Assert.Equal(OperativeAuthStatus.Invalid, reuse.Status);
    }

    [Fact]
    public async Task Refresh_is_invalid_once_the_engagement_is_archived()
    {
        var sut = await CreateAsync();
        await sut.Service.RequestOtpAsync(new RequestOtpRequest(PhoneNational));
        var enrol = await sut.Service.VerifyOtpAsync(new VerifyOtpRequest(PhoneNational, Code, "device-1", null));

        var engagement = await sut.Engagements.GetByCompanyAndPersonAsync(CompanyId, sut.PersonId);
        engagement!.Status = EngagementStatus.Archived;
        await sut.Engagements.UpdateAsync(engagement);

        var refreshed = await sut.Service.RefreshAsync(new RefreshTokenRequest(enrol.Result!.RefreshToken, "device-1"));
        Assert.Equal(OperativeAuthStatus.Invalid, refreshed.Status);
    }

    private sealed record Sut(
        OperativeAuthService Service,
        InMemoryPersonRepository People,
        InMemoryEngagementRepository Engagements,
        InMemoryOperativeDeviceRepository Devices,
        InMemoryOtpChallengeRepository Challenges,
        CapturingSmsSender Sms,
        Guid PersonId);

    private static async Task<Sut> CreateAsync(bool seedOperative = true)
    {
        var store = new InMemoryOrganisationStore(seed: false);
        var people = new InMemoryPersonRepository(store);
        var engagements = new InMemoryEngagementRepository(store);

        var personId = Guid.Empty;
        if (seedOperative)
        {
            var person = new Person { PhoneNumber = PhoneNumber.Parse(Phone) };
            await people.AddAsync(person);
            await engagements.AddAsync(new Engagement { CompanyId = CompanyId, PersonId = person.Id, Name = "Alex Operative" });
            personId = person.Id;
        }

        var devices = new InMemoryOperativeDeviceRepository();
        var challenges = new InMemoryOtpChallengeRepository();
        var sms = new CapturingSmsSender();
        var service = new OperativeAuthService(
            people, engagements, devices, challenges, sms,
            new FakeOperativeTokenIssuer(), new FixedOtpCodeGenerator(Code), new JwtOptions());

        return new Sut(service, people, engagements, devices, challenges, sms, personId);
    }

    private sealed class CapturingSmsSender : ISmsSender
    {
        public List<(string To, string Message)> Sent { get; } = new();

        public Task SendAsync(string toNumber, string message, CancellationToken cancellationToken = default)
        {
            Sent.Add((toNumber, message));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOperativeTokenIssuer : IOperativeTokenIssuer
    {
        public IssuedToken IssueAccessToken(Guid personId, Guid companyId, string name, string deviceId)
            => new($"access-{personId}", DateTimeOffset.UtcNow.AddMinutes(60));
    }

    private sealed class FixedOtpCodeGenerator : IOtpCodeGenerator
    {
        private readonly string _code;
        public FixedOtpCodeGenerator(string code) => _code = code;
        public string Generate() => _code;
    }
}
