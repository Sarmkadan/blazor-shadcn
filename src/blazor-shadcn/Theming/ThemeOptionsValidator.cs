using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace BlazorShadcn.Theming;

/// <summary>
/// Validates <see cref="ThemeOptions"/> so that a misconfigured theme fails at startup instead of
/// in the browser. Registered by <c>AddBlazorShadcn</c> and run through <c>ValidateOnStart</c>.
/// </summary>
/// <remarks>
/// Checks: <see cref="ThemeOptions.StorageKey"/> is non-empty without whitespace;
/// <see cref="ThemeOptions.DarkClass"/> is a valid CSS class name; <see cref="ThemeOptions.DefaultMode"/> is a defined
/// <see cref="ThemeMode"/>; <see cref="ThemeOptions.ModulePath"/> is non-empty and ends with <c>.js</c>.
/// </remarks>
public sealed class ThemeOptionsValidator : IValidateOptions<ThemeOptions>
{
    // A CSS identifier used as a class name: optional leading "-" or "--", then a letter or underscore, then
    // letters, digits, underscores or hyphens. Anything else (digits first, spaces, dots, quotes) is rejected.
    private static readonly Regex _cssClassName = new(@"^(--|-)?[A-Za-z_][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant);

    /// <summary>Validates <paramref name="options"/> and collects every problem, not just the first.</summary>
    /// <param name="name">The options instance name. Ignored: <see cref="ThemeOptions"/> is only registered under the default name.</param>
    /// <param name="options">The options to check.</param>
    /// <returns><see cref="ValidateOptionsResult.Success"/> when valid; otherwise a failure listing each violation.</returns>
    public ValidateOptionsResult Validate(string? name, ThemeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrEmpty(options.StorageKey))
        {
            failures.Add($"{nameof(ThemeOptions.StorageKey)} must not be empty.");
        }
        else if (options.StorageKey.Any(char.IsWhiteSpace))
        {
            failures.Add($"{nameof(ThemeOptions.StorageKey)} must not contain whitespace: '{options.StorageKey}'.");
        }

        if (string.IsNullOrEmpty(options.DarkClass))
        {
            failures.Add($"{nameof(ThemeOptions.DarkClass)} must not be empty.");
        }
        else if (!_cssClassName.IsMatch(options.DarkClass))
        {
            failures.Add($"{nameof(ThemeOptions.DarkClass)} must be a valid CSS class name: '{options.DarkClass}'.");
        }

        if (!Enum.IsDefined(options.DefaultMode))
        {
            failures.Add($"{nameof(ThemeOptions.DefaultMode)} must be a defined {nameof(ThemeMode)} value: '{(int)options.DefaultMode}'.");
        }

        if (string.IsNullOrEmpty(options.ModulePath))
        {
            failures.Add($"{nameof(ThemeOptions.ModulePath)} must not be empty.");
        }
        else if (!options.ModulePath.EndsWith(".js", StringComparison.Ordinal))
        {
            failures.Add($"{nameof(ThemeOptions.ModulePath)} must end with '.js': '{options.ModulePath}'.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
