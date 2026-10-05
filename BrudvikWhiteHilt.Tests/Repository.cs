using System;
using System.IO;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Finds files in the repository, for tests that check the source and data files themselves.
/// </summary>
internal static class Repository
{
    private static readonly Lazy<string> root = new(FindRoot);

    /// <summary>The repository's root folder.</summary>
    public static string Root => root.Value;

    /// <summary>
    /// A path inside the repository.
    /// </summary>
    /// <param name="parts">Path parts below the root.</param>
    /// <returns>The full path.</returns>
    public static string Path(params string[] parts)
    {
        return System.IO.Path.Combine(Root, System.IO.Path.Combine(parts));
    }

    private static string FindRoot()
    {
        DirectoryInfo directory = new(AppDomain.CurrentDomain.BaseDirectory);
        while (directory != null && !File.Exists(System.IO.Path.Combine(directory.FullName, "BrudvikWhiteHilt.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("BrudvikWhiteHilt.sln not found above the test output.");
    }
}
