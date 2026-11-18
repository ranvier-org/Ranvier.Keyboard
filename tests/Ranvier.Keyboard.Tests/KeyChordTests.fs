module Ranvier.Keyboard.Tests.KeyChordTests

open System
open System.Text
open Expecto
open Ranvier.Keyboard

[<Tests>]
let tests =
    testList
        "KeyChord"
        [
            testCase "rawOfLogical packs all LogicalKey cases and all 32 KeyModifiers into non-negative 30-bit ints" (fun () ->
                let sampleKeys =
                    [|
                        Character (Rune 0)
                        Character (Rune 'a')
                        Character (Rune 'G')
                        Character (Rune 0x1F600)
                        Character (Rune 0x10FFFF)
                        Whitespace Space
                        Whitespace Tab
                        Whitespace Enter
                        Navigation ArrowLeft
                        Navigation PageDown
                        Editing EditingKey.Backspace
                        Editing EditingKey.Clear
                        Ui Escape
                        Ui Cancel
                        FunctionKey 1
                        FunctionKey 12
                        FunctionKey 24
                        Modifier Shift
                        Modifier Fn
                        Dead Grave
                        Dead DotAbove
                        Ime Process
                        Ime ZenkakuHankaku
                        LeaderVirtual
                        Unidentified ""
                        Unidentified "Unidentified"
                        Unidentified "VendorCustom"
                        GraphemeText "e\u0301"
                    |]

                for k in sampleKeys do
                    for bits = 0 to 31 do
                        let mods = KeyModifiers.ofBits bits
                        let chord = KeyChord.rawOfLogical k mods
                        Expect.isGreaterThanOrEqual chord.Packed 0 $"non-negative Packed for {k}"
                        Expect.isLessThanOrEqual chord.Packed 0x3FFFFFFF $"30-bit Packed for {k}"
                        Expect.equal chord.Logical k $"round-trip Logical {k}"
                        Expect.equal chord.Modifiers mods $"round-trip Modifiers {bits}"
                        Expect.equal chord.Ctrl mods.Ctrl "Ctrl prop"
                        Expect.equal chord.Alt mods.Alt "Alt prop"
                        Expect.equal chord.Shift mods.Shift "Shift prop"
                        Expect.equal chord.Meta mods.Meta "Meta prop"
                        Expect.equal chord.AltGraph mods.AltGraph "AltGraph prop"
                        Expect.equal chord.W3cKey (LogicalKey.toW3cKey k) "W3cKey prop"

                        match k with
                        | GraphemeText _
                        | Unidentified "VendorCustom" -> Expect.isNotNull chord.OverflowText "overflow text present"
                        | _ -> Expect.isNull chord.OverflowText $"OverflowText must be null for {k}")

            testCase "isPlainChar matches lowercase, uppercase, shifted ASCII symbols, Space, and AltGr in 1 int32 check" (fun () ->
                let j = KeyChord.ofChar 'j'
                Expect.isTrue (KeyChord.isPlainChar 'j' j) "plain 'j'"
                Expect.isFalse (KeyChord.isPlainChar 'J' j) "'j' != 'J'"
                Expect.isFalse (KeyChord.isPlainChar 'k' j) "'j' != 'k'"

                // Uppercase 'G' from every possible input source
                let gSources =
                    [
                        KeyChord.ofChar 'G'
                        KeyChord.ofW3c ("g", false, false, true)
                        KeyChord.ofW3c ("G", false, false, true)
                        KeyChord.ofW3c ("G", false, false, false)
                        KeyChord.shift "g"
                    ]

                for gChord in gSources do
                    Expect.equal gChord (KeyChord.ofChar 'G') "all uppercase 'G' sources normalize to identical KeyChord"
                    Expect.isTrue (KeyChord.isPlainChar 'G' gChord) "isPlainChar 'G'"
                    Expect.isFalse (KeyChord.isPlainChar 'g' gChord) "not isPlainChar 'g'"
                    Expect.equal (KeyChord.tryPlainChar gChord) (ValueSome 'G') "tryPlainChar 'G'"

                // Shifted ASCII symbol pairs (US QWERTY unshifted+Shift, DOM shifted+Shift, AZERTY/JIS unshifted, ofChar)
                let shiftedPairs =
                    [
                        '`', '~'
                        '1', '!'
                        '2', '@'
                        '3', '#'
                        '4', '$'
                        '5', '%'
                        '6', '^'
                        '7', '&'
                        '8', '*'
                        '9', '('
                        '0', ')'
                        '-', '_'
                        '=', '+'
                        '[', '{'
                        ']', '}'
                        '\\', '|'
                        ';', ':'
                        '\'', '"'
                        ',', '<'
                        '.', '>'
                        '/', '?'
                    ]

                for baseCh, shiftedCh in shiftedPairs do
                    let expected = KeyChord.ofChar shiftedCh
                    let fromUnshiftedShift = KeyChord.ofW3c (string baseCh, false, false, true)
                    let fromDomShifted = KeyChord.ofW3c (string shiftedCh, false, false, true)
                    let fromAzertyUnshifted = KeyChord.ofW3c (string shiftedCh, false, false, false)

                    Expect.equal fromUnshiftedShift expected $"'{baseCh}'+Shift normalizes to ofChar '{shiftedCh}'"
                    Expect.equal fromDomShifted expected $"DOM '{shiftedCh}'+Shift normalizes to ofChar '{shiftedCh}'"
                    Expect.equal fromAzertyUnshifted expected $"unshifted '{shiftedCh}' normalizes to ofChar '{shiftedCh}'"
                    Expect.isTrue (KeyChord.isPlainChar shiftedCh fromUnshiftedShift) $"isPlainChar '{shiftedCh}' from '{baseCh}'+Shift"
                    Expect.isTrue (KeyChord.isPlainChar shiftedCh fromDomShifted) $"isPlainChar '{shiftedCh}' from DOM"
                    Expect.isFalse (KeyChord.isPlainChar baseCh fromUnshiftedShift) $"not isPlainChar '{baseCh}'"

                // German Windows AltGr+7 -> '{'
                let pressAltGr =
                    KeyChord.ofW3cEvent ("{", "Digit7", true, true, false, false, false)

                Expect.isFalse pressAltGr.Modifiers.Ctrl "AltGr clears Ctrl"
                Expect.isFalse pressAltGr.Modifiers.Alt "AltGr clears Alt"
                Expect.isTrue pressAltGr.Modifiers.AltGraph "AltGr sets AltGraph"
                Expect.isTrue (KeyChord.isPlainChar '{' pressAltGr.Chord) "AltGr '{' matches isPlainChar '{'"

                // Space
                Expect.isTrue (KeyChord.isPlainChar ' ' (KeyChord.ofChar ' ')) "ofChar ' '"
                Expect.isTrue (KeyChord.isPlainChar ' ' (KeyChord.ofW3c (" ", false, false, false))) "ofW3c ' '"
                Expect.isTrue (KeyChord.isPlainChar ' ' (KeyChord.ofKey "Spacebar")) "Spacebar"

                // Modified keys must NEVER match isPlainChar
                Expect.isFalse (KeyChord.isPlainChar 'c' (KeyChord.ctrl "c")) "Ctrl+c is not plain 'c'"
                Expect.isFalse (KeyChord.isPlainChar 'j' (KeyChord.alt "j")) "Alt+j is not plain 'j'"
                let metaPress = KeyChord.ofW3cEvent ("s", "KeyS", false, false, false, true, false)
                Expect.isFalse (KeyChord.isPlainChar 's' metaPress.Chord) "Meta+s is not plain 's'")

            testCase "Active patterns PlainChar, PlainRune, PlainText, ClassifiedKey, and CtrlC0 decompose chords accurately" (fun () ->
                match KeyChord.ofRune (Rune 0x1F600) with
                | KeyChord.PlainRune r -> Expect.equal r (Rune 0x1F600) "astral PlainRune"
                | _ -> failtest "expected PlainRune"

                match KeyChord.ofChar 'Z' with
                | KeyChord.PlainChar c -> Expect.equal c 'Z' "PlainChar Z"
                | _ -> failtest "expected PlainChar"

                match KeyChord.ofKey "👨‍👩‍👧‍👦" with
                | KeyChord.PlainText s -> Expect.equal s "👨‍👩‍👧‍👦" "ZWJ PlainText"
                | _ -> failtest "expected PlainText"

                match KeyChord.ctrl "k" with
                | KeyChord.CtrlC0 c0 -> Expect.equal c0 C0Control.Vt "<C-k> -> Vt"
                | _ -> failtest "expected CtrlC0 Vt"

                match KeyChord.ctrl "w" with
                | KeyChord.CtrlC0 c0 -> Expect.equal c0 C0Control.Etb "<C-w> -> Etb"
                | _ -> failtest "expected CtrlC0 Etb"

                match KeyChord.ctrl " " with
                | KeyChord.CtrlC0 c0 -> Expect.equal c0 C0Control.Nul "<C-Space> -> Nul"
                | _ -> failtest "expected CtrlC0 Nul"

                match KeyChord.ofKey "Escape" with
                | KeyChord.CtrlC0 _ -> failtest "plain Escape without Ctrl must not match CtrlC0"
                | KeyChord.ClassifiedKey (Ui Escape, mods, ValueNone) -> Expect.equal mods KeyModifiers.None "plain Escape"
                | _ -> failtest "unexpected ClassifiedKey"

                Expect.isTrue (KeyChord.isModifierOnly (KeyChord.ofKey "ShiftLeft")) "ShiftLeft is modifier-only"
                Expect.isFalse (KeyChord.isModifierOnly (KeyChord.ofChar 'a')) "'a' is not modifier-only"
                Expect.equal KeyChord.leader.Logical LeaderVirtual "leader is LeaderVirtual"

                // Non-ASCII lowercase + Shift normalizes to uppercase Rune
                let eAcuteShift =
                    KeyChord.ofLogical (Character (Rune 'é')) { KeyModifiers.None with Shift = true }

                Expect.equal eAcuteShift.Logical (Character (Rune 'É')) "é + Shift -> É")

            testCase "KeyChord.parse and SIMD parseArray match scalar across >160-char sequences, Vim tags, combining marks, and emoji" (fun () ->
                let longPlain =
                    String.init 175 (fun i ->
                        let ch = char (0x20 + (i % 95))
                        if ch = '<' then "x" else string ch)

                let arrSimd = KeyChord.parseArray (longPlain.AsSpan ())
                let arrScalar = KeyChord.parseArrayScalar (longPlain.AsSpan ())
                let lst = KeyChord.parse longPlain |> List.toArray
                Expect.equal arrSimd.Length 175 "175 plain ASCII chords"
                Expect.equal arrSimd arrScalar "SIMD parseArray == scalar on 175 plain ASCII chars"
                Expect.equal lst arrScalar "parse == scalar on 175 plain ASCII chars"

                let complexSeq =
                    String ('j', 64)
                    + "e\u0301"
                    + "<lt><Space><Bar><Bslash><leader><Esc><CR><BS><Tab><Up><Down><Left><Right><Home><End><PageUp><PageDown><Del><Ins><F12><C-k><C-S-x><C-A-d><M-j><S-g>"
                    + "😀"
                    + "a<b"

                let cSimd = KeyChord.parseArray (complexSeq.AsSpan ())
                let cScalar = KeyChord.parseArrayScalar (complexSeq.AsSpan ())
                let cList = KeyChord.parse complexSeq |> List.toArray
                Expect.equal cSimd cScalar "SIMD == scalar on complex mixed sequence"
                Expect.equal cList cScalar "parse == scalar on complex mixed sequence"

                let tail = cSimd[64..]
                Expect.equal tail[0].Logical (GraphemeText "e\u0301") "combining mark after 64 ASCII chars"
                Expect.equal tail[1] (KeyChord.ofChar '<') "<lt>"
                Expect.equal tail[2] (KeyChord.ofChar ' ') "<Space>"
                Expect.equal tail[3] (KeyChord.ofChar '|') "<Bar>"
                Expect.equal tail[4] (KeyChord.ofChar '\\') "<Bslash>"
                Expect.equal tail[5] KeyChord.leader "<leader>"
                Expect.equal tail[6].Logical (Ui Escape) "<Esc>"
                Expect.equal tail[7].Logical (Whitespace Enter) "<CR>"
                Expect.equal tail[8].Logical (Editing EditingKey.Backspace) "<BS>"
                Expect.equal tail[9].Logical (Whitespace Tab) "<Tab>"
                Expect.equal tail[10].Logical (Navigation ArrowUp) "<Up>"
                Expect.equal tail[11].Logical (Navigation ArrowDown) "<Down>"
                Expect.equal tail[12].Logical (Navigation ArrowLeft) "<Left>"
                Expect.equal tail[13].Logical (Navigation ArrowRight) "<Right>"
                Expect.equal tail[14].Logical (Navigation Home) "<Home>"
                Expect.equal tail[15].Logical (Navigation End) "<End>"
                Expect.equal tail[16].Logical (Navigation PageUp) "<PageUp>"
                Expect.equal tail[17].Logical (Navigation PageDown) "<PageDown>"
                Expect.equal tail[18].Logical (Editing EditingKey.Delete) "<Del>"
                Expect.equal tail[19].Logical (Editing EditingKey.Insert) "<Ins>"
                Expect.equal tail[20].Logical (FunctionKey 12) "<F12>"
                Expect.equal tail[21] (KeyChord.ctrl "k") "<C-k>"
                Expect.equal tail[22].Logical (Character (Rune 'x')) "<C-S-x> logical"

                Expect.isTrue
                    (tail[22].Ctrl
                     && tail[22].Shift
                     && not tail[22].Alt)
                    "<C-S-x> mods"

                Expect.isTrue
                    (tail[23].Ctrl
                     && tail[23].Alt
                     && not tail[23].Shift)
                    "<C-A-d> mods"

                Expect.equal tail[24] (KeyChord.alt "j") "<M-j>"
                Expect.equal tail[25] (KeyChord.ofChar 'G') "<S-g> -> 'G'"
                Expect.equal tail[26] (KeyChord.ofRune (Rune 0x1F600)) "astral 😀"
                Expect.equal tail[27] (KeyChord.ofChar 'a') "trailing 'a'"
                Expect.equal tail[28] (KeyChord.ofChar '<') "unclosed '<'"
                Expect.equal tail[29] (KeyChord.ofChar 'b') "trailing 'b'")
        ]
