# blazor-shadcn

shadcn/ui-style copy-paste Blazor components. The product is source code the consumer owns, not a NuGet dependency. The `BlazorShadcn` project holds the shared primitives (class merging, variants, base component, registry, theming) and the component sources that the registry copies into user projects.

## Commands

```bash
dotnet build                                   # full solution, warnings are errors
dotnet test                                    # xUnit (+ bUnit for components)
dotnet test --filter FullyQualifiedName~ClassNames   # single test class
dotnet pack src/blazor-shadcn/BlazorShadcn.csproj -c Release -o artifacts
dotnet format --verify-no-changes              # style check, matches .editorconfig
```

Target framework: net9.0. Local machine may only have the .NET 10 SDK; building net9.0 works (targeting pack from NuGet), running tests needs a 9.x runtime or `DOTNET_ROLL_FORWARD=Major`.

## Layout

```
src/blazor-shadcn/
  Utils/ClassNames.cs            cn() + tailwind-merge port (conflict resolution)
  Variants/VariantConfig.cs      cva port: base + axes + defaults + compound variants
  Components/ShadcnComponentBase.cs  Class param, attribute splatting, class merging
  Components/<Name>/<Name>.razor     one folder per shadcn component (Button, Card, Dialog...)
  Registry/IComponentRegistry.cs     catalog: files, registry deps, package deps
  Theming/IThemeService.cs           dark/light/system via <html class="dark"> + localStorage
  _Imports.razor
tests/blazor-shadcn.Tests/         xUnit; component tests use bUnit
.github/workflows/ci.yml           build + test + pack on push/PR to main
```

## Architecture

- **Copy-paste first.** Every component must work when its files are copied into a consumer project with only `Microsoft.AspNetCore.Components.Web` and Tailwind present. No hidden dependency on library internals beyond the `core` registry entry.
- **Tailwind classes are the API.** Port shadcn/ui components 1:1: same variant names (`default`, `destructive`, `outline`, `secondary`, `ghost`, `link`), same sizes (`default`, `sm`, `lg`, `icon`), same CSS variables (`--background`, `--primary`, `--radius`, ...).
- **`ClassNames.Cn`** is the single merge point. Consumer `Class` always comes last and wins on conflict. `TwMerge` must know Tailwind conflict groups (spacing, sizing, color, typography, layout).
- **`VariantConfig`** is data, not code; components declare it as a `static readonly` field and call `Resolve`.
- **`ShadcnComponentBase`** handles `Class`, `ChildContent`, `AdditionalAttributes`. Never re-declare these in a component.
- **Registry** is the source of truth for the future CLI (`dotnet shadcn add button`). Dependency order: `core` -> ui -> block.
- **Theming** is CSS-variable based; components never branch on theme in C#.

## Conventions

- File-scoped namespaces, `sealed` by default, XML docs on all public API (`GenerateDocumentationFile` is on, missing docs fail the build).
- `TreatWarningsAsErrors=true`; do not suppress analyzers in code, adjust `.editorconfig` with a comment instead.
- Stubs throw `NotImplementedException`; replace them, do not add parallel implementations.
- Component folder = component name in PascalCase; registry name = kebab-case.
- Tests: `<Subject>Tests.cs`, method names `Method_Scenario_Expectation`.
- Conventional commits (`feat:`, `fix:`, `docs:`, `test:`, `chore:`). No `Co-Authored-By` trailers.
- Do not add a11y shortcuts: keep `role`, `aria-*`, keyboard handling from the upstream Radix/shadcn implementation.
