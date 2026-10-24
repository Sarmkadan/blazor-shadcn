using System.Text.RegularExpressions;

namespace BlazorShadcn.Utils;

/// <summary>
/// Maps Tailwind utilities to the conflict groups that <see cref="ClassNames.TwMerge"/> uses to decide
/// which of two utilities wins, the same way <c>tailwind-merge</c> groups them.
/// </summary>
internal static class TailwindConflictGroups
{
    // Utilities whose group is simply their own key: "p-4" -> "p", "min-w-0" -> "min-w".
    private static readonly string[] PassThroughKeys =
    [
        "p", "px", "py", "pt", "pr", "pb", "pl",
        "m", "mx", "my", "mt", "mr", "mb", "ml",
        "gap", "gap-x", "gap-y",
        "w", "h", "min-w", "max-w", "min-h", "max-h", "size",
        "z", "opacity", "leading", "tracking", "bg", "flex",
        "inset", "inset-x", "inset-y", "top", "right", "bottom", "left",
        "overflow", "overflow-x", "overflow-y",
        "items", "justify", "justify-items", "self", "justify-self",
        "rounded", "rounded-t", "rounded-r", "rounded-b", "rounded-l",
        "rounded-tl", "rounded-tr", "rounded-br", "rounded-bl",
    ];

    // Utilities whose group depends on the value: "text-lg" is a size, "text-red-500" is a color.
    private static readonly string[] ClassifiedKeys =
    [
        "text", "font", "border", "border-x", "border-y", "border-t", "border-r", "border-b", "border-l",
        "ring", "ring-offset", "shadow",
    ];

    // Keys that are valid without a value: "border", "rounded", "ring", "shadow".
    private static readonly HashSet<string> BareKeys = new(StringComparer.Ordinal)
    {
        "border", "border-x", "border-y", "border-t", "border-r", "border-b", "border-l",
        "rounded", "ring", "shadow",
    };

    // Longest first so that "border-x-red-500" matches "border-x" before "border".
    private static readonly string[] KeysLongestFirst = PassThroughKeys
        .Concat(ClassifiedKeys)
        .OrderByDescending(key => key.Length)
        .ThenBy(key => key, StringComparer.Ordinal)
        .ToArray();

    private static readonly HashSet<string> KnownKeys = new(KeysLongestFirst, StringComparer.Ordinal);

    // Whole utilities without a value (display, position, flex direction and wrap).
    private static readonly Dictionary<string, string> ExactGroups = BuildExactGroups();

    private static readonly HashSet<string> TextAlignKeywords = new(StringComparer.Ordinal)
    {
        "left", "center", "right", "justify", "start", "end",
    };

    // "text-*" utilities that are neither a size nor a color.
    private static readonly HashSet<string> NonColorTextKeywords = new(StringComparer.Ordinal)
    {
        "ellipsis", "clip", "wrap", "nowrap", "balance", "pretty",
    };

    private static readonly HashSet<string> BorderStyleKeywords = new(StringComparer.Ordinal)
    {
        "solid", "dashed", "dotted", "double", "hidden", "none",
    };

    private static readonly HashSet<string> ShadowKeywords = new(StringComparer.Ordinal)
    {
        "2xs", "xs", "sm", "md", "lg", "xl", "2xl", "none", "inner",
    };

    private static readonly HashSet<string> FontWeightKeywords = new(StringComparer.Ordinal)
    {
        "thin", "extralight", "light", "normal", "medium", "semibold", "bold", "extrabold", "black",
    };

    private static readonly HashSet<string> FontFamilyKeywords = new(StringComparer.Ordinal)
    {
        "sans", "serif", "mono",
    };

    private static readonly string[] ColorValuePrefixes = ["#", "rgb", "hsl", "oklch", "oklab", "var(", "color:"];

    private static readonly Regex NumericPattern = new(@"^\d+(?:\.\d+)?$", RegexOptions.CultureInvariant);

    private static readonly Regex LengthPattern = new(
        @"^(?:length:)?(?:-?(?:\d+(?:\.\d+)?|\.\d+)(?:px|rem|em|%|vh|vw|vmin|vmax|pt|pc|ch|ex|cm|mm|in|q)?|calc\(.+\))$",
        RegexOptions.CultureInvariant);

    private static readonly Regex TextSizePattern = new(@"^(?:xs|sm|base|lg|xl|[2-9]xl)$", RegexOptions.CultureInvariant);

