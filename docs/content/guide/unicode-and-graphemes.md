---
title: Unicode, C0 & graphemes
description: ECMA-48 C0 control scanners, Unicode character/bracket/quote/word classification, and UAX #29 extended grapheme cluster stepping.
order: 4
---

`Ranvier.Keyboard` includes Unicode scalar classification, ECMA-48 C0 control handling, and Unicode UAX #29 extended grapheme cluster navigation so text editors, terminal emulators, and input engines handle non-ASCII scripts, combining marks, CRLF pairs, and astral emoji cleanly on both `.NET` and `fickl`.

## ECMA-48 C0 controls (`C0Control`)

`C0Control` is a `[<Struct>]` union covering `0x00..0x1F` (`Nul`, `Soh`, `Stx`, `Etx`, ..., `Esc`, `Fs`, `Gs`, `Rs`, `Us`) plus `0x7F` (`Del`):

```fsharp
open System
open Ranvier.Keyboard

let etx   = C0Control.ofModifiedAscii 'c'  // ValueSome Etx (0x03)
let esc   = C0Control.ofModifiedAscii '['  // ValueSome Esc (0x1B)
let caret = C0Control.toCaretNotation Etx  // "^C"
let raw   = C0Control.toByte Del           // 0x7Fuy
```

On `.NET`, `C0Control.indexOfControlByte` (`ReadOnlySpan<byte> -> int`) and `C0Control.indexOfControlChar` (`ReadOnlySpan<char> -> int`) scan buffers for any C0 control (`<= 0x1F`) or `DEL` (`0x7F`) using `Vector512` / `Vector256` / `Vector128` SIMD, returning `-1` when the buffer contains only printable text.

## Character, bracket & word classification (`UnicodeChar`)

`UnicodeChar.classify` maps any `System.Text.Rune` into `UnicodeCharClass`:

```fsharp
[<Struct>]
type UnicodeCharClass =
    | UnicodeLetter of letterCase: LetterCase
    | UnicodeDigit of decimalValue: int voption
    | UnicodeConnector
    | UnicodeBracket of role: BracketRole * partner: Rune
    | UnicodeQuote of quoteStyle: QuoteStyle
    | UnicodeMathOrSymbol
    | UnicodePunctuation
    | UnicodeWhitespace
    | UnicodeControl of c0: C0Control voption
    | UnicodeMarkOrOther
```

- **Paired delimiters (`UnicodeChar.tryBracketPartner`)**: Resolves opening/closing roles and partner `Rune`s across ASCII `()`, `[]`, `{}`, guillemets `«»` / `‹›`, mathematical angle brackets `⟨⟩`, CJK corner/lenticular brackets `〈〉` / `《》` / `「」` / `『』` / `【】`, and fullwidth brackets `（）` / `［］` / `｛｝`.
- **Vim word motion helpers**:
  - `UnicodeChar.isVimWordRune : Rune -> bool` — `true` for Unicode letters, digits, and connector punctuation (`_`).
  - `UnicodeChar.vimWordClass : Rune -> int` — `0` for whitespace, `1` for keyword characters, `2` for punctuation and symbols (used to detect `w` / `b` / `e` word boundaries across scripts).

```fsharp
open System.Text
open Ranvier.Keyboard

let partner = UnicodeChar.tryBracketPartner (Rune '「')
// ValueSome (struct (OpeningBracket, Rune '」'))

let cls = UnicodeChar.vimWordClass (Rune 'λ')
// 1 (keyword character)
```

## UAX #29 extended grapheme clusters (`Grapheme`)

Cursor movement and deletion in UTF-16 strings must step by **extended grapheme clusters** rather than raw `char` indices so surrogate pairs, combining marks (`"e\u0301"`), regional indicator flags (`"🇯🇵"`), ZWJ sequences (`"👩‍💻"`), and `"\r\n"` are never split mid-cluster:

| Function | Signature | Behavior |
|---|---|---|
| `Grapheme.nextClusterLength` | `string -> int -> int` | Length in UTF-16 code units of the cluster starting at `utf16Index` (`0` if out of range). |
| `Grapheme.prevClusterStart` | `string -> int -> int` | Start UTF-16 index of the cluster immediately before `utf16Index` (`0` at start of string). |
| `Grapheme.stepForward` | `int -> string -> int -> int` | Advances `utf16Index` forward by `count` extended grapheme clusters, clamped to `[0 .. line.Length]`. |
| `Grapheme.stepBackward` | `int -> string -> int -> int` | Moves `utf16Index` backward by `count` extended grapheme clusters, clamped to `[0 .. line.Length]`. |
| `Grapheme.runeAt` | `string -> int -> Rune` | Decodes the `Rune` at `utf16Index` (`Rune.ReplacementChar` if out of bounds or on an unpaired surrogate). |
| `Grapheme.toLower` / `toUpper` / `toggleCase` | `string -> string` | Transforms case across all Unicode scalars (using `Vector512` / `Vector256` / `Vector128` `String.Create` fast paths on pure ASCII strings on `.NET`). |

```fsharp
open Ranvier.Keyboard

let line = "a🇯🇵e\u0301b"
// Clusters: "a" (0..1), "🇯🇵" (1..5), "e\u0301" (5..7), "b" (7..8)

let afterFlag   = Grapheme.stepForward 2 line 0   // 5
let afterAccent = Grapheme.stepForward 1 line 5   // 7
let backOne     = Grapheme.stepBackward 1 line 7  // 5
let swapped     = Grapheme.toggleCase "Ranvier_1" // "rANVIER_1"
```

On `.NET`, `Grapheme.stepForward` and `Grapheme.stepBackward` skip contiguous printable ASCII runs (`0x0020..0x007E`) in single SIMD vector steps (checking the trailing code unit for `>= U+0300` combining marks or `>= U+0080` prepend candidates so cluster boundaries stay exact) before falling back to `StringInfo` only at non-ASCII clusters.

Next: [DOM & Ranvier.View adapter](dom-and-view-integration.md).
