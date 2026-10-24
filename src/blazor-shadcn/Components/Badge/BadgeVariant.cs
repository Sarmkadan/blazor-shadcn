namespace BlazorShadcn.Components;

/// <summary>
/// Visual style of a <see cref="Badge"/>, matching the variants of shadcn/ui's <c>badgeVariants</c>.
/// </summary>
public enum BadgeVariant
{
    /// <summary>Filled primary label.</summary>
    Default,

    /// <summary>Filled label in the secondary color.</summary>
    Secondary,

    /// <summary>Filled label for errors or destructive states.</summary>
    Destructive,

    /// <summary>Bordered label on the page background.</summary>
    Outline,
}
