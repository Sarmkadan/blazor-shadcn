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
/// <remarks>
/// Validated on construction, so an invalid catalog item cannot exist. Properties are get-only (no <c>init</c>),
/// which keeps <c>with</c> expressions from bypassing the checks.
/// </remarks>
public sealed record RegistryEntry
{
    /// <summary>Creates a validated registry entry.</summary>
    /// <param name="Name">Kebab-case unique name, e.g. <c>"button"</c>.</param>
    /// <param name="Type">Entry kind; determines the target directory.</param>
    /// <param name="Files">Source files (relative to the registry root) copied into the consumer project. Must not be empty.</param>
    /// <param name="RegistryDependencies">Names of other registry entries this one requires.</param>
    /// <param name="PackageDependencies">NuGet package ids (optionally with <c>@version</c>) required at runtime.</param>
    /// <param name="Description">One-line description shown by <c>list</c> and in docs. May be empty.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="Name"/>, <paramref name="Files"/>, <paramref name="RegistryDependencies"/>,
    /// <paramref name="PackageDependencies"/> or <paramref name="Description"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="Name"/> is not kebab-case; <paramref name="Files"/> is empty or contains a null, blank,
    /// rooted, backslash or <c>..</c> path; a <paramref name="RegistryDependencies"/> element is null or blank;
    /// or the entry lists itself in <paramref name="RegistryDependencies"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="Type"/> is not a defined <see cref="RegistryEntryType"/> value.</exception>
    public RegistryEntry(
        string Name,
        RegistryEntryType Type,
        IReadOnlyList<string> Files,
        IReadOnlyList<string> RegistryDependencies,
        IReadOnlyList<string> PackageDependencies,
        string Description)
    {
        ArgumentNullException.ThrowIfNull(Name);
        if (!RegistryValidation.IsKebabCase(Name))
        {
            throw new ArgumentException($"Registry entry name '{Name}' must be kebab-case (e.g. 'dropdown-menu').", nameof(Name));
        }

        if (!Enum.IsDefined(Type))
        {
            throw new ArgumentOutOfRangeException(nameof(Type), Type, "Registry entry type must be a defined RegistryEntryType value.");
        }

        ArgumentNullException.ThrowIfNull(Files);
        if (Files.Count == 0)
        {
            throw new ArgumentException($"Registry entry '{Name}' must list at least one file.", nameof(Files));
        }

        foreach (var file in Files)
        {
            if (!RegistryValidation.IsSafeRelativePath(file))
            {
                throw new ArgumentException($"Registry entry '{Name}' has an invalid file path '{file}': paths must be relative, use '/' and contain no '..'.", nameof(Files));
            }
        }

        ArgumentNullException.ThrowIfNull(RegistryDependencies);
        foreach (var dependency in RegistryDependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency))
            {
                throw new ArgumentException($"Registry entry '{Name}' has a null or blank dependency name.", nameof(RegistryDependencies));
            }
        }

        if (RegistryDependencies.Contains(Name, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"entry '{Name}' cannot depend on itself.", nameof(RegistryDependencies));
        }

        ArgumentNullException.ThrowIfNull(PackageDependencies);
        ArgumentNullException.ThrowIfNull(Description);

        this.Name = Name;
        this.Type = Type;
        this.Files = Files;
        this.RegistryDependencies = RegistryDependencies;
        this.PackageDependencies = PackageDependencies;
        this.Description = Description;
    }

    /// <summary>Kebab-case unique name, e.g. <c>"button"</c>.</summary>
    public string Name { get; }

    /// <summary>Entry kind; determines the target directory.</summary>
    public RegistryEntryType Type { get; }

    /// <summary>Source files (relative to the registry root) copied into the consumer project.</summary>
    public IReadOnlyList<string> Files { get; }

    /// <summary>Names of other registry entries this one requires.</summary>
    public IReadOnlyList<string> RegistryDependencies { get; }

    /// <summary>NuGet package ids (optionally with <c>@version</c>) required at runtime.</summary>
    public IReadOnlyList<string> PackageDependencies { get; }

    /// <summary>One-line description shown by <c>list</c> and in docs.</summary>
    public string Description { get; }
}
