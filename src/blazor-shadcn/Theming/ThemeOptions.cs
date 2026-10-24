namespace BlazorShadcn.Theming;

/// <summary>
/// Configuration for <see cref="ThemeService"/>, bound through <c>IOptions&lt;ThemeOptions&gt;</c>.
/// Set it with <c>services.AddBlazorShadcn(o =&gt; ...)</c>, bind it from configuration under
/// <see cref="SectionName"/> with <c>services.AddBlazorShadcn(configuration)</c>, or leave the defaults in place.
/// </summary>
public sealed class ThemeOptions
{
    /// <summary>The configuration section that <c>AddBlazorShadcn(IConfiguration)</c> binds to.</summary>
    public const string SectionName = "BlazorShadcn:Theme";

    /// <summary>
    /// The <c>localStorage</c> key used to persist the preference. Defaults to <c>blazor-shadcn-theme</c>.
    /// Must not be empty and must not contain whitespace.
    /// </summary>
    public string StorageKey { get; set; } = ThemeService.DefaultStorageKey;

    /// <summary>
    /// The preference used when nothing has been stored yet. Defaults to <see cref="ThemeMode.System"/>.
    /// An explicit choice of <see cref="ThemeMode.System"/> is stored as well, so it still wins over a non-System default.
    /// </summary>
    public ThemeMode DefaultMode { get; set; } = ThemeMode.System;

    /// <summary>
    /// The CSS class toggled on <c>&lt;html&gt;</c> when the resolved scheme is dark. Defaults to <c>dark</c>.
    /// Must be a valid CSS class name without whitespace.
    /// </summary>
    public string DarkClass { get; set; } = "dark";

    /// <summary>
    /// When <see langword="true"/>, also sets <c>&lt;html data-theme="dark|light"&gt;</c> on every change, for
    /// <c>[data-theme]</c> CSS selectors. Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseColorSchemeAttribute { get; set; }

    /// <summary>
    /// When <see langword="true"/>, a temporary <c>* { transition: none !important }</c> style is injected while the
    /// scheme switches, so colors do not animate between palettes (mirrors <c>next-themes</c>). Defaults to <see langword="true"/>.
    /// </summary>
    public bool DisableTransitionOnChange { get; set; } = true;

    /// <summary>
    /// Path of the JS module imported by <see cref="ThemeService"/>, relative to the importing page.
    /// Defaults to <c>./_content/BlazorShadcn/theme.js</c>. Must end with <c>.js</c>.
    /// </summary>
    public string ModulePath { get; set; } = "./_content/BlazorShadcn/theme.js";
}
