using BlazorShadcn.Utils;

namespace BlazorShadcn.Tests.Utils;

public class ClassNamesCnTests
{
    [Fact]
    public void Cn_NoInputs_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ClassNames.Cn());
    }

    [Fact]
    public void Cn_NullAndFalse_AreDropped()
    {
        Assert.Equal("a", ClassNames.Cn(null, false, "a"));
    }

    [Fact]
    public void Cn_TrueIsIgnored()
    {
        Assert.Equal("a", ClassNames.Cn(true, "a"));
    }

    [Theory]
    [InlineData(true, "px-2 bg-primary")]
    [InlineData(false, "px-2")]
    public void Cn_ConditionalPattern_Works(bool active, string expected)
    {
        // C# has no `cond && "class"` that yields object, so the conditional is the clsx-style `false` branch.
        Assert.Equal(expected, ClassNames.Cn("px-2", active ? "bg-primary" : false));
    }

    [Theory]
    [InlineData("  ", "", "a  b", "a b")]
    [InlineData("", "a", "", "a")]
    [InlineData("\t", "\n", "x", "x")]
    public void Cn_WhitespaceAndEmptyStrings_AreDropped(string first, string second, string third, string expected)
    {
        Assert.Equal(expected, ClassNames.Cn(first, second, third));
    }

    [Fact]
    public void Cn_NestedEnumerables_AreFlattened()
    {
        object?[] inputs = { "a", new List<string?> { "b", null }, new[] { "c" } };

        Assert.Equal("a b c", ClassNames.Cn(inputs));
    }

    [Fact]
    public void Cn_ExactDuplicates_KeepLast()
    {
        Assert.Equal("b a", ClassNames.Cn("a b a"));
    }

    [Fact]
    public void Cn_TailwindConflict_LastWins()
    {
        Assert.Equal("py-1 px-4", ClassNames.Cn("px-2 py-1", "px-4"));
    }

    [Fact]
    public void Cn_NonStringObject_UsesToString()
    {
        Assert.Equal("5", ClassNames.Cn(5));
    }

    [Fact]
    public void Cn_PreservesOrder_OfNonConflictingClasses()
    {
        Assert.Equal("flex items-center gap-2 rounded-md", ClassNames.Cn("flex", "items-center", "gap-2", "rounded-md"));
    }
}
