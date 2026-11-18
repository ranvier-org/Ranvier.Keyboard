# Ranvier.Keyboard

Standards-typed, SIMD-accelerated keyboard primitives and `Ranvier.Dom.KeyboardEvent` adapters for .NET (`net10.0`, `net8.0`, NativeAOT) and Fable (`fickl`).

## Packages & When to Use Which

| Package | Dependencies | Target Host | What It Contains & When to Use It |
|---|---|---|---|
| [`Ranvier.Keyboard`](src/Ranvier.Keyboard/README.md) | None (`Fable.Core` attributes only) | Both `.NET` and `fickl` (JavaScript) | **Core DOM-free keyboard model.** Contains `PhysicalKeyCode` (W3C `KeyboardEvent.code`, USB HID Usage Page `0x07`, ISO/IEC 9995-1 grid coordinates), `LogicalKey` (W3C `KeyboardEvent.key`), `KeyModifiers`, `ComposingAccent` / `DeadKey` (ISO/IEC 9995-3), `Rfc1345Digraph` (IETF RFC 1345 `<C-k>` mnemonics), `C0Control` (ECMA-48 / ISO 6429), `UnicodeChar` / `Grapheme` (Unicode UAX #29 extended grapheme cluster stepping & case transforms), and 30-bit packed `KeyChord` / `KeyPress` with Vim-style chord sequence parsing. **Use `Ranvier.Keyboard` alone** whenever you build modal editing engines, keymap tables, `.fsx` configuration scripts, CLI/TUI tools, SDL/USB HID input pipelines, IPC messages, or headless unit tests without pulling in any DOM dependency. |
| [`Ranvier.Keyboard.Dom`](src/Ranvier.Keyboard.Dom/README.md) | `Ranvier.Keyboard` + `Ranvier.Dom` | Native `.NET` (`Ranvier.View.WebCore`, server/native DOM hosts) | **Native `.NET` DOM event adapter** (`namespace Ranvier.Keyboard.Dom`). Provides `KeyboardEvent.physicalCode`, `KeyboardEvent.modifiers` (with a lazy `ctrlKey \|\| altKey` guard before calling `getModifierState "AltGraph"` to avoid P/Invoke pinning on plain keys), `KeyboardEvent.logicalKey`, `KeyboardEvent.toKeyChord`, `KeyboardEvent.toKeyPress`, and `KeyboardEvent.isEscape` over `Ranvier.Dom.KeyboardEvent`. **Add this package** in native `.NET` / `Ranvier.View.WebCore` UI applications that handle `Ranvier.Dom.KeyboardEvent`. |
| [`Fable.Ranvier.Keyboard.Dom`](src/Ranvier.Keyboard.Dom/README.md) | `Ranvier.Keyboard` + `Fable.Ranvier.Dom` | Browser `fickl` / Fable apps | **Browser Fable (`fickl`) DOM event adapter** (`namespace Ranvier.Keyboard.Dom`, assembly `Ranvier.Keyboard.Dom`). Exposes the exact same `Ranvier.Keyboard.Dom.KeyboardEvent` API compiled against `Fable.Ranvier.Dom` with zero-cost `[<StringEnum>]` `ev.code` reads in JavaScript. **Add this package** in browser `fickl` UI applications that handle `Ranvier.Dom.KeyboardEvent`. |

Every package also ships a `.Traced` twin (`Ranvier.Keyboard.Traced`, `Ranvier.Keyboard.Dom.Traced`, `Fable.Ranvier.Keyboard.Dom.Traced`) for traced Ranvier graph builds. All `.NET 10` builds are compiled with `--reflectionfree` and verified under `PublishAot=true`.

### Decision Rule at a Glance

1. **No `Ranvier.Dom.KeyboardEvent` at this layer?** (Domain models, Vim/modal state machines, keybinding parsers, `.fsx` user configs, BAREWire/IPC payloads, SDL scancodes, headless tests on either `.NET` or `fickl`):
   - Reference **`Ranvier.Keyboard`** only. It works identically on `.NET` and `fickl` with zero DOM dependencies.
2. **Handling `Ranvier.Dom.KeyboardEvent` in a native `.NET` / `Ranvier.View.WebCore` app?**
   - Reference **`Ranvier.Keyboard.Dom`** (which brings `Ranvier.Keyboard` and `Ranvier.Dom`).
3. **Handling `Ranvier.Dom.KeyboardEvent` in a browser `fickl` app?**
   - Reference **`Fable.Ranvier.Keyboard.Dom`** (which brings `Ranvier.Keyboard` and `Fable.Ranvier.Dom`). Isomorphic view code opens `Ranvier.Keyboard` and `Ranvier.Keyboard.Dom` unchanged on both targets.

## Architecture Highlights

- **Memory-safe three-tier SIMD (`Vector512` / `Vector256` / `Vector128` + scalar fallback)** on `.NET 8+`:
  - `PhysicalKeyCode.ofW3cCode` / `ofW3cCodeSpan` / `ofW3cCodeUtf8` and `LogicalKey.ofW3cKey` / `ofW3cKeySpan` / `ofW3cKeyUtf8` pack `2..15`-byte ASCII identifiers without out-of-bounds reads, verify high UTF-16 bits (`0xFF80 = 0`) so non-ASCII never aliases ASCII, and match precomputed 128-entry signature tables in parallel SIMD masks.
  - `C0Control.indexOfControlByte` / `indexOfControlChar`, `Grapheme` ASCII cluster runs & case transforms (`toLower`, `toUpper`, `toggleCase`), and `KeyChord.parseArray` vectorize contiguous ASCII runs and fall back cleanly on non-ASCII clusters or `<tag>` chords.
- **Zero-cost `[<StringEnum>]` & `[<Erase>]` representation under `fickl`**:
  - Under `FABLE_COMPILER`, `PhysicalKeyCode` compiles to its exact W3C `KeyboardEvent.code` string literal (`"KeyA"`, `"Escape"`, `"Unidentified"`), making `PhysicalKeyCode.toW3cCode` and `KeyboardEvent.physicalCode` zero-allocation pass-throughs in JavaScript.
- **30-bit packed `KeyChord`**:
  - Packs `LogicalKey` (4-bit kind tag + 21-bit Unicode `Rune` / enum payload) and 5-bit `KeyModifiers` (`Ctrl`, `Alt`, `Shift`, `Meta`, `AltGraph`) into a non-negative 30-bit `int32` (`OverflowText` is `null` for all single-scalar, function, navigation, editing, modifier, dead, and IME keys).
  - `KeyChord.isPlainChar 'G' chord`, `KeyChord.isPlainChar '*' chord`, `KeyChord.isPlainChar 'j' chord`, and `KeyChord.isPlainChar ' ' chord` execute as a single masked 32-bit integer comparison.

## Documentation

- [Overview](docs/content/index.md)
- [Installation & packages](docs/content/guide/introduction.md) — choosing between `Ranvier.Keyboard`, `Ranvier.Keyboard.Dom`, and `Fable.Ranvier.Keyboard.Dom`
- [Packed chords & keymaps](docs/content/guide/chords-and-keymaps.md) — 30-bit `KeyChord` layout, normalization, O(1) predicates, active patterns, and Vim chord sequence parsing
- [Physical, logical & dead keys](docs/content/guide/physical-and-logical-keys.md) — W3C `PhysicalKeyCode`, USB HID `0x07`, ISO/IEC 9995-1 coordinates, `LogicalKey`, dead keys, and RFC 1345 digraphs
- [Unicode, C0 & graphemes](docs/content/guide/unicode-and-graphemes.md) — ECMA-48 `C0Control`, `UnicodeChar` classification, and UAX #29 `Grapheme` stepping
- [DOM & Ranvier.View adapter](docs/content/guide/dom-and-view-integration.md) — `Ranvier.Keyboard.Dom.KeyboardEvent` across native `Ranvier.View.WebCore` and browser `fickl`
- [Contributing & releases](CONTRIBUTOR.md)

## Development

```bash
dotnet fsi --exec build.fsx -- dev setup
dotnet fsi --exec build.fsx -- ci
dotnet fsi --exec build.fsx -- docs
```

`dev local` switches `Ranvier.Dom` / `Fable.Ranvier.Dom` to project references against a sibling `../Ranvier` checkout; `dev packages` returns to the published NuGet packages pinned in `Directory.Build.props`. Individual CI shards run via `dotnet fsi --exec build.fsx -- test --group dotnet`, `fable`, or `aot`. Pass `--watch` to `docs` to serve live documentation previews locally.
