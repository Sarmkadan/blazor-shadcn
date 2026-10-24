using BlazorShadcn.Theming;
using Microsoft.JSInterop;

namespace BlazorShadcn.Tests.Theming;

public class ThemeServiceTests
{
    [Fact]
    public async Task InitializeAsync_ImportsModuleOnce_WhenCalledTwice()
    {
        var js = new FakeJSRuntime();
        await using var sut = new ThemeService(js);

        await sut.InitializeAsync();
        await sut.InitializeAsync();

        Assert.Single(js.Calls, call => call.Identifier == "import");
    }

    [Fact]
    public async Task InitializeAsync_StoredDark_SetsModeDarkResolvedDark()
    {
        var js = new FakeJSRuntime { InitMode = "dark", InitResolved = "dark" };
        await using var sut = new ThemeService(js);

        await sut.InitializeAsync();

        Assert.Equal(ThemeMode.Dark, sut.Mode);
        Assert.Equal(ThemeMode.Dark, sut.Resolved);
    }

    [Fact]
    public async Task InitializeAsync_StoredSystemWithOsDark_ModeSystemResolvedDark()
    {
        var js = new FakeJSRuntime { InitMode = "system", InitResolved = "dark" };
        await using var sut = new ThemeService(js);

        await sut.InitializeAsync();

        Assert.Equal(ThemeMode.System, sut.Mode);
        Assert.Equal(ThemeMode.Dark, sut.Resolved);
    }

    [Fact]
    public async Task InitializeAsync_DuringPrerender_DoesNotThrow_AndDefaultsToSystemLight()
    {
        var js = new FakeJSRuntime { ThrowOnImport = true };
        await using var sut = new ThemeService(js);

        var exception = await Record.ExceptionAsync(() => sut.InitializeAsync());

        Assert.Null(exception);
        Assert.Equal(ThemeMode.System, sut.Mode);
        Assert.Equal(ThemeMode.Light, sut.Resolved);
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "init");
    }

    [Fact]
    public async Task SetModeAsync_Dark_RaisesResolvedChangedWithDark()
    {
        var js = new FakeJSRuntime { InitMode = "system", InitResolved = "light", SetModeResolved = "dark" };
        await using var sut = new ThemeService(js);
        await sut.InitializeAsync();

        var raised = new List<ThemeMode>();
        sut.ResolvedChanged += raised.Add;

        await sut.SetModeAsync(ThemeMode.Dark);

        Assert.Equal(ThemeMode.Dark, sut.Mode);
        Assert.Equal(ThemeMode.Dark, sut.Resolved);
        Assert.Equal([ThemeMode.Dark], raised);
        Assert.Equal("dark", js.Calls.Single(call => call.Identifier == "setMode").Args![1]);
    }

    [Fact]
    public async Task SetModeAsync_SameResolved_DoesNotRaiseEvent()
    {
        var js = new FakeJSRuntime { InitMode = "system", InitResolved = "light", SetModeResolved = "light" };
        await using var sut = new ThemeService(js);
        await sut.InitializeAsync();

        var raised = new List<ThemeMode>();
        sut.ResolvedChanged += raised.Add;

        await sut.SetModeAsync(ThemeMode.Light);

        Assert.Equal(ThemeMode.Light, sut.Mode);
        Assert.Equal(ThemeMode.Light, sut.Resolved);
        Assert.Empty(raised);
    }

    [Fact]
    public async Task ToggleAsync_FromLight_SetsDark()
    {
        var js = new FakeJSRuntime { InitMode = "light", InitResolved = "light", SetModeResolved = "dark" };
        await using var sut = new ThemeService(js);
        await sut.InitializeAsync();

        await sut.ToggleAsync();

        Assert.Equal(ThemeMode.Dark, sut.Mode);
        Assert.Equal(ThemeMode.Dark, sut.Resolved);
        Assert.Equal("dark", js.Calls.Single(call => call.Identifier == "setMode").Args![1]);
    }

    [Fact]
    public async Task ToggleAsync_FromDark_SetsLight()
    {
        var js = new FakeJSRuntime { InitMode = "dark", InitResolved = "dark", SetModeResolved = "light" };
        await using var sut = new ThemeService(js);
        await sut.InitializeAsync();

        await sut.ToggleAsync();

        Assert.Equal(ThemeMode.Light, sut.Mode);
        Assert.Equal(ThemeMode.Light, sut.Resolved);
        Assert.Equal("light", js.Calls.Single(call => call.Identifier == "setMode").Args![1]);
    }

    [Fact]
    public async Task OnSystemSchemeChanged_InSystemMode_UpdatesResolvedAndRaisesEvent()
    {
        var js = new FakeJSRuntime { InitMode = "system", InitResolved = "light" };
        await using var sut = new ThemeService(js);
        await sut.InitializeAsync();

        var raised = new List<ThemeMode>();
        sut.ResolvedChanged += raised.Add;

        sut.OnSystemSchemeChanged(true);

        Assert.Equal(ThemeMode.System, sut.Mode);
        Assert.Equal(ThemeMode.Dark, sut.Resolved);
        Assert.Equal([ThemeMode.Dark], raised);
    }

    [Fact]
    public async Task OnSystemSchemeChanged_InExplicitMode_IsIgnored()
    {
        var js = new FakeJSRuntime { InitMode = "dark", InitResolved = "dark" };
        await using var sut = new ThemeService(js);
        await sut.InitializeAsync();

        var raised = new List<ThemeMode>();
        sut.ResolvedChanged += raised.Add;

        sut.OnSystemSchemeChanged(false);

        Assert.Equal(ThemeMode.Dark, sut.Mode);
        Assert.Equal(ThemeMode.Dark, sut.Resolved);
        Assert.Empty(raised);
    }

    [Fact]
    public async Task DisposeAsync_CallsDisposeOnModule_AndSwallowsJSDisconnectedException()
    {
        var js = new FakeJSRuntime { DisposeException = new JSDisconnectedException("circuit gone") };
        var sut = new ThemeService(js);
        await sut.InitializeAsync();

        var exception = await Record.ExceptionAsync(() => sut.DisposeAsync().AsTask());

        Assert.Null(exception);
        Assert.Contains(js.Calls, call => call.Identifier == "dispose");
    }

    [Fact]
    public async Task Resolved_NeverReturnsSystem()
    {
        var js = new FakeJSRuntime { InitMode = "system", InitResolved = "light", SetModeResolved = "dark" };
        await using var sut = new ThemeService(js);

        await sut.InitializeAsync();
        Assert.NotEqual(ThemeMode.System, sut.Resolved);

        await sut.SetModeAsync(ThemeMode.System);
        Assert.NotEqual(ThemeMode.System, sut.Resolved);

        sut.OnSystemSchemeChanged(false);
        Assert.NotEqual(ThemeMode.System, sut.Resolved);

        await sut.SetModeAsync(ThemeMode.Dark);
        Assert.NotEqual(ThemeMode.System, sut.Resolved);

        await sut.ToggleAsync();
        Assert.NotEqual(ThemeMode.System, sut.Resolved);
    }
}
