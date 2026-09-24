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
    /// <returns>A reusable resolver function.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="DefaultVariants"/> references an axis or value not present in <see cref="Variants"/>.
    /// </exception>
    public Func<IReadOnlyDictionary<string, string?>?, string?, string> Compile()
        => throw new NotImplementedException();

    /// <summary>
    /// Resolves the class string for a single selection without compiling first.
    /// Convenience wrapper over <see cref="Compile"/> for one-off usage.
    /// </summary>
    /// <param name="selection">Axis name to chosen value; <see langword="null"/> values fall back to defaults.</param>
    /// <param name="className">Extra consumer classes appended last (they win on conflict).</param>
    /// <returns>The merged class string.</returns>
    public string Resolve(IReadOnlyDictionary<string, string?>? selection = null, string? className = null)
        => throw new NotImplementedException();
}

/// <summary>
/// A set of classes applied only when every listed axis/value condition is satisfied.
/// </summary>
/// <param name="Conditions">Axis name to required value.</param>
/// <param name="Classes">Classes to apply when all conditions match.</param>
public sealed record CompoundVariant(IReadOnlyDictionary<string, string> Conditions, string Classes);
