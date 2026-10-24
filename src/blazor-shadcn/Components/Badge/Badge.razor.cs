using BlazorShadcn.Variants;
using Microsoft.AspNetCore.Components;

namespace BlazorShadcn.Components;

/// <summary>
/// A small status label with shadcn/ui variants. Port of the shadcn/ui <c>Badge</c> component.
/// </summary>
public partial class Badge : ShadcnComponentBase
{
    private static readonly VariantConfig Variants = new()
    {
        Base = "inline-flex items-center rounded-md border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
        Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["variant"] = new Dictionary<string, string>
            {
                ["default"] = "border-transparent bg-primary text-primary-foreground shadow hover:bg-primary/80",
                ["secondary"] = "border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80",
                ["destructive"] = "border-transparent bg-destructive text-destructive-foreground shadow hover:bg-destructive/80",
                ["outline"] = "text-foreground",
            },
        },
        DefaultVariants = new Dictionary<string, string>
        {
            ["variant"] = "default",
        },
    };

    /// <summary>Visual style of the badge. Defaults to <see cref="BadgeVariant.Default"/>.</summary>
    [Parameter]
    public BadgeVariant Variant { get; set; }

    private string? CssClass => ClassValue(Variants.Resolve(new Dictionary<string, string?>
    {
        ["variant"] = Variant.ToString().ToLowerInvariant(),
    }));

    /// <summary>
    /// Returns the badge classes for a variant, so any element (for example an <c>&lt;a&gt;</c> link) can be
    /// styled as a badge. Mirrors the <c>badgeVariants</c> export of shadcn/ui.
    /// </summary>
    /// <param name="variant">Visual style to resolve.</param>
    /// <param name="className">Extra classes appended last; they win on Tailwind conflicts.</param>
    /// <returns>The merged class string.</returns>
    public static string BadgeClasses(BadgeVariant variant, string? className = null)
        => Variants.Resolve(new Dictionary<string, string?>
        {
            ["variant"] = variant.ToString().ToLowerInvariant(),
        }, className);
}
