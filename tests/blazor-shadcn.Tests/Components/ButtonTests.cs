using BlazorShadcn.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorShadcn.Tests.Components;

/// <summary>
/// Component tests for <see cref="Button"/>, rendered with bUnit.
/// </summary>
public sealed class ButtonTests : TestContext
{
    [Fact]
    public void Render_Default_HasDefaultVariantAndSizeClasses()
    {
        var cut = RenderComponent<Button>(_ => { });

        var classes = cut.Find("button").ClassList;
        Assert.Contains("bg-primary", classes);
        Assert.Contains("h-9", classes);
        Assert.Contains("px-4", classes);
    }

    [Fact]
    public void Render_DestructiveSm_HasDestructiveAndSmClasses_AndNotDefault()
    {
        var cut = RenderComponent<Button>(p => p
            .Add(b => b.Variant, ButtonVariant.Destructive)
            .Add(b => b.Size, ButtonSize.Sm));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("bg-destructive", classes);
        Assert.Contains("h-8", classes);
        Assert.Contains("px-3", classes);
        Assert.DoesNotContain("bg-primary", classes);
        Assert.DoesNotContain("h-9", classes);
    }

    [Fact]
    public void Render_ClassParameter_OverridesConflictingUtility()
    {
        var cut = RenderComponent<Button>(p => p.Add(b => b.Class, "px-8"));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("px-8", classes);
        Assert.DoesNotContain("px-4", classes);
    }

    [Fact]
    public void Render_SplattedClassAttribute_IsMergedNotDuplicated()
    {
        var cut = RenderComponent<Button>(p => p.AddUnmatched("class", "w-full"));

        var button = cut.Find("button");
        Assert.Single(button.Attributes, a => a.Name == "class");
        Assert.Contains("w-full", button.ClassList);
        Assert.Contains("bg-primary", button.ClassList);
    }

    [Fact]
    public void Render_UnmatchedAttributes_FlowToRootButton()
    {
        var cut = RenderComponent<Button>(p => p
            .AddUnmatched("id", "save-button")
            .AddUnmatched("aria-label", "Save")
            .AddUnmatched("data-testid", "save"));

        var button = cut.Find("button");
        Assert.Equal("save-button", button.Id);
        Assert.Equal("Save", button.GetAttribute("aria-label"));
        Assert.Equal("save", button.GetAttribute("data-testid"));
    }

    [Fact]
    public void Render_TypeDefaultsToButton()
    {
        var cut = RenderComponent<Button>(_ => { });

        Assert.Equal("button", cut.Find("button").GetAttribute("type"));
    }

    [Fact]
    public void Render_TypeSubmit_IsRendered()
    {
        var cut = RenderComponent<Button>(p => p.Add(b => b.Type, "submit"));

        Assert.Equal("submit", cut.Find("button").GetAttribute("type"));
    }

    [Fact]
    public void Render_Disabled_SetsDisabledAttribute()
    {
        var cut = RenderComponent<Button>(p => p.Add(b => b.Disabled, true));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ChildContent_IsRendered()
    {
        var cut = RenderComponent<Button>(p => p.AddChildContent("Save changes"));

        Assert.Equal("Save changes", cut.Find("button").TextContent);
    }

    [Fact]
    public void Click_InvokesSplattedOnclick()
    {
        var clicked = false;
        var cut = RenderComponent<Button>(p => p.AddUnmatched(
            "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true)));

        cut.Find("button").Click();

        Assert.True(clicked);
    }

    [Fact]
    public void Render_LinkVariant_HasUnderlineClasses()
    {
        var cut = RenderComponent<Button>(p => p.Add(b => b.Variant, ButtonVariant.Link));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("text-primary", classes);
        Assert.Contains("underline-offset-4", classes);
        Assert.Contains("hover:underline", classes);
    }

    [Fact]
    public void Render_IconSize_HasSquareClasses()
    {
        var cut = RenderComponent<Button>(p => p.Add(b => b.Size, ButtonSize.Icon));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("h-9", classes);
        Assert.Contains("w-9", classes);
    }
}
