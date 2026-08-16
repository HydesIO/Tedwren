using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Affiliates;
using Tedwren.Abstractions.Contracts.Leads;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Affiliates;
using Tedwren.Application.Leads;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for <see cref="AffiliateService"/> (affiliate programme, Web Plan §7). Uses the in-memory
/// affiliate + lead stores and a fake email sender so the profit-share commission, agreement signing (PDF +
/// confirmation email), associated accounts and payouts are verified without a database or real email.
/// </summary>
public sealed class AffiliateServiceTests
{
    private sealed class FakeEmailSender : IEmailSender
    {
        public List<string> Sent { get; } = new();
        public int AttachmentsSent { get; private set; }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default) =>
            SendHtmlAsync(toEmail, subject, body, cancellationToken);

        public Task SendHtmlAsync(string toEmail, string subject, string contentHtml, CancellationToken cancellationToken = default)
        {
            Sent.Add(subject);
            return Task.CompletedTask;
        }

        public Task SendHtmlWithAttachmentsAsync(string toEmail, string subject, string contentHtml,
            IReadOnlyList<EmailAttachment> attachments, CancellationToken cancellationToken = default)
        {
            Sent.Add(subject);
            AttachmentsSent += attachments.Count;
            return Task.CompletedTask;
        }
    }

    private static (AffiliateService Service, InMemoryLeadRepository Leads, FakeEmailSender Email) Create()
    {
        var leads = new InMemoryLeadRepository();
        var email = new FakeEmailSender();
        var service = new AffiliateService(new InMemoryAffiliateRepository(), leads, new InMemoryPaymentRepository(), email,
            new EmailOptions { ConsoleBaseUrl = "https://console.tedwren.example" });
        return (service, leads, email);
    }

    [Fact]
    public void Commission_IsShareOfProfit_NotRevenue()
    {
        var affiliate = new Affiliate { Name = "A", ContactEmail = "a@x.com", AffiliateRatePct = 0.20m, ProfitMarginPct = 0.33m };
        // £15,000 revenue × 33% margin = £4,950 profit; 20% of that = £990.
        Assert.Equal(990m, affiliate.CommissionOn(15000m));
    }

    [Fact]
    public async Task Create_DraftsAgreement_AndSendsSetupEmail()
    {
        var (service, _, email) = Create();

        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat Partner", "pat@partner.com", "Partner Co"));

        Assert.Equal("Pending", affiliate.Status);
        Assert.Contains(AffiliateEmailSubjects.Setup, email.Sent);

        var detail = await service.GetAsync(affiliate.Id);
        Assert.NotNull(detail!.Agreement);
        Assert.Equal("Sent", detail.Agreement!.Status);
    }

    [Fact]
    public async Task Sign_GeneratesPdf_ActivatesAffiliate_AndSendsConfirmationWithAttachment()
    {
        var (service, _, email) = Create();
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat Partner", "pat@partner.com"));
        var token = (await service.GetAsync(affiliate.Id))!.Agreement!.Token;

        var signed = await service.SignAgreementAsync(token,
            new SignAffiliateAgreementRequest("data:image/png;base64,iVBORw0KGgo=", "Pat Partner"));

        Assert.NotNull(signed);
        Assert.Equal("Signed", signed!.Status);

        var pdf = await service.GetAgreementPdfAsync(token);
        Assert.NotNull(pdf);
        Assert.True(pdf!.Length > 0);

        var detail = await service.GetAsync(affiliate.Id);
        Assert.Equal("Active", detail!.Affiliate.Status);
        Assert.Contains(AffiliateEmailSubjects.Signed, email.Sent);
        Assert.Equal(1, email.AttachmentsSent);
    }

    [Fact]
    public async Task Sign_IsRejected_WhenExpired()
    {
        var repo = new InMemoryAffiliateRepository();
        var service = new AffiliateService(repo, new InMemoryLeadRepository(), new InMemoryPaymentRepository(), new FakeEmailSender(),
            new EmailOptions { ConsoleBaseUrl = "https://console.tedwren.example" });
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat", "pat@partner.com"));
        var token = (await service.GetAsync(affiliate.Id))!.Agreement!.Token;

        // Force the signing link into the past.
        var agreement = await repo.GetAgreementByTokenAsync(token);
        agreement!.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(-1);
        await repo.UpdateAgreementAsync(agreement);

        var view = await service.GetAgreementAsync(token);
        Assert.False(view!.Signable);
        Assert.Equal("Expired", view.Status);

        var signed = await service.SignAgreementAsync(token, new SignAffiliateAgreementRequest("data:image/png;base64,iVBORw0KGgo=", "Pat"));
        Assert.Null(signed);
    }

    [Fact]
    public async Task Sign_RejectsOversizedSignature()
    {
        var (service, _, _) = Create();
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat", "pat@partner.com"));
        var token = (await service.GetAsync(affiliate.Id))!.Agreement!.Token;

        var huge = "data:image/png;base64," + new string('A', 1_500_001);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SignAgreementAsync(token, new SignAffiliateAgreementRequest(huge, "Pat")));
    }

    [Fact]
    public async Task Create_RejectsInvalidEmail()
    {
        var (service, _, _) = Create();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateAffiliateRequest("Pat", "not-an-email")));
    }

    [Fact]
    public async Task Sign_IsRejected_WhenAlreadySigned()
    {
        var (service, _, _) = Create();
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat", "pat@partner.com"));
        var token = (await service.GetAsync(affiliate.Id))!.Agreement!.Token;

        await service.SignAgreementAsync(token, new SignAffiliateAgreementRequest("data:image/png;base64,iVBORw0KGgo=", "Pat"));
        var second = await service.SignAgreementAsync(token, new SignAffiliateAgreementRequest("data:image/png;base64,iVBORw0KGgo=", "Pat"));

        Assert.Null(second); // already signed → not signable
    }

    [Fact]
    public async Task AssociatedAccounts_ShowCommission_ForAttributedLeads()
    {
        var (service, leadRepo, _) = Create();
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat", "pat@partner.com", null, null, null, 0.20m, 0.33m));

        // A converted lead attributed to this affiliate, worth £15,000.
        await leadRepo.AddAsync(new Lead
        {
            CompanyName = "Won Ltd",
            EstimatedRevenue = 15000m,
            AffiliateId = affiliate.Id,
            Status = Domain.Enums.LeadStatus.Converted,
        });

        var detail = await service.GetAsync(affiliate.Id);
        Assert.Single(detail!.AssociatedAccounts);
        Assert.Equal(990m, detail.AssociatedAccounts[0].EstimatedCommission);
    }

    [Fact]
    public async Task EarnedCommission_ComesFromClearedPayments_NetOfChargebacks()
    {
        var leads = new InMemoryLeadRepository();
        var payments = new InMemoryPaymentRepository();
        var service = new AffiliateService(new InMemoryAffiliateRepository(), leads, payments, new FakeEmailSender(),
            new EmailOptions { ConsoleBaseUrl = "https://console.tedwren.example" });
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat", "pat@partner.com", null, null, null, 0.20m, 0.33m));

        // A converted lead linked to a real account (company), attributed to the affiliate.
        var accountId = Guid.NewGuid();
        await leads.AddAsync(new Lead
        {
            CompanyName = "Won Ltd",
            AffiliateId = affiliate.Id,
            ConvertedAccountId = accountId,
            Status = Domain.Enums.LeadStatus.Converted,
        });

        var mandate = Guid.NewGuid();
        // £10,000 cleared (Confirmed) + £5,000 settled (PaidOut) − £3,000 chargeback = £12,000 net revenue.
        await payments.AddAsync(new Payment { CompanyId = accountId, MandateId = mandate, AmountPence = 1_000_000, Status = Domain.Enums.PaymentStatus.Confirmed });
        await payments.AddAsync(new Payment { CompanyId = accountId, MandateId = mandate, AmountPence = 500_000, Status = Domain.Enums.PaymentStatus.PaidOut });
        await payments.AddAsync(new Payment { CompanyId = accountId, MandateId = mandate, AmountPence = 300_000, Status = Domain.Enums.PaymentStatus.ChargedBack });
        await payments.AddAsync(new Payment { CompanyId = accountId, MandateId = mandate, AmountPence = 999_999, Status = Domain.Enums.PaymentStatus.Failed }); // never cleared → ignored

        var detail = await service.GetAsync(affiliate.Id);
        var account = detail!.AssociatedAccounts.Single();
        Assert.Equal(12000m, account.ClearedRevenue);
        Assert.Equal(792m, account.EarnedCommission); // 12000 × 33% × 20%
        Assert.Equal(792m, detail.Commission.Earned);
        Assert.Equal(0m, detail.Commission.Paid);
        Assert.Equal(792m, detail.Commission.Outstanding);
    }

    [Fact]
    public async Task Resend_RefreshesExpiry_ResendsEmail_AndRefusesWhenSigned()
    {
        var repo = new InMemoryAffiliateRepository();
        var email = new FakeEmailSender();
        var service = new AffiliateService(repo, new InMemoryLeadRepository(), new InMemoryPaymentRepository(), email,
            new EmailOptions { ConsoleBaseUrl = "https://console.tedwren.example" });
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat", "pat@partner.com"));
        var token = (await service.GetAsync(affiliate.Id))!.Agreement!.Token;

        // Expire the link, then resend — expiry refreshed and a second setup email sent.
        var agreement = await repo.GetAgreementByTokenAsync(token);
        agreement!.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(-1);
        await repo.UpdateAgreementAsync(agreement);

        Assert.True(await service.ResendAgreementAsync(affiliate.Id));
        Assert.Equal(2, email.Sent.Count(s => s == "You've been set up as a Tedwren affiliate"));
        Assert.True((await service.GetAgreementAsync(token))!.Signable); // no longer expired

        // Once signed, there's nothing to resend.
        await service.SignAgreementAsync(token, new SignAffiliateAgreementRequest("data:image/png;base64,iVBORw0KGgo=", "Pat"));
        Assert.False(await service.ResendAgreementAsync(affiliate.Id));
    }

    [Fact]
    public async Task Payout_Raised_ThenMarkedPaid()
    {
        var (service, _, _) = Create();
        var affiliate = await service.CreateAsync(new CreateAffiliateRequest("Pat", "pat@partner.com"));

        var payout = await service.CreatePayoutAsync(affiliate.Id, new CreatePayoutRequest(990m, "GBP", null, "Q1 commission"));
        Assert.NotNull(payout);
        Assert.Equal("Pending", payout!.Status);

        var paid = await service.MarkPayoutPaidAsync(payout.Id);
        Assert.Equal("Paid", paid!.Status);
        Assert.NotNull(paid.PaidUtc);

        var detail = await service.GetAsync(affiliate.Id);
        Assert.Single(detail!.Payouts);
        Assert.Equal("Paid", detail.Payouts[0].Status);
    }

    /// <summary>Mirror of the email subjects so the test asserts against a single source.</summary>
    private static class AffiliateEmailSubjects
    {
        public const string Setup = "You've been set up as a Tedwren affiliate";
        public const string Signed = "Your Tedwren affiliate agreement is signed";
    }
}
