# Contributing to Ranvier.Keyboard

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) for commit messages and pull request titles:

```text
type(scope): summary
```

The scope is optional and must use lowercase letters, digits, `.`, `_`, `/` or `-`.
Supported contribution types are `build`, `bench`, `chore`, `ci`, `docs`, `feat`,
`fix`, `perf`, `refactor`, `revert`, `style` and `test`.

```text
feat(chord): add Vim key chord parser
fix(dom): guard AltGraph modifier query behind ctrlKey or altKey
```

Mark breaking changes with `!` before the colon, or a `BREAKING CHANGE:` footer.

## Before opening a pull request

Use `dotnet fsi --exec build.fsx -- dev setup` to restore the pinned tools and projects. Run `dotnet fsi --exec build.fsx -- ci` for the local equivalent of the CI gates. Individual checks use `test --group dotnet`, `fable` or `aot`; bare `test` runs all three.

For changes spanning this repository and `Ranvier` (`Ranvier.Dom` / `Fable.Ranvier.Dom`), use `dotnet fsi --exec build.fsx -- dev local [--ranvier-root <path>]` to switch to sibling project references via ignored `Local.Build.props`, and `dev packages` to return to pinned NuGet package references.

Use `dotnet fsi --exec build.fsx -- format` to apply Fantomas formatting, then `format --dry-format` to check it.

## EasyBuild release requirements

[EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt) calculates release versions from conventional commits (`feat` and `fix` cut releases; `chore(deps)` does not).

`src/Ranvier.Keyboard/CHANGELOG.md` updates `Ranvier.Keyboard.fsproj`, and `src/Ranvier.Keyboard.Dom/CHANGELOG.md` updates `Ranvier.Keyboard.Dom.fsproj` and `Fable.Ranvier.Keyboard.Dom.fsproj` in lockstep. Keep `core.autocrlf=false` (LF line endings) when running local ShipIt dry runs.
