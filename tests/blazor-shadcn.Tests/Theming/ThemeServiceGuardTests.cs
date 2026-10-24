using BlazorShadcn.Theming;

namespace BlazorShadcn.Tests.Theming;

public class ThemeServiceGuardTests
{
    [Fact]
    public void Ctor_NullJs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ThemeService(null!));
    }

    [Fact]
    public async Task SetModeAsync_UndefinedEnum_ThrowsArgumentOutOfRange()
    {
        var js = new FakeJSRuntime();
        await using var sut = new ThemeService(js);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.SetModeAsync((ThemeMode)42));

        Assert.Empty(js.Calls);
    }

    [Fact]
    public async Task SetModeAsync_BeforeInitialize_AutoInitializes()
    {
        var js = new FakeJSRuntime { InitMode = "system", InitResolved = "light", SetModeResolved = "dark" };
        await using var sut = new ThemeService(js);

        await sut.SetModeAsync(ThemeMode.Dark);

        Assert.Equal(["import", "init", "setMode"], js.Calls.Select(call => call.Identifier));
        Assert.Single(js.Calls, call => call.Identifier == "import");
        Assert.Equal(ThemeMode.Dark, sut.Mode);
        Assert.Equal(ThemeMode.Dark, sut.Resolved);
    }

    [Fact]
    public async Task AfterDispose_Operations_ThrowObjectDisposed()
    {
        var js = new FakeJSRuntime();
        var sut = new ThemeService(js);
        await sut.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.InitializeAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.SetModeAsync(ThemeMode.Dark));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.ToggleAsync());

        Assert.Empty(js.Calls);
    }

    [Fact]
    public async Task InitializeAsync_MalformedJsResult_FallsBackToSystemLight()
    {
        var js = new FakeJSRuntime { InitMode = "sepia", InitResolved = "purple" };
        await using var sut = new ThemeService(js);

        var exception = await Record.ExceptionAsync(() => sut.InitializeAsync());

        Assert.Null(exception);
        Assert.Equal(ThemeMode.System, sut.Mode);
        Assert.Equal(ThemeMode.Light, sut.Resolved);
    }
}
