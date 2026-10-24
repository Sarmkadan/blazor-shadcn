using BlazorShadcn.Utils;

namespace BlazorShadcn.Tests;

public class ClassNamesTests
{
    [Theory]
    [InlineData("px-2 py-1 px-4", "py-1 px-4")]
    [InlineData("p-2 px-4", "p-2 px-4")]
    [InlineData("px-4 p-2", "p-2")]
    [InlineData("text-sm text-red-500 text-lg", "text-red-500 text-lg")]
    [InlineData("hover:bg-primary hover:bg-secondary bg-primary", "hover:bg-secondary bg-primary")]
    [InlineData("bg-primary/90 bg-destructive", "bg-destructive")]
    [InlineData("w-4 w-[32px]", "w-[32px]")]
    [InlineData("flex inline-flex", "inline-flex")]
    [InlineData("rounded-md rounded-lg", "rounded-lg")]
    [InlineData("h-9 px-4 py-2 h-8 px-3", "py-2 h-8 px-3")]
    [InlineData("  ", "")]
    public void TwMerge_Conflicts_KeepsLastUtility(string input, string expected)
    {
        Assert.Equal(expected, ClassNames.TwMerge(input));
    }

    [Fact]
    public void TwMerge_VariantOrderDiffers_TreatedAsSameVariant()
    {
        Assert.Equal("md:hover:bg-secondary", ClassNames.TwMerge("hover:md:bg-primary md:hover:bg-secondary"));
    }

    [Fact]
    public void TwMerge_DifferentVariants_DoNotConflict()
    {
        Assert.Equal("hover:p-2 px-4", ClassNames.TwMerge("hover:p-2 px-4"));
    }

    [Fact]
    public void TwMerge_ImportantFlag_DoesNotConflictWithPlainUtility()
    {
        Assert.Equal("p-2 !px-4", ClassNames.TwMerge("p-2 !px-4"));
    }

    [Fact]
    public void TwMerge_UnknownUtilities_DeduplicateExactDuplicatesOnly()
    {
        Assert.Equal("other-thing custom-thing", ClassNames.TwMerge("custom-thing other-thing custom-thing"));
    }

    [Fact]
    public void TwMerge_ArbitraryVariant_IsNotSplitAtInnerColon()
    {
        Assert.Equal("[&:hover]:p-2 p-4", ClassNames.TwMerge("[&:hover]:p-2 [&:hover]:p-2 p-4"));
    }
}
