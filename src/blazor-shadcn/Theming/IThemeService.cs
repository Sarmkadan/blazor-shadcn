namespace BlazorShadcn.Theming;

/// <summary>Color scheme preference, matching shadcn/ui's <c>next-themes</c> semantics.</summary>
public enum ThemeMode
{
    /// <summary>Follow the operating system / browser <c>prefers-color-scheme</c>.</summary>
    System,

    /// <summary>Force the light palette.</summary>
    Light,

    /// <summary>Force the dark palette.</summary>
    Dark,
}

/// <summary>
/// Manages the active color scheme by toggling the <c>dark</c> class on <c>&lt;html&gt;</c> and
/// persisting the user's choice. Components consume design tokens through CSS variables
/// (<c>--background</c>, <c>--primary</c>, ...) exactly as shadcn/ui does, so this service never
/// needs to know about individual components.
/// </summary>
/// <remarks>
/// Implementations use JS interop for DOM and <c>localStorage</c> access and must be safe to call
/// during prerendering (no-op until the circuit/runtime is interactive).
/// </remarks>
public interface IThemeService
{
    /// <summary>The user's stored preference (may be <see cref="ThemeMode.System"/>).</summary>
    ThemeMode Mode { get; }

    /// <summary>
    /// The scheme actually applied right now: <see cref="ThemeMode.Light"/> or <see cref="ThemeMode.Dark"/>.
    /// Never <see cref="ThemeMode.System"/>; that value is resolved against the OS preference.
    /// </summary>
    ThemeMode Resolved { get; }

    /// <summary>Raised after <see cref="Resolved"/> changes, including OS-level scheme changes while in System mode.</summary>
    event Action<ThemeMode>? ResolvedChanged;

    /// <summary>
    /// Reads the persisted preference and OS scheme, applies the matching class to the document root
    /// and starts listening for OS scheme changes. Idempotent.
    /// </summary>
    /// <param name="cancellationToken">Cancels the underlying JS interop call.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets and persists the preference, then re-applies the document class.</summary>
    /// <param name="mode">The new preference.</param>
    /// <param name="cancellationToken">Cancels the underlying JS interop call.</param>
    Task SetModeAsync(ThemeMode mode, CancellationToken cancellationToken = default);

    /// <summary>Convenience: switches between Light and Dark based on the current <see cref="Resolved"/> value.</summary>
    /// <param name="cancellationToken">Cancels the underlying JS interop call.</param>
    Task ToggleAsync(CancellationToken cancellationToken = default);
}
