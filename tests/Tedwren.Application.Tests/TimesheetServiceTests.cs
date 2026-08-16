using System.Text;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Timesheets;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Timesheets;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;
using ApprovalScope = Tedwren.Abstractions.Common.ApprovalScope;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the timesheet service (SUB-7–SUB-12, SUB-27, MC-24): roll-up from attendance, the approval
/// lifecycle with recorded granularity (SUB-9), corrections as retained new lines (R16), the neutral
/// return-not-deny vocabulary (R18), worker hours (SUB-27), and CSV/Excel export (SUB-10).
/// </summary>
public sealed class TimesheetServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();
    private static readonly Guid Person = Guid.NewGuid();
    private static readonly Guid Site = Guid.NewGuid();
    private static readonly DateOnly WeekStart = new(2026, 8, 3);
    private static readonly DateOnly WorkDay = new(2026, 8, 4);

    private static TimesheetService CreateService()
    {
        var attendanceStore = new InMemoryAttendanceStore();
        var attendance = new InMemoryAttendanceRepository(attendanceStore);
        // One 8-hour shift (08:00–16:00) on the Tuesday of the week.
        attendanceStore.Records.TryAdd(Guid.NewGuid(), Shift(AttendanceEventType.SignIn, 8));
        attendanceStore.Records.TryAdd(Guid.NewGuid(), Shift(AttendanceEventType.SignOut, 16));

        return new TimesheetService(
            new InMemoryTimesheetRepository(new InMemoryTimesheetStore(seed: false)),
            attendance,
            new FakeEngagements("M. Adeyemi"),
            new FakeSites("Meridian Tower"));
    }

    private static AttendanceRecord Shift(AttendanceEventType type, int hour) => new()
    {
        PersonId = Person,
        SiteId = Site,
        Type = type,
        Outcome = AttendanceOutcome.Accepted,
        OccurredUtc = new DateTimeOffset(WorkDay.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero),
    };

    [Fact]
    public async Task GetOrCreate_RollsUpAttendance_IntoADraftTimesheet()
    {
        var service = CreateService();

        var dto = await service.GetOrCreateForWeekAsync(Company, Person, WeekStart);

        Assert.Equal(TimesheetState.Draft, dto.Status);
        Assert.Equal("M. Adeyemi", dto.OperativeName);
        Assert.Equal(8m, dto.TotalHours);
        var line = Assert.Single(dto.Lines);
        Assert.Equal("Meridian Tower", line.SiteName);
    }

    [Fact]
    public async Task Submit_ThenApprove_RecordsWhoAndScope()
    {
        var service = CreateService();
        var dto = await service.GetOrCreateForWeekAsync(Company, Person, WeekStart);

        await service.SubmitAsync(dto.Id);
        var approved = await service.ApproveAsync(dto.Id, new ApproveTimesheetRequest("Priya Shah", ApprovalScope.Site));

        Assert.NotNull(approved);
        Assert.Equal(TimesheetState.Approved, approved!.Status);
        Assert.Equal("Priya Shah", approved.DecidedBy);
        Assert.Equal(ApprovalScope.Site, approved.DecisionScope);
    }

    [Fact]
    public async Task Approve_WithoutSubmit_IsRejectedByTheGuard()
    {
        var service = CreateService();
        var dto = await service.GetOrCreateForWeekAsync(Company, Person, WeekStart);

        var approved = await service.ApproveAsync(dto.Id, new ApproveTimesheetRequest("Priya Shah", ApprovalScope.All));

        Assert.Null(approved);   // a draft cannot be approved (SUB-8)
    }

    [Fact]
    public async Task Return_RecordsReason_AndNeverDenies()
    {
        var service = CreateService();
        var dto = await service.GetOrCreateForWeekAsync(Company, Person, WeekStart);
        await service.SubmitAsync(dto.Id);

        var returned = await service.ReturnAsync(dto.Id, new ReturnTimesheetRequest("Priya Shah", "Check Tuesday hours"));

        Assert.NotNull(returned);
        Assert.Equal(TimesheetState.Returned, returned!.Status);
        Assert.Equal("Check Tuesday hours", returned.ReturnReason);
        Assert.Contains("Returned", returned.StatusLabel);
        Assert.DoesNotContain("deni", returned.StatusLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Correct_AppendsANewLine_RetainsOriginal_AndUpdatesEffectiveHours()
    {
        var service = CreateService();
        var dto = await service.GetOrCreateForWeekAsync(Company, Person, WeekStart);
        var originalLine = dto.Lines.Single();

        var corrected = await service.CorrectLineAsync(dto.Id, new CorrectLineRequest(originalLine.EntryId, 6m, "Priya Shah", "Left early"));

        Assert.NotNull(corrected);
        var line = Assert.Single(corrected!.Lines);
        Assert.Equal(6m, line.Hours);          // effective hours reflect the correction
        Assert.True(line.IsCorrected);
        Assert.Equal("Left early", line.Reason);
        Assert.Equal(6m, corrected.TotalHours);
    }

    [Fact]
    public async Task GetOperativeHours_ReturnsTheWorkersOwnTotal()
    {
        var service = CreateService();

        var hours = await service.GetOperativeHoursAsync(Company, Person, WeekStart);

        Assert.Equal(Person, hours.PersonId);
        Assert.Equal(8m, hours.TotalHours);
    }

    [Fact]
    public async Task CompanyWeekExport_ProducesCsvAndValidXlsx()
    {
        var service = CreateService();
        await service.GetOrCreateForWeekAsync(Company, Person, WeekStart);

        var csv = await service.ExportCompanyWeekCsvAsync(Company, WeekStart);
        Assert.StartsWith("Operative,Site,Week starting,Status,Hours", csv);
        Assert.Contains("Meridian Tower", csv);

        var xlsx = await service.ExportCompanyWeekXlsxAsync(Company, WeekStart);
        // A valid .xlsx is a ZIP archive — it begins with the "PK" local-file-header signature.
        Assert.True(xlsx.Length > 0);
        Assert.Equal(Encoding.ASCII.GetBytes("PK"), xlsx.Take(2).ToArray());
    }

    /// <summary>Engagement repository fake returning a fixed operative name for the tenant lookup.</summary>
    private sealed class FakeEngagements : IEngagementRepository
    {
        private readonly string _name;
        public FakeEngagements(string name) => _name = name;

        public Task<Engagement?> GetByCompanyAndPersonAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Engagement?>(new Engagement { CompanyId = companyId, PersonId = personId, Name = _name });

        public Task<IReadOnlyList<Engagement>> GetActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Engagement>> GetActiveByPersonAsync(Guid personId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Engagement?> GetAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CountActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Engagement engagement, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Engagement engagement, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>Site repository fake returning a fixed site name for the roll-up.</summary>
    private sealed class FakeSites : ISiteRepository
    {
        private readonly string _name;
        public FakeSites(string name) => _name = name;

        public Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Site?>(new Site { CompanyId = Company, Name = _name, Id = id });

        public Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Site site, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Site site, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
