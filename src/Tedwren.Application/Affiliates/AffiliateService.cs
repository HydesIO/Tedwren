using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Affiliates;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Common;
using Tedwren.Application.Export;
using Tedwren.Application.Notifications.Email;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Affiliates;

/// <summary>
/// The affiliate/partner programme service (Web Plan §7). Holds the rules: setting up affiliates on a
/// profit-share commission plan, listing the accounts they've introduced and the commission each earns
/// (commission = deal revenue × profit margin × affiliate rate), recording payouts, and issuing a
/// token-gated agreement that the affiliate signs — producing a countersigned PDF and a confirmation email.
/// No raw bank details are held. Associated accounts are read from the lead pipeline by affiliate.
/// </summary>
public sealed class AffiliateService : IAffiliateService
{
    private const string CountersignatoryName = "James Darby";
    private const string CountersignatoryTitle = "Director";

    /// <summary>Max accepted length of a drawn-signature data URL (~1 MB of base64) — an anti-abuse cap.</summary>
    private const int MaxSignatureDataUrlLength = 1_500_000;

    /// <summary>Default lifetime of an agreement's signing link before it expires.</summary>
    private static readonly TimeSpan AgreementLifetime = TimeSpan.FromDays(30);

    private readonly IAffiliateRepository _repository;
    private readonly ILeadRepository _leads;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;

    /// <summary>Injects the affiliate repository, the lead repository, the email sender and email options.</summary>
    public AffiliateService(
        IAffiliateRepository repository,
        ILeadRepository leads,
        IEmailSender emailSender,
        EmailOptions emailOptions)
    {
        _repository = repository;
        _leads = leads;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
    }

