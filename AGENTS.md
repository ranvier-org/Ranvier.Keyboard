# AGENTS.md

## Repository

`Ranvier.Keyboard` holds DOM-free keyboard primitives and `Ranvier.Dom.KeyboardEvent` adapters for `.NET` (`net10.0`, `net8.0`, NativeAOT) and Fable (`fickl`).

- `src/Ranvier.Keyboard`: DOM-free keyboard model (`IsoAndW3cKeys.fs`, `UnicodeAndControl.fs`, `DeadKeysAndDigraphs.fs`, `KeyChord.fs`), packed with its Fable sources.
- `src/Ranvier.Keyboard.Dom`: native `.NET` `Ranvier.Dom.KeyboardEvent` adapter (`KeyboardEvent.fs`).
- `src/Fable.Ranvier.Keyboard.Dom`: browser `fickl` `Ranvier.Dom.KeyboardEvent` adapter compiling the same `KeyboardEvent.fs` against `Fable.Ranvier.Dom`.
- `tests/Ranvier.Keyboard.Tests`: the Expecto unit test suite, run untraced and traced.
- `tests/Ranvier.Keyboard.Smoke/Checks.fs`: reflection-free checks compiled by `tests/Ranvier.Keyboard.AotSmoke` (NativeAOT) and `fable/Ranvier.Keyboard.Smoke.Fable` (fickl under Node).

Every package also packs as `<PackageId>.Traced` with `-p:RanvierTrace=true`. Always pass `--exec` when invoking `dotnet fsi` (`dotnet fsi --exec build.fsx -- ci`).

## Library constraints

- All library projects set `RanvierAotClean=true` (`--reflectionfree` + `IsAotCompatible=true` on `net10.0`): never use `sprintf "%A"` or reflection-based formatting.
- SIMD code (`Vector512` / `Vector256` / `Vector128`) is gated behind `#if !FABLE_COMPILER`, never reads past span bounds, verifies ASCII high bits (`0xFF80 = 0`), and retains a scalar fallback.
- `fickl` sources avoid unsupported BCL APIs (`StringInfo`, `s.EnumerateRunes()`, `CharUnicodeInfo.GetDigitValue`, `s.Normalize(NormalizationForm.FormC)`).

## Before reporting work as done

`dotnet fsi --exec build.fsx -- ci` runs every gate (`fantomas --check`, `.NET` untraced and traced tests, `fickl` Node smoke, `NativeAOT` publish and execution, and `.nupkg` verification).
