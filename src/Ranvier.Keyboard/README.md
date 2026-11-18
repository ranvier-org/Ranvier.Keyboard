# Ranvier.Keyboard

DOM-free, standards-typed keyboard model, Unicode UAX #29 grapheme utilities, and 30-bit packed `KeyChord` / `KeyPress` primitives for `.NET` (`net10.0`, `net8.0`, NativeAOT) and Fable (`fickl`).

## What `Ranvier.Keyboard` Contains

All types live in `namespace Ranvier.Keyboard`:

- **Physical keys (`IsoAndW3cKeys.fs`)**:
  - `PhysicalKeyCode` (all 116 W3C `KeyboardEvent.code` scan positions; `[<Struct>]` DU on `.NET` with three-tier `Vector512` / `Vector256` / `Vector128` SIMD `ofW3cCode` / `ofW3cCodeSpan` / `ofW3cCodeUtf8`, and `[<StringEnum>]` under `fickl` for zero-cost JS string interop).
  - `PhysicalKeyCode.ofUsbHidUsage` (USB HID Usage Page `0x07` scancode mapping) and `PhysicalKeyCode.toIso9995` (`Iso9995Coordinate`, `IsoSection`, `IsoRow`).
- **Unicode, C0 controls & UAX #29 graphemes (`UnicodeAndControl.fs`)**:
  - `C0Control` (`0x00..0x1F` + `0x7F` DEL, caret notation `^@`..`^?`, Ctrl+ASCII mapping, and SIMD `indexOfControlByte` / `indexOfControlChar`).
  - `UnicodeChar` (`classify`, `tryBracketPartner`, `isVimWordRune`, `vimWordClass` across `LetterCase`, `BracketRole`, `QuoteStyle`, `UnicodeCharClass`).
  - `Grapheme` (`nextClusterLength`, `prevClusterStart`, `stepForward`, `stepBackward`, `runeAt`, `toLower`, `toUpper`, `toggleCase` with SIMD ASCII fast paths).
- **Dead keys, RFC 1345 digraphs & logical keys (`DeadKeysAndDigraphs.fs`)**:
  - `ComposingAccent` & `DeadKey.compose` (ISO/IEC 9995-3 NFC dead-key composition + stroked letters), `Rfc1345Digraph.lookup` (Vim `<C-k>` two-char mnemonics).
  - `LogicalKey` (`Character of Rune`, `GraphemeText`, `Whitespace`, `Navigation`, `Editing`, `Ui`, `FunctionKey 1..24`, `Modifier`, `Dead`, `Ime`, `LeaderVirtual`, `Unidentified`) with SIMD W3C `KeyboardEvent.key` parsing and `KeyModifiers` (`Ctrl`, `Alt`, `Shift`, `Meta`, `AltGraph`, 5-bit `toBits`/`ofBits`).
- **30-bit packed `KeyChord` & `KeyPress` (`KeyChord.fs`)**:
  - `PackedKey`, `[<Struct>] KeyChord = { Packed: int; OverflowText: string }`, and `[<Struct>] KeyPress = { Chord: KeyChord; Physical: PhysicalKeyCode }`.
  - O(1) `KeyChord.isPlainChar`, `tryPlainChar`, `tryPlainRune`, `tryPlainText`, `c0Control`, active patterns (`ClassifiedKey`, `PlainChar`, `PlainRune`, `PlainText`, `CtrlC0`), and SIMD Vim-notation `KeyChord.parse` / `KeyChord.parseArray` (`"<leader>ff"`, `"<C-w>v"`, `"<C-S-x>"`, `"3dd"`).

## When to Use `Ranvier.Keyboard` vs `Ranvier.Keyboard.Dom` / `Fable.Ranvier.Keyboard.Dom`

- **Use `Ranvier.Keyboard` alone** (on `.NET` or `fickl`) when your project does **not** directly touch `Ranvier.Dom.KeyboardEvent` — for example: modal editing engines, keymap registries, `.fsx` configuration scripts, SDL / USB HID native input loops, IPC protocols, CLI/TUI apps, or unit tests. It ships its F# sources for `fickl` directly inside the `.nupkg` and carries no DOM package dependency.
- **Add `Ranvier.Keyboard.Dom`** on native `.NET` / `Ranvier.View.WebCore` hosts, or **`Fable.Ranvier.Keyboard.Dom`** in browser `fickl` apps, when you want to decode `Ranvier.Dom.KeyboardEvent` instances via `Ranvier.Keyboard.Dom.KeyboardEvent.{toKeyChord, toKeyPress, physicalCode, logicalKey, modifiers, isEscape}`.
