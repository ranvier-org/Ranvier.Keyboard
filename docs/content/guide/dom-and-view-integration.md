---
title: DOM & Ranvier.View adapter
description: Decode Ranvier.Dom.KeyboardEvent with zero string-switch overhead in browser fickl apps and minimal interop in native Ranvier.View.WebCore hosts.
order: 5
---

`Ranvier.Keyboard.Dom` (for `.NET` / `Ranvier.View.WebCore`) and `Fable.Ranvier.Keyboard.Dom` (for browser `fickl`) expose the same `Ranvier.Keyboard.Dom.KeyboardEvent` module over `Ranvier.Dom.KeyboardEvent`.

## `KeyboardEvent` adapter API

```fsharp
namespace Ranvier.Keyboard.Dom

[<RequireQualifiedAccess>]
module KeyboardEvent =
    val physicalCode : ev: Ranvier.Dom.KeyboardEvent -> PhysicalKeyCode
    val modifiers    : ev: Ranvier.Dom.KeyboardEvent -> KeyModifiers
    val logicalKey   : ev: Ranvier.Dom.KeyboardEvent -> LogicalKey
    val toKeyChord   : ev: Ranvier.Dom.KeyboardEvent -> KeyChord
    val toKeyPress   : ev: Ranvier.Dom.KeyboardEvent -> KeyPress
    val isEscape     : ev: Ranvier.Dom.KeyboardEvent -> bool
```

All six functions are total and null-safe (returning `UnidentifiedCode`, `KeyModifiers.None`, `LogicalKey.Unidentified ""`, or `false` when `ev` is `null`).

### Why use `KeyboardEvent` instead of reading `ev.key` directly?

1. **Guarded `AltGraph` interop on native WebCore hosts**: Querying `ev.getModifierState "AltGraph"` on every keystroke crosses the native WebCore P/Invoke boundary and pins the `"AltGraph"` string. `KeyboardEvent.modifiers` checks `if ev.altKey || ev.ctrlKey` first — so plain typing, `Shift`+letter, and `Meta` shortcuts never call `getModifierState`.
2. **Zero-cost `ev.code` in `fickl` JavaScript**: Under `FABLE_COMPILER`, `KeyboardEvent.physicalCode` is an `inline` function that normalizes `""` -> `UnidentifiedCode` and legacy `"OSLeft"` / `"OSRight"` -> `MetaLeft` / `MetaRight`, then unboxes `ev.code` directly as `PhysicalKeyCode` (`[<StringEnum>]`) with no lookup table in the JS bundle.
3. **SIMD `ev.key` & `ev.code` classification on `.NET`**: Native WebCore key events decode `ev.key` and `ev.code` via the SIMD span tables in `Ranvier.Keyboard` and normalize `Shift` + printable characters and Windows `Ctrl+Alt` -> `AltGraph` in one pass.
4. **Fast `Escape` short-circuit (`KeyboardEvent.isEscape`)**: Checks `!ctrlKey && !altKey && !shiftKey && !metaKey && (ev.key = "Escape" || ev.key = "Esc")` with zero allocations and without calling `getModifierState`.

## Using with `Ranvier.View.Dsl`

Attach typed `onKeydown` handlers on elements in `Ranvier.View.Dsl` components and convert `Ranvier.Dom.KeyboardEvent` at the handler boundary:

```fsharp
open Ranvier.Dom
open Ranvier.View
open Ranvier.View.Dsl
open Ranvier.Keyboard
open Ranvier.Keyboard.KeyChord
open Ranvier.Keyboard.Dom

type CommandPalette =
    { Query: Signal<string>
      Submit: string -> unit
      Close: unit -> unit }

let paletteInput: Component<CommandPalette> =
    Component.template
        (fun _ (model: CommandPalette) -> model)
        (input (
            input.type' "text",
            input.value (fun (m: CommandPalette) -> m.Query.Value),
            input.onInput (fun (m: CommandPalette) (ev: Event) ->
                let el = ev.target :?> HTMLInputElement
                m.Query.Value <- el.value),
            input.onKeydown (fun (m: CommandPalette) (ev: KeyboardEvent) ->
                if KeyboardEvent.isEscape ev then
                    ev.preventDefault ()
                    m.Close ()
                else
                    match KeyboardEvent.toKeyChord ev with
                    | ClassifiedKey (Whitespace Enter, mods, _) when mods = KeyModifiers.None ->
                        ev.preventDefault ()
                        m.Submit m.Query.Peek
                    | CtrlC0 C0Control.Nak -> // <C-u>: clear query
                        ev.preventDefault ()
                        m.Query.Value <- ""
                    | _ -> ())
        ))
```

Because `CommandPalette`'s domain callbacks (`Submit`, `Close`) and any downstream keymap engine operate on `KeyChord` rather than `Ranvier.Dom.KeyboardEvent`, you can unit-test all keyboard behavior headlessly by passing chords built with `KeyChord.parse` or `KeyChord.ofChar` without constructing DOM events:

```fsharp
open Ranvier.Keyboard

// Headless test in a project referencing only Ranvier.Keyboard:
let sequence = KeyChord.parse "ggVG"
```

## Sharing view code across `.NET` and `fickl`

In multi-targeted repositories (for example, a native `Ranvier.View.WebCore.Sdl` desktop app + a browser `fickl` web app):

- Reference `Ranvier.Keyboard.Dom` in the `.NET` `.fsproj`.
- Reference `Fable.Ranvier.Keyboard.Dom` in the browser `.fsproj` compiled by `dotnet fickl`.
- Keep the exact same `open Ranvier.Keyboard` and `open Ranvier.Keyboard.Dom` imports in shared `.fs` view files.
