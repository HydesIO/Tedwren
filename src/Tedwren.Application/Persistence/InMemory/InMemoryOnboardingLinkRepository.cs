using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IOnboardingLinkRepository"/>.</summary>
public sealed class InMemoryOnboardingLinkRepository : IOnboardingLinkRepository
{
    private readonly ConcurrentDictionary<Guid, OnboardingLink> _links = new();

    /// <summary>Persists a new onboarding link.</summary>
    public Task AddAsync(OnboardingLink link, CancellationToken cancellationToken = default)
    {
        _links[link.Id] = link;
        return Task.CompletedTask;
    }

    /// <summary>Returns the link for a token, or null.</summary>
    public Task<OnboardingLink?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(_links.Values.FirstOrDefault(l => l.Token == token));

    /// <summary>Persists changes to an existing link.</summary>
    public Task UpdateAsync(OnboardingLink link, CancellationToken cancellationToken = default)
    {
        _links[link.Id] = link;
        return Task.CompletedTask;
    }
}

/// <summary>Test-only in-memory <see cref="IImageStore"/>.</summary>
public sealed class InMemoryImageStore : IImageStore
{
    private readonly ConcurrentDictionary<Guid, StoredImage> _images = new();

    /// <summary>Stores bytes and returns the image reference (its id).</summary>
    public Task<string> SaveAsync(byte[] bytes, string contentType, CancellationToken cancellationToken = default)
    {
        var image = new StoredImage { ContentType = contentType, Bytes = bytes };
        _images[image.Id] = image;
        return Task.FromResult(image.Id.ToString());
    }

    /// <summary>Returns a stored image by reference, or null.</summary>
    public Task<StoredImage?> GetAsync(string reference, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.TryParse(reference, out var id) ? _images.GetValueOrDefault(id) : null);
}
