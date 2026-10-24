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
    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    /// <inheritdoc />
    public ThemeMode Mode { get; private set; } = ThemeMode.System;

    /// <inheritdoc />
    public ThemeMode Resolved { get; private set; } = ThemeMode.Light;

    /// <inheritdoc />
    public event Action<ThemeMode>? ResolvedChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                var module = await GetModuleAsync(cancellationToken);
                _dotNetRef ??= DotNetObjectReference.Create(this);

                var state = await module.InvokeAsync<ThemeState>("init", cancellationToken, DefaultStorageKey, _dotNetRef);
                ApplyState(ParseMode(state.Mode), ParseResolved(state.Resolved));
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
    public async Task SetModeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var module = await GetModuleAsync(cancellationToken);
            var resolved = await module.InvokeAsync<string>("setMode", cancellationToken, DefaultStorageKey, ToJsValue(mode));
            ApplyState(mode, ParseResolved(resolved));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task ToggleAsync(CancellationToken cancellationToken = default) =>
        SetModeAsync(Resolved == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark, cancellationToken);

    /// <summary>Called from <c>theme.js</c> when the OS color scheme changes.</summary>
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
