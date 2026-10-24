using BlazorShadcn.Registry;
using BlazorShadcn.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BlazorShadcn;

/// <summary>Registers BlazorShadcn services with an application's <see cref="IServiceCollection"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IThemeService"/> (scoped, so it works in both Blazor Server circuits and WebAssembly)
    /// and <see cref="IComponentRegistry"/> (singleton, built from the shipped entries). Theme options keep their defaults.
    /// </summary>
    /// <remarks>
    /// Safe to call more than once: existing registrations are kept and nothing is duplicated.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// builder.Services.AddBlazorShadcn();
    /// </code>
    /// </example>
    public static IServiceCollection AddBlazorShadcn(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IThemeService, ThemeService>();
        services.TryAddSingleton<IComponentRegistry>(_ => ComponentRegistry.CreateDefault());
        services.AddOptions<ThemeOptions>();

        return services;
    }

    /// <summary>
    /// Registers the services as <see cref="AddBlazorShadcn(IServiceCollection)"/> does, and applies
    /// <paramref name="configure"/> to <see cref="ThemeOptions"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Sets the theme options, e.g. the default mode or the storage key.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// builder.Services.AddBlazorShadcn(o => o.DefaultMode = ThemeMode.Dark);
    /// </code>
    /// </example>
    public static IServiceCollection AddBlazorShadcn(this IServiceCollection services, Action<ThemeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        AddBlazorShadcn(services);
        services.Configure(configure);

        return services;
    }
}
