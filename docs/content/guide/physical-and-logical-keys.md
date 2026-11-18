---
title: Physical, logical & dead keys
description: W3C PhysicalKeyCode, USB HID Usage Page 0x07, ISO/IEC 9995-1 grid coordinates, LogicalKey, dead-key NFC composition, and RFC 1345 digraphs.
order: 3
---

Keyboard input has two orthogonal identities defined by W3C UI Events:

- **`PhysicalKeyCode` (`KeyboardEvent.code`)**: the physical switch position on the hardware matrix (e.g., `PhysicalKeyCode.KeyQ` is always the top-left letter key, even on AZERTY or Dvorak).
- **`LogicalKey` (`KeyboardEvent.key`)**: the meaning produced after the active OS keyboard layout, dead keys, and IME state apply (e.g., `Character (Rune 'a')` on AZERTY when pressing `PhysicalKeyCode.KeyQ`).

## `PhysicalKeyCode`, USB HID & ISO/IEC 9995-1

`PhysicalKeyCode` (`[<RequireQualifiedAccess>]`) covers all 116 standardized W3C `KeyboardEvent.code` positions across the alphanumeric block (`Backquote`, `Digit0..Digit9`, `KeyA..KeyZ`, `IntlBackslash`, `IntlRo`, `IntlYen`, `IntlHash`, Japanese/Korean `Convert`, `NonConvert`, `KanaMode`, `Lang1`, `Lang2`), function row (`Escape`, `F1..F24`), navigation/editing pad, and numeric keypad (`Numpad0..Numpad9`, `NumpadComma`, `NumpadEqual`, `NumpadEnter`, etc.), plus `UnidentifiedCode`.

### Dual-target representation

- **On `.NET`**: `[<Struct>]` discriminated union. `PhysicalKeyCode.ofW3cCode`, `ofW3cCodeSpan` (`ReadOnlySpan<char>`), and `ofW3cCodeUtf8` (`ReadOnlySpan<byte>`) pack `2..14`-character ASCII identifiers into a `Vector128<byte>` without out-of-bounds reads, verify all high UTF-16 bits (`0xFF80 = 0`), and match precomputed signature tables using `Vector512` / `Vector256` / `Vector128` SIMD.
- **Under `fickl` (`FABLE_COMPILER`)**: `[<StringEnum(CaseRules.None)>]`, where `UnidentifiedCode` carries `[<CompiledName("Unidentified")>]`. Every `PhysicalKeyCode` value **is** its W3C string in JavaScript (`"KeyA"`, `"Escape"`, `"Unidentified"`), making `PhysicalKeyCode.toW3cCode` and `KeyboardEvent.physicalCode` zero-allocation pass-throughs.

### USB HID Usage Page `0x07` & ISO/IEC 9995-1 coordinates

When reading raw scancodes from SDL, evdev/HID reports, or custom hardware protocols, map USB HID Keyboard/Keypad usage IDs (`0x04..0xE7`) directly to `PhysicalKeyCode`, and query standardized physical grid positions via `PhysicalKeyCode.toIso9995`:

```fsharp
open Ranvier.Keyboard

let keyA = PhysicalKeyCode.ofUsbHidUsage 0x04us     // PhysicalKeyCode.KeyA
let isoB00 = PhysicalKeyCode.ofUsbHidUsage 0x64us   // PhysicalKeyCode.IntlBackslash
let w3cName = PhysicalKeyCode.toW3cCode keyA        // "KeyA"

match PhysicalKeyCode.toIso9995 isoB00 with
| ValueSome coord ->
    // coord.Section = Alphanumeric, coord.Row = RowB, coord.Column = 0uy (ISO B00)
    printfn "%A %A %d" coord.Section coord.Row coord.Column
| ValueNone -> ()
```

## `LogicalKey` & `KeyModifiers`

`LogicalKey` classifies W3C `KeyboardEvent.key` values (plus legacy browser aliases such as `"Esc"`, `"Spacebar"`, `"Left"`, `"Del"`, `"OS"`) into a `[<Struct>]` discriminated union:

