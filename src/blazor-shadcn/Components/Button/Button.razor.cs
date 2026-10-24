using BlazorShadcn.Variants;
using Microsoft.AspNetCore.Components;

namespace BlazorShadcn.Components;

/// <summary>
/// A clickable button with shadcn/ui variants and sizes. Port of the shadcn/ui <c>Button</c> component.
/// </summary>
public partial class Button : ShadcnComponentBase
{
    private static readonly VariantConfig Variants = new()
    {
        Base = "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
        Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["variant"] = new Dictionary<string, string>
            {
                ["default"] = "bg-primary text-primary-foreground shadow hover:bg-primary/90",
                ["destructive"] = "bg-destructive text-destructive-foreground shadow-sm hover:bg-destructive/90",
                ["outline"] = "border border-input bg-background shadow-sm hover:bg-accent hover:text-accent-foreground",
                ["secondary"] = "bg-secondary text-secondary-foreground shadow-sm hover:bg-secondary/80",
                ["ghost"] = "hover:bg-accent hover:text-accent-foreground",
                ["link"] = "text-primary underline-offset-4 hover:underline",
            },
            ["size"] = new Dictionary<string, string>
            {
                ["default"] = "h-9 px-4 py-2",
                ["sm"] = "h-8 rounded-md px-3 text-xs",
                ["lg"] = "h-10 rounded-md px-8",
                ["icon"] = "h-9 w-9",
            },
        },
        DefaultVariants = new Dictionary<string, string>
        {
            ["variant"] = "default",
            ["size"] = "default",
        },
    };

    /// <summary>Visual style of the button. Defaults to <see cref="ButtonVariant.Default"/>.</summary>
    [Parameter]
    public ButtonVariant Variant { get; set; }

    /// <summary>Size of the button. Defaults to <see cref="ButtonSize.Default"/>.</summary>
    [Parameter]
    public ButtonSize Size { get; set; }

    /// <summary>Value of the native <c>type</c> attribute. Defaults to <c>button</c> so it never submits a form by accident.</summary>
    [Parameter]
    public string Type { get; set; } = "button";

    /// <summary>Whether the button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? CssClass => ClassValue(Variants.Resolve(new Dictionary<string, string?>
    {
        ["variant"] = Variant.ToString().ToLowerInvariant(),
        ["size"] = Size.ToString().ToLowerInvariant(),
    }));
}
