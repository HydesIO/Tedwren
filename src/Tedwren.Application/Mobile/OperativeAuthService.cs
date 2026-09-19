using System.Security.Cryptography;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Mobile;

/// <summary>
/// Operative (mobile) authentication (M2): mobile-number + one-time-code enrolment (SF-1), device binding
/// ("one operative per device, one active device per operative" — a buddy-punching deterrent), and rotating,
/// device-bound refresh tokens. Only hashes of codes and refresh tokens are stored (PBKDF2). Biometric unlock
/// is entirely on-device (R17); this service issues nothing biometric.
/// </summary>
public sealed class OperativeAuthService : IOperativeAuthService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    private const int MaxAttempts = 5;

    private readonly IPersonRepository _people;
    private readonly IEngagementRepository _engagements;
    private readonly IOperativeDeviceRepository _devices;
    private readonly IOtpChallengeRepository _challenges;
    private readonly ISmsSender _sms;
    private readonly IOperativeTokenIssuer _tokens;
    private readonly IOtpCodeGenerator _codes;
    private readonly JwtOptions _jwt;

    /// <summary>Creates the service over its repositories, the SMS sender, the token issuer and the code generator.</summary>
    public OperativeAuthService(
        IPersonRepository people,
        IEngagementRepository engagements,
        IOperativeDeviceRepository devices,
        IOtpChallengeRepository challenges,
        ISmsSender sms,
        IOperativeTokenIssuer tokens,
        IOtpCodeGenerator codes,
        JwtOptions jwt)
    {
        _people = people;
        _engagements = engagements;
        _devices = devices;
        _challenges = challenges;
        _sms = sms;
        _tokens = tokens;
        _codes = codes;
        _jwt = jwt;
    }

    /// <summary>
    /// Issues and texts a one-time code, but only when the number resolves to an actively-engaged operative.
    /// Returns the same way regardless (no account enumeration); nothing is sent otherwise.
    /// </summary>
    public async Task RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (!PhoneNumber.TryParse(request.MobileNumber, out var phone))
        {
            return;
        }

        var person = await _people.GetByPhoneAsync(phone!, cancellationToken);
        if (person is null)
        {
            return;
        }

        var engagements = await _engagements.GetActiveByPersonAsync(person.Id, cancellationToken);
        if (engagements.Count == 0)
        {
            return;
        }

        var code = _codes.Generate();
        var challenge = new OtpChallenge
        {
            PhoneNumber = phone!.Value,
            CodeHash = PasswordHasher.Hash(code),
            ExpiresUtc = DateTimeOffset.UtcNow.Add(CodeLifetime),
        };

        await _challenges.DeleteByPhoneAsync(phone.Value, cancellationToken);
        await _challenges.AddAsync(challenge, cancellationToken);
        await _sms.SendAsync(phone.Value, $"Your Tedwren code is {code}. It expires in 5 minutes.", cancellationToken);
    }

    /// <summary>Verifies the one-time code, binds this device to the operative, and issues access + refresh tokens.</summary>
    public async Task<OperativeAuthOutcome> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (!PhoneNumber.TryParse(request.MobileNumber, out var phone))
        {
            return OperativeAuthOutcome.Invalid("Enter a valid mobile number.");
        }

        var challenge = await _challenges.GetByPhoneAsync(phone!.Value, cancellationToken);
        if (challenge is null)
        {
            return OperativeAuthOutcome.Invalid("That code was wrong or has expired.");
        }

        var now = DateTimeOffset.UtcNow;
        if (challenge.IsExpired(now) || challenge.Attempts >= MaxAttempts)
        {
            await _challenges.DeleteAsync(challenge.Id, cancellationToken);
            return OperativeAuthOutcome.Invalid("That code was wrong or has expired.");
        }

        if (!PasswordHasher.Verify(request.Code, challenge.CodeHash))
        {
            challenge.Attempts++;
            await _challenges.UpdateAsync(challenge, cancellationToken);
            return OperativeAuthOutcome.Invalid("That code was wrong or has expired.");
        }

        var person = await _people.GetByPhoneAsync(phone!, cancellationToken);
        if (person is null)
        {
            return OperativeAuthOutcome.Invalid("That code was wrong or has expired.");
        }

        var engagements = await _engagements.GetActiveByPersonAsync(person.Id, cancellationToken);
        var engagement = engagements.FirstOrDefault();
        if (engagement is null)
        {
            return OperativeAuthOutcome.Invalid("You are not currently set up on any site.");
        }

        // One operative per device, one active device per operative (buddy-punching deterrent).
        var byDevice = await _devices.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        if (byDevice is { Status: OperativeDeviceStatus.Active } && byDevice.PersonId != person.Id)
        {
            return OperativeAuthOutcome.Conflict("This device is already registered to another operative.");
        }

        var activeForPerson = await _devices.GetActiveByPersonIdAsync(person.Id, cancellationToken);
        if (activeForPerson is not null && activeForPerson.DeviceId != request.DeviceId)
        {
            return OperativeAuthOutcome.Conflict("You are already set up on another device. Ask an administrator to move you to this one.");
        }

        var (rawRefresh, refreshHash, refreshExpiry) = NewRefreshToken(now);
        var device = byDevice ?? new OperativeDevice { PersonId = person.Id, CompanyId = engagement.CompanyId, DeviceId = request.DeviceId };
        device.PersonId = person.Id;
        device.CompanyId = engagement.CompanyId;
        device.DeviceName = request.DeviceName;
        device.Status = OperativeDeviceStatus.Active;
        device.RefreshTokenHash = refreshHash;
        device.RefreshTokenExpiresUtc = refreshExpiry;
        device.LastSeenUtc = now;

        if (byDevice is null)
        {
            await _devices.AddAsync(device, cancellationToken);
        }
        else
        {
            await _devices.UpdateAsync(device, cancellationToken);
        }

        await _challenges.DeleteAsync(challenge.Id, cancellationToken);

        var access = _tokens.IssueAccessToken(person.Id, engagement.CompanyId, engagement.Name, request.DeviceId);
        return OperativeAuthOutcome.Ok(new MobileAuthResultDto(
            access.Token, access.ExpiresUtc, rawRefresh, refreshExpiry, engagement.Name, person.Id, engagement.CompanyId));
    }

    /// <summary>Rotates a valid, device-bound refresh token for a fresh access token (and a new refresh token).</summary>
    public async Task<OperativeAuthOutcome> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var device = await _devices.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (device is null || !device.CanRefresh(now) || !PasswordHasher.Verify(request.RefreshToken, device.RefreshTokenHash))
        {
            return OperativeAuthOutcome.Invalid("Your session has expired. Please sign in again.");
        }

        // The operative must still be actively engaged in the token's company (archiving revokes access, SF-3).
        var engagement = await _engagements.GetByCompanyAndPersonAsync(device.CompanyId, device.PersonId, cancellationToken);
        if (engagement is null || engagement.Status != EngagementStatus.Active)
        {
            return OperativeAuthOutcome.Invalid("Your session has expired. Please sign in again.");
        }

        var (rawRefresh, refreshHash, refreshExpiry) = NewRefreshToken(now);
        device.RefreshTokenHash = refreshHash;
        device.RefreshTokenExpiresUtc = refreshExpiry;
        device.LastSeenUtc = now;
        await _devices.UpdateAsync(device, cancellationToken);

        var access = _tokens.IssueAccessToken(device.PersonId, device.CompanyId, engagement.Name, device.DeviceId);
        return OperativeAuthOutcome.Ok(new MobileAuthResultDto(
            access.Token, access.ExpiresUtc, rawRefresh, refreshExpiry, engagement.Name, device.PersonId, device.CompanyId));
    }

    /// <summary>Generates a new opaque refresh token, returning the raw value, its hash to store, and its expiry.</summary>
    private (string Raw, string Hash, DateTimeOffset ExpiresUtc) NewRefreshToken(DateTimeOffset now)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (raw, PasswordHasher.Hash(raw), now.AddDays(_jwt.RefreshLifetimeDays));
    }
}
