using System.Collections;

namespace BlazorShadcn.Utils;

/// <summary>
/// Tailwind-aware class name composition, the Blazor equivalent of shadcn/ui's <c>cn()</c> helper
/// (<c>clsx</c> + <c>tailwind-merge</c>).
/// </summary>
/// <remarks>
/// Every generated component calls <see cref="Cn(object?[])"/> to combine its variant classes with
/// the consumer-supplied <c>class</c> attribute. Conflicting Tailwind utilities (e.g. <c>px-2</c> and
/// <c>px-4</c>) must be resolved so that the later class wins, exactly like <c>tailwind-merge</c>.
/// </remarks>
public static class ClassNames
{
    /// <summary>
    /// Joins the given inputs into a single space-separated class string, dropping
    /// <see langword="null"/>, <see langword="false"/>, empty strings and whitespace,
    /// then resolves conflicting Tailwind utilities so that the last one wins.
    /// </summary>
    /// <param name="inputs">
    /// Any mix of <see cref="string"/>, <see cref="bool"/> (ignored), <see cref="IEnumerable{T}"/> of strings,
    /// or <see langword="null"/>. Nested enumerables are flattened.
    /// </param>
    /// <returns>A normalized class string with duplicates and conflicts removed.</returns>
    /// <example>
    /// <code>
    /// ClassNames.Cn("px-2 py-1", isActive &amp;&amp; "bg-primary", "px-4")
    /// // => "py-1 bg-primary px-4"
    /// </code>
    /// </example>
    public static string Cn(params object?[] inputs)
    {
        var tokens = new List<string>();
        Collect(inputs, tokens);
        return tokens.Count == 0 ? string.Empty : TwMerge(string.Join(' ', tokens));
    }

    /// <summary>
    /// Recursively flattens one clsx-style input into whitespace-separated class tokens.
    /// </summary>
    private static void Collect(object? input, List<string> tokens)
    {
        switch (input)
        {
            case null or bool:
                return;
            case string text:
                tokens.AddRange(text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                return;
            case IEnumerable items:
                foreach (var item in items)
                {
                    Collect(item, tokens);
                }
                return;
            default:
                Collect(input.ToString(), tokens);
                return;
        }
    }

    /// <summary>
    /// Resolves Tailwind utility conflicts in an already-joined class string. The last occurrence
    /// of each conflicting group (spacing, colors, sizing, typography, etc.) is kept.
    /// </summary>
    /// <param name="classes">A space-separated class string.</param>
    /// <returns>The class string with earlier conflicting utilities removed.</returns>
    public static string TwMerge(string classes)
    {
        var tokens = classes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var keptGroups = new List<ParsedToken>(tokens.Length);
        var kept = new List<string>(tokens.Length);

        // Walk from the last token to the first so the later utility always wins.
        for (var i = tokens.Length - 1; i >= 0; i--)
        {
            var parsed = Parse(tokens[i]);
            if (seenKeys.Contains(parsed.Key) || IsOverridden(parsed, keptGroups))
            {
                continue;
            }

            seenKeys.Add(parsed.Key);
            keptGroups.Add(parsed);
            kept.Add(tokens[i]);
        }

        kept.Reverse();
        return string.Join(' ', kept);
    }

    /// <summary>
    /// A class token split into the parts that decide whether two tokens conflict.
    /// </summary>
    /// <param name="Scope">Sorted variant prefix (<c>hover:md:</c>) plus the important marker, so only tokens with the same scope can conflict.</param>
    /// <param name="Group">Tailwind conflict group, or <see langword="null"/> when the utility is not recognized.</param>
    /// <param name="Key">Identity of the utility within its scope; equal keys mean the later token replaces the earlier one.</param>
    private readonly record struct ParsedToken(string Scope, string? Group, string Key);

    /// <summary>
    /// Splits a token into its variant prefix, important flag and base utility, and computes its conflict key.
    /// </summary>
    private static ParsedToken Parse(string token)
    {
        var parts = SplitTopLevel(token, ':');
        var utility = parts[^1];
        var variants = parts.Take(parts.Count - 1).Order(StringComparer.Ordinal);
        var variantPrefix = string.Concat(variants.Select(variant => variant + ':'));

        var important = false;
        if (utility.StartsWith('!'))
        {
            important = true;
            utility = utility[1..];
        }
        else if (utility.EndsWith('!'))
        {
            important = true;
            utility = utility[..^1];
        }

        var scope = variantPrefix + (important ? "!" : string.Empty);
        var group = utility.Length == 0 ? null : TailwindConflictGroups.GetGroup(utility);
        var key = group is null ? "?" + scope + utility : scope + group;
        return new ParsedToken(scope, group, key);
    }

    /// <summary>
    /// Whether a later kept token with the same scope overrides this token through a shorthand
    /// (e.g. <c>p-4</c> overrides an earlier <c>px-2</c>).
    /// </summary>
    private static bool IsOverridden(ParsedToken token, List<ParsedToken> keptTokens)
    {
        if (token.Group is null)
        {
            return false;
        }

        foreach (var kept in keptTokens)
        {
            if (kept.Group is not null
                && kept.Scope == token.Scope
                && TailwindConflictGroups.AreShorthandConflicts(kept.Group, token.Group))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Splits on <paramref name="separator"/> except inside <c>[...]</c> or <c>(...)</c>, so arbitrary
    /// variants such as <c>[&amp;:hover]:p-2</c> stay intact.
    /// </summary>
    private static List<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
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
            else if (c == separator && depth == 0)
            {
                parts.Add(text[start..i]);
                start = i + 1;
            }
        }

        parts.Add(text[start..]);
        return parts;
    }
}
