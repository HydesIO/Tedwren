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

    [Fact] // Approving makes the submission the live version operatives read and sign (spec Stage 3).
    public async Task Approve_MakesLiveVersion()
    {
        var service = CreateSut();
        var dto = await service.SubmitAsync(CompanyA, NewSubmission());
        Assert.False(dto.IsLive);

        await service.ApproveAsync(CompanyA, dto.Id, "Mgr");

        var live = (await service.ListAsync(CompanyA)).Single();
        Assert.Equal("Approved", live.Status);
        Assert.True(live.IsLive);
    }

    [Fact] // Approve-with-comments needs a written note; with one it approves, records the comments and goes live.
    public async Task ApproveWithComments_RequiresNote_AndGoesLive()
    {
        var service = CreateSut();
        var dto = await service.SubmitAsync(CompanyA, NewSubmission());

        await Assert.ThrowsAsync<ArgumentException>(() => service.ApproveWithCommentsAsync(CompanyA, dto.Id, "Mgr", "   "));

        Assert.True(await service.ApproveWithCommentsAsync(CompanyA, dto.Id, "Mgr", "Watch the exclusion zone"));
        var live = (await service.ListAsync(CompanyA)).Single();
        Assert.Equal("ApprovedWithComments", live.Status);
        Assert.Equal("Watch the exclusion zone", live.ReviewNote);
        Assert.True(live.IsLive);
    }

    [Fact] // A RAMS registered from an uploaded document reuses the stored file and versions within its family.
    public async Task RegisterFromDocument_ReusesFile_AndVersionsWithinFamily()
    {
        var service = CreateSut();
        var familyId = Guid.NewGuid();

        var v1 = await service.RegisterFromDocumentAsync(CompanyA,
            new RegisterRamsFromDocumentRequest("Apex", "Method statement", "blob-123", familyId, null, null));
        Assert.Equal(familyId, v1.FamilyId);
        Assert.Equal(1, v1.Version);
        Assert.True(v1.HasFile);            // reused the stored blob reference (no re-upload)
        Assert.Equal("Submitted", v1.Status);

        var v2 = await service.RegisterFromDocumentAsync(CompanyA,
            new RegisterRamsFromDocumentRequest("Apex", "Method statement v2", "blob-456", familyId, null, null));
        Assert.Equal(familyId, v2.FamilyId);
        Assert.Equal(2, v2.Version);        // resubmits into the same family as the next version
    }

    [Fact] // Approving a newer version moves the live pointer off the earlier one (append-only history preserved).
    public async Task Approve_NewVersion_MovesLivePointer()
    {
        var service = CreateSut();
        var v1 = await service.SubmitAsync(CompanyA, NewSubmission());
        await service.ApproveAsync(CompanyA, v1.Id, "Mgr");

        var v2 = await service.SubmitAsync(CompanyA, NewSubmission(v1.FamilyId));
        await service.ApproveAsync(CompanyA, v2.Id, "Mgr");

        var all = await service.ListAsync(CompanyA);
        Assert.Equal(2, all.Count);         // both versions retained (R4/R16)
        Assert.True(all.Single(r => r.Version == 2).IsLive);
        Assert.False(all.Single(r => r.Version == 1).IsLive);
    }
}
