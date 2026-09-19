using Tedwren.Abstractions.Contracts.Documents;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Common;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Documents;

/// <summary>
/// Store-agnostic document distribution &amp; acknowledgement service (PRD §8.2). Sending a document creates one
/// acknowledgement row per recipient (the completion matrix); a recipient's signed receipt sets the row's
/// timestamp. Everything is scoped to the distributing company (R15); the document is stored through
/// <see cref="IImageStore"/> (no permanent public URL, R9) after size/type validation.
/// </summary>
public sealed class DocumentDistributionService : IDocumentDistributionService
{
    private readonly IDocumentDistributionRepository _repo;
    private readonly IImageStore _images;

    /// <summary>Creates the service over the distribution repository and the document store.</summary>
    public DocumentDistributionService(IDocumentDistributionRepository repo, IImageStore images)
    {
        _repo = repo;
        _images = images;
    }

    /// <summary>Distributes a document to the request's recipients and returns it with its completion counts.</summary>
    public async Task<DocumentDistributionDto> CreateAsync(Guid companyId, string sentBy, CreateDistributionRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("A document title is required.", nameof(request));
        }

        var recipients = (request.Recipients ?? new List<string>())
            .Select(r => r?.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (recipients.Count == 0)
        {
            throw new ArgumentException("At least one recipient is required.", nameof(request));
        }

        string? fileReference = null;
        if (!string.IsNullOrWhiteSpace(request.FileBase64))
        {
            UploadValidation.Validate(request.FileBase64, request.FileContentType, UploadKind.Document);
            var bytes = Convert.FromBase64String(StripDataUrl(request.FileBase64));
            fileReference = await _images.SaveAsync(bytes, request.FileContentType ?? "application/pdf", cancellationToken);
        }

        var distribution = new DocumentDistribution
        {
            CompanyId = companyId,
            Title = request.Title.Trim(),
            Category = Clean(request.Category),
            Audience = Clean(request.Audience),
            FileReference = fileReference,
            SentBy = string.IsNullOrWhiteSpace(sentBy) ? "System" : sentBy.Trim(),
        };

        var acknowledgements = recipients.Select(name => new DocumentAcknowledgement
        {
            DistributionId = distribution.Id,
            CompanyId = companyId,
            RecipientName = name!,
        }).ToList();

        await _repo.AddAsync(distribution, acknowledgements, cancellationToken);
        return ToDto(distribution, acknowledgements);
    }

    /// <summary>Returns a company's distributions, newest first, each with its signed/total counts.</summary>
    public async Task<IReadOnlyList<DocumentDistributionDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var distributions = await _repo.GetByCompanyAsync(companyId, cancellationToken);
        var acknowledgements = await _repo.GetAcknowledgementsForCompanyAsync(companyId, cancellationToken);
        var byDistribution = acknowledgements.GroupBy(a => a.DistributionId).ToDictionary(g => g.Key, g => (IReadOnlyList<DocumentAcknowledgement>)g.ToList());
        return distributions
            .Select(d => ToDto(d, byDistribution.TryGetValue(d.Id, out var list) ? list : Array.Empty<DocumentAcknowledgement>()))
            .ToList();
    }

    /// <summary>Returns a distribution with its completion matrix, or null when missing/cross-tenant (R15).</summary>
    public async Task<DocumentDistributionDetailDto?> GetAsync(Guid companyId, Guid distributionId, CancellationToken cancellationToken = default)
    {
        var distribution = await _repo.GetAsync(distributionId, cancellationToken);
        if (distribution is null || distribution.CompanyId != companyId)
        {
            return null;
        }

        var acknowledgements = await _repo.GetAcknowledgementsAsync(distributionId, cancellationToken);
        var rows = acknowledgements
            .OrderBy(a => a.RecipientName)
            .Select(a => new DocumentAcknowledgementDto(a.Id, a.RecipientName, a.AcknowledgedUtc is not null, a.AcknowledgedUtc))
            .ToList();
        return new DocumentDistributionDetailDto(ToDto(distribution, acknowledgements), rows);
    }

    /// <summary>Records a recipient's acknowledgement (idempotent), scoped to the company (R15).</summary>
    public async Task<bool> AcknowledgeAsync(Guid companyId, Guid acknowledgementId, CancellationToken cancellationToken = default)
    {
        var acknowledgement = await _repo.GetAcknowledgementAsync(acknowledgementId, cancellationToken);
        if (acknowledgement is null || acknowledgement.CompanyId != companyId)
        {
            return false;
        }

        if (acknowledgement.AcknowledgedUtc is null)
        {
            acknowledgement.AcknowledgedUtc = DateTimeOffset.UtcNow;
            await _repo.UpdateAcknowledgementAsync(acknowledgement, cancellationToken);
        }

        return true;
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Strips a "data:...;base64," prefix from a base64 payload, if present.</summary>
    private static string StripDataUrl(string base64)
    {
        var comma = base64.IndexOf(',');
        return base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0 ? base64[(comma + 1)..] : base64;
    }

    /// <summary>Maps a distribution + its acknowledgement rows to the summary DTO with completion counts.</summary>
    private static DocumentDistributionDto ToDto(DocumentDistribution d, IReadOnlyList<DocumentAcknowledgement> acknowledgements) => new(
        d.Id, d.Title, d.Category, d.Audience, d.FileReference is not null, d.SentBy, d.SentUtc,
        acknowledgements.Count(a => a.AcknowledgedUtc is not null), acknowledgements.Count);
}
