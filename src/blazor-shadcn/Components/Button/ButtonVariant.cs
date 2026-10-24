namespace BlazorShadcn.Components;

/// <summary>
/// Visual style of a <see cref="Button"/>, matching the variants of shadcn/ui's <c>buttonVariants</c>.
/// </summary>
public enum ButtonVariant
{
    /// <summary>Filled primary action.</summary>
    Default,

    /// <summary>Filled action for destructive operations such as deleting data.</summary>
    Destructive,

    /// <summary>Bordered button on the page background.</summary>
    Outline,

    /// <summary>Filled secondary action.</summary>
    Secondary,

    /// <summary>Transparent button that shows the accent color on hover.</summary>
    Ghost,

    /// <summary>Text-only button styled as a hyperlink.</summary>
    Link,
}
