using System.Text;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.UiComponents.SampleData;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IQualificationService"/> for client <c>DataSource=Mock</c>. It maps the existing sample
/// qualification library to the shared DTOs so the Compliance page looks exactly as before. The card
/// operations are demo-only (the sample data has no cards), matching the prior "nothing is persisted"
/// behaviour; the API-backed service provides the real lifecycle.
/// </summary>
public sealed class ClientMockQualificationService : IQualificationService
{
    private readonly IListSampleDataService _list;

    /// <summary>Creates the service over the sample-data service.</summary>
    public ClientMockQualificationService(IListSampleDataService list) => _list = list;

    /// <summary>Maps the sample qualification library to type DTOs (with a stable id derived from the name).</summary>
    public Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QualificationTypeDto> types = _list.GetQualificationTypes()
            .Select(q => new QualificationTypeDto(
                StableId(q.Name), q.Name, q.Category, q.Issuer, q.ValidMonths,
                IsCscsVerifiable: q.Name.Contains("CSCS", StringComparison.OrdinalIgnoreCase), q.HeldBy))
            .ToList();
        return Task.FromResult(types);
    }

    /// <summary>No cards in the sample data — returns empty (the API path serves the real lifecycle).</summary>
    public Task<IReadOnlyList<QualificationCardDto>> GetCardsForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualificationCardDto>>(Array.Empty<QualificationCardDto>());

    /// <summary>Demo capture — not persisted in mock mode.</summary>
    public Task<Guid> CaptureCardAsync(CaptureCardRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());

    /// <summary>Demo confirm — not persisted in mock mode.</summary>
    public Task<bool> ConfirmCardAsync(ConfirmCardRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    /// <summary>Demo renew — not persisted in mock mode.</summary>
    public Task<Guid?> RenewCardAsync(RenewCardRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(Guid.NewGuid());

    /// <summary>No requirements evaluated in mock mode — returns an empty shortfall.</summary>
    public Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CompetencyShortfallDto(personId, trade, Array.Empty<string>()));

    /// <summary>
    /// Derives a stable GUID from a name so the sample library keeps consistent ids across renders. A
    /// simple non-cryptographic fold is used deliberately: it needs only be deterministic for a small
    /// fixed list, and it avoids <c>System.Security.Cryptography</c>, which is unsupported in the browser.
    /// </summary>
    private static Guid StableId(string value)
    {
        var bytes = new byte[16];
        var text = Encoding.UTF8.GetBytes(value);
        for (var i = 0; i < text.Length; i++)
        {
            bytes[i % 16] ^= text[i];
        }

        return new Guid(bytes);
    }
}
