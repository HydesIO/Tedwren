using System.Security.Cryptography;
using System.Text;
using Tedwren.Application.Billing;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Tests for the GoCardless webhook path (Phase C): HMAC signature verification and the event processor
/// (status updates, returned-payment reasons, dedupe, and ignoring unknown resources).
/// </summary>
public sealed class GoCardlessWebhookTests
{
    private const string Secret = "whsec_test";

    private static string Sign(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }

    [Fact]
    public void Signature_ValidHmac_IsAccepted()
    {
        const string body = "{\"events\":[]}";
        Assert.True(GoCardlessSignatureVerifier.IsValid(body, Sign(body, Secret), Secret));
    }

    [Fact]
    public void Signature_TamperedBody_IsRejected()
    {
        var signature = Sign("{\"events\":[]}", Secret);
        Assert.False(GoCardlessSignatureVerifier.IsValid("{\"events\":[{}]}", signature, Secret));
    }

    [Fact]
    public void Signature_EmptySecretOrMissingHeader_IsRejected()
    {
        Assert.False(GoCardlessSignatureVerifier.IsValid("{}", Sign("{}", Secret), secret: ""));
        Assert.False(GoCardlessSignatureVerifier.IsValid("{}", signatureHeader: null, Secret));
    }

    private static (GoCardlessWebhookProcessor Processor, InMemoryMandateRepository Mandates, InMemoryPaymentRepository Payments, InMemoryWebhookEventRepository Events) Create()
    {
        var mandates = new InMemoryMandateRepository();
        var payments = new InMemoryPaymentRepository();
        var events = new InMemoryWebhookEventRepository();
        return (new GoCardlessWebhookProcessor(events, mandates, payments), mandates, payments, events);
    }

    [Fact]
    public async Task Process_PaymentFailedEvent_MarksPaymentFailedWithReason()
    {
        var (processor, _, payments, _) = Create();
        var payment = new Payment
        {
            CompanyId = Guid.NewGuid(),
            MandateId = Guid.NewGuid(),
            AmountPence = 1000,
            GoCardlessPaymentId = "PM123",
            Status = PaymentStatus.Submitted,
        };
        await payments.AddAsync(payment);

        var body = """
        {"events":[{"id":"EV1","resource_type":"payments","action":"failed",
        "links":{"payment":"PM123"},"details":{"description":"Insufficient funds"}}]}
        """;
        var result = await processor.ProcessAsync(body);

        Assert.Equal(1, result.Applied);
        var stored = await payments.GetByGoCardlessPaymentIdAsync("PM123");
        Assert.Equal(PaymentStatus.Failed, stored!.Status);
        Assert.Equal("Insufficient funds", stored.FailureReason);
    }

    [Fact]
    public async Task Process_MandateActiveEvent_MarksMandateActive()
    {
        var (processor, mandates, _, _) = Create();
        var mandate = new Mandate
        {
            CompanyId = Guid.NewGuid(),
            GoCardlessMandateId = "MD123",
            Status = MandateStatus.Submitted,
        };
        await mandates.AddAsync(mandate);

        var body = """
        {"events":[{"id":"EV2","resource_type":"mandates","action":"active","links":{"mandate":"MD123"}}]}
        """;
        var result = await processor.ProcessAsync(body);

        Assert.Equal(1, result.Applied);
        var stored = await mandates.GetByGoCardlessMandateIdAsync("MD123");
        Assert.Equal(MandateStatus.Active, stored!.Status);
    }

    [Fact]
    public async Task Process_SameEventTwice_IsDedupedAndAppliedOnce()
    {
        var (processor, _, payments, events) = Create();
        var payment = new Payment
        {
            CompanyId = Guid.NewGuid(),
            MandateId = Guid.NewGuid(),
            AmountPence = 1000,
            GoCardlessPaymentId = "PM777",
            Status = PaymentStatus.Submitted,
        };
        await payments.AddAsync(payment);

        var body = """
        {"events":[{"id":"EV-DUP","resource_type":"payments","action":"confirmed","links":{"payment":"PM777"}}]}
        """;
        var first = await processor.ProcessAsync(body);
        var second = await processor.ProcessAsync(body);

        Assert.Equal(1, first.Applied);
        Assert.Equal(1, second.Duplicates);
        Assert.Equal(0, second.Applied);
        var stored = await events.GetRecentAsync(10);
        Assert.Single(stored);
    }

    [Fact]
    public async Task Process_UnknownResource_IsIgnored()
    {
        var (processor, _, _, _) = Create();

        var body = """
        {"events":[{"id":"EV3","resource_type":"payments","action":"failed","links":{"payment":"PM-UNKNOWN"}}]}
        """;
        var result = await processor.ProcessAsync(body);

        Assert.Equal(0, result.Applied);
        Assert.Equal(1, result.Ignored);
    }
}
