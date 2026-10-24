using Microsoft.AspNetCore.Components;

namespace BlazorShadcn.Components;

/// <summary>
/// A styled form label. Port of the shadcn/ui <c>Label</c> component.
/// </summary>
/// <remarks>
/// The HTML <c>for</c> attribute is exposed as <see cref="For"/>, because <c>for</c> is a C# keyword.
/// It dims together with its control through the <c>peer-disabled</c> classes, so place the control
/// that it describes as a <c>peer</c> sibling where the consumer wants that behavior.
/// </remarks>
public partial class Label : ShadcnComponentBase
{
    private const string BaseClasses = "text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70";

    /// <summary>
    /// Maps to the HTML <c>for</c> attribute: the <c>id</c> of the form control this label describes,
    /// for example an <see cref="Input"/>. Not rendered when <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// When set, a <c>for</c> key supplied through unmatched attributes is dropped so that this value wins.
    /// </remarks>
    [Parameter]
    public string? For { get; set; }

    /// <summary>
    /// Returns the splatted attributes without <c>class</c>, and without <c>for</c> when <see cref="For"/> is set,
    /// so that the explicit <c>for</c> on the root element is never duplicated.
    /// </summary>
    /// <returns>Attributes safe to splat onto the label element.</returns>
    private IReadOnlyDictionary<string, object>? AttributesWithoutFor()
    {
        var attributes = AttributesWithoutClass();
        if (For is null || attributes is null)
        {
            return attributes;
        }

        var filtered = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in attributes)
        {
            if (!string.Equals(key, "for", StringComparison.OrdinalIgnoreCase))
            {
                filtered[key] = value;
            }
        }

        return filtered;
    }
}
