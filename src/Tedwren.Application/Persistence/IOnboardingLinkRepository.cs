using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for self-service onboarding links (SF-4, SUB-2).</summary>
public interface IOnboardingLinkRepository
{
    /// <summary>Persists a new onboarding link.</summary>
    Task AddAsync(OnboardingLink link, CancellationToken cancellationToken = default);

    /// <summary>Returns the link for a token, or null.</summary>
    Task<OnboardingLink?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing link (status, person/engagement).</summary>
    Task UpdateAsync(OnboardingLink link, CancellationToken cancellationToken = default);
}

/// <summary>Store for private binary assets (card photographs, SF-5), served only via an authorised endpoint (R9).</summary>
public interface IImageStore
{
    /// <summary>Stores bytes and returns the image reference (its id).</summary>
    Task<string> SaveAsync(byte[] bytes, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Returns a stored image by reference, or null.</summary>
    Task<StoredImage?> GetAsync(string reference, CancellationToken cancellationToken = default);
}
