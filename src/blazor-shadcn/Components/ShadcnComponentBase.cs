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
        => throw new NotImplementedException();

    /// <summary>
    /// Returns <see cref="AdditionalAttributes"/> without the <c>class</c> key, so the merged class
    /// from <see cref="ClassValue"/> can be rendered explicitly without duplication.
    /// </summary>
    /// <returns>Attributes safe to splat onto the root element.</returns>
    protected IReadOnlyDictionary<string, object>? AttributesWithoutClass()
        => throw new NotImplementedException();
}
