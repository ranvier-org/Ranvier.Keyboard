---
title: Packed chords & keymaps
description: 30-bit integer layout, modifier normalization, O(1) character predicates, active patterns, and Vim chord sequence parsing.
order: 2
---

`KeyChord` and `KeyPress` (`namespace Ranvier.Keyboard`) replace heap-allocated key names and string-concatenated chord keys (`"Ctrl+Shift+P"`) with a compact value-type representation designed for single-instruction comparisons.

## 30-bit `PackedKey` & `KeyChord` layout

Every `KeyChord` is a `[<Struct>]` carrying a non-negative 30-bit `Packed: int` and an optional `OverflowText: string`:

```text
Bit:  31 30 │ 29       28     27      26    25   │ 24 .. 21 │ 20 ............ 0
     ───────┼────────────────────────────────────┼──────────┼───────────────────
      0  0  │ AltGraph Meta   Shift   Alt   Ctrl │ Kind (4b)│ Payload (21 bits)
```

- **Bits `0..20` (21 bits — payload)**:
  - Unicode scalar `Rune.Value` (`0x0000..0x10FFFF`) when `Kind = 0` (`Character`), covering all BMP and astral planes (including emoji) inline without heap allocation.
  - Enum case ordinal for `Whitespace` (`1`), `Navigation` (`2`), `Editing` (`3`), `Ui` (`4`), `Modifier` (`6`), `Dead` (`7`), and `Ime` (`8`).
  - Function key number (`1..24`) for `FunctionKey` (`5`).
- **Bits `21..24` (4 bits — `LogicalKey` kind tag `0..11`)**: Distinguishes `Character`, `Whitespace`, `Navigation`, `Editing`, `Ui`, `FunctionKey`, `Modifier`, `Dead`, `Ime`, `LeaderVirtual`, `Unidentified`, and `GraphemeText`.
- **Bits `25..29` (5 bits — `KeyModifiers`)**: `Ctrl` (`1 <<< 25`), `Alt` (`1 <<< 26`), `Shift` (`1 <<< 27`), `Meta` (`1 <<< 28`), `AltGraph` (`1 <<< 29`).
- **`OverflowText: string`**: `null` for **all** single-scalar Unicode characters, whitespace, navigation, editing, UI, function (`F1..F24`), modifier, dead, IME, and `<leader>` chords. Non-null only when a single key event carries a multi-scalar grapheme cluster (`GraphemeText`, e.g. `"e\u0301"` or a ZWJ sequence) or a custom vendor `Unidentified` string.

`KeyPress` pairs a normalized `KeyChord` with the physical scan code when layout-independent position matters (such as WASD movement or raw shortcut binding):

```fsharp
[<Struct>]
type KeyPress =
    { Chord: KeyChord
      Physical: PhysicalKeyCode }
```

## Constructing & normalizing chords

`KeyChord.ofLogical` (and all high-level constructors: `ofChar`, `ofRune`, `ofKey`, `ofW3c`, `ctrl`, `alt`, `shift`, `parse`, and `KeyboardEvent.toKeyChord`) applies two canonical normalizations so equivalent key events compare equal across platforms:

1. **Shift + printable character canonicalization**: When `Ctrl`, `Alt`, and `Meta` are inactive, `Shift + 'a'` normalizes to `Character (Rune 'A')` with `Shift = true`, `Shift + '/'` normalizes to `Character (Rune '?')` with `Shift = false` (`Shift` is retained on uppercase letters and cleared on shifted symbols whose identity is already encoded in the character), and unshifted `'G'` sets `Shift = true`.
2. **Windows `Ctrl + Alt` -> ISO/IEC 9995-3 `AltGraph`**: Windows keyboard layouts report `AltGr` as `Ctrl = true, Alt = true`. When `LogicalKey.normalizeAltGraph` sees `Ctrl && Alt` (or `AltGraph`) alongside a printable Level-3 symbol or non-ASCII character, it clears `Ctrl` and `Alt` and sets `AltGraph = true` so typing `@`, `€`, `{`, or `[` on German/French/Nordic layouts never triggers `Ctrl+Alt` application shortcuts.

```fsharp
open System.Text
open Ranvier.Keyboard

let esc    = KeyChord.ofKey "Escape"
let save   = KeyChord.ctrl "s"
let splitV = KeyChord.ctrl "w"
let star   = KeyChord.ofChar '*'
let omega  = KeyChord.ofRune (Rune 0x03C9) // ω
let leader = KeyChord.leader               // virtual <leader> chord
```

Use `KeyChord.rawOfLogical` only when you intentionally want unnormalized modifier bits.

## O(1) matching & active patterns

### Fast single-character check (`KeyChord.isPlainChar`)

`KeyChord.isPlainChar` masks `chord.Packed` with `0x17FFFFFF` (retaining the 21-bit payload, 4-bit kind tag, `Ctrl`, `Alt`, and `Meta` while ignoring `Shift` and `AltGraph`) and compares against a single integer constant:

