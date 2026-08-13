namespace Tedwren.Web.Tests.Support;

/// <summary>
/// Locates source directories on disk for tests that scan shipped files (rather than the compiled
/// output) — the content lint gate needs the real <c>src/Tedwren.Web</c> copy files. Walks up from the
/// test binary until it finds the repository root (the directory holding <c>Tedwren.sln</c>).
/// </summary>
public static class RepoPaths
{
    /// <summary>Absolute path to the repository root (the directory containing <c>Tedwren.sln</c>).</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>Absolute path to the <c>src/Tedwren.Web</c> project directory.</summary>
    public static string WebProject { get; } = Path.Combine(RepoRoot, "src", "Tedwren.Web");

    /// <summary>Walks parent directories from the test binary until <c>Tedwren.sln</c> is found.</summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tedwren.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root (Tedwren.sln) from the test binary.");
    }
}
