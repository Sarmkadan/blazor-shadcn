namespace BlazorShadcn.Variants;

/// <summary>
/// Declarative variant configuration for a component, the Blazor equivalent of
/// <c>class-variance-authority</c> (<c>cva</c>) used throughout shadcn/ui.
/// </summary>
/// <remarks>
/// A component declares its base classes, a set of named variant axes (e.g. <c>variant</c>, <c>size</c>),
/// the allowed values for each axis with their classes, default values, and optional compound variants
/// that apply only when several axes match at once.
/// </remarks>
public sealed class VariantConfig
{
    /// <summary>Classes always applied regardless of variant selection.</summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>
    /// Variant axes keyed by axis name; each axis maps a value name to its classes.
    /// Example: <c>{ "size": { "sm": "h-8 px-3", "lg": "h-10 px-8" } }</c>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Variants { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, string>>();

    /// <summary>Default value per axis, used when the caller does not specify one.</summary>
    public IReadOnlyDictionary<string, string> DefaultVariants { get; init; }
        = new Dictionary<string, string>();

    /// <summary>Compound variants applied when all of their conditions match the selection.</summary>
    public IReadOnlyList<CompoundVariant> CompoundVariants { get; init; } = [];

    /// <summary>
    /// Builds a variant resolver from this configuration. The returned function accepts a selection
    /// (axis name to chosen value) plus an optional extra class string, and returns the final merged
    /// class string via <see cref="Utils.ClassNames.Cn(object?[])"/>.
    /// </summary>
    /// <remarks>
    /// Selection keys that do not name a defined axis are ignored, so a caller can pass a wider props bag
    /// without breaking resolution. A selected value that is not defined for a known axis throws.
    /// </remarks>
    /// <returns>A reusable resolver function.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="DefaultVariants"/> or a <see cref="CompoundVariant"/> references an axis or value
    /// not present in <see cref="Variants"/>.
    /// </exception>
    public Func<IReadOnlyDictionary<string, string?>?, string?, string> Compile()
    {
        foreach (var (axis, value) in DefaultVariants)
        {
            ValidateAxisValue(axis, value, "DefaultVariants");
        }

        foreach (var compound in CompoundVariants)
        {
            foreach (var (axis, value) in compound.Conditions)
            {
                ValidateAxisValue(axis, value, "CompoundVariants");
            }
        }

        var baseClasses = Base;
        var axes = Variants;
        var defaults = DefaultVariants;
        var compounds = CompoundVariants.ToArray();

        return (selection, className) =>
        {
            var parts = new List<object?> { baseClasses };
            var effective = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var (axis, values) in axes)
            {
                string? chosen = selection is not null && selection.TryGetValue(axis, out var selected) && selected is not null
                    ? selected
                    : defaults.TryGetValue(axis, out var fallback) ? fallback : null;
                if (chosen is null)
                {
                    continue;
                }

                if (!values.TryGetValue(chosen, out var classes))
                {
                    throw new ArgumentException($"Value '{chosen}' is not defined for variant axis '{axis}'.", nameof(selection));
                }

                effective[axis] = chosen;
                parts.Add(classes);
            }

            foreach (var compound in compounds)
            {
                if (compound.Conditions.All(condition => effective.TryGetValue(condition.Key, out var value) && value == condition.Value))
                {
                    parts.Add(compound.Classes);
                }
            }

            parts.Add(className);
            return Utils.ClassNames.Cn(parts.ToArray());
        };
    }

    /// <summary>
    /// Resolves the class string for a single selection, compiling this configuration on first use and
    /// caching the compiled resolver. Convenience wrapper over <see cref="Compile"/> for one-off usage.
    /// </summary>
    /// <param name="selection">Axis name to chosen value; <see langword="null"/> values fall back to defaults.</param>
    /// <param name="className">Extra consumer classes appended last (they win on conflict).</param>
    /// <returns>The merged class string.</returns>
    /// <exception cref="ArgumentException">Thrown when a selected value is not defined for its axis.</exception>
    /// <example>
    /// Button with <c>variant="destructive"</c> and <c>size="sm"</c> plus a consumer class:
    /// <code>
    /// var classes = buttonVariants.Resolve(
    ///     new Dictionary&lt;string, string?&gt; { ["variant"] = "destructive", ["size"] = "sm" },
    ///     "w-full");
    /// // => "inline-flex ... bg-destructive text-destructive-foreground ... h-8 rounded-md px-3 text-xs w-full"
    /// </code>
    /// </example>
    public string Resolve(IReadOnlyDictionary<string, string?>? selection = null, string? className = null)
        => (_compiled ??= Compile())(selection, className);

    private Func<IReadOnlyDictionary<string, string?>?, string?, string>? _compiled;

    private void ValidateAxisValue(string axis, string value, string source)
    {
        if (!Variants.TryGetValue(axis, out var values))
        {
            throw new InvalidOperationException($"{source} references variant axis '{axis}' which is not defined in Variants.");
        }

        if (!values.ContainsKey(value))
        {
            throw new InvalidOperationException($"{source} references value '{value}' which is not defined for variant axis '{axis}'.");
        }
    }
}

/// <summary>
/// A set of classes applied only when every listed axis/value condition is satisfied.
/// </summary>
/// <param name="Conditions">Axis name to required value.</param>
/// <param name="Classes">Classes to apply when all conditions match.</param>
public sealed record CompoundVariant(IReadOnlyDictionary<string, string> Conditions, string Classes);
