using Microsoft.AspNetCore.Components;

namespace BlazorShadcn.Components;

/// <summary>
/// A styled single-line text input. Port of the shadcn/ui <c>Input</c> component.
/// </summary>
/// <remarks>
/// Deliberately not derived from <c>InputBase&lt;T&gt;</c>, so it works both inside and outside an <c>EditForm</c>.
/// <c>@bind-Value</c> is supported through the <see cref="Value"/> / <see cref="ValueChanged"/> pair.
/// </remarks>
public partial class Input : ShadcnComponentBase
{
    private const string BaseClasses = "flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-sm transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 md:text-sm";

    /// <summary>The current text of the input.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Raised with the new text when the value changes. Enables <c>@bind-Value</c>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Value of the native <c>type</c> attribute, e.g. <c>text</c>, <c>email</c> or <c>password</c>. Defaults to <c>text</c>.</summary>
    [Parameter]
    public string Type { get; set; } = "text";

    /// <summary>Hint text shown while the input is empty.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Whether the input is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the input is read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// When <see langword="false"/> (default) <see cref="ValueChanged"/> is raised on the native <c>change</c> event,
    /// i.e. when the input loses focus or is committed. When <see langword="true"/> it is raised on every <c>input</c> event.
    /// </summary>
    [Parameter]
    public bool UpdateOnInput { get; set; }

    private async Task OnChange(ChangeEventArgs args)
    {
        if (!UpdateOnInput)
        {
            await UpdateValueAsync(args.Value?.ToString());
        }
    }

    private async Task OnInput(ChangeEventArgs args)
    {
        if (UpdateOnInput)
        {
            await UpdateValueAsync(args.Value?.ToString());
        }
    }

    private async Task UpdateValueAsync(string? next)
    {
        // Null and empty are the same visible state, so they must not count as a change.
        if (string.Equals(next ?? string.Empty, Value ?? string.Empty, StringComparison.Ordinal))
        {
            return;
        }

        Value = next;
        await ValueChanged.InvokeAsync(next);
    }
}
