using Tedwren.Application.Persistence;
using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Expiry.Sources;

/// <summary>
/// The company-document expiry source (SUB-4): company insurances, accreditations and policies carry an expiry date
/// and are covered by the same SF-9 warning schedule as a card. Projects each current (non-superseded) document with
/// an expiry into an <see cref="ExpiryItem"/> for its owning company — email only, since a company document has no
/// operative to text.
/// </summary>
public sealed class CompanyDocumentExpirySource : IExpirySource
{
    private readonly ICompanyRepository _companies;
    private readonly ICompanyDocumentRepository _documents;

    /// <summary>Creates the source over the company and company-document repositories.</summary>
    public CompanyDocumentExpirySource(ICompanyRepository companies, ICompanyDocumentRepository documents)
    {
        _companies = companies;
        _documents = documents;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpiryItem>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        var items = new List<ExpiryItem>();

        foreach (var company in companies)
        {
            var documents = await _documents.GetByCompanyAsync(company.Id, cancellationToken);
            foreach (var document in documents.Where(d => !d.IsSuperseded && d.ExpiresOn is not null))
            {
                items.Add(new ExpiryItem(
                    ExpirySource.CompanyDocument,
                    document.Id,
                    PersonId: null,
                    company.Id,
                    document.ExpiresOn!.Value,
                    document.Name,
                    PersonName: null,
                    WorkerNumber: null,   // company-level document — no operative to text
                    company.ContactEmail));
            }
        }

        return items;
    }
}
