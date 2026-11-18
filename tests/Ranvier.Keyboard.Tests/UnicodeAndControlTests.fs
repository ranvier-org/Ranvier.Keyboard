module Ranvier.Keyboard.Tests.UnicodeAndControlTests

open System
open System.Text
open Expecto
open Ranvier.Keyboard

[<Tests>]
let tests =
    testList
        "UnicodeAndControl"
        [
            testCase "C0Control ofByte and toByte round-trip all 33 control codes and reject printable/high bytes" (fun () ->
                let mutable count = 0

                for b = 0 to 255 do
                    let by = byte b

                    if b <= 0x1F || b = 0x7F then
                        count <- count + 1

                        match C0Control.ofByte by with
                        | ValueSome ctrl -> Expect.equal (C0Control.toByte ctrl) by $"round-trip 0x{b:X2}"
                        | ValueNone -> failtest $"expected C0Control for 0x{b:X2}"
                    else
                        Expect.equal (C0Control.ofByte by) ValueNone $"non-control 0x{b:X2}"

                Expect.equal count 33 "33 C0 control codes")

            testCase "C0Control ofModifiedAscii and toCaretNotation follow ISO/IEC 646 caret notation" (fun () ->
                Expect.equal (C0Control.ofModifiedAscii '@') (ValueSome C0Control.Nul) "@ -> Nul"
                Expect.equal (C0Control.ofModifiedAscii ' ') (ValueSome C0Control.Nul) "Space -> Nul"
                Expect.equal (C0Control.ofModifiedAscii 'a') (ValueSome C0Control.Soh) "a -> Soh"
                Expect.equal (C0Control.ofModifiedAscii 'A') (ValueSome C0Control.Soh) "A -> Soh"
                Expect.equal (C0Control.ofModifiedAscii 'w') (ValueSome C0Control.Etb) "w -> Etb"
                Expect.equal (C0Control.ofModifiedAscii '[') (ValueSome C0Control.Esc) "[ -> Esc"
                Expect.equal (C0Control.ofModifiedAscii '_') (ValueSome C0Control.Us) "_ -> Us"
                Expect.equal (C0Control.ofModifiedAscii '?') (ValueSome C0Control.Del) "? -> Del"
                Expect.equal (C0Control.ofModifiedAscii '1') ValueNone "1 -> ValueNone"

                Expect.equal (C0Control.toCaretNotation C0Control.Nul) "^@" "Nul caret"
                Expect.equal (C0Control.toCaretNotation C0Control.Soh) "^A" "Soh caret"
                Expect.equal (C0Control.toCaretNotation C0Control.Etb) "^W" "Etb caret"
                Expect.equal (C0Control.toCaretNotation C0Control.Esc) "^[" "Esc caret"
                Expect.equal (C0Control.toCaretNotation C0Control.Us) "^_" "Us caret"
                Expect.equal (C0Control.toCaretNotation C0Control.Del) "^?" "Del caret")

            testCase "C0Control SIMD indexOfControlByte and indexOfControlChar match scalar across lengths 0..150" (fun () ->
                for len = 0 to 150 do
                    let cleanBytes = Array.init len (fun i -> byte (0x20 + (i % 95)))
                    // Ensure high bytes >= 0x80 are not misclassified as control bytes
                    if len > 4 then
                        cleanBytes[len / 2] <- 0x80uy
                        cleanBytes[len - 1] <- 0xFFuy

                    Expect.equal (C0Control.indexOfControlByte (ReadOnlySpan cleanBytes)) -1 $"clean bytes len={len}"
                    Expect.equal (C0Control.indexOfControlByteScalar (ReadOnlySpan cleanBytes)) -1 $"scalar clean bytes len={len}"

                    let cleanChars =
                        Array.init len (fun i ->
                            if i % 7 = 0 then '\u0100' // low byte 0x00 but high byte 0x01!
                            elif i % 11 = 0 then '\u017F' // low byte 0x7F but high byte 0x01!
                            else char (0x20 + (i % 95)))

                    Expect.equal (C0Control.indexOfControlChar (ReadOnlySpan cleanChars)) -1 $"clean chars len={len}"
                    Expect.equal (C0Control.indexOfControlCharScalar (ReadOnlySpan cleanChars)) -1 $"scalar clean chars len={len}"

                    for pos = 0 to len - 1 do
                        for ctrlByte in [ 0x00uy; 0x0Auy; 0x1Buy; 0x1Fuy; 0x7Fuy ] do
                            cleanBytes[pos] <- ctrlByte
                            let bActual = C0Control.indexOfControlByte (ReadOnlySpan cleanBytes)
                            let bScalar = C0Control.indexOfControlByteScalar (ReadOnlySpan cleanBytes)
                            Expect.equal bActual pos $"byte ctrl=0x{ctrlByte:X2} len={len} pos={pos}"
                            Expect.equal bActual bScalar "byte SIMD=scalar"
                            cleanBytes[pos] <- 0x41uy

                            let savedChar = cleanChars[pos]
                            cleanChars[pos] <- char ctrlByte
                            let cActual = C0Control.indexOfControlChar (ReadOnlySpan cleanChars)
                            let cScalar = C0Control.indexOfControlCharScalar (ReadOnlySpan cleanChars)
                            Expect.equal cActual pos $"char ctrl=0x{ctrlByte:X2} len={len} pos={pos}"
                            Expect.equal cActual cScalar "char SIMD=scalar"
                            cleanChars[pos] <- savedChar)

            testCase "UnicodeChar classifies brackets, quotes, letters, digits, connectors, symbols, and Vim word classes" (fun () ->
                Expect.equal (UnicodeChar.tryBracketPartner (Rune '(')) (ValueSome (struct (OpeningBracket, Rune ')'))) "( -> )"

                Expect.equal (UnicodeChar.tryBracketPartner (Rune '』')) (ValueSome (struct (ClosingBracket, Rune '『'))) "』 -> 『"

                Expect.equal (UnicodeChar.tryBracketPartner (Rune 'x')) ValueNone "x has no bracket partner"

                Expect.equal (UnicodeChar.classify (Rune 'A')) (UnicodeLetter LetterCase.Uppercase) "A uppercase"
                Expect.equal (UnicodeChar.classify (Rune 'z')) (UnicodeLetter LetterCase.Lowercase) "z lowercase"
                Expect.equal (UnicodeChar.classify (Rune 'ǅ')) (UnicodeLetter LetterCase.Titlecase) "ǅ titlecase"
                Expect.equal (UnicodeChar.classify (Rune 'ʰ')) (UnicodeLetter LetterCase.ModifierLetter) "ʰ modifier"
                Expect.equal (UnicodeChar.classify (Rune 'あ')) (UnicodeLetter LetterCase.CaselessLetter) "あ caseless"
                Expect.equal (UnicodeChar.classify (Rune '7')) (UnicodeDigit (ValueSome 7)) "ASCII 7"
                Expect.equal (UnicodeChar.classify (Rune '٥')) (UnicodeDigit (ValueSome 5)) "Arabic-Indic 5"
                Expect.equal (UnicodeChar.classify (Rune '７')) (UnicodeDigit (ValueSome 7)) "Fullwidth 7"
                Expect.equal (UnicodeChar.classify (Rune 'Ⅳ')) (UnicodeDigit ValueNone) "Roman IV"
                Expect.equal (UnicodeChar.classify (Rune '_')) UnicodeConnector "_ connector"
                Expect.equal (UnicodeChar.classify (Rune '"')) (UnicodeQuote DoubleAscii) "double quote"
                Expect.equal (UnicodeChar.classify (Rune '“')) (UnicodeQuote CurvedDoubleOpen) "curved open quote"
                Expect.equal (UnicodeChar.classify (Rune '[')) (UnicodeBracket (OpeningBracket, Rune ']')) "[ bracket"
                Expect.equal (UnicodeChar.classify (Rune '+')) UnicodeMathOrSymbol "+ math"
                Expect.equal (UnicodeChar.classify (Rune '!')) UnicodePunctuation "! punct"
                Expect.equal (UnicodeChar.classify (Rune ' ')) UnicodeWhitespace "space"
                Expect.equal (UnicodeChar.classify (Rune 0x1B)) (UnicodeControl (ValueSome C0Control.Esc)) "ESC control"
                Expect.equal (UnicodeChar.classify (Rune 0x0301)) UnicodeMarkOrOther "combining acute"

                Expect.isTrue (UnicodeChar.isVimWordRune (Rune 'a')) "a is word"
                Expect.isTrue (UnicodeChar.isVimWordRune (Rune '_')) "_ is word"
                Expect.isTrue (UnicodeChar.isVimWordRune (Rune '9')) "9 is word"
                Expect.isFalse (UnicodeChar.isVimWordRune (Rune '.')) ". is not word"
                Expect.equal (UnicodeChar.vimWordClass (Rune ' ')) 0 "space class 0"
                Expect.equal (UnicodeChar.vimWordClass (Rune 'w')) 1 "word class 1"
                Expect.equal (UnicodeChar.vimWordClass (Rune '+')) 2 "symbol class 2")

            testCase "Grapheme cluster boundaries handle CRLF, combining marks, astral emoji, ZWJ, and long ASCII SIMD runs" (fun () ->
                Expect.equal (Grapheme.nextClusterLength "" 0) 0 "empty"
                Expect.equal (Grapheme.nextClusterLength "abc" -1) 0 "negative"
                Expect.equal (Grapheme.nextClusterLength "abc" 3) 0 "at end"
                Expect.equal (Grapheme.nextClusterLength "\r\n" 0) 2 "CRLF length 2"
                Expect.equal (Grapheme.prevClusterStart "\r\n" 2) 0 "CRLF prev start 0"

                // Combining mark right after 63 ASCII chars (critical SIMD 64-lane boundary!)
                let boundaryLine =
                    String ('a', 63)
                    + "e\u0301"
                    + "😀"
                    + "👨‍👩‍👧‍👦"
                    + String ('z', 80)

                Expect.equal (Grapheme.stepForward 63 boundaryLine 0) 63 "step 63 reaches 'e' before combining acute"
                Expect.equal (Grapheme.stepForward 64 boundaryLine 0) 65 "step 64 crosses 'e\\u0301' (2 UTF-16 units)"
                Expect.equal (Grapheme.stepForward 65 boundaryLine 0) 67 "step 65 crosses '😀' (surrogate pair)"

                let afterZwj = Grapheme.stepForward 66 boundaryLine 0
                Expect.equal afterZwj (67 + "👨‍👩‍👧‍👦".Length) "step 66 crosses ZWJ family cluster"
                Expect.equal (Grapheme.stepForward 146 boundaryLine 0) boundaryLine.Length "step 146 reaches end"

                // Step backward across the entire mixed string
                Expect.equal (Grapheme.stepBackward 80 boundaryLine boundaryLine.Length) afterZwj "stepBackward 80 ASCII chars"
                Expect.equal (Grapheme.stepBackward 81 boundaryLine boundaryLine.Length) 67 "stepBackward across ZWJ family"
                Expect.equal (Grapheme.stepBackward 82 boundaryLine boundaryLine.Length) 65 "stepBackward across 😀"
                Expect.equal (Grapheme.stepBackward 83 boundaryLine boundaryLine.Length) 63 "stepBackward across e\\u0301"
                Expect.equal (Grapheme.stepBackward 146 boundaryLine boundaryLine.Length) 0 "stepBackward all the way to 0")

            testCase "Grapheme runeAt and SIMD/Unicode case transforms preserve ASCII, Greek, accented, and emoji text" (fun () ->
                let emoji = "a😀b"
                Expect.equal (Grapheme.runeAt emoji 0) (Rune 'a') "runeAt 0"
                Expect.equal (Grapheme.runeAt emoji 1) (Rune 0x1F600) "runeAt astral"
                Expect.equal (Grapheme.runeAt emoji 2) Rune.ReplacementChar "runeAt trailing surrogate -> U+FFFD"
                Expect.equal (Grapheme.runeAt emoji -1) Rune.ReplacementChar "runeAt -1 -> U+FFFD"

                let longAscii =
                    String.init 145 (fun i ->
                        if i % 3 = 0 then string (char (int 'A' + (i % 26)))
                        elif i % 3 = 1 then string (char (int 'a' + (i % 26)))
                        else string (char (int '0' + (i % 10))))

                let expectedLower = longAscii.ToLowerInvariant ()
                let expectedUpper = longAscii.ToUpperInvariant ()

                let expectedToggled =
                    longAscii.ToCharArray ()
                    |> Array.map (fun c ->
                        if Char.IsUpper c then Char.ToLowerInvariant c
                        elif Char.IsLower c then Char.ToUpperInvariant c
                        else c)
                    |> String

                Expect.equal (Grapheme.toLower longAscii) expectedLower "SIMD ASCII toLower (145 chars)"
                Expect.equal (Grapheme.toUpper longAscii) expectedUpper "SIMD ASCII toUpper (145 chars)"
                Expect.equal (Grapheme.toggleCase longAscii) expectedToggled "SIMD ASCII toggleCase (145 chars)"

                Expect.equal (Grapheme.toLower "Héllo ΩMEGA 😀!") "héllo ωmega 😀!" "Unicode toLower"
                Expect.equal (Grapheme.toUpper "Héllo ωmega 😀!") "HÉLLO ΩMEGA 😀!" "Unicode toUpper"
                Expect.equal (Grapheme.toggleCase "Héllo Ωmega 😀!") "hÉLLO ωMEGA 😀!" "Unicode toggleCase")
        ]
