using Microsoft.JSInterop;

namespace BlazorShadcn.Theming;

/// <summary>
/// JS-interop implementation of <see cref="IThemeService"/>. Persists the preference in
/// <c>localStorage</c> and toggles the <c>dark</c> class on <c>&lt;html&gt;</c> through
/// <c>wwwroot/theme.js</c>.
/// </summary>
/// <remarks>
/// Register as scoped (per circuit) or singleton (per WebAssembly app). Call
/// <see cref="InitializeAsync"/> once the component tree is interactive, e.g. from
/// <c>OnAfterRenderAsync(firstRender: true)</c>; during prerendering it is a no-op.
/// <para>
/// <see cref="SetModeAsync"/> and <see cref="ToggleAsync"/> auto-initialize: if
/// <see cref="InitializeAsync"/> has not completed yet they call it first, so the module is imported
/// exactly once and consumers do not need to order the calls themselves.
/// </para>
/// <para>
/// Untrusted JS results are never trusted blindly: an unknown or missing mode falls back to
/// <see cref="ThemeMode.System"/> and an unknown or missing resolved scheme falls back to
/// <see cref="ThemeMode.Light"/>.
/// </para>
/// </remarks>
public sealed class ThemeService : IThemeService, IAsyncDisposable
{
    /// <summary>The <c>localStorage</c> key used to persist the theme preference.</summary>
    public const string DefaultStorageKey = "blazor-shadcn-theme";

    private const string _modulePath = "./_content/BlazorShadcn/theme.js";

    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IJSObjectReference? _module;
    private DotNetObjectReference<ThemeService>? _dotNetRef;
    private bool _initialized;
    private bool _disposed;

    /// <summary>Creates a theme service that talks to the browser through <paramref name="js"/>.</summary>
    /// <param name="js">The JS runtime used to import <c>theme.js</c> and call its functions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="js"/> is <see langword="null"/>.</exception>
    public ThemeService(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);

        _js = js;
    }

    /// <inheritdoc />
    public ThemeMode Mode { get; private set; } = ThemeMode.System;

    /// <inheritdoc />
    public ThemeMode Resolved { get; private set; } = ThemeMode.Light;

    /// <inheritdoc />
    public event Action<ThemeMode>? ResolvedChanged;

    /// <inheritdoc />
    /// <remarks>
    /// Safe to call repeatedly; only the first successful call talks to JS. Prerendering and a disconnected
    /// circuit are swallowed and leave the defaults in place, so a later call can still initialize.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_initialized)
            {
                return;
            }

            try
            {
                var module = await GetModuleAsync(cancellationToken);
                _dotNetRef ??= DotNetObjectReference.Create(this);

                var state = await module.InvokeAsync<ThemeState?>("init", cancellationToken, DefaultStorageKey, _dotNetRef);
                ApplyState(ParseMode(state?.Mode), ParseResolved(state?.Resolved));
                _initialized = true;
            }
            catch (InvalidOperationException)
            {
                // Prerendering: JS interop is not available yet. Keep the defaults and wait for the interactive render.
            }
            catch (JSDisconnectedException)
            {
                // The circuit went away before the call completed; nothing to apply to.
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Auto-initializes: if <see cref="InitializeAsync"/> has not completed, it is awaited first.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a defined <see cref="ThemeMode"/> value.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public async Task SetModeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown ThemeMode.");
        }

        await EnsureInitializedAsync(cancellationToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var module = await GetModuleAsync(cancellationToken);
            var resolved = await module.InvokeAsync<string?>("setMode", cancellationToken, DefaultStorageKey, ToJsValue(mode));
            ApplyState(mode, ParseResolved(resolved));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Auto-initializes, so the toggle is based on the stored scheme rather than the pre-init default.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public async Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(cancellationToken);

        await SetModeAsync(Resolved == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark, cancellationToken);
    }

    /// <summary>
    /// Called by <c>theme.js</c> through a <see cref="DotNetObjectReference{T}"/> when the OS color scheme changes.
    /// </summary>
    /// <remarks>
    /// For JS use only. Do not call this from C# code; use <see cref="SetModeAsync"/> to change the preference.
    /// It is public only because the JS interop requires it.
    /// </remarks>
    /// <param name="isDark">Whether the OS now prefers the dark scheme.</param>
    [JSInvokable]
    public void OnSystemSchemeChanged(bool isDark)
    {
        if (Mode != ThemeMode.System)
        {
            return;
        }

        ApplyState(ThemeMode.System, isDark ? ThemeMode.Dark : ThemeMode.Light);
    }

    /// <summary>Releases the JS module, unsubscribes the OS listener and disposes the .NET callback reference.</summary>
    /// <remarks>
    /// Idempotent. After disposal, <see cref="InitializeAsync"/>, <see cref="SetModeAsync"/> and
    /// <see cref="ToggleAsync"/> throw <see cref="ObjectDisposedException"/>.
    /// </remarks>
    /// <returns>A task that completes when cleanup has finished.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_module is not null)
            {
                try
                {
                    await _module.InvokeVoidAsync("dispose");
                    await _module.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // The circuit is already gone, so the module is already unreachable.
                }
            }

            _dotNetRef?.Dispose();
            _dotNetRef = null;
            _module = null;
        }
        finally
        {
            _gate.Release();
        }

        _gate.Dispose();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, _modulePath);
        return _module;
    }

    private void ApplyState(ThemeMode mode, ThemeMode resolved)
    {
        Mode = mode;

        if (Resolved == resolved)
        {
            return;
        }

        Resolved = resolved;
        ResolvedChanged?.Invoke(resolved);
    }

    // JS data is untrusted: anything other than a known value falls back to the defaults instead of throwing.
    private static ThemeMode ParseMode(string? value) => value switch
    {
        "light" => ThemeMode.Light,
        "dark" => ThemeMode.Dark,
        _ => ThemeMode.System,
    };

    private static ThemeMode ParseResolved(string? value) =>
        value == "dark" ? ThemeMode.Dark : ThemeMode.Light;

    private static string ToJsValue(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => "light",
        ThemeMode.Dark => "dark",
        _ => "system",
    };

    /// <summary>Shape of the object returned by <c>theme.js</c> <c>init</c>.</summary>
    internal sealed record ThemeState(string? Mode, string? Resolved);
}
