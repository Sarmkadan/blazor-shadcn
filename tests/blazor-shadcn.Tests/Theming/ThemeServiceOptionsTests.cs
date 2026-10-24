using BlazorShadcn.Theming;
using Microsoft.Extensions.Options;

namespace BlazorShadcn.Tests.Theming;

public class ThemeServiceOptionsTests
{
    [Fact]
    public async Task InitializeAsync_ConfiguredOptions_PassesStorageKeyAndDefaultToInit()
    {
        var js = new FakeJSRuntime();
        var options = Options.Create(new ThemeOptions { StorageKey = "custom-key", DefaultMode = ThemeMode.Dark });
        await using var sut = new ThemeService(js, options);

        await sut.InitializeAsync();

        var init = js.Calls.Single(call => call.Identifier == "init");
        Assert.Equal("custom-key", init.Args![0]);
        Assert.Equal("dark", init.Args![2]);
    }

    [Fact]
    public void Constructor_ConfiguredDefaultMode_IsReportedBeforeInitialize()
    {
        var options = Options.Create(new ThemeOptions { DefaultMode = ThemeMode.Dark });

        var sut = new ThemeService(new FakeJSRuntime(), options);

        Assert.Equal(ThemeMode.Dark, sut.Mode);
    }

    [Fact]
    public void Constructor_BlankStorageKey_ThrowsArgumentException()
    {
        var options = Options.Create(new ThemeOptions { StorageKey = "  " });

        Assert.Throws<ArgumentException>(() => new ThemeService(new FakeJSRuntime(), options));
    }

    [Fact]
    public void Constructor_UndefinedDefaultMode_ThrowsArgumentOutOfRangeException()
    {
        var options = Options.Create(new ThemeOptions { DefaultMode = (ThemeMode)99 });

        Assert.Throws<ArgumentOutOfRangeException>(() => new ThemeService(new FakeJSRuntime(), options));
    }
}
