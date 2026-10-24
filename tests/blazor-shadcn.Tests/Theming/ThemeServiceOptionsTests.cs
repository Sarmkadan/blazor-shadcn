using System.Text.Json;
using BlazorShadcn.Theming;
using Microsoft.Extensions.Options;

namespace BlazorShadcn.Tests.Theming;

public class ThemeServiceOptionsTests
{
    private static readonly JsonSerializerOptions _webOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task InitializeAsync_ConfiguredOptions_PassesOptionsObjectToInit()
    {
        var js = new FakeJSRuntime();
        var options = Options.Create(new ThemeOptions
        {
            StorageKey = "custom-key",
            DefaultMode = ThemeMode.Dark,
            DarkClass = "theme-dark",
            UseColorSchemeAttribute = true,
            DisableTransitionOnChange = false,
        });
        await using var sut = new ThemeService(js, options);

        await sut.InitializeAsync();

        var init = js.Calls.Single(call => call.Identifier == "init");
        var passed = ToJson(init.Args![0]);
        Assert.Equal("custom-key", passed.GetProperty("storageKey").GetString());
        Assert.Equal("dark", passed.GetProperty("defaultMode").GetString());
        Assert.Equal("theme-dark", passed.GetProperty("darkClass").GetString());
        Assert.True(passed.GetProperty("useColorSchemeAttribute").GetBoolean());
        Assert.False(passed.GetProperty("disableTransitionOnChange").GetBoolean());
    }

    [Fact]
    public async Task SetModeAsync_ConfiguredOptions_PassesSameOptionsObjectToSetMode()
    {
        var js = new FakeJSRuntime();
        var options = Options.Create(new ThemeOptions { StorageKey = "custom-key", DarkClass = "theme-dark" });
        await using var sut = new ThemeService(js, options);

        await sut.SetModeAsync(ThemeMode.Dark);

        var setMode = js.Calls.Single(call => call.Identifier == "setMode");
        var passed = ToJson(setMode.Args![0]);
        Assert.Equal("custom-key", passed.GetProperty("storageKey").GetString());
        Assert.Equal("theme-dark", passed.GetProperty("darkClass").GetString());
        Assert.Equal("dark", setMode.Args![1]);
    }

    [Fact]
    public async Task InitializeAsync_CustomModulePath_ImportsConfiguredModule()
    {
        var js = new FakeJSRuntime();
        var options = Options.Create(new ThemeOptions { ModulePath = "./custom/theme-bridge.js" });
        await using var sut = new ThemeService(js, options);

        await sut.InitializeAsync();

        var import = js.Calls.Single(call => call.Identifier == "import");
        Assert.Equal("./custom/theme-bridge.js", import.Args![0]);
    }

    [Fact]
    public void Constructor_BlankModulePath_ThrowsArgumentException()
    {
        var options = Options.Create(new ThemeOptions { ModulePath = " " });

        Assert.Throws<ArgumentException>(() => new ThemeService(new FakeJSRuntime(), options));
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

    // Blazor serializes interop arguments with web defaults, so the options object is inspected as JSON, the way JS sees it.
    private static JsonElement ToJson(object? value) =>
        JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(value, _webOptions), _webOptions);
}