```fsharp
open Ranvier.Keyboard

let stepNormalMode (chord: KeyChord) =
    if KeyChord.isPlainChar 'h' chord then moveLeft ()
    elif KeyChord.isPlainChar 'j' chord then moveDown ()
    elif KeyChord.isPlainChar 'k' chord then moveUp ()
    elif KeyChord.isPlainChar 'l' chord then moveRight ()
    elif KeyChord.isPlainChar 'G' chord then jumpToEnd ()
    elif KeyChord.isPlainChar ' ' chord then triggerLeader ()
```

### Value-option extractors (zero-allocation)

For hot loops where avoiding `FSharpOption` allocation matters, use the `voption` helpers on `KeyChord`:

- `KeyChord.tryPlainChar : KeyChord -> char voption` — unmodified BMP `char` or `' '`.
- `KeyChord.tryPlainRune : KeyChord -> Rune voption` — unmodified Unicode `Rune` (including astral scalars) or `Rune ' '`.
- `KeyChord.tryPlainText : KeyChord -> string voption` — unmodified printable string (`Character`, `GraphemeText`, or `" "`).
- `KeyChord.c0Control : KeyChord -> C0Control voption` — maps `Ctrl + a..z`, `Ctrl + [`, `Ctrl + ?`, `Ctrl + Space`, `Ctrl + Enter`, `Ctrl + Tab`, `Ctrl + Backspace`, and `Ctrl + Escape` to `C0Control`.
- `KeyChord.isModifierOnly : KeyChord -> bool` — `true` for pure modifier keys (`Shift`, `Control`, `Alt`, `Meta`, `AltGraph`, lock keys), IME `Process`, and `Unidentified` events that should not advance modal state machines.

### Pattern matching with active patterns

Open `Ranvier.Keyboard.KeyChord` to bring `|ClassifiedKey|`, `|PlainChar|_|`, `|PlainRune|_|`, `|PlainText|_|`, and `|CtrlC0|_|` into scope:

```fsharp
open Ranvier.Keyboard
open Ranvier.Keyboard.KeyChord

let dispatchInsert (chord: KeyChord) =
    match chord with
    | _ when KeyChord.isModifierOnly chord -> Ignore
    | ClassifiedKey (Ui Escape, mods, _) when mods = KeyModifiers.None -> ExitInsertMode
    | CtrlC0 C0Control.Etb -> DeletePreviousWord   // <C-w>
    | CtrlC0 C0Control.Nak -> DeleteToLineStart    // <C-u>
    | ClassifiedKey (Editing EditingKey.Backspace, _, _) -> DeleteBackward
    | ClassifiedKey (Whitespace Enter, _, _) -> InsertNewline
    | PlainText text -> InsertGrapheme text
    | _ -> Unhandled
```

## Parsing Vim-style chord sequences

`KeyChord.parse` (`string -> KeyChord list`) and on `.NET` `KeyChord.parseArray` (`ReadOnlySpan<char> -> KeyChord array`) parse Vim keymap notation into normalized `KeyChord` sequences:

```fsharp
open Ranvier.Keyboard

let findFiles  = KeyChord.parse "<leader>ff"
let vsplit     = KeyChord.parse "<C-w>v"
let redo       = KeyChord.parse "<C-r>"
let toggleTerm = KeyChord.parse "<C-S-`>"
let countDel   = KeyChord.parse "3dd"
```

Supported `<...>` tags (case-insensitive tag names):

| Tag syntax | Parsed `KeyChord` |
|---|---|
| `<leader>` | `KeyChord.leader` (`LogicalKey.LeaderVirtual`) |
| `<Esc>`, `<Escape>` | `Ui Escape` |
| `<CR>`, `<Return>`, `<Enter>` | `Whitespace Enter` |
| `<Space>` | `Whitespace Space` |
| `<Tab>` | `Whitespace Tab` |
| `<BS>`, `<Backspace>` | `Editing EditingKey.Backspace` |
| `<Del>`, `<Delete>`, `<Ins>`, `<Insert>` | `Editing EditingKey.Delete` / `EditingKey.Insert` |
| `<Up>`, `<Down>`, `<Left>`, `<Right>`, `<Home>`, `<End>`, `<PageUp>`, `<PageDown>` | `Navigation ArrowUp` .. `PageDown` |
| `<F1>` .. `<F24>` | `FunctionKey 1` .. `FunctionKey 24` |
| `<lt>`, `<Bar>`, `<Bslash>` | Literal `'<'`, `'|'`, `'\\'` |
| `<C-x>`, `<A-x>` / `<M-x>`, `<S-x>`, `<D-x>` | Single-modifier chords (`Ctrl`, `Alt`, `Shift`, `Meta`) |
| `<C-S-x>`, `<C-A-x>` / `<C-M-x>`, `<A-S-x>` / `<M-S-x>` | Two-modifier chords (either modifier order, e.g. `<S-C-x>`) |

On `.NET`, `KeyChord.parseArray` scans contiguous printable ASCII runs without `'<'` using `Vector512` / `Vector256` / `Vector128` and populates chords directly from a 128-entry packed lookup table (`OverflowText = null`), while preserving multi-codepoint UAX #29 grapheme clusters when non-ASCII characters or combining marks appear.

Next: [Physical, logical & dead keys](physical-and-logical-keys.md).
