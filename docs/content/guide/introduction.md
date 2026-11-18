---
title: Installation & packages
description: Choose between Ranvier.Keyboard, Ranvier.Keyboard.Dom, and Fable.Ranvier.Keyboard.Dom across .NET, WebCore, and browser Fable apps.
order: 1
---

:::info
Pre-1.0 — the APIs of `Ranvier.Keyboard` may evolve before `1.0.0`.
:::

## Installation

Choose the package matching the layer you are building:

```bash frame="terminal"
# 1. Core DOM-free keyboard model (.NET 8, .NET 10 NativeAOT, and fickl / Fable)
dotnet add package Ranvier.Keyboard --version 0.1.0

# 2. Native .NET / Ranvier.View.WebCore DOM KeyboardEvent adapter
dotnet add package Ranvier.Keyboard.Dom --version 0.1.0

# 3. Browser Fable (fickl) DOM KeyboardEvent adapter
dotnet add package Fable.Ranvier.Keyboard.Dom --version 0.1.0
```

Every package has a `.Traced` twin (`Ranvier.Keyboard.Traced`, `Ranvier.Keyboard.Dom.Traced`, `Fable.Ranvier.Keyboard.Dom.Traced`) for traced `Ranvier.Traced` graph builds.

## Which package should I reference?

`Ranvier.Keyboard` separates **pure keyboard & text primitives** from **`Ranvier.Dom.KeyboardEvent` decoding** so domain, input, and scripting projects never take a DOM dependency:

| Package | Dependencies | Target host | When to reference it |
|---|---|---|---|
| `Ranvier.Keyboard` | None (`Fable.Core` attributes only) | Both `.NET` (`net10.0`, `net8.0`) and `fickl` (JavaScript) | **Default for every non-DOM project.** Modal editing engines, keymap registries, `.fsx` configuration DSLs, SDL / USB HID native input loops, BAREWire / IPC messages, CLI/TUI tools, and headless unit tests. |
| `Ranvier.Keyboard.Dom` | `Ranvier.Keyboard` + `Ranvier.Dom` | Native `.NET` (`Ranvier.View.WebCore`, server/native DOM hosts) | **Native UI shells** that receive `Ranvier.Dom.KeyboardEvent` and decode it via `Ranvier.Keyboard.Dom.KeyboardEvent`. |
| `Fable.Ranvier.Keyboard.Dom` | `Ranvier.Keyboard` + `Fable.Ranvier.Dom` | Browser `fickl` / Fable apps | **Browser UI shells** that receive `Ranvier.Dom.KeyboardEvent` and decode it via `Ranvier.Keyboard.Dom.KeyboardEvent`. |

### Layering in a multi-project application

In a structured application (such as an editor or workbench), keep `Ranvier.Keyboard.Dom` at the outermost shell boundary and pass `KeyChord` or `KeyPress` values inward:

```text
Ranvier.Dom.KeyboardEvent  (WebCore / Browser DOM boundary)
        │
        ▼  Ranvier.Keyboard.Dom / Fable.Ranvier.Keyboard.Dom
    KeyPress / KeyChord    (30-bit packed struct, zero DOM references)
        │
        ├──► Modal / Shortcut Engine   (references Ranvier.Keyboard only)
        ├──► User Scripting (.fsx)     (references Ranvier.Keyboard only)
        └──► Headless Unit Tests       (references Ranvier.Keyboard only)
```

Both `Ranvier.Keyboard.Dom` and `Fable.Ranvier.Keyboard.Dom` compile to the same assembly name (`Ranvier.Keyboard.Dom`) and expose the same `Ranvier.Keyboard.Dom.KeyboardEvent` module, so isomorphic `Ranvier.View.Dsl` components compile unchanged across native desktop and browser builds without `#if FABLE_COMPILER` guards.

## Target & runtime guarantees

- **`.NET 10` & NativeAOT**: Built with `--reflectionfree` (`RanvierAotClean=true`) and verified in CI with `PublishAot=true`.
- **Three-tier SIMD on `.NET 8+`**: Automatically selects `Vector512`, `Vector256`, or `Vector128` hardware intrinsics (with exact scalar fallback) for W3C `code` / `key` lookup, ASCII grapheme cluster runs, case transforms, C0 control scanning, and Vim chord sequence parsing.
- **Zero-cost `fickl` JavaScript representation**: `PhysicalKeyCode` compiles to `[<StringEnum(CaseRules.None)>]` (its literal W3C `KeyboardEvent.code` string), and `PackedKey` compiles to `[<Erase>]` (`number`), avoiding wrapper allocations on hot browser event paths.

Next: [Packed chords & keymaps](chords-and-keymaps.md).
