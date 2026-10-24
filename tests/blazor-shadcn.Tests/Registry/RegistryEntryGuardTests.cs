using BlazorShadcn.Registry;

namespace BlazorShadcn.Tests.Registry;

public class RegistryEntryGuardTests
{
    private static readonly string[] _buttonFile = ["Components/Button/Button.razor"];
    private static readonly string[] _buttonFileWithNull = ["Components/Button/Button.razor", null!];
    private static readonly string[] _noEntries = [];
    private static readonly string[] _coreAndBlank = ["core", " "];
    private static readonly string[] _cardAndButton = ["card", "button"];
    private static readonly string[] _packageWithVersion = ["Foo@1.0.0"];

    [Theory]
    [InlineData("Button")]
    [InlineData("-a")]
    [InlineData("a-")]
    [InlineData("a--b")]
    [InlineData("a b")]
    [InlineData("1button")]
    [InlineData("a_b")]
    [InlineData("button\n")]
    [InlineData("")]
    public void Ctor_NameNotKebabCase_ThrowsArgumentException(string name)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(name: name));

        Assert.Equal("Name", ex.ParamName);
    }

    [Fact]
    public void Ctor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RegistryEntry(
            null!, RegistryEntryType.Ui, _buttonFile, [], _noEntries, "A component."));
    }

    [Fact]
    public void Ctor_UndefinedType_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Create(type: (RegistryEntryType)999));

        Assert.Equal("Type", ex.ParamName);
    }

    [Fact]
    public void Ctor_EmptyFiles_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Create(files: _noEntries));
    }

    [Fact]
    public void Ctor_NullFiles_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RegistryEntry(
            "button", RegistryEntryType.Ui, null!, [], _noEntries, "A component."));
    }

    [Theory]
    [InlineData("/x")]
    [InlineData("../x")]
    [InlineData("Components/../../x")]
    [InlineData("Components\\Button.razor")]
    [InlineData("   ")]
    [InlineData("")]
    public void Ctor_UnsafeFilePath_ThrowsArgumentNamingThePath(string path)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(files: [path]));

        Assert.Equal("Files", ex.ParamName);
        Assert.Contains(path.Trim(), ex.Message);
    }

    [Fact]
    public void Ctor_NullFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Create(files: _buttonFileWithNull));
    }

    [Fact]
    public void Ctor_NullRegistryDependencies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RegistryEntry(
            "button", RegistryEntryType.Ui, _buttonFile, null!, [], "A component."));
    }

    [Fact]
    public void Ctor_BlankRegistryDependency_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Create(registryDependencies: _coreAndBlank));
    }

    [Fact]
    public void Ctor_NullPackageDependencies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RegistryEntry(
            "button", RegistryEntryType.Ui, _buttonFile, [], null!, "A component."));
    }

    [Theory]
    [InlineData("button")]
    [InlineData("Button")]
    public void Ctor_SelfDependency_ThrowsArgumentExceptionNamingTheEntry(string dependency)
    {
        var ex = Assert.Throws<ArgumentException>(() => Create(name: "button", registryDependencies: [dependency]));

        Assert.Contains("entry 'button' cannot depend on itself", ex.Message);
    }

    [Fact]
    public void Ctor_NullDescription_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RegistryEntry(
            "button", RegistryEntryType.Ui, _buttonFile, [], _noEntries, null!));
    }

    [Fact]
    public void Ctor_EmptyDescription_IsAllowed()
    {
        var entry = Create(description: string.Empty);

        Assert.Equal(string.Empty, entry.Description);
    }

    [Fact]
    public void Ctor_ValidArguments_Succeeds()
    {
        var entry = Create(name: "login-form", registryDependencies: _cardAndButton, packageDependencies: _packageWithVersion);

        Assert.Equal("login-form", entry.Name);
        Assert.Equal(_cardAndButton, entry.RegistryDependencies);
    }

    [Fact]
    public void DefaultRegistryEntries_All_PassesGuards()
    {
        var ex = Record.Exception(() => new ComponentRegistry(DefaultRegistryEntries.All));

        Assert.Null(ex);
    }

    private static RegistryEntry Create(
        string name = "button",
        RegistryEntryType type = RegistryEntryType.Ui,
        IReadOnlyList<string>? files = null,
        IReadOnlyList<string>? registryDependencies = null,
        IReadOnlyList<string>? packageDependencies = null,
        string? description = "A component.") =>
        new(
            name,
            type,
            files ?? _buttonFile,
            registryDependencies ?? _noEntries,
            packageDependencies ?? _noEntries,
            description!);
}
