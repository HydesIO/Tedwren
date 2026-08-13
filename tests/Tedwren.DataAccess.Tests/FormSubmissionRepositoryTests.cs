using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the Forms Library submission repository against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and deleted
/// afterwards, so existing data is never modified.
/// </summary>
public sealed class FormSubmissionRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Submission_RoundTrips_WithAnswersFilesAndReview()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(factory, new SqlServerDialect()).RunAsync();
        var repository = new FormSubmissionRepository(factory);

        var companyId = Guid.NewGuid();
        var submission = new FormSubmission
        {
            CompanyId = companyId,
            FormTemplateId = Guid.NewGuid(),
            FormTemplateVersion = 1,
            FormName = "Test Inspection",
            Scope = FormScope.Site,
            SiteId = Guid.NewGuid(),
            Answers = new List<FormAnswer>
            {
                new("f1", "Green", new List<string>()),
                new("f2", null, new List<string> { "A", "B" }),
            },
            SubmittedBy = "Tester",
        };
        var file = new FormSubmissionFile
        {
            CompanyId = companyId,
            SubmissionId = submission.Id,
            FieldId = "photo",
            FileName = "site.png",
            ContentType = "image/png",
            Content = new byte[] { 9, 8, 7 },
        };

        try
        {
            await repository.AddAsync(submission);
            await repository.AddFileAsync(file);

            var reloaded = await repository.GetByIdAsync(submission.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(FormScope.Site, reloaded!.Scope);
            Assert.Equal(2, reloaded.Answers.Count);
            Assert.Contains(reloaded.Answers, a => a.FieldId == "f2" && a.Values.Count == 2);

            // File metadata comes back without bytes; the single-file fetch includes them.
            var meta = Assert.Single(await repository.GetFilesBySubmissionAsync(submission.Id));
            Assert.Equal("site.png", meta.FileName);
            var full = await repository.GetFileAsync(file.Id);
            Assert.Equal(new byte[] { 9, 8, 7 }, full!.Content);

            // Review status persists.
            reloaded.Status = FormSubmissionStatus.Approved;
            reloaded.ReviewNote = "Approved";
            await repository.UpdateAsync(reloaded);
            Assert.Equal(FormSubmissionStatus.Approved, (await repository.GetByIdAsync(submission.Id))!.Status);

            Assert.Single(await repository.GetByCompanyAsync(companyId));
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM FormSubmissionFiles WHERE CompanyId = @id", new { id = companyId });
            await connection.ExecuteAsync("DELETE FROM FormSubmissions WHERE CompanyId = @id", new { id = companyId });
        }
    }
}
