/// <summary>
/// Cross-target checks compiled three ways: NativeAOT (--reflectionfree), Fable (fickl under Node), and .NET.
/// Uses zero reflection and zero printf/sprintf formatting so it builds cleanly under --reflectionfree.
/// </summary>
module Ranvier.Keyboard.Smoke.Checks

open System
open System.Text
open Ranvier.Keyboard
open Ranvier.Keyboard.Dom

type private SmokeKeyboardEvent(key: string, code: string, ctrl: bool, alt: bool, shift: bool, meta: bool, altGraph: bool) =
    let mutable queries = 0
    let mutable cancelBubble = false
    let mutable returnValue = true

    member _.Queries = queries

    interface Ranvier.Dom.KeyboardEvent with
        member _.``type`` = "keydown"
        member _.target = null
        member _.currentTarget = null
        member _.eventPhase = 2us
        member _.stopPropagation() = ()

        member _.cancelBubble
            with get () = cancelBubble
            and set (v: bool) = cancelBubble <- v

        member _.stopImmediatePropagation() = ()
        member _.bubbles = true
        member _.cancelable = true
        member _.preventDefault() = ()
        member _.defaultPrevented = false
        member _.composed = true
        member _.isTrusted = true

        member _.initEvent(_, ?bubbles, ?cancelable) =
            ignore (bubbles, cancelable)

        member _.srcElement = null

        member _.returnValue
            with get () = returnValue
            and set (v: bool) = returnValue <- v

        member _.detail = 0
        member _.layerX = 0
        member _.layerY = 0
        member _.pageX = 0.0
        member _.pageY = 0.0
        member _.which = 0u
        member _.key = key
        member _.code = code
        member _.location = 0u
        member _.ctrlKey = ctrl
        member _.shiftKey = shift
        member _.altKey = alt
        member _.metaKey = meta
        member _.repeat = false
        member _.isComposing = false

        member _.getModifierState(keyArg: string) =
            queries <- queries + 1
            keyArg = "AltGraph" && altGraph

        member _.keyIdentifier = ""
        member _.charCode = 0u
        member _.keyCode = 0u

