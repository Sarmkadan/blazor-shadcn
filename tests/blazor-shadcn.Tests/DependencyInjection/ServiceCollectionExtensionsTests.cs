using BlazorShadcn.Registry;
using BlazorShadcn.Tests.Theming;
using BlazorShadcn.Theming;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorShadcn.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBlazorShadcn_RegistersThemeServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddBlazorShadcn();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IThemeService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(ThemeService), descriptor.ImplementationType);
    }

    [Fact]
    public async Task AddBlazorShadcn_RegistersRegistryAsSingleton_WithDefaultEntries()
    {
        var services = new ServiceCollection();
        services.AddBlazorShadcn();
        await using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IComponentRegistry>();

        Assert.NotNull(registry.Find("core"));
        Assert.Same(registry, provider.GetRequiredService<IComponentRegistry>());
    }

    [Fact]
    public void AddBlazorShadcn_CalledTwice_DoesNotDuplicateRegistrations()
    {
        var services = new ServiceCollection();

        services.AddBlazorShadcn();
        services.AddBlazorShadcn();

        Assert.Single(services, d => d.ServiceType == typeof(IThemeService));
        Assert.Single(services, d => d.ServiceType == typeof(IComponentRegistry));
    }

    [Fact]
    public async Task AddBlazorShadcn_ConfigureDelegate_IsApplied()
    {
        var services = new ServiceCollection();
        services.AddBlazorShadcn(o =>
        {
            o.DefaultMode = ThemeMode.Dark;
            o.StorageKey = "custom-key";
        });
        await using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ThemeOptions>>().Value;

        Assert.Equal(ThemeMode.Dark, options.DefaultMode);
        Assert.Equal("custom-key", options.StorageKey);
    }

    [Fact]
    public void AddBlazorShadcn_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddBlazorShadcn());
        Assert.Throws<ArgumentNullException>(() => services.AddBlazorShadcn(_ => { }));
    }

    [Fact]
    public void AddBlazorShadcn_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddBlazorShadcn((Action<ThemeOptions>)null!));
    }

    [Fact]
    public void AddBlazorShadcn_NullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddBlazorShadcn((IConfiguration)null!));
    }

    [Fact]
    public async Task AddBlazorShadcn_Configuration_BindsThemeSection()
    {
        var services = new ServiceCollection();
        services.AddBlazorShadcn(BuildConfiguration(new Dictionary<string, string?>
        {
            ["BlazorShadcn:Theme:DefaultMode"] = "Dark",
            ["BlazorShadcn:Theme:StorageKey"] = "from-config",
            ["BlazorShadcn:Theme:DarkClass"] = "theme-dark",
            ["BlazorShadcn:Theme:UseColorSchemeAttribute"] = "true",
        }));
        await using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ThemeOptions>>().Value;

        Assert.Equal(ThemeMode.Dark, options.DefaultMode);
        Assert.Equal("from-config", options.StorageKey);
        Assert.Equal("theme-dark", options.DarkClass);
        Assert.True(options.UseColorSchemeAttribute);
        Assert.True(options.DisableTransitionOnChange);
    }

    [Fact]
    public async Task AddBlazorShadcn_InvalidThemeOptions_FailsWhenOptionsResolve()
    {
        var services = new ServiceCollection();
        services.AddBlazorShadcn(o => o.ModulePath = "theme.css");
        await using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ThemeOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains("ModulePath"));
    }

    [Fact]
    public void AddBlazorShadcn_CalledTwice_DoesNotDuplicateValidator()
    {
        var services = new ServiceCollection();

        services.AddBlazorShadcn();
        services.AddBlazorShadcn(_ => { });

        Assert.Single(services, d => d.ServiceType == typeof(IValidateOptions<ThemeOptions>));
    }

    [Fact]
    public async Task AddBlazorShadcn_ResolvesThemeServiceInScope_WithConfiguredOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        services.AddBlazorShadcn(o => o.DefaultMode = ThemeMode.Light);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var theme = scope.ServiceProvider.GetRequiredService<IThemeService>();

        Assert.Equal(ThemeMode.Light, theme.Mode);
        Assert.Same(theme, scope.ServiceProvider.GetRequiredService<IThemeService>());
    }

    [Fact]
    public void AddBlazorShadcn_ReturnsSameCollection_ForChaining()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddBlazorShadcn());
        Assert.Same(services, services.AddBlazorShadcn(_ => { }));
        Assert.Same(services, services.AddBlazorShadcn(BuildConfiguration([])));
    }

    private static IConfiguration BuildConfiguration(IEnumerable<KeyValuePair<string, string?>> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
