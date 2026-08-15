using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ILeadRepository"/> for sales leads and their activity log (commercial database).</summary>
public sealed class LeadRepository : AdminRepositoryBase, ILeadRepository
{
    private const string Columns =
        "Id, CompanyName, ContactName, ContactEmail, Location, Model, NumberOfSites, EstimatedRevenue, Status, " +
        "AccountManager, CompanyNumber, AffiliateId, ConvertedAccountId, Source, CreatedUtc, UpdatedUtc";

    /// <summary>Creates the repository over the commercial connection factory.</summary>
    public LeadRepository(IAdminDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Inserts a new lead.</summary>
    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Leads (Id, CompanyName, ContactName, ContactEmail, Location, Model, NumberOfSites, " +
            "EstimatedRevenue, Status, AccountManager, CompanyNumber, AffiliateId, ConvertedAccountId, Source, CreatedUtc, UpdatedUtc) " +
            "VALUES (@Id, @CompanyName, @ContactName, @ContactEmail, @Location, @Model, @NumberOfSites, " +
            "@EstimatedRevenue, @Status, @AccountManager, @CompanyNumber, @AffiliateId, @ConvertedAccountId, @Source, @CreatedUtc, @UpdatedUtc)",
            ToParameters(lead), cancellationToken);

    /// <summary>Returns a lead by id, or null.</summary>
    public async Task<Lead?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Leads WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns every lead, newest-updated first.</summary>
    public async Task<IReadOnlyList<Lead>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Leads ORDER BY UpdatedUtc DESC", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the most recent open lead matching a company + email, or null.</summary>
    public async Task<Lead?> FindOpenByCompanyAndEmailAsync(string companyName, string? email, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Leads " +
            "WHERE Status NOT IN (@Converted, @Lost) AND LOWER(CompanyName) = @Company " +
            "AND LOWER(COALESCE(ContactEmail, '')) = @Email " +
            "ORDER BY UpdatedUtc DESC",
            new
            {
                Converted = (int)LeadStatus.Converted,
                Lost = (int)LeadStatus.Lost,
                Company = (companyName ?? string.Empty).ToLowerInvariant(),
                Email = (email ?? string.Empty).ToLowerInvariant(),
            },
            cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Updates a lead's mutable fields.</summary>
    public Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Leads SET CompanyName = @CompanyName, ContactName = @ContactName, ContactEmail = @ContactEmail, " +
            "Location = @Location, Model = @Model, NumberOfSites = @NumberOfSites, EstimatedRevenue = @EstimatedRevenue, " +
            "Status = @Status, AccountManager = @AccountManager, CompanyNumber = @CompanyNumber, AffiliateId = @AffiliateId, " +
            "ConvertedAccountId = @ConvertedAccountId, Source = @Source, UpdatedUtc = @UpdatedUtc WHERE Id = @Id",
            ToParameters(lead), cancellationToken);

    /// <summary>Appends a note to a lead's activity log.</summary>
    public Task AddNoteAsync(LeadNote note, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO LeadNotes (Id, LeadId, Author, Body, StatusChange, CreatedUtc) " +
            "VALUES (@Id, @LeadId, @Author, @Body, @StatusChange, @CreatedUtc)",
            new { note.Id, note.LeadId, note.Author, note.Body, note.StatusChange, note.CreatedUtc }, cancellationToken);

    /// <summary>Returns a lead's activity log, newest first.</summary>
    public async Task<IReadOnlyList<LeadNote>> ListNotesAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<NoteRow>(
            "SELECT Id, LeadId, Author, Body, StatusChange, CreatedUtc FROM LeadNotes WHERE LeadId = @LeadId ORDER BY CreatedUtc DESC",
            new { LeadId = leadId }, cancellationToken);
        return rows.Select(r => new LeadNote
        {
            Id = r.Id,
            LeadId = r.LeadId,
            Author = r.Author,
            Body = r.Body,
            StatusChange = r.StatusChange,
            CreatedUtc = r.CreatedUtc,
        }).ToList();
    }

    /// <summary>Flattens a lead to Dapper parameters (enums as int).</summary>
    private static object ToParameters(Lead l) => new
    {
        l.Id,
        l.CompanyName,
        l.ContactName,
        l.ContactEmail,
        l.Location,
        Model = (int)l.Model,
        l.NumberOfSites,
        l.EstimatedRevenue,
        Status = (int)l.Status,
        l.AccountManager,
        l.CompanyNumber,
        l.AffiliateId,
        l.ConvertedAccountId,
        l.Source,
        l.CreatedUtc,
        l.UpdatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static Lead ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyName = r.CompanyName,
        ContactName = r.ContactName,
        ContactEmail = r.ContactEmail,
        Location = r.Location,
        Model = (LeadModel)r.Model,
        NumberOfSites = r.NumberOfSites,
        EstimatedRevenue = r.EstimatedRevenue,
        Status = (LeadStatus)r.Status,
        AccountManager = r.AccountManager,
        CompanyNumber = r.CompanyNumber,
        AffiliateId = r.AffiliateId,
        ConvertedAccountId = r.ConvertedAccountId,
        Source = r.Source,
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };

    /// <summary>Flat row shape Dapper maps lead query results into.</summary>
    private sealed record Row(
        Guid Id, string CompanyName, string? ContactName, string? ContactEmail, string? Location, int Model,
        int? NumberOfSites, decimal? EstimatedRevenue, int Status, string? AccountManager, string? CompanyNumber,
        Guid? AffiliateId, Guid? ConvertedAccountId, string? Source, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);

    /// <summary>Flat row shape Dapper maps note query results into.</summary>
    private sealed record NoteRow(Guid Id, Guid LeadId, string? Author, string Body, string? StatusChange, DateTimeOffset CreatedUtc);
}