/// <summary>Runs all cross-target keyboard and DOM adapter checks.</summary>
let run () : (string * bool) list =
    let plainEv = SmokeKeyboardEvent ("G", "KeyG", false, false, true, false, false)
    let plainPress = KeyboardEvent.toKeyPress plainEv

    let altGrEv = SmokeKeyboardEvent ("€", "KeyE", false, true, false, false, true)
    let altGrPress = KeyboardEvent.toKeyPress altGrEv

    let escEv =
        SmokeKeyboardEvent ("Escape", "Escape", false, false, false, false, false)

    let parsed = KeyChord.parse "<leader>ff<C-k><C-S-x>3G* :"

    [
        "PhysicalKeyCode round-trips KeyA and IntlBackslash",
        PhysicalKeyCode.ofW3cCode "KeyA" = PhysicalKeyCode.KeyA
        && PhysicalKeyCode.toW3cCode PhysicalKeyCode.KeyA = "KeyA"
        && PhysicalKeyCode.ofW3cCode "IntlBackslash" = PhysicalKeyCode.IntlBackslash
        && PhysicalKeyCode.toW3cCode PhysicalKeyCode.UnidentifiedCode = "Unidentified"
        "PhysicalKeyCode maps OSLeft/OSRight, USB HID, and ISO 9995-1",
        PhysicalKeyCode.ofW3cCode "OSLeft" = PhysicalKeyCode.MetaLeft
        && PhysicalKeyCode.ofW3cCode "OSRight" = PhysicalKeyCode.MetaRight
        && PhysicalKeyCode.ofUsbHidUsage 0x04us = PhysicalKeyCode.KeyA
        && PhysicalKeyCode.toIso9995 PhysicalKeyCode.IntlBackslash =
            ValueSome
                {
                    Section = Alphanumeric
                    Row = RowB
                    Column = 0uy
                }
        "C0Control round-trips bytes and caret notation",
        C0Control.ofModifiedAscii 'w' = ValueSome C0Control.Etb
        && C0Control.toCaretNotation C0Control.Etb = "^W"
        && C0Control.toCaretNotation C0Control.Del = "^?"
        "UnicodeChar classifies ASCII and non-ASCII decimal digits and brackets",
        UnicodeChar.classify (Rune '7') = UnicodeDigit (ValueSome 7)
        && UnicodeChar.classify (Rune '٥') = UnicodeDigit (ValueSome 5)
        && UnicodeChar.classify (Rune '(') = UnicodeBracket (OpeningBracket, Rune ')')
        && UnicodeChar.vimWordClass (Rune '_') = 1
        "Grapheme steps across combining marks, astral emoji, ZWJ, and CRLF",
        Grapheme.nextClusterLength "\r\n" 0 = 2
        && Grapheme.prevClusterStart "\r\n" 2 = 0
        && Grapheme.stepForward 2 "e\u0301😀x" 0 = 4
        && Grapheme.stepBackward 1 "e\u0301😀x" 4 = 2
        && Grapheme.stepBackward 2 "e\u0301😀x" 4 = 0
        && Grapheme.toggleCase "AbC Ω 😀" = "aBc ω 😀"
        && Grapheme.toLower "HÉLLO" = "héllo"
        && Grapheme.toUpper "héllo" = "HÉLLO"
        "DeadKey NFC composition and RFC 1345 digraphs compose cleanly",
        DeadKey.compose Acute (Rune 'e') = "é"
        && DeadKey.compose Stroke (Rune 'l') = "ł"
        && DeadKey.compose Grave (Rune ' ') = "`"
        && Rfc1345Digraph.lookup '-' '>' = ValueSome (Rune 0x2192)
        && Rfc1345Digraph.lookup 'l' '*' = ValueSome (Rune 'λ')
        "LogicalKey parses F1..F24, aliases, and astral emoji",
        LogicalKey.ofW3cKey "F12" = FunctionKey 12
        && LogicalKey.ofW3cKey "f24" = FunctionKey 24
        && LogicalKey.toW3cKey (FunctionKey 12) = "F12"
        && LogicalKey.ofW3cKey "Esc" = Ui Escape
        && LogicalKey.ofW3cKey "😀" = Character (Rune 0x1F600)
        "KeyChord 30-bit packing and isPlainChar match shifted ASCII and Space",
        KeyChord.isPlainChar 'G' (KeyChord.ofW3c ("g", false, false, true))
        && KeyChord.isPlainChar '*' (KeyChord.ofW3c ("8", false, false, true))
        && KeyChord.isPlainChar ':' (KeyChord.ofW3c (";", false, false, true))
        && KeyChord.isPlainChar ' ' (KeyChord.ofChar ' ')
        && not (KeyChord.isPlainChar 'c' (KeyChord.ctrl "c"))
        "KeyChord.parse decodes leader, modifiers, and plain ASCII runs",
        parsed.Length = 10
        && parsed[0] = KeyChord.leader
        && parsed[3] = KeyChord.ctrl "k"
        && parsed[4].Ctrl
        && parsed[4].Shift
        && KeyChord.isPlainChar 'G' parsed[6]
        && KeyChord.isPlainChar '*' parsed[7]
        && KeyChord.isPlainChar ' ' parsed[8]
        && KeyChord.isPlainChar ':' parsed[9]
        "KeyboardEvent skips getModifierState on unmodified keys and queries AltGraph on Alt/Ctrl",
        plainEv.Queries = 0
        && plainPress.Physical = PhysicalKeyCode.KeyG
        && KeyChord.isPlainChar 'G' plainPress.Chord
        && altGrEv.Queries = 1
        && altGrPress.Modifiers.AltGraph
        && KeyChord.isPlainChar '€' altGrPress.Chord
        && KeyboardEvent.isEscape escEv
        && escEv.Queries = 0
    ]
