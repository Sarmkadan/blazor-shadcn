using BlazorShadcn.Variants;

namespace BlazorShadcn.Tests.Variants;

public class VariantConfigTests
{
    private const string _baseClasses = "inline-flex rounded-md";

    private static VariantConfig ButtonLike(
        IReadOnlyDictionary<string, string>? defaults = null,
        IReadOnlyList<CompoundVariant>? compounds = null) => new()
        {
            Base = _baseClasses,
            Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["variant"] = new Dictionary<string, string>
                {
                    ["default"] = "bg-primary",
                    ["outline"] = "border bg-background",
                },
                ["size"] = new Dictionary<string, string>
                {
                    ["default"] = "h-9 px-4",
                    ["sm"] = "h-8 px-3",
                    ["lg"] = "h-10 px-8",
                },
            },
            DefaultVariants = defaults ?? new Dictionary<string, string>
            {
                ["variant"] = "default",
                ["size"] = "default",
            },
            CompoundVariants = compounds ?? [
            new CompoundVariant(
                new Dictionary<string, string> { ["variant"] = "outline", ["size"] = "sm" },
                "border-dashed"),
        ],
        };

    [Fact]
    public void Resolve_NoSelection_UsesDefaults()
    {
        var classes = ButtonLike().Resolve();

        Assert.Equal("inline-flex rounded-md bg-primary h-9 px-4", classes);
    }

    [Fact]
    public void Resolve_PartialSelection_FillsMissingAxesFromDefaults()
    {
        var classes = ButtonLike().Resolve(new Dictionary<string, string?> { ["variant"] = "outline" });

        Assert.Equal("inline-flex rounded-md border bg-background h-9 px-4", classes);
    }

    [Fact]
    public void Resolve_NullValueInSelection_FallsBackToDefault()
    {
        var classes = ButtonLike().Resolve(new Dictionary<string, string?>
        {
            ["variant"] = null,
            ["size"] = "lg",
        });

        Assert.Equal("inline-flex rounded-md bg-primary h-10 px-8", classes);
    }

    [Fact]
    public void Resolve_CompoundVariant_AppliedOnlyWhenAllConditionsMatch()
    {
        var config = ButtonLike();

        var matching = config.Resolve(new Dictionary<string, string?> { ["variant"] = "outline", ["size"] = "sm" });
        var wrongSize = config.Resolve(new Dictionary<string, string?> { ["variant"] = "outline", ["size"] = "lg" });
        var wrongVariant = config.Resolve(new Dictionary<string, string?> { ["variant"] = "default", ["size"] = "sm" });

        Assert.Contains("border-dashed", matching);
        Assert.DoesNotContain("border-dashed", wrongSize);
        Assert.DoesNotContain("border-dashed", wrongVariant);
    }

    [Fact]
    public void Resolve_ClassName_AppendedLastAndWinsConflicts()
    {
        var classes = ButtonLike().Resolve(null, "px-8");

        Assert.Equal("inline-flex rounded-md bg-primary h-9 px-8", classes);
    }

    [Fact]
    public void Resolve_UnknownAxisValue_ThrowsArgumentException()
    {
        var config = ButtonLike();
        var selection = new Dictionary<string, string?> { ["size"] = "xl" };

        var ex = Assert.Throws<ArgumentException>(() => config.Resolve(selection));

        Assert.Contains("xl", ex.Message);
        Assert.Contains("size", ex.Message);
    }

    [Fact]
    public void Resolve_UnknownAxisInSelection_ThrowsArgumentExceptionListingKnownAxes()
    {
        var config = ButtonLike();
        var selection = new Dictionary<string, string?> { ["colour"] = "red" };

        var ex = Assert.Throws<ArgumentException>(() => config.Resolve(selection));

        Assert.Equal("selection", ex.ParamName);
        Assert.Contains("colour", ex.Message);
        Assert.Contains("Known axes: variant, size.", ex.Message);
    }

    [Fact]
    public void Resolve_BlankSelectionKey_ThrowsArgumentExceptionForSelection()
    {
        var config = ButtonLike();
        var selection = new Dictionary<string, string?> { ["  "] = "default" };

        var ex = Assert.Throws<ArgumentException>(() => config.Resolve(selection));

        Assert.Equal("selection", ex.ParamName);
    }

    [Fact]
    public void Resolve_SelectedValueNotOnAxis_ListsAllowedValues()
    {
        var config = ButtonLike();
        var selection = new Dictionary<string, string?> { ["size"] = "xl" };

        var ex = Assert.Throws<ArgumentException>(() => config.Resolve(selection));

        Assert.Equal("selection", ex.ParamName);
        Assert.Contains("'xl'", ex.Message);
        Assert.Contains("'size'", ex.Message);
        Assert.Contains("Allowed values: default, sm, lg.", ex.Message);
    }

    [Fact]
    public void Compile_AxisNameWhitespace_ThrowsInvalidOperationException()
    {
        var config = new VariantConfig
        {
            Base = _baseClasses,
            Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [" "] = new Dictionary<string, string> { ["on"] = "block" },
            },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => config.Compile());

        Assert.Contains("axis name", ex.Message);
    }

    [Fact]
    public void Compile_AxisWithNoValues_ThrowsInvalidOperationExceptionNamingAxis()
    {
        var config = new VariantConfig
        {
            Base = _baseClasses,
            Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["size"] = new Dictionary<string, string>(),
            },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => config.Compile());

        Assert.Equal("VariantConfig axis 'size' has no values.", ex.Message);
    }

    [Fact]
    public void Compile_ValueWithNullClasses_ThrowsInvalidOperationExceptionNamingValue()
    {
        var config = new VariantConfig
        {
            Base = _baseClasses,
            Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["size"] = new Dictionary<string, string> { ["sm"] = null! },
            },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => config.Compile());

        Assert.Contains("'sm'", ex.Message);
        Assert.Contains("'size'", ex.Message);
    }

    [Fact]
    public void Compile_ValueWithEmptyClasses_IsAllowed()
    {
        var config = new VariantConfig
        {
            Base = _baseClasses,
            Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["size"] = new Dictionary<string, string> { ["none"] = string.Empty },
            },
            DefaultVariants = new Dictionary<string, string> { ["size"] = "none" },
        };

        Assert.Equal(_baseClasses, config.Resolve());
    }

    [Fact]
    public void CompoundVariant_NoConditions_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new CompoundVariant(new Dictionary<string, string>(), "shadow-none"));

        Assert.Equal("conditions", ex.ParamName);
        Assert.Contains("no conditions", ex.Message);
    }

    [Fact]
    public void CompoundVariant_NullConditions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new CompoundVariant(null!, "shadow-none"));

        Assert.Equal("conditions", ex.ParamName);
    }

    [Fact]
    public void CompoundVariant_NullClasses_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CompoundVariant(new Dictionary<string, string> { ["size"] = "sm" }, null!));

        Assert.Equal("classes", ex.ParamName);
    }

    [Fact]
    public void Init_NullBase_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VariantConfig { Base = null! });
    }

    [Fact]
    public void Init_NullVariants_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VariantConfig { Variants = null! });
    }

    [Fact]
    public void Init_NullDefaultVariants_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VariantConfig { DefaultVariants = null! });
    }

    [Fact]
    public void Init_NullCompoundVariants_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VariantConfig { CompoundVariants = null! });
    }

    [Fact]
    public void Compile_DefaultReferencesUnknownAxis_ThrowsInvalidOperationException()
    {
        var config = ButtonLike(defaults: new Dictionary<string, string> { ["color"] = "default" });

        var ex = Assert.Throws<InvalidOperationException>(() => config.Compile());

        Assert.Contains("color", ex.Message);
    }

    [Fact]
    public void Compile_DefaultReferencesUnknownValue_ThrowsInvalidOperationException()
    {
        var config = ButtonLike(defaults: new Dictionary<string, string> { ["size"] = "xl" });

        var ex = Assert.Throws<InvalidOperationException>(() => config.Compile());

        Assert.Contains("xl", ex.Message);
    }

    [Fact]
    public void Compile_CompoundConditionReferencesUnknownValue_ThrowsInvalidOperationException()
    {
        var config = ButtonLike(compounds:
        [
            new CompoundVariant(
                new Dictionary<string, string> { ["variant"] = "ghost" },
                "shadow-none"),
        ]);

        var ex = Assert.Throws<InvalidOperationException>(() => config.Compile());

        Assert.Contains("ghost", ex.Message);
    }

    [Fact]
    public void Compile_ReturnedDelegate_IsReusable()
    {
        var resolve = ButtonLike().Compile();

        var first = resolve(new Dictionary<string, string?> { ["variant"] = "outline", ["size"] = "sm" }, null);
        var second = resolve(new Dictionary<string, string?> { ["size"] = "lg" }, null);

        Assert.Equal("inline-flex rounded-md border bg-background h-8 px-3 border-dashed", first);
        Assert.Equal("inline-flex rounded-md bg-primary h-10 px-8", second);
    }

    [Fact]
    public void Resolve_EmptyConfig_ReturnsBaseOnly()
    {
        var config = new VariantConfig { Base = _baseClasses };

        Assert.Equal(_baseClasses, config.Resolve());
    }

    [Fact]
    public void Resolve_CompletelyEmpty_ReturnsEmptyString()
    {
        var config = new VariantConfig();

        Assert.Equal(string.Empty, config.Resolve());
    }

    [Fact]
    public void Resolve_AxisWithoutDefaultAndWithoutSelection_IsSkipped()
    {
        var config = new VariantConfig
        {
            Base = _baseClasses,
            Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["tone"] = new Dictionary<string, string> { ["muted"] = "text-muted" },
            },
        };

        Assert.Equal(_baseClasses, config.Resolve());
    }
}
