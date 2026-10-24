using BlazorShadcn.Variants;
using Microsoft.AspNetCore.Components;

namespace BlazorShadcn.Components;

/// <summary>
/// A visual divider between sections of content. Port of the shadcn/ui <c>Separator</c> component,
/// which wraps Radix <c>Separator</c> semantics.
/// </summary>
/// <remarks>
/// A separator has no content: <see cref="ShadcnComponentBase.ChildContent"/> is inherited but ignored,
/// because the element is rendered as an empty <c>div</c>.
/// </remarks>
public partial class Separator : ShadcnComponentBase
{
    private static readonly VariantConfig Variants = new()
    {
        Base = "shrink-0 bg-border",
        Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["orientation"] = new Dictionary<string, string>
            {
                ["horizontal"] = "h-[1px] w-full",
                ["vertical"] = "h-full w-[1px]",
            },
        },
        DefaultVariants = new Dictionary<string, string>
        {
            ["orientation"] = "horizontal",
        },
    };

    /// <summary>Direction of the line. Defaults to <see cref="SeparatorOrientation.Horizontal"/>.</summary>
    [Parameter]
    public SeparatorOrientation Orientation { get; set; } = SeparatorOrientation.Horizontal;

    /// <summary>
    /// Whether the separator is purely visual. When <see langword="true"/> (the default) it is hidden from
    /// assistive technology with <c>role="none"</c>. When <see langword="false"/> it is exposed as
    /// <c>role="separator"</c>, which is meaningful only when it divides distinct regions of content.
    /// </summary>
    [Parameter]
    public bool Decorative { get; set; } = true;

    private string? CssClass => ClassValue(Variants.Resolve(new Dictionary<string, string?>
    {
        ["orientation"] = Orientation.ToString().ToLowerInvariant(),
    }));

    /// <summary>ARIA role of the root element: <c>none</c> when decorative, otherwise <c>separator</c>.</summary>
    private string Role => Decorative ? "none" : "separator";

    /// <summary>
    /// <c>aria-orientation</c> value. Emitted only for a non-decorative vertical separator; horizontal is
    /// the implicit default for <c>role="separator"</c>, so it is omitted.
    /// </summary>
    private string? AriaOrientation => !Decorative && Orientation == SeparatorOrientation.Vertical ? "vertical" : null;
}
