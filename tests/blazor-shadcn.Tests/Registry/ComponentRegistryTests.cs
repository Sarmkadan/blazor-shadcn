using BlazorShadcn.Registry;

namespace BlazorShadcn.Tests.Registry;

public class ComponentRegistryTests
{
    [Fact]
    public void Ctor_DuplicateNames_ThrowsArgumentExceptionNamingTheDuplicate()
    {
        var entries = Arr(E("button"), E("button"));

        var ex = Assert.Throws<ArgumentException>(() => new ComponentRegistry(entries));

        Assert.Contains("'button'", ex.Message);
    }

    [Fact]
    public void Ctor_NullEntries_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ComponentRegistry(null!));
    }

    [Fact]
    public void Ctor_NullElement_ThrowsArgumentExceptionWithIndex()
    {
        var entries = Arr(E("core"), null!, E("button"));

        var ex = Assert.Throws<ArgumentException>(() => new ComponentRegistry(entries));

        Assert.Contains("index 1", ex.Message);
    }

    [Fact]
    public void Find_BlankName_ThrowsArgumentException()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        Assert.Throws<ArgumentException>(() => registry.Find("   "));
    }

    [Fact]
    public void Find_NullName_ThrowsArgumentNullException()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        Assert.Throws<ArgumentNullException>(() => registry.Find(null!));
    }

    [Fact]
    public void ResolveWithDependencies_NullNames_ThrowsArgumentNullException()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        Assert.Throws<ArgumentNullException>(() => registry.ResolveWithDependencies(null!));
    }

    [Fact]
    public void ResolveWithDependencies_NullElement_ThrowsArgumentNullExceptionForNames()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        var ex = Assert.Throws<ArgumentNullException>(() => registry.ResolveWithDependencies(Arr("button", null!)));

        Assert.Equal("names", ex.ParamName);
    }

    [Fact]
    public void ResolveWithDependencies_BlankElement_ThrowsArgumentExceptionForNames()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        var ex = Assert.Throws<ArgumentException>(() => registry.ResolveWithDependencies(Arr("button", "  ")));

        Assert.Equal("names", ex.ParamName);
    }

    [Fact]
    public void ResolveWithDependencies_PaddedName_IsTrimmedBeforeLookup()
    {
        var registry = new ComponentRegistry(Arr(E("core"), E("button", "core")));

        var result = registry.ResolveWithDependencies(Arr("  button\t"));

        Assert.Equal(Arr("core", "button"), Names(result));
    }

    [Fact]
    public void GetAll_ReturnsSortedByName()
    {
        var registry = new ComponentRegistry(Arr(E("c"), E("a"), E("b")));

        Assert.Equal(Arr("a", "b", "c"), Names(registry.GetAll()));
    }

    [Fact]
    public void GetAll_Empty_ReturnsEmptyList()
    {
        var registry = new ComponentRegistry(Array.Empty<RegistryEntry>());

        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        Assert.Equal("button", registry.Find("BUTTON")?.Name);
    }

    [Fact]
    public void Find_Unknown_ReturnsNull()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        Assert.Null(registry.Find("card"));
    }

    [Fact]
    public void ResolveWithDependencies_SingleEntryNoDeps_ReturnsItself()
    {
        var registry = new ComponentRegistry(Arr(E("core"), E("button", "core")));

        var result = registry.ResolveWithDependencies(Arr("core"));

        Assert.Equal(Arr("core"), Names(result));
    }

    [Fact]
    public void ResolveWithDependencies_DependenciesComeFirst()
    {
        var registry = new ComponentRegistry(Arr(E("core"), E("card", "core")));

        var result = registry.ResolveWithDependencies(Arr("card"));

        Assert.Equal(Arr("core", "card"), Names(result));
    }

    [Fact]
    public void ResolveWithDependencies_TransitiveChain_ResolvesEachEntryOnce()
    {
        var registry = new ComponentRegistry(Arr(
            E("core"),
            E("card", "core"),
            E("button", "core"),
            E("login-form", "card", "button")
        ));

        var result = registry.ResolveWithDependencies(Arr("login-form"));

        Assert.Equal(Arr("core", "card", "button", "login-form"), Names(result));
    }

    [Fact]
    public void ResolveWithDependencies_DiamondDependency_NoDuplicates()
    {
        var registry = new ComponentRegistry(Arr(
            E("core"),
            E("left", "core"),
            E("right", "core"),
            E("top", "left", "right")
        ));

        var result = registry.ResolveWithDependencies(Arr("top"));

        Assert.Equal(Arr("core", "left", "right", "top"), Names(result));
    }

    [Fact]
    public void ResolveWithDependencies_RequestedTwice_Deduplicated()
    {
        var registry = new ComponentRegistry(Arr(E("core"), E("button", "core")));

        var result = registry.ResolveWithDependencies(Arr("button", "Button"));

        Assert.Equal(Arr("core", "button"), Names(result));
    }

    [Fact]
    public void ResolveWithDependencies_PreservesRequestOrderForIndependentEntries()
    {
        var registry = new ComponentRegistry(Arr(E("core"), E("badge", "core"), E("button", "core")));

        var result = registry.ResolveWithDependencies(Arr("badge", "button"));

        Assert.Equal(Arr("core", "badge", "button"), Names(result));
    }

    [Fact]
    public void ResolveWithDependencies_UnknownRequestedName_ThrowsKeyNotFoundException()
    {
        var registry = new ComponentRegistry(Arr(E("button")));

        var ex = Assert.Throws<KeyNotFoundException>(() => registry.ResolveWithDependencies(Arr("missing")));

        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void ResolveWithDependencies_UnknownTransitiveDependency_ThrowsKeyNotFoundException()
    {
        var registry = new ComponentRegistry(Arr(E("card", "core")));

        var ex = Assert.Throws<KeyNotFoundException>(() => registry.ResolveWithDependencies(Arr("card")));

        Assert.Contains("core", ex.Message);
        Assert.Contains("card", ex.Message);
    }

    [Fact]
    public void ResolveWithDependencies_Cycle_ThrowsInvalidOperationException()
    {
        var registry = new ComponentRegistry(Arr(E("a", "b"), E("b", "a")));

        var ex = Assert.Throws<InvalidOperationException>(() => registry.ResolveWithDependencies(Arr("a")));

        Assert.Contains("a -> b -> a", ex.Message);
    }

    [Fact]
    public void ResolveWithDependencies_EmptyInput_ReturnsEmpty()
    {
        var registry = new ComponentRegistry(Arr(E("core")));

        var result = registry.ResolveWithDependencies(Array.Empty<string>());

        Assert.Empty(result);
    }

    private static RegistryEntry E(string name, params string[] deps) =>
        new(name, RegistryEntryType.Ui, Arr($"{name}.razor"), deps, Array.Empty<string>(), $"{name} component");

    private static T[] Arr<T>(params T[] items) => items;

    private static string[] Names(IEnumerable<RegistryEntry> entries) => entries.Select(e => e.Name).ToArray();
}