```fsharp
[<Struct>]
type LogicalKey =
    | Character of character: Rune
    | GraphemeText of text: string
    | Whitespace of whitespace: WhitespaceKey     // Space | Tab | Enter
    | Navigation of navigation: NavigationKey     // ArrowLeft..ArrowDown | Home | End | PageUp | PageDown
    | Editing of editing: EditingKey              // Backspace | Delete | Insert | Clear
    | Ui of ui: UiKey                             // Escape | ContextMenu | Pause | Cancel
    | FunctionKey of number: int                  // 1..24
    | Modifier of modifier: ModifierKey           // Shift | Control | Alt | AltGraph | Meta | CapsLock..Fn
    | Dead of accent: ComposingAccent             // Grave | Acute | Circumflex..DotAbove
    | Ime of ime: ImeKey                          // Process | Convert | NonConvert | KanaMode | HangulMode..
    | LeaderVirtual
    | Unidentified of raw: string
```

Key conversion helpers on `LogicalKey`:

- `LogicalKey.ofW3cKey : string -> LogicalKey` (plus `.NET` span overloads `ofW3cKeySpan : ReadOnlySpan<char> -> LogicalKey` and `ofW3cKeyUtf8 : ReadOnlySpan<byte> -> LogicalKey`).
- `LogicalKey.toW3cKey : LogicalKey -> string` — formats back to the canonical W3C key string (`" "`, `"Escape"`, `"ArrowDown"`, `"F12"`, etc.).
- `LogicalKey.tryPrintedText : LogicalKey -> string voption` — returns `ValueSome` text for `Character`, non-empty `GraphemeText`, and `Whitespace Space`, or `ValueNone` for non-printing keys.
- `LogicalKey.isNonActionableModifier : LogicalKey -> bool` — `true` for `Modifier _`, `Ime Process`, and `Unidentified _`.

`KeyModifiers` (`{ Ctrl; Alt; Shift; Meta; AltGraph }`, `KeyModifiers.None`) packs losslessly into a 5-bit integer (`0..31`) via `KeyModifiers.toBits` and `KeyModifiers.ofBits`.

## ISO/IEC 9995-3 dead keys (`DeadKey`)

When building custom text input or terminal/SDL composition handlers, `DeadKey.compose` combines any of the 14 `ComposingAccent` cases (`Grave`, `Acute`, `Circumflex`, `Tilde`, `Diaeresis`, `RingAbove`, `Cedilla`, `Caron`, `Macron`, `Breve`, `DoubleAcute`, `Ogonek`, `Stroke`, `DotAbove`) with a base `Rune`:

```fsharp
open System.Text
open Ranvier.Keyboard

let eAcute  = DeadKey.compose Acute (Rune 'e')      // "é" (Unicode NFC)
let lStroke = DeadKey.compose Stroke (Rune 'l')     // "ł"
let oSlash  = DeadKey.compose Stroke (Rune 'O')     // "Ø"
let spacing = DeadKey.compose Circumflex (Rune ' ') // "^" (spacing mark on Space)
```

## IETF RFC 1345 digraphs (`Rfc1345Digraph`)

`Rfc1345Digraph.lookup` (`char -> char -> Rune voption`) implements standardized two-character mnemonics (used by Vim `<C-k>{c1}{c2}` insert-mode digraphs) across arrows, mathematical operators, Greek letters, currency symbols, Latin ligatures, and accented letters:

```fsharp
open Ranvier.Keyboard

let arrow  = Rfc1345Digraph.lookup '-' '>' // ValueSome (Rune 0x2192) -> '→'
let lambda = Rfc1345Digraph.lookup 'l' '*' // ValueSome (Rune 0x03BB) -> 'λ'
let forall = Rfc1345Digraph.lookup 'F' 'A' // ValueSome (Rune 0x2200) -> '∀'
let euro   = Rfc1345Digraph.lookup 'E' 'u' // ValueSome (Rune 0x20AC) -> '€'
let aRing  = Rfc1345Digraph.lookup 'a' '0' // ValueSome (Rune 0x00E5) -> 'å'
```

Next: [Unicode, C0 & graphemes](unicode-and-graphemes.md).
