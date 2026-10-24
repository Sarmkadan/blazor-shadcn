using BlazorShadcn.Theming;
using Microsoft.Extensions.Options;

namespace BlazorShadcn.Tests.Theming;

public class ThemeOptionsValidatorTests
{
    public static TheoryData<Action<ThemeOptions>, string, string> InvalidOptions => new()
    {
        { o => o.StorageKey = string.Empty, "StorageKey", "must not be empty" },
        { o => o.StorageKey = "my key", "StorageKey", "must not contain whitespace" },
        { o => o.DarkClass = "my class", "DarkClass", "valid CSS class name" },
        { o => o.DefaultMode = (ThemeMode)7, "DefaultMode", "defined ThemeMode value" },
        { o => o.ModulePath = "theme.css", "ModulePath", "must end with '.js'" },
    };

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public void Validate_InvalidOptions_Fails(Action<ThemeOptions> mutate, string property, string reason)
    {
        var options = new ThemeOptions();
        mutate(options);

        var result = new ThemeOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains(property) && failure.Contains(reason));
    }

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = new ThemeOptionsValidator().Validate(null, new ThemeOptions());

        Assert.True(result.Succeeded);
        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_MultipleViolations_ReportsEachOne()
    {
        var options = new ThemeOptions
        {
            StorageKey = "bad key",
            DarkClass = "1dark",
            ModulePath = "theme.css",
        };

        var result = new ThemeOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(3, result.Failures.Count());
    }
}
