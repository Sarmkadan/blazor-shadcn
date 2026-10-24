using BlazorShadcn.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorShadcn.Tests.Components;

/// <summary>
/// Test-only subclass that exposes the protected helpers of <see cref="ShadcnComponentBase"/>
/// and renders a root <c>div</c> with a fixed base class list.
/// </summary>
public sealed class TestProbeComponent : ShadcnComponentBase
{
    /// <summary>Exposes <see cref="ShadcnComponentBase.ClassValue"/> for direct assertions.</summary>
    public string? PublicClassValue(string? c) => ClassValue(c);

    /// <summary>Exposes <see cref="ShadcnComponentBase.AttributesWithoutClass"/> for direct assertions.</summary>
    public IReadOnlyDictionary<string, object>? PublicAttributesWithoutClass() => AttributesWithoutClass();

    /// <summary>
    /// Sets the parameters for pure (non-rendering) tests. Assignments live inside the component type,
    /// because the Razor analyzer (BL0005) rejects parameter writes from outside the component.
    /// </summary>
    public TestProbeComponent WithParameters(string? cls = null, IReadOnlyDictionary<string, object>? attributes = null)
    {
        Class = cls;
        AdditionalAttributes = attributes;
        return this;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", ClassValue("base-a px-4"));
        builder.AddMultipleAttributes(2, AttributesWithoutClass());
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}

/// <summary>
/// Test-only subclass of <see cref="ShadcnComponentBase"/> with no base classes, so that the
/// <c>class</c> attribute is only produced from <see cref="ShadcnComponentBase.Class"/> and splatted attributes.
/// </summary>
public sealed class BareTestProbeComponent : ShadcnComponentBase
{
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", ClassValue(null));
        builder.AddMultipleAttributes(2, AttributesWithoutClass());
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
