using Tedwren.Application.Mobile;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies <see cref="MobileFormService"/> (M6): an operative sees only the forms assigned to them —
/// Organisation-scope, Operator-scope to them, and Site-scope for sites they attend — each resolved to the family's
/// latest published version; unpublished families and other operatives'/sites' assignments are excluded (R15).
/// </summary>
public sealed class MobileFormServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();
    private static readonly Guid Person = Guid.NewGuid();
    private static readonly Guid OtherPerson = Guid.NewGuid();

    private static (MobileFormService Service, InMemoryFormStore Forms, InMemorySiteStore Sites, InMemoryAttendanceStore Attendance) CreateSut()
    {
        var forms = new InMemoryFormStore(seed: false);
        var sites = new InMemorySiteStore(seed: false);
        var attendance = new InMemoryAttendanceStore(seed: false);
        var service = new MobileFormService(
            new InMemoryFormAssignmentRepository(forms),
            new InMemoryFormTemplateRepository(forms),
            new InMemoryAttendanceRepository(attendance),
            new InMemorySiteRepository(sites));
        return (service, forms, sites, attendance);
    }

    private static Guid AddPublishedVersion(InMemoryFormStore forms, Guid familyId, int version)
    {
        var template = new FormTemplate
        {
            CompanyId = Company,
            FamilyId = familyId,
            Name = "Site Inspection",
            Version = version,
            Status = FormTemplateStatus.Published,
            Sections = new List<FormSectionDef>
            {
                new("s1", "General", new List<FormField>
                {
                    new("f1", FormFieldKind.RagStatus, "Housekeeping", null, true, null, null, 0),
                }, 0),
            },
        };
        new InMemoryFormTemplateRepository(forms).AddAsync(template).GetAwaiter().GetResult();
        return template.Id;
    }

    private static void AddAssignment(InMemoryFormStore forms, Guid familyId, FormScope scope, Guid? siteId = null, Guid? personId = null)
    {
        new InMemoryFormAssignmentRepository(forms).AddAsync(new FormAssignment
        {
            CompanyId = Company,
            FormTemplateFamilyId = familyId,
            FormName = "Site Inspection",
            Scope = scope,
            SiteId = siteId,
            PersonId = personId,
            Schedule = FormSchedule.Daily,
        }).GetAwaiter().GetResult();
    }

    private static void AddAttendance(InMemoryAttendanceStore attendance, Guid siteId) =>
        attendance.Records[Guid.NewGuid()] = new AttendanceRecord
        {
            PersonId = Person,
            SiteId = siteId,
            Type = AttendanceEventType.SignIn,
            Outcome = AttendanceOutcome.Accepted,
            OccurredUtc = DateTimeOffset.UtcNow.AddDays(-1),
        };

    [Fact]
    public async Task Organisation_scope_is_included_and_resolves_latest_published_version()
    {
        var (service, forms, _, _) = CreateSut();
        var family = Guid.NewGuid();
        AddPublishedVersion(forms, family, version: 1);
        var latest = AddPublishedVersion(forms, family, version: 2);
        AddAssignment(forms, family, FormScope.Organisation);

        var result = await service.GetAssignmentsAsync(Company, Person);

        Assert.Single(result);
        Assert.Equal(latest, result[0].TemplateVersionId);
        Assert.Equal("Organisation", result[0].Scope);
    }

    [Fact]
    public async Task Operator_scope_is_included_only_for_the_matching_person()
    {
        var (service, forms, _, _) = CreateSut();
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();
        AddPublishedVersion(forms, mine, 1);
        AddPublishedVersion(forms, theirs, 1);
        AddAssignment(forms, mine, FormScope.Operator, personId: Person);
        AddAssignment(forms, theirs, FormScope.Operator, personId: OtherPerson);

        var result = await service.GetAssignmentsAsync(Company, Person);

        Assert.Single(result);
        Assert.Equal(mine, result[0].FamilyId);
    }

    [Fact]
    public async Task Site_scope_is_included_only_for_attended_sites()
    {
        var (service, forms, sites, attendance) = CreateSut();
        var attendedSite = Guid.NewGuid();
        var otherSite = Guid.NewGuid();
        sites.Sites[attendedSite] = new Site { Id = attendedSite, CompanyId = Company, Name = "Riverside" };
        AddAttendance(attendance, attendedSite);
        var famA = Guid.NewGuid();
        var famB = Guid.NewGuid();
        AddPublishedVersion(forms, famA, 1);
        AddPublishedVersion(forms, famB, 1);
        AddAssignment(forms, famA, FormScope.Site, siteId: attendedSite);
        AddAssignment(forms, famB, FormScope.Site, siteId: otherSite);

        var result = await service.GetAssignmentsAsync(Company, Person);

        Assert.Single(result);
        Assert.Equal(attendedSite, result[0].SiteId);
        Assert.Equal("Riverside", result[0].SiteName);
    }

    [Fact]
    public async Task Family_without_a_published_version_is_excluded()
    {
        var (service, forms, _, _) = CreateSut();
        var family = Guid.NewGuid();
        // A draft-only family (no published version).
        await new InMemoryFormTemplateRepository(forms).AddAsync(new FormTemplate
        {
            CompanyId = Company, FamilyId = family, Name = "Draft form", Version = 1,
            Status = FormTemplateStatus.Draft, Sections = new List<FormSectionDef>(),
        });
        AddAssignment(forms, family, FormScope.Organisation);

        var result = await service.GetAssignmentsAsync(Company, Person);

        Assert.Empty(result);
    }
}
