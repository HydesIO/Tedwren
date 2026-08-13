using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the Forms Library domain defaults (PRD-Phase 2 checklist/inspection engine): a new template
/// starts as version 1 / Draft with its own family, and field/section descriptors carry their configuration.
/// </summary>
public sealed class FormTemplateTests
{
    private static FormTemplate NewTemplate() => new()
    {
        CompanyId = Guid.NewGuid(),
        Name = "Site Inspection",
        Sections = new List<FormSectionDef>
        {
            new("s1", "General", new List<FormField>
            {
                new("f1", FormFieldKind.RagStatus, "Housekeeping", null, true, null, null, 0),
            }, 0),
        },
    };

    [Fact] // A new template is an editable version 1 draft with its own family id.
    public void NewTemplate_DefaultsToDraftVersionOne()
    {
        var template = NewTemplate();

        Assert.Equal(1, template.Version);
        Assert.Equal(FormTemplateStatus.Draft, template.Status);
        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.NotEqual(Guid.Empty, template.FamilyId);
    }

    [Fact] // R11 — created/updated timestamps are stamped in UTC.
    public void NewTemplate_StampsUtcTimestamps()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var template = NewTemplate();

        Assert.True(template.CreatedUtc >= before);
        Assert.True(template.UpdatedUtc >= before);
    }

    [Fact] // A field carries its kind, requiredness and per-kind config.
    public void FormField_CarriesKindAndConfiguration()
    {
        var field = new FormField("f1", FormFieldKind.Number, "Temperature", "In °C", true, "{\"min\":0}", null, 2);

        Assert.Equal(FormFieldKind.Number, field.Kind);
        Assert.True(field.Required);
        Assert.Equal("{\"min\":0}", field.ValidationJson);
        Assert.Equal(2, field.Order);
    }

    [Fact] // The field spectrum is broad enough to cover the PRD's listed kinds (:509).
    public void FormFieldKind_CoversTheListedSpectrum()
    {
        foreach (var kind in new[]
        {
            FormFieldKind.ShortText, FormFieldKind.LongText, FormFieldKind.Number, FormFieldKind.Date,
            FormFieldKind.Dropdown, FormFieldKind.MultiSelect, FormFieldKind.YesNo, FormFieldKind.RagStatus,
            FormFieldKind.Photo, FormFieldKind.Signature,
        })
        {
            Assert.True(Enum.IsDefined(kind));
        }
    }
}
