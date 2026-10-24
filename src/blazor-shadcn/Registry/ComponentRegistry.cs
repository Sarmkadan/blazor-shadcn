using System.Collections.Immutable;

namespace BlazorShadcn.Registry;

/// <summary>
/// In-memory <see cref="IComponentRegistry"/> built from a fixed set of <see cref="RegistryEntry"/> values.
/// Names are matched case-insensitively, and dependency resolution is a depth-first topological sort.
/// </summary>
public sealed class ComponentRegistry : IComponentRegistry
{
    private readonly Dictionary<string, RegistryEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ImmutableArray<RegistryEntry> _all;

    /// <summary>Creates a registry from the given entries.</summary>
    /// <param name="entries">Entries to register. Names must be unique, ignoring case.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="entries"/> contains a <see langword="null"/> element, or two entries share the same name (case-insensitive).
    /// </exception>
    public ComponentRegistry(IEnumerable<RegistryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var index = 0;
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                throw new ArgumentException($"entries contains a null element at index {index}.", nameof(entries));
            }

            index++;
            if (!_entries.TryAdd(entry.Name, entry))
            {
                throw new ArgumentException($"Duplicate registry entry name '{entry.Name}'.", nameof(entries));
            }
        }

        _all = _entries.Values
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    /// <summary>Creates the registry containing the entries shipped with this library.</summary>
    /// <returns>A registry built from <see cref="DefaultRegistryEntries.All"/>.</returns>
    public static ComponentRegistry CreateDefault() => new(DefaultRegistryEntries.All);

    /// <inheritdoc />
    public IReadOnlyList<RegistryEntry> GetAll() => _all;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace.</exception>
    public RegistryEntry? Find(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _entries.TryGetValue(name, out var entry) ? entry : null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Each requested name is trimmed and visited in order; its <see cref="RegistryEntry.RegistryDependencies"/> are
    /// visited before the entry itself is emitted. Entries already emitted are skipped, so the output is
    /// de-duplicated, dependencies precede their dependents, and independent entries keep request order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> is <see langword="null"/>, or one of its elements is.</exception>
    /// <exception cref="ArgumentException">One of <paramref name="names"/> is empty or whitespace.</exception>
    public IReadOnlyList<RegistryEntry> ResolveWithDependencies(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        // Validate every requested name before resolving any of them, so a bad element never leaves partial work behind.
        var requested = new List<string>();
        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(names));
            requested.Add(name.Trim());
        }

        var output = new List<RegistryEntry>();
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        foreach (var name in requested.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Visit(name, requiredBy: null, output, emitted, visiting, path);
        }

        return output;
    }

    private void Visit(
        string name,
        string? requiredBy,
        List<RegistryEntry> output,
        HashSet<string> emitted,
        HashSet<string> visiting,
        List<string> path)
    {
        var entry = Find(name) ?? throw new KeyNotFoundException(requiredBy is null
            ? $"Registry entry '{name}' was requested but does not exist."
            : $"Registry entry '{name}' is required by '{requiredBy}' but does not exist.");

        if (emitted.Contains(entry.Name))
        {
            return;
        }

        if (!visiting.Add(entry.Name))
        {
            // The entry is still on the current path, so the path from its first occurrence closes a cycle.
            var cycleStart = path.FindIndex(p => string.Equals(p, entry.Name, StringComparison.OrdinalIgnoreCase));
            var cycle = path.Skip(cycleStart).Append(entry.Name);
            throw new InvalidOperationException($"Circular registry dependency: {string.Join(" -> ", cycle)}.");
        }

        path.Add(entry.Name);

        foreach (var dependency in entry.RegistryDependencies)
        {
            Visit(dependency, entry.Name, output, emitted, visiting, path);
        }

        path.RemoveAt(path.Count - 1);
        visiting.Remove(entry.Name);
        emitted.Add(entry.Name);
        output.Add(entry);
    }
}
