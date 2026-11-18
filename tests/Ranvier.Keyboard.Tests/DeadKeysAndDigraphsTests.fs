module Ranvier.Keyboard.Tests.DeadKeysAndDigraphsTests

open System
open System.Text
open Expecto
open Ranvier.Keyboard

let private assertLogicalParsersMatch (input: string) (expected: LogicalKey) =
    Expect.equal (LogicalKey.ofW3cKey input) expected $"ofW3cKey '{input}'"
    Expect.equal (LogicalKey.ofW3cKeyScalar (input.AsSpan ())) expected $"ofW3cKeyScalar '{input}'"
    Expect.equal (LogicalKey.ofW3cKeySpan (input.AsSpan ())) expected $"ofW3cKeySpan '{input}'"
    let utf8 = Encoding.UTF8.GetBytes input
    Expect.equal (LogicalKey.ofW3cKeyUtf8 (ReadOnlySpan<byte> utf8)) expected $"ofW3cKeyUtf8 '{input}'"

[<Tests>]
let tests =
    testList
        "DeadKeysAndDigraphs"
        [
            testCase "DeadKey.compose produces spacing accents on space, NFC precomposed Latin letters, and stroked letters" (fun () ->
                Expect.equal (DeadKey.compose Grave (Rune ' ')) "`" "Grave + space"
                Expect.equal (DeadKey.compose Acute (Rune ' ')) "\u00B4" "Acute + space"
                Expect.equal (DeadKey.compose Diaeresis (Rune ' ')) "\u00A8" "Diaeresis + space"
                Expect.equal (DeadKey.compose Acute (Rune 'e')) "é" "Acute + e"
                Expect.equal (DeadKey.compose Grave (Rune 'A')) "À" "Grave + A"
                Expect.equal (DeadKey.compose Circumflex (Rune 'o')) "ô" "Circumflex + o"
                Expect.equal (DeadKey.compose Tilde (Rune 'n')) "ñ" "Tilde + n"
                Expect.equal (DeadKey.compose Diaeresis (Rune 'u')) "ü" "Diaeresis + u"
                Expect.equal (DeadKey.compose RingAbove (Rune 'a')) "å" "RingAbove + a"
                Expect.equal (DeadKey.compose Cedilla (Rune 'c')) "ç" "Cedilla + c"
                Expect.equal (DeadKey.compose Caron (Rune 's')) "š" "Caron + s"
                Expect.equal (DeadKey.compose Macron (Rune 'a')) "ā" "Macron + a"
                Expect.equal (DeadKey.compose Breve (Rune 'g')) "ğ" "Breve + g"
                Expect.equal (DeadKey.compose DoubleAcute (Rune 'o')) "ő" "DoubleAcute + o"
                Expect.equal (DeadKey.compose Ogonek (Rune 'e')) "ę" "Ogonek + e"
                Expect.equal (DeadKey.compose DotAbove (Rune 'z')) "ż" "DotAbove + z"
                Expect.equal (DeadKey.compose Stroke (Rune 'l')) "ł" "Stroke + l"
                Expect.equal (DeadKey.compose Stroke (Rune 'L')) "Ł" "Stroke + L"
                Expect.equal (DeadKey.compose Stroke (Rune 'o')) "ø" "Stroke + o"
                Expect.equal (DeadKey.compose Stroke (Rune 'O')) "Ø" "Stroke + O"
                Expect.equal (DeadKey.compose Stroke (Rune 'd')) "đ" "Stroke + d"
                Expect.equal (DeadKey.compose Stroke (Rune 'D')) "Đ" "Stroke + D"
                Expect.equal (DeadKey.compose Acute (Rune 'q')) "q\u0301" "Acute + q fallback")

            testCase "Rfc1345Digraph.lookup resolves arrows, logic, Greek, currency, accented suffixes, and rejects invalid" (fun () ->
                Expect.equal (Rfc1345Digraph.lookup '-' '>') (ValueSome (Rune 0x2192)) "-> arrow"
                Expect.equal (Rfc1345Digraph.lookup '=' '>') (ValueSome (Rune 0x21D2)) "=> arrow"
                Expect.equal (Rfc1345Digraph.lookup '!' '=') (ValueSome (Rune 0x2260)) "!= neq"
                Expect.equal (Rfc1345Digraph.lookup 'F' 'A') (ValueSome (Rune 0x2200)) "FA forall"
                Expect.equal (Rfc1345Digraph.lookup 'l' '*') (ValueSome (Rune 'λ')) "l* lambda"
                Expect.equal (Rfc1345Digraph.lookup 'W' '*') (ValueSome (Rune 'Ω')) "W* Omega"
                Expect.equal (Rfc1345Digraph.lookup 'E' 'u') (ValueSome (Rune '€')) "Eu Euro"
                Expect.equal (Rfc1345Digraph.lookup '=' 'e') (ValueSome (Rune '€')) "=e Euro"
                Expect.equal (Rfc1345Digraph.lookup 's' 's') (ValueSome (Rune 'ß')) "ss sharp s"
                Expect.equal (Rfc1345Digraph.lookup 'e' '\'') (ValueSome (Rune 'é')) "e' acute"
                Expect.equal (Rfc1345Digraph.lookup 'o' '/') (ValueSome (Rune 'ø')) "o/ stroke"
                Expect.equal (Rfc1345Digraph.lookup 'q' '\'') ValueNone "q' does not precompose to 1 Rune"
                Expect.equal (Rfc1345Digraph.lookup 'z' 'z') ValueNone "zz unknown")

            testCase "KeyModifiers.toBits and ofBits round-trip all 32 5-bit combinations" (fun () ->
                Expect.equal (KeyModifiers.toBits KeyModifiers.None) 0 "None = 0"
                let seen = System.Collections.Generic.HashSet<KeyModifiers>()

                for bits = 0 to 31 do
                    let m = KeyModifiers.ofBits bits
                    Expect.isTrue (seen.Add m) $"bits {bits} maps to distinct KeyModifiers"
                    Expect.equal (KeyModifiers.toBits m) bits $"round-trip bits {bits}")

            testCase "LogicalKey canonical cases and W3C aliases round-trip across SIMD, scalar, and UTF-8" (fun () ->
                let canonicalCases: (LogicalKey * string) array =
                    [|
                        Whitespace Space, " "
                        Whitespace Tab, "Tab"
                        Whitespace Enter, "Enter"
                        Navigation ArrowLeft, "ArrowLeft"
                        Navigation ArrowRight, "ArrowRight"
                        Navigation ArrowUp, "ArrowUp"
                        Navigation ArrowDown, "ArrowDown"
                        Navigation Home, "Home"
                        Navigation End, "End"
                        Navigation PageUp, "PageUp"
                        Navigation PageDown, "PageDown"
                        Editing EditingKey.Backspace, "Backspace"
                        Editing EditingKey.Delete, "Delete"
                        Editing EditingKey.Insert, "Insert"
                        Editing EditingKey.Clear, "Clear"
                        Ui Escape, "Escape"
                        Ui ContextMenu, "ContextMenu"
                        Ui Pause, "Pause"
                        Ui Cancel, "Cancel"
                        Modifier Shift, "Shift"
                        Modifier Control, "Control"
                        Modifier Alt, "Alt"
                        Modifier AltGraph, "AltGraph"
                        Modifier Meta, "Meta"
                        Modifier CapsLock, "CapsLock"
                        Modifier NumLock, "NumLock"
                        Modifier ScrollLock, "ScrollLock"
                        Modifier Symbol, "Symbol"
                        Modifier Fn, "Fn"
                        Dead Grave, "DeadGrave"
                        Dead Acute, "DeadAcute"
                        Dead Circumflex, "DeadCircumflex"
                        Dead Tilde, "DeadTilde"
                        Dead Diaeresis, "DeadDiaeresis"
                        Dead RingAbove, "DeadRingAbove"
                        Dead Cedilla, "DeadCedilla"
                        Dead Caron, "DeadCaron"
                        Dead Macron, "DeadMacron"
                        Dead Breve, "DeadBreve"
                        Dead DoubleAcute, "DeadDoubleAcute"
                        Dead Ogonek, "DeadOgonek"
                        Dead Stroke, "DeadStroke"
                        Dead DotAbove, "DeadDotAbove"
                        Ime Process, "Process"
                        Ime Convert, "Convert"
                        Ime NonConvert, "NonConvert"
                        Ime ModeChange, "ModeChange"
                        Ime KanjiMode, "KanjiMode"
                        Ime KanaMode, "KanaMode"
                        Ime HangulMode, "HangulMode"
                        Ime HanjaMode, "HanjaMode"
                        Ime Eisu, "Eisu"
                        Ime ZenkakuHankaku, "ZenkakuHankaku"
                        LeaderVirtual, "<leader>"
                        Unidentified "Unidentified", "Unidentified"
                    |]

                for key, name in canonicalCases do
                    Expect.equal (LogicalKey.toW3cKey key) name $"toW3cKey {name}"
                    assertLogicalParsersMatch name key

                for n = 1 to 24 do
                    let fk = FunctionKey n
                    let upper = "F" + string n
                    let lower = "f" + string n
                    Expect.equal (LogicalKey.toW3cKey fk) upper $"toW3cKey F{n}"
                    assertLogicalParsersMatch upper fk
                    assertLogicalParsersMatch lower fk

                let aliases: (string * LogicalKey) array =
                    [|
                        "", Unidentified ""
                        "Spacebar", Whitespace Space
                        "Return", Whitespace Enter
                        "Left", Navigation ArrowLeft
                        "Right", Navigation ArrowRight
                        "Up", Navigation ArrowUp
                        "Down", Navigation ArrowDown
                        "Del", Editing EditingKey.Delete
                        "Esc", Ui Escape
                        "ShiftLeft", Modifier Shift
                        "ShiftRight", Modifier Shift
                        "ControlLeft", Modifier Control
                        "ControlRight", Modifier Control
                        "AltLeft", Modifier Alt
                        "AltRight", Modifier Alt
                        "MetaLeft", Modifier Meta
                        "MetaRight", Modifier Meta
                        "OS", Modifier Meta
                        "Super", Modifier Meta
                        "Hyper", Modifier Meta
                        "Dead", Dead Acute
                        "DeadAboveDot", Dead DotAbove
                    |]

                for alias, expected in aliases do
                    assertLogicalParsersMatch alias expected

                Expect.equal (LogicalKey.ofW3cKey null) (Unidentified "") "null -> Unidentified ''"
                Expect.equal (LogicalKey.toW3cKey (Unidentified "")) "Unidentified" "empty Unidentified formats as Unidentified")

            testCase "LogicalKey handles single ASCII/BMP/astral Runes, multi-codepoint graphemes, F0/F25, and high-bit non-ASCII" (fun () ->
                assertLogicalParsersMatch "a" (Character (Rune 'a'))
                assertLogicalParsersMatch "é" (Character (Rune 'é'))
                assertLogicalParsersMatch "あ" (Character (Rune 'あ'))
                assertLogicalParsersMatch "😀" (Character (Rune 0x1F600))
                assertLogicalParsersMatch "e\u0301" (GraphemeText "e\u0301")
                assertLogicalParsersMatch "👨‍👩‍👧‍👦" (GraphemeText "👨‍👩‍👧‍👦")
                assertLogicalParsersMatch "F0" (GraphemeText "F0")
                assertLogicalParsersMatch "F25" (GraphemeText "F25")
                assertLogicalParsersMatch "F01" (GraphemeText "F01")
                assertLogicalParsersMatch "TabExtra" (GraphemeText "TabExtra")
                // High-bit spoof of "Tab" ('T', 'a', 'b' | 0x0100 = '\u0162') must be GraphemeText, never Tab
                let spoofTab = String [| 'T'; 'a'; char (uint16 'b' ||| 0x0100us) |]
                assertLogicalParsersMatch spoofTab (GraphemeText spoofTab))

            testCase "LogicalKey tryPrintedText, isNonActionableModifier, and normalizeAltGraph follow ISO 9995-3" (fun () ->
                Expect.equal (LogicalKey.tryPrintedText (Character (Rune 'x'))) (ValueSome "x") "char printed"
                Expect.equal (LogicalKey.tryPrintedText (Whitespace Space)) (ValueSome " ") "space printed"
                Expect.equal (LogicalKey.tryPrintedText (GraphemeText "e\u0301")) (ValueSome "e\u0301") "grapheme printed"
                Expect.equal (LogicalKey.tryPrintedText (Whitespace Enter)) ValueNone "Enter not printed"
                Expect.equal (LogicalKey.tryPrintedText (Ui Escape)) ValueNone "Escape not printed"

                Expect.isTrue (LogicalKey.isNonActionableModifier (Modifier Shift)) "Shift is non-actionable"
                Expect.isTrue (LogicalKey.isNonActionableModifier (Ime Process)) "IME Process is non-actionable"
                Expect.isTrue (LogicalKey.isNonActionableModifier (Unidentified "")) "Unidentified is non-actionable"
                Expect.isFalse (LogicalKey.isNonActionableModifier (Character (Rune 'a'))) "'a' is actionable"

                let winCtrlAlt =
                    { KeyModifiers.None with
                        Ctrl = true
                        Alt = true
                    }

                Expect.equal
                    (LogicalKey.normalizeAltGraph (Character (Rune '{')) winCtrlAlt)
                    { KeyModifiers.None with
                        AltGraph = true
                    }
                    "Ctrl+Alt+'{' normalizes to AltGraph"

                Expect.equal
                    (LogicalKey.normalizeAltGraph (Character (Rune '€')) winCtrlAlt)
                    { KeyModifiers.None with
                        AltGraph = true
                    }
                    "Ctrl+Alt+'€' normalizes to AltGraph"

                Expect.equal (LogicalKey.normalizeAltGraph (Character (Rune 'a')) winCtrlAlt) winCtrlAlt "Ctrl+Alt+'a' stays Ctrl+Alt")
        ]
