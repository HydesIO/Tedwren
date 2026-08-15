using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.LaunchList;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.LaunchList;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for <see cref="LaunchListService"/> (launch list, Web Content Spec §6.9). Uses the in-memory
/// signup store and a fake email sender so signup dedupe and the per-address announcement send are verified
/// without a database or real email.
/// </summary>
public sealed class LaunchListServiceTests
{
    /// <summary>Records every recipient and can be told to fail for a specific address.</summary>
    private sealed class FakeEmailSender : IEmailSender
    {
        public List<string> Sent { get; } = new();
        public string? FailFor { get; set; }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default) =>
            SendHtmlAsync(toEmail, subject, body, cancellationToken);

        public Task SendHtmlAsync(string toEmail, string subject, string contentHtml, CancellationToken cancellationToken = default)
        {
            if (string.Equals(toEmail, FailFor, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated delivery failure.");
            }

            Sent.Add(toEmail);
            return Task.CompletedTask;
        }
    }

    private static LaunchListService CreateService(FakeEmailSender sender) =>
        new(new InMemoryLaunchSignupRepository(), sender, new EmailOptions { PublicBaseUrl = "https://tedwren.example" });

    [Fact]
    public async Task SignUp_IsDeduplicated_CaseInsensitively()
    {
        var service = CreateService(new FakeEmailSender());

        var first = await service.SignUpAsync(new CreateLaunchSignupRequest("Person@Example.com"));
        var second = await service.SignUpAsync(new CreateLaunchSignupRequest("person@example.com"));

        Assert.True(first.Accepted);
        Assert.False(first.AlreadyOnList);
        Assert.True(second.Accepted);
        Assert.True(second.AlreadyOnList);

        var list = await service.ListAsync();
        Assert.Single(list);
        Assert.Equal("person@example.com", list[0].Email); // stored lower-cased
    }

    [Fact]
    public async Task SignUp_RejectsEmptyEmail()
    {
        var service = CreateService(new FakeEmailSender());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SignUpAsync(new CreateLaunchSignupRequest("   ")));
    }

    [Fact]
    public async Task Notify_SendsToEachUnnotified_AndMarksThem()
    {
        var sender = new FakeEmailSender();
        var service = CreateService(sender);
        await service.SignUpAsync(new CreateLaunchSignupRequest("a@example.com"));
        await service.SignUpAsync(new CreateLaunchSignupRequest("b@example.com"));

        var result = await service.NotifyAsync(new NotifyLaunchRequest(OnlyUnnotified: true));

        Assert.Equal(2, result.Targeted);
        Assert.Equal(2, result.Sent);
        Assert.Equal(0, result.Failed);
        Assert.Equal(2, sender.Sent.Count);

        // A second run targets nobody, because both are now marked notified.
        var again = await service.NotifyAsync(new NotifyLaunchRequest(OnlyUnnotified: true));
        Assert.Equal(0, again.Targeted);
        Assert.Equal(2, sender.Sent.Count);

        var list = await service.ListAsync();
        Assert.All(list, s => Assert.True(s.Notified));
    }

    [Fact]
    public async Task Notify_CountsFailures_AndDoesNotAbortTheRun()
    {
        var sender = new FakeEmailSender { FailFor = "bad@example.com" };
        var service = CreateService(sender);
        await service.SignUpAsync(new CreateLaunchSignupRequest("good@example.com"));
        await service.SignUpAsync(new CreateLaunchSignupRequest("bad@example.com"));

        var result = await service.NotifyAsync(new NotifyLaunchRequest(OnlyUnnotified: true));

        Assert.Equal(2, result.Targeted);
        Assert.Equal(1, result.Sent);
        Assert.Equal(1, result.Failed);

        // The failed address stays unnotified so a later run can retry it.
        var list = await service.ListAsync();
        Assert.Contains(list, s => s.Email == "bad@example.com" && !s.Notified);
        Assert.Contains(list, s => s.Email == "good@example.com" && s.Notified);
    }
}
