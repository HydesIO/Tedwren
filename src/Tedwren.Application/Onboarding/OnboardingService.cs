using Tedwren.Abstractions.Contracts.Onboarding;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.CompliancePacks;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Onboarding;

/// <summary>
/// The self-service onboarding flow (SF-4, SUB-2). Reuses the organisation service to create the
/// person/engagement (SF-1/SF-2) and the qualification service to capture cards unconfirmed (SF-5/SF-6);
/// card photos are stored privately (R9). Link tokens/passcodes reuse the compliance-pack helpers.
/// </summary>
public sealed class OnboardingService : IOnboardingService
{
    private readonly IOnboardingLinkRepository _links;
    private readonly ICompanyRepository _companies;
    private readonly IOrganisationService _organisation;
    private readonly IQualificationService _qualifications;
    private readonly IImageStore _images;

    /// <summary>Default link lifetime (SUB-18: 30 days).</summary>
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromDays(30);

    /// <summary>Creates the service over its dependencies.</summary>
    public OnboardingService(
        IOnboardingLinkRepository links,
        ICompanyRepository companies,
        IOrganisationService organisation,
        IQualificationService qualifications,
        IImageStore images)
    {
        _links = links;
        _companies = companies;
        _organisation = organisation;
        _qualifications = qualifications;
        _images = images;
    }

    /// <summary>Creates an onboarding link and returns the token + (optional) passcode to share.</summary>
    public async Task<OnboardingLinkDto> CreateAsync(CreateOnboardingLinkRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        if (request.CompanyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(request));
        }

        string? passcode = null;
        string? passcodeHash = null;
        if (request.RequirePasscode)
        {
            passcode = PackPasscode.Generate();
            passcodeHash = PackPasscode.Hash(passcode);
        }

        var link = new OnboardingLink
        {
            Token = PackToken.Generate(),
            PasscodeHash = passcodeHash,
            CompanyId = request.CompanyId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            Trade = string.IsNullOrWhiteSpace(request.Trade) ? null : request.Trade.Trim(),
            ExpiresUtc = DateTimeOffset.UtcNow.Add(LinkLifetime),
            CreatedByUserId = createdByUserId,
        };

        await _links.AddAsync(link, cancellationToken);
        return new OnboardingLinkDto(link.Token, passcode, link.ExpiresUtc);
    }

    /// <summary>Returns the operative-facing view, or null when the link/passcode is invalid or expired.</summary>
    public async Task<OnboardingViewDto?> GetByTokenAsync(string token, string? passcode, CancellationToken cancellationToken = default)
    {
        var link = await AuthorizeAsync(token, passcode, cancellationToken);
        return link is null ? null : await BuildViewAsync(link, cancellationToken);
    }

    /// <summary>Submits the operative's details, creating/reusing the person + engagement (SF-1/SF-2).</summary>
    public async Task<OnboardingViewDto?> SubmitDetailsAsync(string token, string? passcode, SubmitOnboardingDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var link = await AuthorizeAsync(token, passcode, cancellationToken);
        if (link is null)
        {
            return null;
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Your name is required.", nameof(request));
        }

        var result = await _organisation.AddOperativeAsync(
            new AddOperativeRequest(link.CompanyId, name, request.MobileNumber, request.Trade ?? link.Trade, null), cancellationToken);

        if (result.PersonId is null)
        {
            // Only an unusable mobile number leaves no person; a duplicate still resolves to the existing one.
            throw new ArgumentException(result.Error ?? "Those details could not be accepted.", nameof(request));
        }

        link.PersonId = result.PersonId;
        link.EngagementId = result.EngagementId;
        link.Name = name;
        link.Trade = request.Trade ?? link.Trade;
        link.Status = OnboardingLinkStatus.Submitted;
        await _links.UpdateAsync(link, cancellationToken);

        return await BuildViewAsync(link, cancellationToken);
    }

    /// <summary>Captures a card (with optional photo) against the submitted operative (SF-5). Null if details not yet submitted.</summary>
    public async Task<OnboardingViewDto?> CaptureCardAsync(string token, string? passcode, CaptureOnboardingCardRequest request, CancellationToken cancellationToken = default)
    {
        var link = await AuthorizeAsync(token, passcode, cancellationToken);
        if (link?.PersonId is not { } personId)
        {
            return null;
        }

        string? imageReference = null;
        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            var bytes = Convert.FromBase64String(request.ImageBase64);
            imageReference = await _images.SaveAsync(bytes, request.ImageContentType ?? "image/jpeg", cancellationToken);
        }

        await _qualifications.CaptureCardAsync(new CaptureCardRequest(
            personId, request.QualificationTypeId, request.CardNumber, request.HolderName,
            request.IssuedOn, request.ExpiresOn, NeedsReview: true, imageReference), cancellationToken);

        return await BuildViewAsync(link, cancellationToken);
    }

    /// <summary>Validates the token, usability and passcode; returns the link or null.</summary>
    private async Task<OnboardingLink?> AuthorizeAsync(string token, string? passcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var link = await _links.GetByTokenAsync(token, cancellationToken);
        if (link is null || !link.IsUsable(DateTimeOffset.UtcNow))
        {
            return null;
        }

        if (link.PasscodeHash is not null &&
            (string.IsNullOrEmpty(passcode) || !PackPasscode.Verify(passcode, link.PasscodeHash)))
        {
            return null;
        }

        return link;
    }

    /// <summary>Builds the operative-facing view for a link, including any captured cards.</summary>
    private async Task<OnboardingViewDto> BuildViewAsync(OnboardingLink link, CancellationToken cancellationToken)
    {
        var company = await _companies.GetByIdAsync(link.CompanyId, cancellationToken);

        var cards = new List<OnboardingCardDto>();
        if (link.PersonId is { } personId)
        {
            var held = await _qualifications.GetCardsForPersonAsync(personId, cancellationToken);
            cards.AddRange(held
                .Where(c => !c.IsSuperseded)
                .Select(c => new OnboardingCardDto(c.QualificationName, c.CardNumber, c.ExpiresOn)));
        }

        return new OnboardingViewDto(
            company?.Name ?? "your employer",
            link.Name,
            link.Trade,
            link.Status.ToString(),
            DetailsSubmitted: link.PersonId is not null,
            cards);
    }
}
