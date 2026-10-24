namespace BlazorShadcn.Registry;

/// <summary>
/// Registry entries shipped with the library. <see cref="ComponentRegistry.CreateDefault"/> builds its catalog from these.
/// </summary>
public static class DefaultRegistryEntries
{
    /// <summary>Every entry shipped with the library, in declaration order.</summary>
    public static IReadOnlyList<RegistryEntry> All { get; } =
    [
        new RegistryEntry(
            Name: "core",
            Type: RegistryEntryType.Core,
            Files:
            [
                "Utils/ClassNames.cs",
                "Utils/TailwindConflictGroups.cs",
                "Variants/VariantConfig.cs",
                "Components/ShadcnComponentBase.cs",
            ],
            RegistryDependencies: [],
            PackageDependencies: ["Microsoft.AspNetCore.Components.Web"],
            Description: "cn(), variants and the component base class."),

        new RegistryEntry(
            Name: "button",
            Type: RegistryEntryType.Ui,
            Files:
            [
                "Components/Button/Button.razor",
                "Components/Button/Button.razor.cs",
                "Components/Button/ButtonVariant.cs",
                "Components/Button/ButtonSize.cs",
            ],
            RegistryDependencies: ["core"],
            PackageDependencies: [],
            Description: "Button with shadcn variants and sizes."),

        new RegistryEntry(
            Name: "badge",
            Type: RegistryEntryType.Ui,
            Files:
            [
                "Components/Badge/Badge.razor",
                "Components/Badge/Badge.razor.cs",
                "Components/Badge/BadgeVariant.cs",
            ],
            RegistryDependencies: ["core"],
            PackageDependencies: [],
            Description: "Small status label with shadcn variants."),

        new RegistryEntry(
            Name: "card",
            Type: RegistryEntryType.Ui,
            Files:
            [
                "Components/Card/Card.razor",
                "Components/Card/CardHeader.razor",
                "Components/Card/CardTitle.cs",
                "Components/Card/CardDescription.razor",
                "Components/Card/CardContent.razor",
                "Components/Card/CardFooter.razor",
            ],
            RegistryDependencies: ["core"],
            PackageDependencies: [],
            Description: "Card container with header, title, description, content and footer."),

        new RegistryEntry(
            Name: "input",
            Type: RegistryEntryType.Ui,
            Files:
            [
                "Components/Input/Input.razor",
                "Components/Input/Input.razor.cs",
            ],
            RegistryDependencies: ["core"],
            PackageDependencies: [],
            Description: "Styled text input."),

        new RegistryEntry(
            Name: "label",
            Type: RegistryEntryType.Ui,
            Files:
            [
                "Components/Label/Label.razor",
                "Components/Label/Label.razor.cs",
            ],
            RegistryDependencies: ["core"],
            PackageDependencies: [],
            Description: "Form label."),

        new RegistryEntry(
            Name: "separator",
            Type: RegistryEntryType.Ui,
            Files:
            [
                "Components/Separator/Separator.razor",
                "Components/Separator/Separator.razor.cs",
                "Components/Separator/SeparatorOrientation.cs",
            ],
            RegistryDependencies: ["core"],
            PackageDependencies: [],
            Description: "Horizontal or vertical divider line."),

        new RegistryEntry(
            Name: "skeleton",
            Type: RegistryEntryType.Ui,
            Files:
            [
                "Components/Skeleton/Skeleton.razor",
            ],
            RegistryDependencies: ["core"],
            PackageDependencies: [],
            Description: "Placeholder shimmer for loading content."),

        new RegistryEntry(
            Name: "theme-service",
            Type: RegistryEntryType.Service,
            Files:
            [
                "Theming/IThemeService.cs",
                "Theming/ThemeService.cs",
                "wwwroot/theme.js",
            ],
            RegistryDependencies: [],
            PackageDependencies: ["Microsoft.JSInterop"],
            Description: "Dark, light and system theme switching via CSS variables and localStorage."),

        new RegistryEntry(
            Name: "login-form",
            Type: RegistryEntryType.Block,
            Files:
            [
                "Components/Blocks/LoginForm.razor",
            ],
            RegistryDependencies: ["card", "button", "input", "label"],
            PackageDependencies: [],
            Description: "Login form block composed from card, button, input and label."),
    ];

    /// <summary>Names of every entry in <see cref="All"/>, in the same order. Used for CLI autocomplete.</summary>
    public static IReadOnlyList<string> Names { get; } =
        All.Select(entry => entry.Name).ToArray();
}
