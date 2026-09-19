using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Rams;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for the RAMS submission &amp; approval workflow (PRD §8.2): reference + versioning, the review queue,
/// approve/reject/return (reject/return require a note), company scoping (R15) and append-only history.
/// </summary>
public sealed class RamsServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    private static RamsService CreateSut() => new(new InMemoryRamsRepository(), new InMemoryImageStore());

    private static SubmitRamsRequest NewSubmission(Guid? familyId = null) =>
        new("Apex Groundworks", "Excavation RAMS", null, "Meridian Tower", null, null, familyId);

    [Fact]
    public async Task Submit_ReturnsReference_Version1_Submitted()
    {
        var service = CreateSut();

        var dto = await service.SubmitAsync(CompanyA, NewSubmission());

        Assert.StartsWith("RAMS-", dto.Reference);
        Assert.Equal(1, dto.Version);
        Assert.Equal("Submitted", dto.Status);
    }

    [Fact]
    public async Task Resubmit_SameFamily_IncrementsVersion_AndKeepsEarlier()
    {
        var service = CreateSut();
        var first = await service.SubmitAsync(CompanyA, NewSubmission());

        var second = await service.SubmitAsync(CompanyA, NewSubmission(first.FamilyId));

        Assert.Equal(first.FamilyId, second.FamilyId);
        Assert.Equal(2, second.Version);
        Assert.Equal(2, (await service.ListAsync(CompanyA)).Count);   // earlier version intact (append-only)
    }

    [Fact]
    public async Task Resubmit_UnknownFamily_Throws()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync(CompanyA, NewSubmission(Guid.NewGuid())));
    }

    [Fact]
    public async Task ReviewQueue_ShowsSubmitted_NotOverdue_WhenFresh()
    {
        var service = CreateSut();
        await service.SubmitAsync(CompanyA, NewSubmission());

        var queue = await service.GetReviewQueueAsync(CompanyA);

        Assert.False(Assert.Single(queue).Overdue);
    }

    [Fact]
    public async Task Approve_SetsApproved_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.SubmitAsync(CompanyA, NewSubmission());

        Assert.False(await service.ApproveAsync(CompanyB, dto.Id, "Mgr"));   // R15
        Assert.True(await service.ApproveAsync(CompanyA, dto.Id, "Mgr"));
        Assert.Equal("Approved", (await service.ListAsync(CompanyA)).Single().Status);
    }

    [Fact]
    public async Task Reject_WithoutNote_Throws()
    {
        var service = CreateSut();
        var dto = await service.SubmitAsync(CompanyA, NewSubmission());

        await Assert.ThrowsAsync<ArgumentException>(() => service.RejectAsync(CompanyA, dto.Id, "Mgr", "   "));
    }

    [Fact]
    public async Task Reject_WithNote_SetsRejectedAndRecordsNote()
    {
        var service = CreateSut();
        var dto = await service.SubmitAsync(CompanyA, NewSubmission());

        Assert.True(await service.RejectAsync(CompanyA, dto.Id, "Mgr", "Missing lifting plan"));

        var rejected = (await service.ListAsync(CompanyA)).Single();
        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal("Missing lifting plan", rejected.ReviewNote);
        Assert.Equal("Mgr", rejected.ReviewedBy);
    }

    [Fact]
    public async Task Return_WithNote_SetsReturned()
    {
        var service = CreateSut();
        var dto = await service.SubmitAsync(CompanyA, NewSubmission());

        Assert.True(await service.ReturnAsync(CompanyA, dto.Id, "Mgr", "Please add COSHH"));
        Assert.Equal("Returned", (await service.ListAsync(CompanyA)).Single().Status);
    }

    [Fact]
    public async Task Review_AlreadyDecided_Throws()
    {
        var service = CreateSut();
        var dto = await service.SubmitAsync(CompanyA, NewSubmission());
        await service.ApproveAsync(CompanyA, dto.Id, "Mgr");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(CompanyA, dto.Id, "Mgr"));
    }

    [Fact]
    public async Task Review_Missing_ReturnsFalse()
    {
        var service = CreateSut();

        Assert.False(await service.ApproveAsync(CompanyA, Guid.NewGuid(), "Mgr"));
    }
}
