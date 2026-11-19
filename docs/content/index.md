---
title: Ranvier.Keyboard
description: Standards-typed keyboard primitives, 30-bit packed key chords, Unicode UAX #29 graphemes, and zero-cost DOM adapters for .NET and Fable
toc: false
---

Standards-typed, SIMD-accelerated keyboard primitives and `Ranvier.Dom.KeyboardEvent` adapters for .NET (`net10.0`, `net8.0`, NativeAOT) and Fable (`fickl`).

Convert browser or native SDL/WebCore keyboard events once at the input boundary into a 30-bit packed value-type `KeyChord`, then dispatch across modal editors, keymap tables, configuration scripts, and UI shells with single-integer comparisons and zero string allocations:

```fsharp
open Ranvier.Keyboard
open Ranvier.Keyboard.KeyChord
open Ranvier.Keyboard.Dom

let handleKey (ev: Ranvier.Dom.KeyboardEvent) =
    let chord = KeyboardEvent.toKeyChord ev

    if KeyChord.isPlainChar 'j' chord then
        moveDown 1
    elif KeyChord.isPlainChar 'G' chord then
        jumpToLastLine ()
    else
        match chord with
        | CtrlC0 C0Control.Cr -> submit ()
        | ClassifiedKey (Navigation PageDown, mods, _) when not mods.Ctrl -> pageDown ()
        | PlainText text -> insertText text
        | _ -> ()
```

Start with [Installation & packages](guide/introduction.md), then explore:

- [Packed chords & keymaps](guide/chords-and-keymaps.md): the 30-bit `KeyChord` layout, `Shift`/`AltGraph` normalization, O(1) predicates, active patterns, and SIMD Vim-notation chord parsing (`"<leader>ff"`, `"<C-w>v"`, `"3dd"`).
- [Physical, logical & dead keys](guide/physical-and-logical-keys.md): W3C `PhysicalKeyCode` (`KeyboardEvent.code`), USB HID Usage Page `0x07`, ISO/IEC 9995-1 grid coordinates, `LogicalKey`, ISO/IEC 9995-3 dead-key composition, and RFC 1345 `<C-k>` digraphs.
- [Unicode, C0 & graphemes](guide/unicode-and-graphemes.md): ECMA-48 `C0Control` scanners, `UnicodeChar` bracket/quote/word classification, and Unicode UAX #29 `Grapheme` cluster stepping and case transforms.
- [DOM & Ranvier.View adapter](guide/dom-and-view-integration.md): decoding `Ranvier.Dom.KeyboardEvent` across native `Ranvier.View.WebCore` and browser `fickl` targets with shared source code.

The [repository](https://github.com/ranvier-org/Ranvier.Keyboard) contains the core library, the native and Fable DOM adapters, and the `.NET`, `fickl`, and NativeAOT test suites.
