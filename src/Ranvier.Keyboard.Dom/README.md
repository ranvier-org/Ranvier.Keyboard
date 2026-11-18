# Ranvier.Keyboard.Dom / Fable.Ranvier.Keyboard.Dom

Shared DOM `KeyboardEvent` adapter (`namespace Ranvier.Keyboard.Dom`, assembly `Ranvier.Keyboard.Dom`) bridging `Ranvier.Dom.KeyboardEvent` to `Ranvier.Keyboard`.

## What This Adapter Contains

`Ranvier.Keyboard.Dom.KeyboardEvent` provides six zero-overhead functions over `Ranvier.Dom.KeyboardEvent`:

- `KeyboardEvent.physicalCode (ev: KeyboardEvent) : PhysicalKeyCode` — under `fickl`, an `inline` zero-cost read of `ev.code` (normalizing `""`/`null` to `UnidentifiedCode` and legacy `"OSLeft"`/`"OSRight"` to `MetaLeft`/`MetaRight`); on `.NET`, SIMD `PhysicalKeyCode.ofW3cCode`.
- `KeyboardEvent.modifiers (ev: KeyboardEvent) : KeyModifiers` — reads `ctrlKey`, `altKey`, `shiftKey`, `metaKey`, and **only calls `ev.getModifierState "AltGraph"` when `altKey || ctrlKey`**, avoiding P/Invoke / `GCHandle` string pinning overhead on unmodified keystrokes in native WebCore hosts.
- `KeyboardEvent.logicalKey (ev: KeyboardEvent) : LogicalKey` — parses `ev.key` into `LogicalKey`.
- `KeyboardEvent.toKeyChord (ev: KeyboardEvent) : KeyChord` — normalizes `ev` into a 30-bit packed `KeyChord` (including Shift+ASCII and Windows `Ctrl+Alt` -> `AltGraph` normalization).
- `KeyboardEvent.toKeyPress (ev: KeyboardEvent) : KeyPress` — combines the normalized `KeyChord` with `PhysicalKeyCode`.
- `KeyboardEvent.isEscape (ev: KeyboardEvent) : bool` — fast allocation-free check for an unmodified `Escape` / `Esc` key press.

## Which Package to Reference

| Scenario | Package to Reference |
|---|---|
| Native `.NET` UI host (`Ranvier.View.WebCore`, server/native `Ranvier.Dom`) | **`Ranvier.Keyboard.Dom`** (or `Ranvier.Keyboard.Dom.Traced` in traced builds) |
| Browser Fable (`fickl`) UI app (`Fable.Ranvier.Dom`, `Ranvier.View.Dom`) | **`Fable.Ranvier.Keyboard.Dom`** (or `Fable.Ranvier.Keyboard.Dom.Traced` in traced builds) |
| Pure key engine, keymap parser, `.fsx` config, SDL/HID input, IPC, or headless tests (no `Ranvier.Dom.KeyboardEvent`) | **`Ranvier.Keyboard`** alone (works on both `.NET` and `fickl` without any DOM dependency) |

Both `Ranvier.Keyboard.Dom` and `Fable.Ranvier.Keyboard.Dom` compile to the same assembly name (`Ranvier.Keyboard.Dom`) and namespace (`Ranvier.Keyboard.Dom`), so shared view / component source files compile against either package without `#if` directives.
