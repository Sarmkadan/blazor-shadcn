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
            PackageDependencies: [],
            Description: "Class merging, variant configuration and the shared component base."),
    ];
}