    // Shorthand -> the more specific groups it overrides when it comes later in the class list.
    private static readonly Dictionary<string, string[]> ShorthandOverrides = new(StringComparer.Ordinal)
    {
        ["p"] = ["px", "py", "pt", "pr", "pb", "pl"],
        ["px"] = ["pl", "pr"],
        ["py"] = ["pt", "pb"],
        ["m"] = ["mx", "my", "mt", "mr", "mb", "ml"],
        ["mx"] = ["mr", "ml"],
        ["my"] = ["mt", "mb"],
        ["gap"] = ["gap-x", "gap-y"],
        ["size"] = ["w", "h"],
        ["inset"] = ["inset-x", "inset-y", "top", "right", "bottom", "left"],
        ["inset-x"] = ["left", "right"],
        ["inset-y"] = ["top", "bottom"],
        ["rounded"] = ["rounded-t", "rounded-r", "rounded-b", "rounded-l", "rounded-tl", "rounded-tr", "rounded-br", "rounded-bl"],
        ["rounded-t"] = ["rounded-tl", "rounded-tr"],
        ["rounded-r"] = ["rounded-tr", "rounded-br"],
        ["rounded-b"] = ["rounded-br", "rounded-bl"],
        ["rounded-l"] = ["rounded-tl", "rounded-bl"],
        ["border-w"] = ["border-x", "border-y", "border-t", "border-r", "border-b", "border-l"],
        ["border-x"] = ["border-l", "border-r"],
        ["border-y"] = ["border-t", "border-b"],
        ["border-color"] = ["border-color-x", "border-color-y", "border-color-t", "border-color-r", "border-color-b", "border-color-l"],
        ["border-color-x"] = ["border-color-l", "border-color-r"],
        ["border-color-y"] = ["border-color-t", "border-color-b"],
        ["overflow"] = ["overflow-x", "overflow-y"],
    };

    /// <summary>
    /// Returns the conflict group of a single Tailwind utility, or <see langword="null"/> when the utility
    /// is not recognized. Variant prefixes (<c>hover:</c>), the important marker (<c>!</c>), negative
    /// values (<c>-mt-2</c>) and opacity modifiers (<c>bg-primary/90</c>) do not change the group.
    /// </summary>
    /// <param name="utility">A single class token, e.g. <c>px-2</c>, <c>w-[32px]</c> or <c>bg-[#fff]</c>.</param>
    /// <returns>A group key such as <c>"px"</c>, <c>"text-size"</c> or <c>"bg"</c>, or <see langword="null"/>.</returns>
    internal static string? GetGroup(string utility)
    {
        var name = Normalize(utility);
        if (name.Length == 0)
        {
            return null;
        }

        if (ExactGroups.TryGetValue(name, out var exact))
        {
            return exact;
        }

        string key;
        string? value;
        bool arbitrary;
        var bracket = name.IndexOf("-[", StringComparison.Ordinal);
        if (bracket > 0 && name.EndsWith(']'))
        {
            key = name[..bracket];
            value = name[(bracket + 2)..^1];
            arbitrary = true;
            if (value.Length == 0 || !KnownKeys.Contains(key))
            {
                return null;
            }
        }
        else
        {
            if (!TryMatchKey(name, out key, out value))
            {
                return null;
            }

            arbitrary = false;
        }

        if (value is null && !BareKeys.Contains(key))
        {
            return null;
        }

        return Classify(key, value, arbitrary);
    }

    /// <summary>
    /// Returns <see langword="true"/> when a utility in <paramref name="groupA"/> placed later in the class
    /// list overrides an earlier utility in <paramref name="groupB"/> (e.g. <c>p</c> overrides <c>px</c>,
    /// <c>inset</c> overrides <c>top</c>, <c>size</c> overrides <c>w</c>). The reverse is not a conflict,
    /// and identical groups are not shorthand conflicts (the caller resolves those directly).
    /// </summary>
    /// <param name="groupA">The group of the later utility.</param>
    /// <param name="groupB">The group of the earlier utility.</param>
    /// <returns><see langword="true"/> if <paramref name="groupA"/> overrides <paramref name="groupB"/>.</returns>
    internal static bool AreShorthandConflicts(string groupA, string groupB)
    {
        return ShorthandOverrides.TryGetValue(groupA, out var overridden) && Array.IndexOf(overridden, groupB) >= 0;
    }

