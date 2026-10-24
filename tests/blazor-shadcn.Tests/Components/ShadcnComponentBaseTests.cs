using BlazorShadcn.Components;

namespace BlazorShadcn.Tests.Components;

/// <summary>
/// Tests for <see cref="ShadcnComponentBase"/> helpers (<c>ClassValue</c>, <c>AttributesWithoutClass</c>)
/// and the rendered root element, exercised through <see cref="TestProbeComponent"/>.
/// </summary>
public sealed class ShadcnComponentBaseTests : TestContext
{
    [Fact]
    public void ClassValue_AllNull_ReturnsNull()
    {
        var probe = new TestProbeComponent();

        Assert.Null(probe.PublicClassValue(null));
    }

    [Fact]
    public void ClassValue_OnlyComponentClasses_ReturnsThem()
    {
        var probe = new TestProbeComponent();

        Assert.Equal("base-a px-4", probe.PublicClassValue("base-a px-4"));
    }

    [Fact]
    public void ClassValue_ClassParameter_AppendedAfterComponentClasses()
    {
        var probe = new TestProbeComponent().WithParameters(cls: "mt-2 text-sm");

        Assert.Equal("base-a px-4 mt-2 text-sm", probe.PublicClassValue("base-a px-4"));
    }

    [Fact]
    public void ClassValue_ClassParameter_WinsTailwindConflict()
    {
        var probe = new TestProbeComponent().WithParameters(cls: "px-8");

        var result = probe.PublicClassValue("base-a px-4");

        Assert.Equal("base-a px-8", result);
        Assert.DoesNotContain("px-4", result);
    }

    [Fact]
    public void ClassValue_SplattedClass_AppendedLast_AndWins()
    {
        var probe = new TestProbeComponent().WithParameters(
            cls: "px-8",
            attributes: new Dictionary<string, object> { ["class"] = "px-2" });

        var result = probe.PublicClassValue("base-a px-4");

        Assert.EndsWith("px-2", result);
        Assert.DoesNotContain("px-8", result);
        Assert.DoesNotContain("px-4", result);
    }

    [Fact]
    public void ClassValue_SplattedClassKey_IsCaseInsensitive()
    {
        var probe = new TestProbeComponent().WithParameters(
            attributes: new Dictionary<string, object> { ["Class"] = "px-2" });

        var result = probe.PublicClassValue("base-a px-4");

        Assert.Equal("base-a px-2", result);
    }

    [Fact]
    public void ClassValue_WhitespaceOnly_ReturnsNull()
    {
        var probe = new TestProbeComponent().WithParameters(cls: "   ");

        Assert.Null(probe.PublicClassValue("  \t "));
    }

    [Fact]
    public void AttributesWithoutClass_Null_ReturnsNull()
    {
        var probe = new TestProbeComponent().WithParameters();

        Assert.Null(probe.PublicAttributesWithoutClass());
    }

    [Fact]
    public void AttributesWithoutClass_NoClassKey_ReturnsSameInstance()
    {
        var attributes = new Dictionary<string, object> { ["id"] = "probe" };
        var probe = new TestProbeComponent().WithParameters(attributes: attributes);

        Assert.Same(attributes, probe.PublicAttributesWithoutClass());
    }

    [Fact]
    public void AttributesWithoutClass_WithClassKey_ReturnsCopyWithoutIt_AndKeepsOthers()
    {
        var attributes = new Dictionary<string, object>
        {
            ["id"] = "probe",
            ["class"] = "px-2",
            ["data-testid"] = "x",
        };
        var probe = new TestProbeComponent().WithParameters(attributes: attributes);

        var result = probe.PublicAttributesWithoutClass();

        Assert.NotNull(result);
        Assert.NotSame(attributes, result);
        Assert.DoesNotContain("class", result.Keys);
        Assert.Equal("probe", result["id"]);
        Assert.Equal("x", result["data-testid"]);
        Assert.Equal(2, result.Count);
        Assert.Equal(3, attributes.Count);
    }

    [Fact]
    public void AttributesWithoutClass_OnlyClassKey_ReturnsEmptyDictionaryOrNull()
    {
        var probe = new TestProbeComponent().WithParameters(
            attributes: new Dictionary<string, object> { ["class"] = "px-2" });

        // Documented behaviour: a dictionary that held only `class` yields an empty dictionary (never a
        // dictionary containing `class`); splatting an empty dictionary is a no-op.
        var result = probe.PublicAttributesWithoutClass();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Render_MergesClassAndSplatsRest()
    {
        var cut = RenderComponent<TestProbeComponent>(p => p
            .Add(c => c.Class, "px-8")
            .AddUnmatched("class", "px-2")
            .AddUnmatched("id", "probe")
            .AddChildContent("label"));

        var div = cut.Find("div");
        Assert.Single(div.Attributes, a => a.Name == "class");
        Assert.Contains("base-a", div.ClassList);
        Assert.Contains("px-2", div.ClassList);
        Assert.DoesNotContain("px-4", div.ClassList);
        Assert.DoesNotContain("px-8", div.ClassList);
        Assert.Equal("probe", div.GetAttribute("id"));
        Assert.Equal("label", div.TextContent);
    }

    [Fact]
    public void Render_NoAttributes_OmitsClassAttribute()
    {
        var cut = RenderComponent<BareTestProbeComponent>(_ => { });

        var div = cut.Find("div");
        Assert.False(div.HasAttribute("class"));
    }
}
