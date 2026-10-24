using BlazorShadcn.Utils;

namespace BlazorShadcn.Tests.Utils;

public class TwMergeTests
{
    [Theory]
    [InlineData("px-2 py-1 px-4", "py-1 px-4")]
    [InlineData("p-2 px-4", "p-2 px-4")]
    [InlineData("px-4 p-2", "p-2")]
    [InlineData("mt-2 m-0", "m-0")]
    [InlineData("m-0 mt-2", "m-0 mt-2")]
    [InlineData("text-sm text-lg", "text-lg")]
    [InlineData("text-sm text-red-500", "text-sm text-red-500")]
    [InlineData("text-red-500 text-primary", "text-primary")]
    [InlineData("font-medium font-bold", "font-bold")]
    [InlineData("bg-primary/90 bg-destructive", "bg-destructive")]
    [InlineData("hover:bg-primary hover:bg-secondary", "hover:bg-secondary")]
    [InlineData("hover:bg-primary bg-secondary", "hover:bg-primary bg-secondary")]
    [InlineData("md:hover:px-2 hover:md:px-4", "hover:md:px-4")]
    [InlineData("w-4 w-[32px]", "w-[32px]")]
    [InlineData("h-9 w-9 size-4", "size-4")]
    [InlineData("flex inline-flex", "inline-flex")]
    [InlineData("block hidden", "hidden")]
    [InlineData("absolute relative", "relative")]
    [InlineData("rounded-md rounded-lg", "rounded-lg")]
    [InlineData("rounded-t-md rounded-lg", "rounded-lg")]
    [InlineData("rounded-lg rounded-t-md", "rounded-lg rounded-t-md")]
    [InlineData("border border-2", "border-2")]
    [InlineData("border border-input", "border border-input")]
    [InlineData("shadow shadow-sm", "shadow-sm")]
    [InlineData("ring-1 ring-2", "ring-2")]
    [InlineData("!px-2 px-4", "!px-2 px-4")]
    [InlineData("-mt-2 mt-4", "mt-4")]
    [InlineData("h-9 px-4 py-2 h-8 px-3", "py-2 h-8 px-3")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("foo bar foo", "bar foo")]
    [InlineData("[&_svg]:size-4 [&_svg]:size-5", "[&_svg]:size-5")]
    public void TwMerge_ParityCases_MatchTailwindMerge(string input, string expected)
    {
        Assert.Equal(expected, ClassNames.TwMerge(input));
    }

    [Fact]
    public void TwMerge_UnknownUtilities_ArePreservedInOrder()
    {
        // Unrecognized utilities have no conflict group, so they are never dropped for conflicts and keep their position.
        Assert.Equal("foo bar px-4 baz", ClassNames.TwMerge("foo px-2 bar px-4 baz"));
    }

    [Fact]
    public void TwMerge_IsIdempotent()
    {
        const string button =
            "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium " +
            "transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring " +
            "disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 " +
            "[&_svg]:shrink-0 bg-primary text-primary-foreground hover:bg-primary/90 h-9 px-4 py-2 " +
            "has-[>svg]:px-3 px-2 bg-destructive text-sm text-lg";

        var once = ClassNames.TwMerge(button);
        var twice = ClassNames.TwMerge(once);

        Assert.Equal(once, twice);
    }
}
