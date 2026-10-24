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
    private string _base = string.Empty;
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _variants = new Dictionary<string, IReadOnlyDictionary<string, string>>();
    private IReadOnlyDictionary<string, string> _defaultVariants = new Dictionary<string, string>();
    private IReadOnlyList<CompoundVariant> _compoundVariants = [];

    /// <summary>Classes always applied regardless of variant selection.</summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null"/>.</exception>
    public string Base
    {
        get => _base;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _base = value;
        }
    }

    /// <summary>
    /// Variant axes keyed by axis name; each axis maps a value name to its classes.
    /// Example: <c>{ "size": { "sm": "h-8 px-3", "lg": "h-10 px-8" } }</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null"/>.</exception>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Variants
    {
        get => _variants;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _variants = value;
        }
    }

    /// <summary>Default value per axis, used when the caller does not specify one.</summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null"/>.</exception>
    public IReadOnlyDictionary<string, string> DefaultVariants
    {
        get => _defaultVariants;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _defaultVariants = value;
        }
    }

    /// <summary>Compound variants applied when all of their conditions match the selection.</summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null"/>.</exception>
    public IReadOnlyList<CompoundVariant> CompoundVariants
    {
        get => _compoundVariants;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _compoundVariants = value;
        }
    }

    /// <summary>
    /// Builds a variant resolver from this configuration. The returned function accepts a selection
    /// (axis name to chosen value) plus an optional extra class string, and returns the final merged
    /// class string via <see cref="Utils.ClassNames.Cn(object?[])"/>.
    /// </summary>
    /// <remarks>
    /// The configuration itself is validated once here: every axis needs a non-blank name and at least one value,
    /// every value needs a class string (empty is allowed and means "no classes"), and defaults and compound
    /// conditions must reference existing axes and values. Each call of the returned resolver validates its
    /// selection against the axes, so an unknown key cannot pass silently.
    /// </remarks>
    /// <returns>A reusable resolver function.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an axis name is null or whitespace, an axis has no values, a value has a <see langword="null"/>
    /// class string, or <see cref="DefaultVariants"/> or a <see cref="CompoundVariant"/> references an axis or value
    /// not present in <see cref="Variants"/>.
    /// </exception>
    public Func<IReadOnlyDictionary<string, string?>?, string?, string> Compile()
    {
        foreach (var (axis, values) in Variants)
        {
            if (string.IsNullOrWhiteSpace(axis))
            {
                throw new InvalidOperationException($"VariantConfig axis name '{axis}' is null or whitespace.");
            }

            if (values is null || values.Count == 0)
            {
                throw new InvalidOperationException($"VariantConfig axis '{axis}' has no values.");
            }

            foreach (var (value, classes) in values)
            {
                if (classes is null)
                {
                    throw new InvalidOperationException($"VariantConfig value '{value}' of axis '{axis}' has a null class string.");
                }
            }
        }

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
            if (selection is not null)
            {
                foreach (var (key, selected) in selection)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(selection));

                    if (!axes.TryGetValue(key, out var allowed))
                    {
                        throw new ArgumentException($"Unknown variant axis '{key}'. Known axes: {DescribeNames(axes.Keys)}.", nameof(selection));
                    }

                    if (selected is not null && !allowed.ContainsKey(selected))
                    {
                        throw new ArgumentException($"Value '{selected}' is not defined for variant axis '{key}'. Allowed values: {DescribeNames(allowed.Keys)}.", nameof(selection));
                    }
                }
            }

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

                effective[axis] = chosen;
                parts.Add(values[chosen]);
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
    /// <exception cref="ArgumentException">
    /// Thrown when a selection key is null or whitespace, names an axis not defined in <see cref="Variants"/>,
    /// or selects a value not defined for its axis.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the configuration is invalid; see <see cref="Compile"/>.</exception>
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

    private static string DescribeNames(IEnumerable<string> names)
    {
        var list = string.Join(", ", names);
        return list.Length == 0 ? "none" : list;
    }

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
public sealed record CompoundVariant
{
    /// <summary>
    /// Creates a compound variant.
    /// </summary>
    /// <param name="conditions">Axis name to required value. Must contain at least one entry.</param>
    /// <param name="classes">Classes to apply when all conditions match. May be empty.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="conditions"/> or <paramref name="classes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="conditions"/> is empty, since such a variant would always apply.</exception>
    public CompoundVariant(IReadOnlyDictionary<string, string> conditions, string classes)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(classes);

        if (conditions.Count == 0)
        {
            throw new ArgumentException("A compound variant with no conditions would always apply.", nameof(conditions));
        }

        Conditions = conditions;
        Classes = classes;
    }

    /// <summary>Axis name to required value. Never empty.</summary>
    public IReadOnlyDictionary<string, string> Conditions { get; }

    /// <summary>Classes to apply when all conditions match.</summary>
    public string Classes { get; }

    /// <summary>Deconstructs the variant into its conditions and classes.</summary>
    /// <param name="conditions">The axis name to required value mapping.</param>
    /// <param name="classes">The classes applied when all conditions match.</param>
    public void Deconstruct(out IReadOnlyDictionary<string, string> conditions, out string classes)
    {
        conditions = Conditions;
        classes = Classes;
    }
}