    /// <summary>Returns every affiliate, newest-updated first.</summary>
    public async Task<IReadOnlyList<AffiliateDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var affiliates = await _repository.ListAsync(cancellationToken);
        return affiliates.Select(ToDto).ToList();
    }

    /// <summary>Returns an affiliate with its associated accounts, payouts and agreement, or null.</summary>
    public async Task<AffiliateDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var affiliate = await _repository.GetAsync(id, cancellationToken);
        if (affiliate is null)
        {
            return null;
        }

        var leads = await _leads.ListByAffiliateAsync(id, cancellationToken);
        var accounts = leads.Select(l => new AssociatedAccountDto(
            l.Id, l.CompanyName, l.Status.ToString(), l.EstimatedRevenue,
            affiliate.CommissionOn(l.EstimatedRevenue ?? 0m))).ToList();

        var payouts = await _repository.ListPayoutsAsync(id, cancellationToken);
        var agreement = await _repository.GetAgreementByAffiliateAsync(id, cancellationToken);

        return new AffiliateDetailDto(
            ToDto(affiliate),
            accounts,
            payouts.Select(ToPayoutDto).ToList(),
            agreement is null ? null : ToAgreementSummary(agreement));
    }

    /// <summary>Creates an affiliate, drafts their agreement and emails them their setup + agreement link.</summary>
    public async Task<AffiliateDto> CreateAsync(CreateAffiliateRequest request, CancellationToken cancellationToken = default)
    {
        var name = Require(request.Name, "A name is required.");
        var email = RequireEmail(request.ContactEmail);

        var affiliate = new Affiliate
        {
            Name = name,
            Company = Trim(request.Company),
            ContactEmail = email,
            AccountManager = Trim(request.AccountManager),
            PayeeReference = Trim(request.PayeeReference),
            AffiliateRatePct = request.AffiliateRatePct,
            ProfitMarginPct = request.ProfitMarginPct,
            Status = AffiliateStatus.Pending,
        };
        await _repository.AddAsync(affiliate, cancellationToken);

        // Draft the agreement (fixed HTML from the commission plan) and issue it.
        var agreement = new AffiliateAgreement
        {
            AffiliateId = affiliate.Id,
            Token = Guid.NewGuid().ToString("N"),
            TermsHtml = AffiliateAgreementTemplate.BuildHtml(name, affiliate.Company, affiliate.AffiliateRatePct, affiliate.ProfitMarginPct),
            CountersignatoryName = CountersignatoryName,
            CountersignatoryTitle = CountersignatoryTitle,
            Status = AffiliateAgreementStatus.Sent,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(AgreementLifetime),
        };
        await _repository.AddAgreementAsync(agreement, cancellationToken);

        // Notify the affiliate with their terms and the link to sign.
        var url = AgreementUrl(agreement.Token);
        var content = AffiliateEmails.BuildSetupContent(name, affiliate.AffiliateRatePct, affiliate.ProfitMarginPct, url);
        await SafeSendAsync(email, AffiliateEmails.SetupSubject, content, cancellationToken);

        return ToDto(affiliate);
    }

    /// <summary>Updates an affiliate's details and commission plan.</summary>
    public async Task<AffiliateDto?> UpdateAsync(Guid id, UpdateAffiliateRequest request, CancellationToken cancellationToken = default)
    {
        var affiliate = await _repository.GetAsync(id, cancellationToken);
        if (affiliate is null)
        {
            return null;
        }

        affiliate.Name = Require(request.Name, "A name is required.");
        affiliate.ContactEmail = RequireEmail(request.ContactEmail);
        affiliate.Company = Trim(request.Company);
        affiliate.AccountManager = Trim(request.AccountManager);
        affiliate.PayeeReference = Trim(request.PayeeReference);
        affiliate.AffiliateRatePct = request.AffiliateRatePct;
        affiliate.ProfitMarginPct = request.ProfitMarginPct;
        if (Enum.TryParse<AffiliateStatus>(request.Status, ignoreCase: true, out var status))
        {
            affiliate.Status = status;
        }

        affiliate.UpdatedUtc = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(affiliate, cancellationToken);
        return ToDto(affiliate);
    }

    /// <summary>Raises a payout for an affiliate.</summary>
    public async Task<AffiliatePayoutDto?> CreatePayoutAsync(Guid affiliateId, CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        var affiliate = await _repository.GetAsync(affiliateId, cancellationToken);
        if (affiliate is null)
        {
            return null;
        }

        var payout = new AffiliatePayout
        {
            AffiliateId = affiliateId,
            LeadId = request.LeadId,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "GBP" : request.Currency!.ToUpperInvariant(),
            Reference = Trim(request.Reference),
            Status = AffiliatePayoutStatus.Pending,
        };
        await _repository.AddPayoutAsync(payout, cancellationToken);
        return ToPayoutDto(payout);
    }

    /// <summary>Marks a payout paid.</summary>
    public async Task<AffiliatePayoutDto?> MarkPayoutPaidAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        var payout = await _repository.GetPayoutAsync(payoutId, cancellationToken);
        if (payout is null)
        {
            return null;
        }

        payout.Status = AffiliatePayoutStatus.Paid;
        payout.PaidUtc = DateTimeOffset.UtcNow;
        await _repository.UpdatePayoutAsync(payout, cancellationToken);
        return ToPayoutDto(payout);
    }

    /// <summary>Returns the public, token-gated view of an agreement, or null when the token is unknown.</summary>
    public async Task<AffiliateAgreementViewDto?> GetAgreementAsync(string token, CancellationToken cancellationToken = default)
    {
        var agreement = await _repository.GetAgreementByTokenAsync(token, cancellationToken);
        if (agreement is null)
        {
            return null;
        }

        var affiliate = await _repository.GetAsync(agreement.AffiliateId, cancellationToken);
        return ToAgreementView(agreement, affiliate);
    }

    /// <summary>Signs an agreement: captures the signature, generates the PDF, activates the affiliate and emails confirmation.</summary>
    public async Task<AffiliateAgreementViewDto?> SignAgreementAsync(string token, SignAffiliateAgreementRequest request, CancellationToken cancellationToken = default)
    {
        var agreement = await _repository.GetAgreementByTokenAsync(token, cancellationToken);
        if (agreement is null || !agreement.IsSignable || agreement.IsExpired(DateTimeOffset.UtcNow))
        {
            return null;
        }

        // Cap the signature payload so an oversized data URL can't be used to exhaust memory/storage.
        if ((request.SignatureDataUrl?.Length ?? 0) > MaxSignatureDataUrlLength)
        {
            throw new ArgumentException("The signature is too large.");
        }

        var signedName = Require(request.SignedByName, "A name is required to sign.");
        var affiliate = await _repository.GetAsync(agreement.AffiliateId, cancellationToken);

        agreement.SignatureDataUrl = request.SignatureDataUrl;
        agreement.SignedByName = signedName;
        agreement.SignedUtc = DateTimeOffset.UtcNow;
        agreement.Status = AffiliateAgreementStatus.Signed;

        // Generate the immutable countersigned PDF from the same clause source the affiliate read.
        var clauses = AffiliateAgreementTemplate.BuildClauses(
            affiliate?.Name ?? signedName, affiliate?.Company,
            affiliate?.AffiliateRatePct ?? 0m, affiliate?.ProfitMarginPct ?? 0m);
        var pdf = AffiliateAgreementPdfRenderer.Render(new AffiliateAgreementPdfModel(
            affiliate?.Name ?? signedName, affiliate?.Company, clauses,
            agreement.CountersignatoryName, agreement.CountersignatoryTitle,
            signedName, agreement.SignedUtc.Value, DecodeSignature(request.SignatureDataUrl)));
        agreement.PdfBytes = pdf;
        await _repository.UpdateAgreementAsync(agreement, cancellationToken);

        // Activate the affiliate now the agreement is in place.
        if (affiliate is not null && affiliate.Status == AffiliateStatus.Pending)
        {
            affiliate.Status = AffiliateStatus.Active;
            affiliate.UpdatedUtc = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(affiliate, cancellationToken);
        }

        // Email the signed confirmation with the PDF attached.
        if (affiliate is not null)
        {
            var content = AffiliateEmails.BuildSignedContent(affiliate.Name);
            var attachment = new EmailAttachment("affiliate-agreement.pdf", "application/pdf", pdf);
            await SafeSendWithAttachmentAsync(affiliate.ContactEmail, AffiliateEmails.SignedSubject, content, attachment, cancellationToken);
        }

        return ToAgreementView(agreement, affiliate);
    }

    /// <summary>Returns the signed agreement PDF for a token, or null when unavailable.</summary>
    public async Task<byte[]?> GetAgreementPdfAsync(string token, CancellationToken cancellationToken = default)
    {
        var agreement = await _repository.GetAgreementByTokenAsync(token, cancellationToken);
        return agreement?.PdfBytes;
    }

    /// <summary>Builds the public link to view/sign an agreement (served by the console's anonymous page).</summary>
    private string AgreementUrl(string token)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_emailOptions.ConsoleBaseUrl)
            ? "https://localhost"
            : _emailOptions.ConsoleBaseUrl.TrimEnd('/');
        return $"{baseUrl}/affiliate-agreement/{token}";
    }

    /// <summary>Decodes a PNG data URL to bytes, or null when it isn't a data URL.</summary>
    private static byte[]? DecodeSignature(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            return null;
        }

        var comma = dataUrl.IndexOf(',');
        var payload = comma >= 0 ? dataUrl[(comma + 1)..] : dataUrl;
        try
        {
            return Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private async Task SafeSendAsync(string email, string subject, string content, CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendHtmlAsync(email, subject, content, cancellationToken);
        }
        catch (Exception)
        {
            // A notification failure must not fail the affiliate operation.
        }
    }

    private async Task SafeSendWithAttachmentAsync(string email, string subject, string content, EmailAttachment attachment, CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendHtmlWithAttachmentsAsync(email, subject, content, new[] { attachment }, cancellationToken);
        }
        catch (Exception)
        {
            // A notification failure must not fail signing.
        }
    }

    private static string Require(string? value, string message)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException(message);
        }

        return trimmed;
    }

    /// <summary>Requires a contact email and rejects it when not a valid address.</summary>
    private static string RequireEmail(string? value)
    {
        var email = Require(value, "A contact email is required.");
        if (!EmailValidation.IsValid(email))
        {
            throw new ArgumentException("A valid contact email is required.");
        }

        return email;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AffiliateDto ToDto(Affiliate a) => new(
        a.Id, a.Name, a.Company, a.ContactEmail, a.Status.ToString(), a.AccountManager, a.PayeeReference,
        a.AffiliateRatePct, a.ProfitMarginPct, a.CreatedUtc, a.UpdatedUtc);

    private static AffiliatePayoutDto ToPayoutDto(AffiliatePayout p) => new(
        p.Id, p.AffiliateId, p.LeadId, p.Amount, p.Currency, p.Status.ToString(), p.Reference, p.CreatedUtc, p.PaidUtc);

    private static AffiliateAgreementSummaryDto ToAgreementSummary(AffiliateAgreement a) =>
        new(a.Id, a.Token, a.Status.ToString(), a.SignedUtc, a.SignedByName);

    private static AffiliateAgreementViewDto ToAgreementView(AffiliateAgreement a, Affiliate? affiliate) => new(
        affiliate?.Name ?? a.SignedByName ?? "Affiliate",
        affiliate?.Company,
        a.TermsHtml,
        a.CountersignatoryName,
        a.CountersignatoryTitle,
        a.IsExpired(DateTimeOffset.UtcNow) ? "Expired" : a.Status.ToString(),
        a.IsSignable && !a.IsExpired(DateTimeOffset.UtcNow),
        a.SignedUtc,
        a.SignedByName);
}
