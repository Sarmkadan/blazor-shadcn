namespace BlazorShadcn.Utils;

/// <summary>
/// Tailwind-aware class name composition, the Blazor equivalent of shadcn/ui's <c>cn()</c> helper
/// (<c>clsx</c> + <c>tailwind-merge</c>).
/// </summary>
/// <remarks>
/// Every generated component calls <see cref="Cn(object?[])"/> to combine its variant classes with
/// the consumer-supplied <c>class</c> attribute. Conflicting Tailwind utilities (e.g. <c>px-2</c> and
/// <c>px-4</c>) must be resolved so that the later class wins, exactly like <c>tailwind-merge</c>.
/// </remarks>
public static class ClassNames
{
    /// <summary>
    /// Joins the given inputs into a single space-separated class string, dropping
    /// <see langword="null"/>, <see langword="false"/>, empty strings and whitespace,
    /// then resolves conflicting Tailwind utilities so that the last one wins.
    /// </summary>
    /// <param name="inputs">
    /// Any mix of <see cref="string"/>, <see cref="bool"/> (ignored), <see cref="IEnumerable{T}"/> of strings,
    /// or <see langword="null"/>. Nested enumerables are flattened.
    /// </param>
    /// <returns>A normalized class string with duplicates and conflicts removed.</returns>
    /// <example>
    /// <code>
    /// ClassNames.Cn("px-2 py-1", isActive &amp;&amp; "bg-primary", "px-4")
    /// // => "py-1 bg-primary px-4"
    /// </code>
    /// </example>
    public static string Cn(params object?[] inputs)
        => throw new NotImplementedException();

    /// <summary>
    /// Resolves Tailwind utility conflicts in an already-joined class string. The last occurrence
    /// of each conflicting group (spacing, colors, sizing, typography, etc.) is kept.
    /// </summary>
    /// <param name="classes">A space-separated class string.</param>
    /// <returns>The class string with earlier conflicting utilities removed.</returns>
    public static string TwMerge(string classes)
        => throw new NotImplementedException();
}
