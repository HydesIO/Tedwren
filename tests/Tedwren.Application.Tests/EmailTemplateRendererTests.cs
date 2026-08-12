using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Notifications.Email;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the branded HTML email template (PRD-Phase 7): the logo header, content container and private &amp;
/// confidential footer are always present, and the optional components render their expected markup.
/// </summary>
public sealed class EmailTemplateRendererTests
{
    private static EmailTemplateRenderer CreateSut(EmailOptions? options = null) =>
        new(options ?? new EmailOptions
        {
            PublicBaseUrl = "https://api.tedwren.test",
            FromName = "Tedwren",
            CompanyLegalName = "Tedwren Ltd",
        });

    [Fact]
    public void RenderPlainText_IncludesLogo_Container_And_ConfidentialFooter()
    {
        var html = CreateSut().RenderPlainText("Test subject", "Hello world.");

        Assert.Contains("<!DOCTYPE html>", html);
        // Logo top-left, referenced by the absolute public URL (email clients can't resolve relative URLs).
        Assert.Contains("https://api.tedwren.test/api/email-assets/logo.png", html);
        // Content container at the fixed template width.
        Assert.Contains("max-width:600px", html);
        // Confidential footer with the legal entity name.
        Assert.Contains("Private &amp; Confidential", html);
        Assert.Contains("Tedwren Ltd", html);
        // The subject becomes the document title and a heading.
        Assert.Contains("<title>Test subject</title>", html);
    }

    [Fact]
    public void RenderPlainText_SplitsBlankLineBlocks_IntoParagraphs()
    {
        var html = CreateSut().RenderPlainText("Subject", "First block.\n\nSecond block.");

        Assert.Contains("First block.", html);
        Assert.Contains("Second block.", html);
        // Two body blocks plus the subject heading => at least two paragraphs.
        Assert.True(html.Split("<p ").Length - 1 >= 2);
    }

    [Fact]
    public void RenderPlainText_EncodesHtml_InBody()
    {
        var html = CreateSut().RenderPlainText("Subject", "5 < 6 & \"quoted\"");

        Assert.Contains("5 &lt; 6 &amp;", html);
        Assert.DoesNotContain("5 < 6 &", html);
    }

    [Fact]
    public void Render_WithComponents_EmitsButton_Code_And_Table()
    {
        var content = new EmailContentBuilder()
            .Heading("Welcome")
            .Paragraph("Body text.")
            .Button("Open console", "https://app.tedwren.test/go")
            .VerificationCode("482913", "Your code")
            .Table(
                new[] { "Item", "Status" },
                new IReadOnlyList<string>[] { new[] { "CSCS Card", "Valid" } })
            .Build();

        var html = CreateSut().Render("Welcome", content);

        // Button links to the target.
        Assert.Contains("href=\"https://app.tedwren.test/go\"", html);
        Assert.Contains("Open console", html);
        // 2FA code block renders the code.
        Assert.Contains("482913", html);
        // Table renders header and cell values.
        Assert.Contains("Status", html);
        Assert.Contains("CSCS Card", html);
    }

    [Fact]
    public void Render_UsesPreheader_WhenProvided()
    {
        var html = CreateSut().Render("Subject", "<p>Body</p>", preheader: "Inbox preview snippet");

        Assert.Contains("Inbox preview snippet", html);
    }
}
