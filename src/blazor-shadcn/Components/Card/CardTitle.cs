using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorShadcn.Components;

/// <summary>
/// The title of a <see cref="Card"/>. Port of the shadcn/ui <c>CardTitle</c> component.
/// </summary>
/// <remarks>
/// Renders an <c>h3</c> by default, matching shadcn/ui. Blazor has no dynamic element, so the tag is
/// chosen at render time through <see cref="As"/>; the markup is built in code instead of <c>.razor</c> markup.
/// </remarks>
public partial class CardTitle : ShadcnComponentBase
{
    private const string BaseClasses = "font-semibold leading-none tracking-tight";

    /// <summary>
    /// HTML tag name of the rendered title element, e.g. <c>h2</c> or <c>div</c>. Defaults to <c>h3</c>.
    /// </summary>
    [Parameter]
    public string As { get; set; } = "h3";

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As);
        builder.AddAttribute(1, "class", ClassValue(BaseClasses));
        builder.AddMultipleAttributes(2, AttributesWithoutClass());
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