    private static Dictionary<string, string> BuildExactGroups()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        AddAll(map, "display", "block", "inline-block", "inline", "flex", "inline-flex", "grid", "inline-grid",
            "hidden", "contents", "flow-root", "list-item", "table", "inline-table", "table-caption", "table-cell",
            "table-column", "table-column-group", "table-footer-group", "table-header-group", "table-row-group",
            "table-row");
        AddAll(map, "position", "static", "fixed", "absolute", "relative", "sticky");
        AddAll(map, "flex-direction", "flex-row", "flex-row-reverse", "flex-col", "flex-col-reverse");
        AddAll(map, "flex-wrap", "flex-wrap", "flex-wrap-reverse", "flex-nowrap");
        return map;
    }

    private static void AddAll(Dictionary<string, string> map, string group, params string[] names)
    {
        foreach (var name in names)
        {
            map[name] = group;
        }
    }

    /// <summary>
    /// Strips the variant prefix, important marker, negative sign and opacity modifier from a utility.
    /// </summary>
    private static string Normalize(string utility)
    {
        var name = utility.Trim();
        var colon = IndexOfTopLevel(name, ':', last: true);
        if (colon >= 0)
        {
            name = name[(colon + 1)..];
        }

        if (name.StartsWith('!'))
        {
            name = name[1..];
        }

        if (name.EndsWith('!'))
        {
            name = name[..^1];
        }

        if (name.StartsWith('-'))
        {
            name = name[1..];
        }

        var slash = IndexOfTopLevel(name, '/', last: false);
        if (slash >= 0)
        {
            name = name[..slash];
        }

        return name;
    }

    /// <summary>
    /// Finds <paramref name="target"/> outside of <c>[...]</c> and <c>(...)</c>, so arbitrary values such as
    /// <c>[&amp;:hover]</c> or <c>w-[calc(100%/2)]</c> are not split.
    /// </summary>
    private static int IndexOfTopLevel(string text, char target, bool last)
    {
        var depth = 0;
        var found = -1;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '[' or '(')
            {
                depth++;
            }
            else if (c is ']' or ')')
            {
                depth = Math.Max(0, depth - 1);
            }
            else if (c == target && depth == 0)
            {
                found = i;
                if (!last)
                {
                    return i;
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Finds the longest known key that the utility starts with, followed by either the end or a <c>-</c>.
    /// </summary>
    private static bool TryMatchKey(string name, out string key, out string? value)
    {
        foreach (var candidate in KeysLongestFirst)
        {
            if (!name.StartsWith(candidate, StringComparison.Ordinal))
            {
                continue;
            }

            if (name.Length == candidate.Length)
            {
                key = candidate;
                value = null;
                return true;
            }

            if (name[candidate.Length] == '-' && name.Length > candidate.Length + 1)
            {
                key = candidate;
                value = name[(candidate.Length + 1)..];
                return true;
            }
        }

        key = string.Empty;
        value = null;
        return false;
    }

    private static string? Classify(string key, string? value, bool arbitrary)
    {
        return key switch
        {
            "text" => ClassifyText(value!, arbitrary),
            "font" => ClassifyFont(value!, arbitrary),
            "border" => ClassifyBorder(value, arbitrary),
            "border-x" or "border-y" or "border-t" or "border-r" or "border-b" or "border-l" =>
                IsWidth(value, arbitrary) ? key : "border-color-" + key["border-".Length..],
            "ring" => ClassifyRing(value, arbitrary),
            "ring-offset" => IsWidth(value, arbitrary) ? "ring-offset-w" : "ring-offset-color",
            "shadow" => ClassifyShadow(value, arbitrary),
            _ => key,
        };
    }

    private static string? ClassifyText(string value, bool arbitrary)
    {
        if (!arbitrary && TextAlignKeywords.Contains(value))
        {
            return "text-align";
        }

        if (!arbitrary && NonColorTextKeywords.Contains(value))
        {
            return null;
        }

        var isSize = arbitrary ? LengthPattern.IsMatch(value) : TextSizePattern.IsMatch(value);
        return isSize ? "text-size" : "text-color";
    }

    private static string? ClassifyFont(string value, bool arbitrary)
    {
        if (arbitrary)
        {
            return NumericPattern.IsMatch(value) ? "font-weight" : "font-family";
        }

        if (FontWeightKeywords.Contains(value))
        {
            return "font-weight";
        }

        return FontFamilyKeywords.Contains(value) ? "font-family" : null;
    }

    private static string? ClassifyBorder(string? value, bool arbitrary)
    {
        if (value is null)
        {
            return "border-w";
        }

        if (!arbitrary && BorderStyleKeywords.Contains(value))
        {
            return "border-style";
        }

        if (!arbitrary && (value is "collapse" or "separate" || value.StartsWith("spacing", StringComparison.Ordinal)))
        {
            return null;
        }

        return IsWidth(value, arbitrary) ? "border-w" : "border-color";
    }

    private static string ClassifyRing(string? value, bool arbitrary)
    {
        if (value is null)
        {
            return "ring-w";
        }

        if (!arbitrary && value == "inset")
        {
            return "ring-inset";
        }

        return IsWidth(value, arbitrary) ? "ring-w" : "ring-color";
    }

    private static string ClassifyShadow(string? value, bool arbitrary)
    {
        if (value is null)
        {
            return "shadow";
        }

        if (arbitrary)
        {
            return ColorValuePrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal))
                ? "shadow-color"
                : "shadow";
        }

        return ShadowKeywords.Contains(value) ? "shadow" : "shadow-color";
    }

    /// <summary>
    /// Whether a width-like value (<c>0</c>, <c>2</c>, <c>[3px]</c>) rather than a color. A missing value is a
    /// bare utility such as <c>border</c>, which is a width.
    /// </summary>
    private static bool IsWidth(string? value, bool arbitrary)
    {
        if (value is null)
        {
            return true;
        }

        return (arbitrary ? LengthPattern : NumericPattern).IsMatch(value);
    }
}
