# Contributing

Thanks for helping build blazor-shadcn.

## Setup

```bash
git clone https://github.com/Sarmkadan/blazor-shadcn.git
cd blazor-shadcn
dotnet build
dotnet test
```

Requires the .NET 9 SDK (or newer with the net9.0 targeting pack).

## Workflow

1. Open an issue first for new components or breaking API changes.
2. Branch from `main`: `feat/<component>`, `fix/<short-description>`.
3. Keep PRs focused: one component or one fix per PR.
4. `dotnet build` must pass with zero warnings (`TreatWarningsAsErrors` is on).
5. Add or update tests in `tests/blazor-shadcn.Tests` (bUnit for components).
6. Use conventional commit messages: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.

## Adding a component

- Port the shadcn/ui component 1:1 where possible: same variant names, same Tailwind classes, same a11y attributes.
- Inherit from `ShadcnComponentBase`; declare variants with `VariantConfig`; merge classes with `ClassNames.Cn`.
- Register the component in the registry with its file list and dependencies.
- Add a short usage example to the component's XML docs.

## Code style

`.editorconfig` is authoritative. File-scoped namespaces, `var` when the type is obvious, explicit `public` API with XML docs.

## License

By contributing you agree your contributions are licensed under the MIT License.
