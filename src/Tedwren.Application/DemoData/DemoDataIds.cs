using System.Security.Cryptography;
using System.Text;

namespace Tedwren.Application.DemoData;

/// <summary>
/// Deterministic identifiers for the demonstration dataset. Every demo record derives its <see cref="Guid"/>
/// from a fixed namespace plus a stable string key, so seeding always produces the same ids: a delete can find
/// and remove exactly what was created, and a recreate is idempotent. The two demo companies use fixed
/// literals in the same <c>demo</c> range so they are easy to recognise and can never collide with the seeded
/// Tedwren tenant or the qualification-library ids.
/// </summary>
public static class DemoDataIds
{
    /// <summary>The demo main contractor's company id.</summary>
    public static readonly Guid MainCompanyId = new("dede0001-0000-4000-8000-000000000001");

    /// <summary>The demo subcontractor's company id.</summary>
    public static readonly Guid SubCompanyId = new("dede0002-0000-4000-8000-000000000002");

    /// <summary>Namespace all derived demo ids hang off (a fixed, arbitrary GUID).</summary>
    private static readonly Guid Namespace = new("de300000-0000-4000-8000-00000000d340");

    /// <summary>The two demo company ids, for status counts and teardown scoping.</summary>
    public static IReadOnlyList<Guid> CompanyIds { get; } = new[] { MainCompanyId, SubCompanyId };

    /// <summary>
    /// Derives a stable <see cref="Guid"/> from a string key (RFC-4122 name-based v5-style, using SHA-1 over the
    /// fixed namespace). The same key always yields the same id, which is what makes the seed reproducible.
    /// </summary>
    public static Guid Derive(string key)
    {
        var namespaceBytes = Namespace.ToByteArray();
        var nameBytes = Encoding.UTF8.GetBytes(key);
        var buffer = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, buffer, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, buffer, namespaceBytes.Length, nameBytes.Length);

        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(buffer, hash);

        var guid = new byte[16];
        hash[..16].CopyTo(guid);
        // Set the version (5) and variant bits so it is a well-formed GUID.
        guid[6] = (byte)((guid[6] & 0x0F) | 0x50);
        guid[8] = (byte)((guid[8] & 0x3F) | 0x80);
        return new Guid(guid);
    }
}
