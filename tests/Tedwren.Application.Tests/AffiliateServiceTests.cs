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
        var service = new AffiliateService(new InMemoryAffiliateRepository(), leads, email,
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
        Assert.Equal(990m, detail.AssociatedAccounts[0].Commission);
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
