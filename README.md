# blazor-shadcn

[![CI](https://github.com/Sarmkadan/blazor-shadcn/actions/workflows/ci.yml/badge.svg)](https://github.com/Sarmkadan/blazor-shadcn/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BlazorShadcn.svg)](https://www.nuget.org/packages/BlazorShadcn)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

shadcn/ui-style copy-paste Blazor components. Not a component library you depend on - source code you own.

Every component is a plain `.razor` file styled with Tailwind CSS that you copy into your project and change however you like. No black-box NuGet, no CSS overrides fight, no waiting for upstream to expose a parameter.

## Features

- **You own the code.** Components live in your repo. Edit them freely.
- **1:1 shadcn/ui port.** Same variants, same sizes, same CSS variables, same accessibility behaviour.
- **`Cn()` with Tailwind conflict resolution.** A `tailwind-merge` port so `Class="px-8"` actually overrides `px-4`.
- **`VariantConfig` (cva port).** Declare variants as data, resolve to classes in one call.
- **Attribute splatting done right.** `id`, `aria-*`, `data-*` and event handlers flow to the root element; `class` is merged, never clobbered.
- **Dark mode out of the box.** `IThemeService` toggles `<html class="dark">` and persists the choice; components only use CSS variables.
- **Registry.** Each component declares its files and dependencies, ready for a `dotnet shadcn add` CLI.
- **Server, WebAssembly and Auto render modes.** Nothing here assumes a specific hosting model.

## Quick start

```razor
@* Components/Ui/Button/Button.razor is copied into your project *@
<Button Variant="ButtonVariant.Destructive" Size="ButtonSize.Sm" Class="w-full" @onclick="Delete">
    Delete account
</Button>
```

Behind the scenes a component is ~30 lines:

```csharp
public partial class Button : ShadcnComponentBase
{
    private static readonly VariantConfig Variants = new()
    {
        Base = "inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors " +
               "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50",
        Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["variant"] = new Dictionary<string, string>
            {
                ["default"] = "bg-primary text-primary-foreground hover:bg-primary/90",
                ["destructive"] = "bg-destructive text-destructive-foreground hover:bg-destructive/90",
                ["outline"] = "border border-input bg-background hover:bg-accent hover:text-accent-foreground",
            },
            ["size"] = new Dictionary<string, string>
            {
                ["default"] = "h-9 px-4 py-2",
                ["sm"] = "h-8 rounded-md px-3 text-xs",
                ["lg"] = "h-10 rounded-md px-8",
            },
        },
        DefaultVariants = new Dictionary<string, string> { ["variant"] = "default", ["size"] = "default" },
    };

    [Parameter] public ButtonVariant Variant { get; set; }
    [Parameter] public ButtonSize Size { get; set; }

    private string? CssClass => ClassValue(Variants.Resolve(new Dictionary<string, string?>
    {
        ["variant"] = Variant.ToString().ToLowerInvariant(),
        ["size"] = Size.ToString().ToLowerInvariant(),
    }));
}
```

## Installation

1. Add Tailwind CSS to your Blazor project (standalone CLI or PostCSS) and the shadcn CSS variables to your `app.css`.
2. Copy the `core` files into your project:
   - `Utils/ClassNames.cs`
   - `Variants/VariantConfig.cs`
   - `Components/ShadcnComponentBase.cs`
3. Copy the components you need from `src/blazor-shadcn/Components/<Name>/`.
4. Optionally register theming: `builder.Services.AddScoped<IThemeService, ThemeService>();`

A CLI (`dotnet tool install -g BlazorShadcn.Cli` then `dotnet shadcn add button`) that automates steps 2-3 is planned; the registry it reads from already lives in this repo.

If you prefer a package for the core primitives only:

```bash
dotnet add package BlazorShadcn
```

## API overview

| Type | Purpose |
|------|---------|
| `ClassNames.Cn(params object?[])` | Join classes, drop falsy, resolve Tailwind conflicts (last wins). |
| `ClassNames.TwMerge(string)` | Conflict resolution only, for pre-joined strings. |
| `VariantConfig` | `Base`, `Variants`, `DefaultVariants`, `CompoundVariants`; `Resolve(selection, class)` / `Compile()`. |
| `ShadcnComponentBase` | `Class`, `ChildContent`, `AdditionalAttributes`; `ClassValue(...)`, `AttributesWithoutClass()`. |
| `IComponentRegistry` | `GetAll()`, `Find(name)`, `ResolveWithDependencies(names)`. |
| `RegistryEntry` | Name, type (`Core`/`Ui`/`Service`/`Block`), files, registry deps, package deps. |
| `IThemeService` | `Mode`, `Resolved`, `InitializeAsync`, `SetModeAsync`, `ToggleAsync`, `ResolvedChanged`. |

## Roadmap

- [ ] `ClassNames` with full tailwind-merge conflict groups
- [ ] `VariantConfig` resolver + compound variants
- [ ] Core components: Button, Badge, Card, Input, Label, Textarea, Checkbox, Switch, Separator, Skeleton
- [ ] Overlay components: Dialog, Sheet, Popover, Tooltip, DropdownMenu (focus trap, Escape, outside click)
- [ ] Form components: Select, RadioGroup, Form with `EditContext` integration
- [ ] `ThemeService` + `ThemeToggle`
- [ ] `dotnet shadcn` CLI reading the registry
- [ ] Docs site with live previews

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) - Copyright (c) 2026 Vladyslav Zaiets
