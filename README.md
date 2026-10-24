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

## Core primitives

shadcn/ui components are styled with [Tailwind CSS](https://tailwindcss.com) utility classes: `px-4` is 1rem of horizontal padding, `text-sm` is a small font size. You do not need to know either library in depth to use this section. Two helpers do the bookkeeping:

- `ClassNames` builds one class string from several sources and removes conflicting Tailwind utilities.
- `VariantConfig` maps a component option such as `variant="destructive"` to the classes it should add.

Both live in `Utils/ClassNames.cs` and `Variants/VariantConfig.cs` and depend only on the .NET base library.

### Cn()

`ClassNames.Cn(params object?[] inputs)` is the Blazor version of shadcn's `cn()` helper. Pass it any mix of class sources. It flattens them, drops the empty ones, and resolves Tailwind conflicts so that the last class wins.

```csharp
ClassNames.Cn("px-2 py-1", "px-4");   // "py-1 px-4"
```

What it accepts:

| Input | Behaviour |
|-------|-----------|
| `string` | Split on whitespace, so `"px-2  py-1"` is two classes. |
| `null`, `false` | Dropped. |
| `true` | Ignored. |
| `IEnumerable` (list, array) | Flattened. `null` and `false` items inside it are dropped too. |
| Any other object | `ToString()`, so `Cn(5)` gives `"5"`. |

Exact duplicates are removed as well, and the last occurrence keeps its position.

#### Conditional classes

C# has no clsx-style `condition && "class"`. A `bool && string` expression does not compile, so this line is an error:

```csharp
// Does not compile: bool && string
ClassNames.Cn("px-2", isActive && "bg-primary");
```

Use a ternary that yields `null` (or `false`) when the condition is off:

```csharp
ClassNames.Cn("px-2", isActive ? "bg-primary" : null);
// isActive == true  -> "px-2 bg-primary"
// isActive == false -> "px-2"
```

#### Examples

| Call | Result | Why |
|------|--------|-----|
| `Cn("px-2 py-1", "px-4")` | `py-1 px-4` | Both set horizontal padding (`px`). The later `px-4` wins. |
| `Cn("p-2", "px-4")` | `p-2 px-4` | `p` sets all four sides, `px` only left and right. Both survive, and `px-4` wins on the horizontal sides. |
| `Cn("text-sm", "text-red-500")` | `text-sm text-red-500` | `text-sm` is a font size and `text-red-500` is a color. Different groups, so both stay. |
| `Cn("hover:bg-primary", "bg-secondary")` | `hover:bg-primary bg-secondary` | The `hover:` prefix is part of the scope. The hover rule and the base rule never conflict. |
| `Cn("w-4", "w-[32px]")` | `w-[32px]` | Arbitrary values are classified like named ones. Both are widths (`w`), so the later one wins. |

### TwMerge()

`ClassNames.TwMerge(string classes)` runs only the conflict step on a string that is already joined. `Cn` calls it for you, so reach for `TwMerge` only when you already have a single class string.

Two utilities conflict when they are in the same group and have the same variant scope. The groups follow `tailwind-merge`:

| Area | Groups | Example that conflicts |
|------|--------|------------------------|
| Spacing | `p` `px` `py` `pt` `pr` `pb` `pl`, `m` `mx` `my` `mt` `mr` `mb` `ml`, `gap` `gap-x` `gap-y` | `px-2` and `px-4` |
| Sizing | `w` `h` `size` `min-w` `max-w` `min-h` `max-h` | `w-4` and `w-[32px]` |
| Typography | `text-size`, `text-color`, `text-align`, `font-weight`, `font-family`, `leading`, `tracking` | `text-sm` and `text-lg`; `font-medium` and `font-bold` |
| Colors | `bg`, `text-color`, `border-color`, `ring-color`, `shadow-color` | `bg-primary` and `bg-destructive` |
| Borders | `border-w`, `border-style`, `border-x` to `border-l`, `border-color-*`, `rounded`, `rounded-t` to `rounded-bl` | `border` and `border-2`; `rounded-md` and `rounded-lg` |
| Layout | `display`, `position`, `flex`, `flex-direction`, `flex-wrap`, `inset`, `top` `right` `bottom` `left`, `z`, `overflow`, `items`, `justify`, `self` | `flex` and `inline-flex`; `relative` and `absolute` |
| Effects | `shadow`, `shadow-color`, `ring-w`, `ring-color`, `ring-inset`, `ring-offset-w`, `ring-offset-color`, `opacity` | `ring-1` and `ring-2`; `shadow` and `shadow-sm` |

**Shorthands override the sides they cover.** A later shorthand removes the earlier, more specific utilities in the same scope. The reverse order keeps both:

```csharp
ClassNames.TwMerge("px-2 p-4");   // "p-4"        p covers px, so px-2 is removed
ClassNames.TwMerge("p-4 px-2");   // "p-4 px-2"   px is more specific and comes later, so both stay
```

The same rule applies to the other pairs: `size-*` covers `w-*` and `h-*`, `inset-*` covers `top-*` through `left-*`, `gap-*` covers `gap-x-*` and `gap-y-*`, and `rounded-*` covers every corner.

Scope rules:

- Variant order does not matter. `hover:md:px-2` and `md:hover:px-4` conflict.
- A variant changes the scope. `hover:px-2` and `px-4` do not conflict.
- The important marker is part of the scope. `p-2 !px-4` keeps both.

#### Limitations

- Only the utilities in the table above are recognized. Anything else, such as plugin classes like `scrollbar-hide`, is kept verbatim. It never conflicts with another class and is removed only when an exact duplicate appears later.
- `bg-*` is one group, so `bg-cover` and `bg-primary` conflict with each other. Put a background image and a background color on different elements, or use an arbitrary value.
- Your Tailwind config is not read. Only the built-in text sizes (`xs` to `9xl`) count as sizes, so a custom size such as `text-display` is treated as a color and will not replace `text-sm`. Use an arbitrary value such as `text-[2.5rem]` instead.

### VariantConfig

`VariantConfig` is a port of `class-variance-authority` (cva). A component describes its options as data, and each call to `Resolve` turns the caller's choices into classes. It has four properties:

- `Base`: classes that are always applied.
- `Variants`: each axis (for example `variant` or `size`) maps its value names to classes.
- `DefaultVariants`: the value used for each axis when the caller does not choose one.
- `CompoundVariants`: classes applied only when several axis values match at the same time.

This example is a simplified version of the `Badge` component in `Components/Badge/Badge.razor.cs`, with a `size` axis and a compound variant added:

```csharp
using BlazorShadcn.Variants;

public static class Badges
{
    public static readonly VariantConfig Badge = new()
    {
        Base = "inline-flex items-center rounded-md border font-semibold transition-colors",
        Variants = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["variant"] = new Dictionary<string, string>
            {
                ["default"] = "border-transparent bg-primary text-primary-foreground",
                ["secondary"] = "border-transparent bg-secondary text-secondary-foreground",
                ["outline"] = "text-foreground",
            },
            ["size"] = new Dictionary<string, string>
            {
                ["default"] = "px-2.5 py-0.5 text-xs",
                ["sm"] = "px-2 py-0 text-[10px]",
            },
        },
        DefaultVariants = new Dictionary<string, string>
        {
            ["variant"] = "default",
            ["size"] = "default",
        },
        CompoundVariants =
        [
            new CompoundVariant(
                new Dictionary<string, string> { ["variant"] = "outline", ["size"] = "sm" },
                "border-dashed"),
        ],
    };
}
```

Resolving it:

```csharp
Badges.Badge.Resolve();
// "inline-flex items-center rounded-md border font-semibold transition-colors
//  border-transparent bg-primary text-primary-foreground px-2.5 py-0.5 text-xs"

Badges.Badge.Resolve(
    new Dictionary<string, string?> { ["variant"] = "secondary" },
    className: "ml-2");
// "... border-transparent bg-secondary text-secondary-foreground px-2.5 py-0.5 text-xs ml-2"

Badges.Badge.Resolve(new Dictionary<string, string?> { ["variant"] = "outline", ["size"] = "sm" });
// "... text-foreground px-2 py-0 text-[10px] border-dashed"
```

A selection value of `null` means "use the default". Axes you leave out also use their default, so `Resolve()` with no arguments is the default badge.

#### CompoundVariants

A compound variant applies only when every one of its conditions matches the effective selection, meaning the caller's choices after defaults are filled in. In the example above, `border-dashed` is added only for `variant="outline"` together with `size="sm"`. a selection of only `size = "sm"` does not add it, because `variant` falls back to `default`.

#### Resolve() or Compile()

- `Resolve(selection, className)` is what components call. The first call compiles the configuration and stores the result on that instance, so later calls skip the setup. Keep the `VariantConfig` in a `static readonly` field, as the components do, and call `Resolve`.
- `Compile()` validates the configuration and returns the delegate itself, of type `Func<IReadOnlyDictionary<string, string?>?, string?, string>`. You only need it when you want to hold the delegate directly, for example in a field of another type. It does the same work `Resolve` caches.

Validation happens at `Compile()`, which means an invalid configuration throws on the first `Resolve` call, not when the object is created. The dictionaries you pass in are stored by reference, not copied, so do not change them after the first call.

#### Exceptions

| Situation | Exception | Message (abridged) |
|-----------|-----------|--------------------|
| `DefaultVariants` names an axis that is not in `Variants` | `InvalidOperationException` | `DefaultVariants references variant axis 'color' which is not defined in Variants.` |
| `DefaultVariants` names a value that the axis does not have | `InvalidOperationException` | `DefaultVariants references value 'xl' which is not defined for variant axis 'size'.` |
| A compound condition names an unknown axis or value | `InvalidOperationException` | `CompoundVariants references value 'ghost' ...` |
| An axis has no values, or a value has a `null` class string | `InvalidOperationException` | `VariantConfig axis 'size' has no values.` |
| A selection key is not an axis, e.g. `colour` | `ArgumentException` (parameter `selection`) | `Unknown variant axis 'colour'. Known axes: variant, size.` |
| A selection value is not on its axis, e.g. `size = "xl"` | `ArgumentException` (parameter `selection`) | `Value 'xl' is not defined for variant axis 'size'. Allowed values: default, sm.` |
| A selection key is null or blank | `ArgumentException` (parameter `selection`) | |
| A `CompoundVariant` has no conditions | `ArgumentException` at construction | `A compound variant with no conditions would always apply.` |

Mistakes in `selection` are reported on every call. Mistakes in the configuration are reported once, when it is first compiled.

## Components

Each folder under `src/blazor-shadcn/Components/` is one shadcn/ui component, ported 1:1 (same variant names, sizes and CSS variables). `Card` and `Skeleton` are families: the sub-components are separate files in the same folder.

| Component | Parameters | shadcn/ui source |
|-----------|------------|------------------|
| `Button` | `Variant` (`ButtonVariant`: `Default`, `Destructive`, `Outline`, `Secondary`, `Ghost`, `Link`), `Size` (`ButtonSize`: `Default`, `Sm`, `Lg`, `Icon`), `Type` (default `button`), `Disabled` | `button.tsx` |
| `Badge` | `Variant` (`BadgeVariant`: `Default`, `Secondary`, `Destructive`, `Outline`) | `badge.tsx` |
| `Card`, `CardHeader`, `CardTitle`, `CardDescription`, `CardContent`, `CardFooter` | `CardTitle.As` (default `h3`); the rest take only `Class` and `ChildContent` | `card.tsx` |
| `Input` | `Value` / `ValueChanged` (`@bind-Value`), `Type` (default `text`), `Placeholder`, `Disabled`, `ReadOnly`, `UpdateOnInput` (default: update on `change`) | `input.tsx` |
| `Label` | `For` (the `id` of the control it describes) | `label.tsx` |
| `Separator` | `Orientation` (`SeparatorOrientation`: `Horizontal`, `Vertical`), `Decorative` (default `true`) | `separator.tsx` |
| `Skeleton`, `SkeletonText` | `SkeletonText.Lines` (default `3`), `SkeletonText.LineClass` (default `h-4`) | `skeleton.tsx` |

Every component inherits `ShadcnComponentBase`, so it shares the same attribute contract: `Class` (or a `class` attribute) is merged into the root element's classes with `Cn()` and wins Tailwind conflicts, while `id`, `aria-*`, `data-*` and event handlers such as `@onclick` are splatted onto the root element.

### Button

```razor
<Button Variant="ButtonVariant.Destructive" Size="ButtonSize.Sm" Class="mt-4" @onclick="Delete">
    Delete account
</Button>

@code {
    private void Delete()
    {
        // ...
    }
}
```

### Badge

```razor
<Badge Variant="BadgeVariant.Secondary">Beta</Badge>
<Badge Variant="BadgeVariant.Destructive" id="sync-status">Sync failed</Badge>
```

### Card

```razor
<Card Class="w-[380px]">
    <CardHeader>
        <CardTitle>Create project</CardTitle>
        <CardDescription>Deploy the app to your default region.</CardDescription>
    </CardHeader>
    <CardContent>
        <p>The project is created with the default settings.</p>
    </CardContent>
    <CardFooter class="justify-between">
        <Button Variant="ButtonVariant.Outline">Cancel</Button>
        <Button>Deploy</Button>
    </CardFooter>
</Card>
```

### Input and Label

`Label.For` renders the HTML `for` attribute, so it must match the `id` that is splatted onto the `<input>`:

```razor
<div class="grid gap-2">
    <Label For="email">Email</Label>
    <Input id="email" type="email" placeholder="you@example.com" @bind-Value="email" />
</div>

@code {
    private string? email;
}
```

`@bind-Value` updates `email` when the input loses focus. Set `UpdateOnInput="true"` to update on every keystroke.

### Separator

A vertical separator stretches to the height of its flex row:

```razor
<div class="flex h-5 items-center gap-4 text-sm">
    <span>Blog</span>
    <Separator Orientation="SeparatorOrientation.Vertical" />
    <span>Docs</span>
    <Separator Orientation="SeparatorOrientation.Vertical" />
    <span>Source</span>
</div>
```

### Skeleton

Size and shape come from `Class`. The skeleton is hidden from assistive technology (`aria-hidden="true"`), so give the loading region its own status text if screen reader users need it.

```razor
@if (isLoading)
{
    <Card Class="w-[380px]">
        <CardHeader>
            <Skeleton Class="h-5 w-40" />
            <Skeleton Class="h-4 w-64" />
        </CardHeader>
        <CardContent>
            <SkeletonText Lines="4" />
        </CardContent>
    </Card>
}
else
{
    <p>Profile loaded.</p>
}

@code {
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        // Replace with your data call.
        await Task.Delay(500);
        isLoading = false;
    }
}
```

## Registry and theming

### Registry

`IComponentRegistry` is the catalog behind the planned `dotnet shadcn` CLI. `ComponentRegistry.CreateDefault()` builds it from `DefaultRegistryEntries.All` (`src/blazor-shadcn/Registry/`). Each `RegistryEntry` lists its source files, the registry entries it depends on, and its NuGet package dependencies.

`ResolveWithDependencies` returns the install set in dependency order, dependencies first:

```csharp
using BlazorShadcn.Registry;

IComponentRegistry registry = ComponentRegistry.CreateDefault();

foreach (var entry in registry.ResolveWithDependencies(["login-form"]))
{
    Console.WriteLine(entry.Name);
}

// core
// card
// button
// input
// label
// login-form
```

Each entry has one of four types. The CLI does not exist yet, so the destinations below are the planned ones, matching the folder layout in this README:

| Type | Entries | Planned destination in your project |
|------|---------|-------------------------------------|
| `Core` | `core` | `Utils/`, `Variants/` and `Components/ShadcnComponentBase.cs` |
| `Ui` | `button`, `badge`, `card`, `input`, `label`, `separator`, `skeleton` | `Components/<Name>/` |
| `Service` | `theme-service` | `Theming/` (C# files) and `wwwroot/` (`theme.js`) |
| `Block` | `login-form` | `Components/Blocks/` |

The `login-form` block is registered, but its `Components/Blocks/LoginForm.razor` source is not in this repository yet.

### Theming

`IThemeService` switches between light, dark and system color schemes by toggling the `dark` class on `<html>`. Components only use CSS variables, so they never check the theme themselves.

Register the service as scoped, one instance per circuit or app:

```csharp
builder.Services.AddScoped<IThemeService, ThemeService>();
```

Call `InitializeAsync` once the component is interactive. Do it in `OnAfterRenderAsync` with `firstRender` set to `true`, in a component that runs interactively, such as your layout:

```razor
@inject IThemeService Theme

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Theme.InitializeAsync();
        }
    }
}
```

A toggle button uses `ToggleAsync` and re-renders when `ResolvedChanged` fires. The event also fires when the OS scheme changes while the mode is `System`:

```razor
@implements IDisposable
@inject IThemeService Theme

<Button Variant="ButtonVariant.Ghost" @onclick="Toggle">
    @(Theme.Resolved == ThemeMode.Dark ? "Switch to light" : "Switch to dark")
</Button>

@code {
    protected override void OnInitialized()
    {
        Theme.ResolvedChanged += OnResolvedChanged;
    }

    private Task Toggle() => Theme.ToggleAsync();

    private void OnResolvedChanged(ThemeMode _) => StateHasChanged();

    public void Dispose()
    {
        Theme.ResolvedChanged -= OnResolvedChanged;
    }
}
```

`ToggleAsync` and `SetModeAsync` call `InitializeAsync` themselves if it has not run yet. `InitializeAsync` is idempotent.

The JavaScript module is served from the library's static assets at `_content/BlazorShadcn/theme.js`. `ThemeService` imports that path directly, so if you copy the `theme-service` files into your project instead of referencing the package, change the path in `ThemeService.cs` to match where `theme.js` is served.

The preference is stored in `localStorage` under the key `blazor-shadcn-theme`, with the values `light` or `dark`. The key is removed for `System`, which is the default when nothing is stored.

**Prerendering caveat.** During prerendering there is no JavaScript interop, so `InitializeAsync` does nothing. The prerendered HTML has no `dark` class, and `Resolved` stays `Light`. Once the component is interactive, the stored preference is applied, so a user who chose dark may see a flash of light content on first load. The library does not ship a fix. If the flash matters, add a small inline script to `<head>` that reads `blazor-shadcn-theme` and sets the `dark` class before the first paint.

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
| `VariantConfig` | `Base`, `Variants`, `DefaultVariants`, `CompoundVariants`; `Resolve(selection?, className?)` / `Compile()`. |
| `ShadcnComponentBase` | `Class`, `ChildContent`, `AdditionalAttributes`; `ClassValue(...)`, `AttributesWithoutClass()`. |
| `IComponentRegistry` | `GetAll()`, `Find(name)`, `ResolveWithDependencies(names)`. |
| `RegistryEntry` | Name, type (`Core`/`Ui`/`Service`/`Block`), files, registry deps, package deps. |
| `IThemeService` | `Mode`, `Resolved`, `InitializeAsync`, `SetModeAsync`, `ToggleAsync`, `ResolvedChanged`. |

## Roadmap

- [ ] `ClassNames` with full tailwind-merge conflict groups
- [x] `VariantConfig` resolver + compound variants
- [x] Core components: Button, Badge, Card, Input, Label, Separator, Skeleton
- [ ] Core components: Textarea, Checkbox, Switch
- [ ] Overlay components: Dialog, Sheet, Popover, Tooltip, DropdownMenu (focus trap, Escape, outside click)
- [ ] Form components: Select, RadioGroup, Form with `EditContext` integration
- [x] `ThemeService`
- [ ] `ThemeToggle` component (the README shows a snippet; no component ships yet)
- [ ] `dotnet shadcn` CLI reading the registry
- [ ] Docs site with live previews

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) - Copyright (c) 2026 Vladyslav Zaiets
