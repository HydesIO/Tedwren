using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Core.Forms;

namespace Tedwren.Mobile.Core.Tests.Forms;

/// <summary>Verifies client-side required-by-default validation (M6) mirrors the server: value + file kinds, display-only ignored.</summary>
public class FormValidationTests
{
    private static FormTemplateDto Template() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Inspection", null, 1, "Published",
        new List<FormSectionDto>
        {
            new("s1", "General", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "Housekeeping", null, true, null, null, 0),  // required value
                new("photo", "Photo", "Photo", null, true, null, null, 1),          // required file
                new("note", "LongText", "Notes", null, false, null, null, 2),       // optional
                new("h", "Heading", "Section", null, true, null, null, 3),          // display-only — never required
            }, 0),
        },
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public void Reports_missing_required_value_and_file()
    {
        var missing = FormValidation.MissingRequired(Template(), Array.Empty<FormAnswerDto>(), Array.Empty<FormSubmissionFileInput>());

        Assert.Contains("Housekeeping", missing);
        Assert.Contains("Photo", missing);
        Assert.DoesNotContain("Notes", missing);
        Assert.DoesNotContain("Section", missing);
    }

    [Fact]
    public void Complete_form_has_no_missing()
    {
        var answers = new List<FormAnswerDto> { new("f1", "Green", Array.Empty<string>()) };
        var files = new List<FormSubmissionFileInput> { new("photo", "p.jpg", "image/jpeg", "AAAA") };

        var missing = FormValidation.MissingRequired(Template(), answers, files);

        Assert.Empty(missing);
    }
}
