using System.Text.RegularExpressions;
using BlazorShadcn.Registry;
using Xunit.Abstractions;

namespace BlazorShadcn.Tests.Registry;

/// <summary>
/// Contract tests for <see cref="DefaultRegistryEntries.All"/>. The CLI relies on these invariants
/// (unique kebab-case names, resolvable dependencies, files that can be copied), so a broken catalog fails here.
/// </summary>
public sealed class DefaultRegistryEntriesTests(ITestOutputHelper output)
{
    private static readonly Regex KebabCase = new(@"^[a-z][a-z0-9]*(-[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    [Fact]
    public void All_NamesAreUniqueCaseInsensitive()
    {
        var duplicates = DefaultRegistryEntries.All
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void All_NamesAreKebabCase()
    {
        var invalid = DefaultRegistryEntries.All
            .Select(entry => entry.Name)
            .Where(name => !KebabCase.IsMatch(name))
            .ToList();

        Assert.Empty(invalid);
    }

    [Fact]
    public void All_EveryEntryHasAtLeastOneFile()
    {
        var empty = DefaultRegistryEntries.All
            .Where(entry => entry.Files.Count == 0)
            .Select(entry => entry.Name)
            .ToList();

        Assert.Empty(empty);
    }

    [Fact]
    public void All_EveryEntryHasNonEmptyDescription()
    {
        var missing = DefaultRegistryEntries.All
            .Where(entry => string.IsNullOrWhiteSpace(entry.Description))
            .Select(entry => entry.Name)
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void All_RegistryDependenciesReferToExistingEntries()
    {
        var names = DefaultRegistryEntries.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dangling = DefaultRegistryEntries.All
            .SelectMany(entry => entry.RegistryDependencies.Select(dep => (entry.Name, Dependency: dep)))
            .Where(pair => !names.Contains(pair.Dependency))
            .Select(pair => $"{pair.Name} -> {pair.Dependency}")
            .ToList();

        Assert.Empty(dangling);
    }

    [Fact]
    public void All_UiAndBlockEntriesDependOnCore_DirectlyOrTransitively()
    {
        var registry = new ComponentRegistry(DefaultRegistryEntries.All);

        var violations = new List<string>();
        foreach (var entry in DefaultRegistryEntries.All.Where(e => e.Type is RegistryEntryType.Ui or RegistryEntryType.Block))
        {
            var resolved = registry.ResolveWithDependencies([entry.Name]);
            var names = resolved.Select(r => r.Name).ToList();

            if (!names.Contains("core", StringComparer.OrdinalIgnoreCase))
            {
                violations.Add($"{entry.Name}: core is not in its resolved dependencies");
            }
            else if (!string.Equals(names[0], "core", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"{entry.Name}: core is not first (resolved order: {string.Join(", ", names)})");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void All_CoreEntryHasNoRegistryDependencies()
    {
        var core = DefaultRegistryEntries.All.Single(entry => entry.Name == "core");

        Assert.Empty(core.RegistryDependencies);
    }

    [Fact]
    public void All_ResolvingEveryEntryTogether_DoesNotThrow()
    {
        var registry = new ComponentRegistry(DefaultRegistryEntries.All);

        var resolved = registry.ResolveWithDependencies(DefaultRegistryEntries.Names);

        Assert.Equal(DefaultRegistryEntries.All.Count, resolved.Count);
    }

    [Fact]
    public void All_FilesAreRelativePathsWithForwardSlashes()
    {
        var invalid = DefaultRegistryEntries.All
            .SelectMany(entry => entry.Files.Select(file => (entry.Name, File: file)))
            .Where(pair =>
                pair.File.StartsWith('/') ||
                pair.File.Contains('\\') ||
                pair.File.Split('/').Contains(".."))
            .Select(pair => $"{pair.Name}: {pair.File}")
            .ToList();

        Assert.Empty(invalid);
    }

    [Fact]
    public void All_FilesForExistingComponents_ExistOnDisk()
    {
        var repoRoot = FindRepoRoot();
        var sourceRoot = Path.Combine(repoRoot, "src", "blazor-shadcn");

        var missing = new List<string>();
        foreach (var entry in DefaultRegistryEntries.All)
        {
            // An entry whose first file is absent has not been created yet, so it is skipped rather than failed.
            if (!File.Exists(Path.Combine(sourceRoot, entry.Files[0])))
            {
                output.WriteLine($"Skipped '{entry.Name}': not created yet (missing {entry.Files[0]}).");
                continue;
            }

            missing.AddRange(entry.Files
                .Where(file => !File.Exists(Path.Combine(sourceRoot, file)))
                .Select(file => $"{entry.Name}: {file}"));
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void Names_MatchesAll()
    {
        Assert.Equal(
            DefaultRegistryEntries.All.Select(entry => entry.Name),
            DefaultRegistryEntries.Names);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "blazor-shadcn.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
