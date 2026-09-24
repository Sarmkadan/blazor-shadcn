namespace BlazorShadcn.Registry;

/// <summary>
/// Catalog of copy-paste components. Mirrors the shadcn/ui registry: each entry lists the source
/// files to copy, the other entries it depends on, and the NuGet packages it requires.
/// </summary>
/// <remarks>
/// The registry is consumed by the CLI (<c>dotnet shadcn add button</c>) and can also be queried
/// programmatically, e.g. by a docs site that renders the component gallery.
/// </remarks>
public interface IComponentRegistry
{
    /// <summary>Returns every registered entry, sorted by <see cref="RegistryEntry.Name"/>.</summary>
    IReadOnlyList<RegistryEntry> GetAll();

    /// <summary>
    /// Looks up a single entry by its kebab-case name (e.g. <c>"button"</c>, <c>"dropdown-menu"</c>).
    /// </summary>
    /// <param name="name">Registry entry name, case-insensitive.</param>
    /// <returns>The entry, or <see langword="null"/> when no entry with that name exists.</returns>
    RegistryEntry? Find(string name);

    /// <summary>
    /// Resolves the full install set for the given entries: the entries themselves plus every
    /// transitive <see cref="RegistryEntry.RegistryDependencies"/>, in dependency order (dependencies first).
    /// </summary>
    /// <param name="names">Entry names requested by the user.</param>
    /// <returns>Ordered, de-duplicated list of entries to install.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when a requested or transitive name is unknown.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the dependency graph contains a cycle.</exception>
    IReadOnlyList<RegistryEntry> ResolveWithDependencies(IEnumerable<string> names);
}

/// <summary>Kind of registry entry, used to pick the destination folder on install.</summary>
public enum RegistryEntryType
{
    /// <summary>Shared infrastructure such as <c>ClassNames</c> and <c>ShadcnComponentBase</c>.</summary>
    Core,

    /// <summary>A UI component (<c>Button</c>, <c>Card</c>, <c>Dialog</c>, ...).</summary>
    Ui,

    /// <summary>A reusable hook-like service (<c>IThemeService</c>, focus trap, ...).</summary>
    Service,

    /// <summary>A pre-composed block built from several UI components (login form, dashboard shell).</summary>
    Block,
}

/// <summary>A single installable unit in the registry.</summary>
/// <param name="Name">Kebab-case unique name, e.g. <c>"button"</c>.</param>
/// <param name="Type">Entry kind; determines the target directory.</param>
/// <param name="Files">Source files (relative to the registry root) copied into the consumer project.</param>
/// <param name="RegistryDependencies">Names of other registry entries this one requires.</param>
/// <param name="PackageDependencies">NuGet package ids (optionally with <c>@version</c>) required at runtime.</param>
/// <param name="Description">One-line description shown by <c>list</c> and in docs.</param>
public sealed record RegistryEntry(
    string Name,
    RegistryEntryType Type,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> RegistryDependencies,
    IReadOnlyList<string> PackageDependencies,
    string Description);
