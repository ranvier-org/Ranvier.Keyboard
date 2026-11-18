# Contributing to Ranvier.Keyboard

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) for commit messages and pull request titles:

```text
type(scope): summary
```

The scope is optional and must use lowercase letters, digits, `.`, `_`, `/` or `-` (no commas).
Supported contribution types are `build`, `bench`, `chore`, `ci`, `docs`, `feat`,
`fix`, `perf`, `refactor`, `revert`, `style` and `test`.

```text
feat(chord): add Meta-Shift tag parsing
fix(dom): guard getModifierState on null event
docs: add packed chord layout guide
```

Mark breaking changes with `!` before the colon, or a `BREAKING CHANGE:` footer.
Explain the migration in the commit body or pull request description.

The **Contribution / Conventional commits** check validates human pull request
titles; bot pull requests are exempt, matching the Ranvier workflow. Use squash
merging and preserve the conventional PR title as the squash commit subject.

## Before opening a pull request

Use `dotnet fsi --exec build.fsx -- dev setup` to restore the pinned tools and projects. Run `dotnet fsi --exec build.fsx -- ci` for the local equivalent of the CI gates. Individual checks use `test --group dotnet`, `fable` or `aot`; bare `test` runs all three:

- `dotnet`: runs `tests/Ranvier.Keyboard.Tests` in both untraced and `-p:RanvierTrace=true` configurations.
- `fable`: compiles `fable/Ranvier.Keyboard.Smoke.Fable` with `dotnet fickl` (`--noCache`) and runs the generated JavaScript under Node.js.
- `aot`: publishes `tests/Ranvier.Keyboard.AotSmoke` with `PublishAot=true` (`--reflectionfree` on `.NET 10`) and runs the native binary.

Build and link-check the documentation site with:

```bash
dotnet fsi --exec build.fsx -- docs
```

Pass `--watch` to serve live previews locally.

For changes spanning this repository and `Ranvier` (`Ranvier.Dom` / `Fable.Ranvier.Dom`), use
`dotnet fsi --exec build.fsx -- dev local [--ranvier-root <path>]`. This clones a missing
sibling checkout and enables project references using ignored `Local.Build.props`.
`dev packages` returns to pinned published NuGet references. CI always validates against
published packages.

Use `dotnet fsi --exec build.fsx -- format` to apply Fantomas formatting, and
`dotnet fsi --exec build.fsx -- format --dry-format` to verify it without rewriting files.

## EasyBuild release requirements

[EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt) calculates
release versions from conventional commits. `feat` and `fix` commits cut a release;
`docs`, `chore`, `ci`, `test` and `build` commits do not.

Each packaged project family has a `CHANGELOG.md`:

- `src/Ranvier.Keyboard/CHANGELOG.md` updates `Ranvier.Keyboard.fsproj` (`Ranvier.Keyboard` and `Ranvier.Keyboard.Traced`).
- `src/Ranvier.Keyboard.Dom/CHANGELOG.md` updates both `Ranvier.Keyboard.Dom.fsproj` and `Fable.Ranvier.Keyboard.Dom.fsproj` in lockstep (`Ranvier.Keyboard.Dom`, `Fable.Ranvier.Keyboard.Dom`, and their `.Traced` twins), declaring `depends_on` `src/Ranvier.Keyboard`.

ShipIt owns `<Version>` in every `.fsproj`; do not edit `<Version>` or generated changelog release sections by hand. Run local `bump --dry-run` checks only in an LF checkout (`core.autocrlf=false`).

After CI succeeds on `main`, `.github/workflows/shipit.yml` opens or updates the `release/main` pull request titled `chore: release ...`. Close and reopen that PR if `GITHUB_TOKEN` holds its `pull_request` checks, then squash-merge keeping the exact `chore: release ...` title so `.github/workflows/publish.yml` publishes the validated CI artifacts via NuGet Trusted Publishing.
