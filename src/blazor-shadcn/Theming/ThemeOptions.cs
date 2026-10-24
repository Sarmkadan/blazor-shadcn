namespace BlazorShadcn.Theming;

/// <summary>
/// Configuration for <see cref="ThemeService"/>, bound through <c>IOptions&lt;ThemeOptions&gt;</c>.
/// Set it with <c>services.AddBlazorShadcn(o =&gt; ...)</c>, or leave the defaults in place.
/// </summary>
public sealed class ThemeOptions
{
    /// <summary>
    /// The <c>localStorage</c> key used to persist the preference. Defaults to <see cref="ThemeService.DefaultStorageKey"/>.
    /// Must not be empty or whitespace.
    /// </summary>
    public string StorageKey { get; set; } = ThemeService.DefaultStorageKey;

    /// <summary>
    /// The preference used when nothing has been stored yet. Defaults to <see cref="ThemeMode.System"/>.
    /// An explicit choice of <see cref="ThemeMode.System"/> is stored as well, so it still wins over a non-System default.
    /// </summary>
    public ThemeMode DefaultMode { get; set; } = ThemeMode.System;
}
