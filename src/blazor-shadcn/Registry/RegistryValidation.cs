using System.Text.RegularExpressions;

namespace BlazorShadcn.Registry;

/// <summary>Argument checks shared by <see cref="RegistryEntry"/> and <see cref="ComponentRegistry"/>.</summary>
internal static partial class RegistryValidation
{
    [GeneratedRegex(@"^[a-z][a-z0-9]*(-[a-z0-9]+)*\z", RegexOptions.CultureInvariant)]
    private static partial Regex KebabCaseRegex();

    /// <summary>Returns <see langword="true"/> when <paramref name="name"/> is lowercase kebab-case, e.g. <c>"dropdown-menu"</c>.</summary>
    public static bool IsKebabCase(string name) => KebabCaseRegex().IsMatch(name);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="path"/> is a non-blank relative path that uses
    /// forward slashes and has no <c>..</c> sequence, so it cannot escape the registry root.
    /// </summary>
    public static bool IsSafeRelativePath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && !Path.IsPathRooted(path)
        && path.IndexOf('\\') < 0
        && !path.Contains("..", StringComparison.Ordinal);
}
