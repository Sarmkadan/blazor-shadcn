using BlazorShadcn.Utils;
using Microsoft.AspNetCore.Components;

namespace BlazorShadcn.Components;

/// <summary>
/// Base class shared by every copy-paste component in the registry. Provides the standard
/// <c>Class</c> parameter, attribute splatting and the <see cref="ClassValue"/> helper so that
/// component <c>.razor</c> files stay minimal and consumer overrides always win.
/// </summary>
/// <remarks>
/// Components are meant to be copied into the consumer's project, so this base class is also
/// copied (it is part of the <c>core</c> registry entry). Keep it dependency-free beyond
/// <c>Microsoft.AspNetCore.Components</c>.
/// </remarks>
public abstract class ShadcnComponentBase : ComponentBase
{
    /// <summary>Additional CSS classes appended after the component's own variant classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Content rendered inside the component's root element.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Unmatched attributes forwarded to the root element (e.g. <c>id</c>, <c>aria-*</c>, <c>data-*</c>,
    /// event handlers). A consumer-supplied <c>class</c> here is merged, not overwritten.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Computes the final class string for the root element by merging the component's base and
    /// variant classes with <see cref="Class"/> and any <c>class</c> found in <see cref="AdditionalAttributes"/>.
    /// </summary>
    /// <param name="componentClasses">Classes produced by the component's variant resolver.</param>
    /// <returns>A merged class string, or <see langword="null"/> when empty.</returns>
    protected string? ClassValue(string? componentClasses)
    {
        // Order matters: variant classes first, then Class, then the splatted class so consumer overrides win.
        var merged = ClassNames.Cn(componentClasses, Class, SplattedClass());
        return merged.Length == 0 ? null : merged;
    }

    /// <summary>
    /// Returns <see cref="AdditionalAttributes"/> without the <c>class</c> key, so the merged class
    /// from <see cref="ClassValue"/> can be rendered explicitly without duplication.
    /// </summary>
    /// <returns>Attributes safe to splat onto the root element.</returns>
    protected IReadOnlyDictionary<string, object>? AttributesWithoutClass()
    {
        if (AdditionalAttributes is null or { Count: 0 })
        {
            return null;
        }

        if (!HasClassKey(AdditionalAttributes))
        {
            return AdditionalAttributes;
        }

        var filtered = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in AdditionalAttributes)
        {
            if (!IsClassKey(key))
            {
                filtered[key] = value;
            }
        }

        return filtered;
    }

    /// <summary>
    /// Root classes for components without variants: equivalent to <c>ClassValue(null)</c>.
    /// </summary>
    protected virtual string? RootClass => ClassValue(null);

    /// <summary>Reads the <c>class</c> value from <see cref="AdditionalAttributes"/>, if present.</summary>
    private string? SplattedClass()
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        foreach (var (key, value) in AdditionalAttributes)
        {
            if (IsClassKey(key))
            {
                return value?.ToString();
            }
        }

        return null;
    }

    private static bool HasClassKey(IReadOnlyDictionary<string, object> attributes)
    {
        foreach (var key in attributes.Keys)
        {
            if (IsClassKey(key))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsClassKey(string key)
        => string.Equals(key, "class", StringComparison.OrdinalIgnoreCase);
}
