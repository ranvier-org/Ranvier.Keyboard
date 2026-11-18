module Ranvier.Keyboard.Tests.IsoAndW3cKeysTests

open System
open System.Text
open Expecto
open Ranvier.Keyboard

type private P = PhysicalKeyCode

let private allCases: (PhysicalKeyCode * string) array =
    [|
        P.Backquote, "Backquote"
        P.Digit1, "Digit1"
        P.Digit2, "Digit2"
        P.Digit3, "Digit3"
        P.Digit4, "Digit4"
        P.Digit5, "Digit5"
        P.Digit6, "Digit6"
        P.Digit7, "Digit7"
        P.Digit8, "Digit8"
        P.Digit9, "Digit9"
        P.Digit0, "Digit0"
        P.Minus, "Minus"
        P.Equal, "Equal"
        P.IntlYen, "IntlYen"
        P.Backspace, "Backspace"
        P.Tab, "Tab"
        P.KeyQ, "KeyQ"
        P.KeyW, "KeyW"
        P.KeyE, "KeyE"
        P.KeyR, "KeyR"
        P.KeyT, "KeyT"
        P.KeyY, "KeyY"
        P.KeyU, "KeyU"
        P.KeyI, "KeyI"
        P.KeyO, "KeyO"
        P.KeyP, "KeyP"
        P.BracketLeft, "BracketLeft"
        P.BracketRight, "BracketRight"
        P.Backslash, "Backslash"
        P.CapsLock, "CapsLock"
        P.KeyA, "KeyA"
        P.KeyS, "KeyS"
        P.KeyD, "KeyD"
        P.KeyF, "KeyF"
        P.KeyG, "KeyG"
        P.KeyH, "KeyH"
        P.KeyJ, "KeyJ"
        P.KeyK, "KeyK"
        P.KeyL, "KeyL"
        P.Semicolon, "Semicolon"
        P.Quote, "Quote"
        P.IntlHash, "IntlHash"
        P.Enter, "Enter"
        P.ShiftLeft, "ShiftLeft"
        P.IntlBackslash, "IntlBackslash"
        P.KeyZ, "KeyZ"
        P.KeyX, "KeyX"
        P.KeyC, "KeyC"
        P.KeyV, "KeyV"
        P.KeyB, "KeyB"
        P.KeyN, "KeyN"
        P.KeyM, "KeyM"
        P.Comma, "Comma"
        P.Period, "Period"
        P.Slash, "Slash"
        P.IntlRo, "IntlRo"
        P.ShiftRight, "ShiftRight"
        P.ControlLeft, "ControlLeft"
        P.MetaLeft, "MetaLeft"
        P.AltLeft, "AltLeft"
        P.Space, "Space"
        P.AltRight, "AltRight"
        P.MetaRight, "MetaRight"
        P.ContextMenu, "ContextMenu"
        P.ControlRight, "ControlRight"
        P.Convert, "Convert"
        P.NonConvert, "NonConvert"
        P.KanaMode, "KanaMode"
        P.Lang1, "Lang1"
        P.Lang2, "Lang2"
        P.Fn, "Fn"
        P.Escape, "Escape"
        P.F1, "F1"
        P.F2, "F2"
        P.F3, "F3"
        P.F4, "F4"
        P.F5, "F5"
        P.F6, "F6"
        P.F7, "F7"
        P.F8, "F8"
        P.F9, "F9"
        P.F10, "F10"
        P.F11, "F11"
        P.F12, "F12"
        P.F13, "F13"
        P.F14, "F14"
        P.F15, "F15"
        P.F16, "F16"
        P.F17, "F17"
        P.F18, "F18"
        P.F19, "F19"
        P.F20, "F20"
        P.F21, "F21"
        P.F22, "F22"
        P.F23, "F23"
        P.F24, "F24"
        P.PrintScreen, "PrintScreen"
        P.ScrollLock, "ScrollLock"
        P.Pause, "Pause"
        P.Insert, "Insert"
        P.Home, "Home"
        P.PageUp, "PageUp"
        P.Delete, "Delete"
        P.End, "End"
        P.PageDown, "PageDown"
        P.ArrowUp, "ArrowUp"
        P.ArrowDown, "ArrowDown"
        P.ArrowLeft, "ArrowLeft"
        P.ArrowRight, "ArrowRight"
        P.NumLock, "NumLock"
        P.NumpadDivide, "NumpadDivide"
        P.NumpadMultiply, "NumpadMultiply"
        P.NumpadSubtract, "NumpadSubtract"
        P.NumpadAdd, "NumpadAdd"
        P.NumpadEnter, "NumpadEnter"
        P.NumpadDecimal, "NumpadDecimal"
        P.NumpadComma, "NumpadComma"
        P.NumpadEqual, "NumpadEqual"
        P.Numpad0, "Numpad0"
        P.Numpad1, "Numpad1"
        P.Numpad2, "Numpad2"
        P.Numpad3, "Numpad3"
        P.Numpad4, "Numpad4"
        P.Numpad5, "Numpad5"
        P.Numpad6, "Numpad6"
        P.Numpad7, "Numpad7"
        P.Numpad8, "Numpad8"
        P.Numpad9, "Numpad9"
        P.UnidentifiedCode, "Unidentified"
    |]

let private assertAllParsersMatch (input: string) (expected: PhysicalKeyCode) =
    Expect.equal (PhysicalKeyCode.ofW3cCode input) expected $"ofW3cCode '{input}'"
    Expect.equal (PhysicalKeyCode.ofW3cCodeScalar (input.AsSpan ())) expected $"ofW3cCodeScalar '{input}'"
    Expect.equal (PhysicalKeyCode.ofW3cCodeSpan (input.AsSpan ())) expected $"ofW3cCodeSpan '{input}'"
    let utf8 = Encoding.UTF8.GetBytes input
    Expect.equal (PhysicalKeyCode.ofW3cCodeUtf8 (ReadOnlySpan<byte> utf8)) expected $"ofW3cCodeUtf8 '{input}'"

[<Tests>]
let tests =
    testList
        "IsoAndW3cKeys"
        [
            testCase "all 129 PhysicalKeyCode cases round-trip across string, span, utf8, and scalar" (fun () ->
                Expect.equal allCases.Length 129 "exact 129 cases"
                let distinctCases = allCases |> Array.map fst |> Array.distinct
                Expect.equal distinctCases.Length 129 "all 129 union cases are distinct"

                for code, name in allCases do
                    Expect.equal (PhysicalKeyCode.toW3cCode code) name $"toW3cCode {name}"
                    assertAllParsersMatch name code)

            testCase "legacy OSLeft and OSRight aliases map to MetaLeft and MetaRight across all parsers" (fun () ->
                assertAllParsersMatch "OSLeft" P.MetaLeft
                assertAllParsersMatch "OSRight" P.MetaRight
                Expect.equal (PhysicalKeyCode.ofW3cCode null) P.UnidentifiedCode "null maps to UnidentifiedCode")

            testCase "off-by-1 mutations, prefixes, suffixes, and case flips reject cleanly with SIMD/scalar parity" (fun () ->
                let validNames =
                    Set.ofArray (Array.append (allCases |> Array.map snd) [| "OSLeft"; "OSRight" |])

                for _, name in Array.append allCases [| P.MetaLeft, "OSLeft"; P.MetaRight, "OSRight" |] do
                    // Single-char mutations at every index
                    for idx = 0 to name.Length - 1 do
                        let chars = name.ToCharArray ()
                        chars[idx] <- if chars[idx] = 'X' then 'Y' else 'X'
                        let mutated = String chars

                        if not (validNames.Contains mutated) then
                            assertAllParsersMatch mutated P.UnidentifiedCode

                        // Case flip at idx
                        let caseFlipped = name.ToCharArray ()

                        caseFlipped[idx] <-
                            if Char.IsUpper caseFlipped[idx] then
                                Char.ToLowerInvariant caseFlipped[idx]
                            else
                                Char.ToUpperInvariant caseFlipped[idx]

                        let flippedStr = String caseFlipped

                        if not (validNames.Contains flippedStr) then
                            assertAllParsersMatch flippedStr P.UnidentifiedCode

                    // Prefixes and suffixes
                    for prefixLen = 0 to name.Length - 1 do
                        let pre = name.Substring (0, prefixLen)

                        if not (validNames.Contains pre) then
                            assertAllParsersMatch pre P.UnidentifiedCode

                    assertAllParsersMatch (name + "X") P.UnidentifiedCode
                    assertAllParsersMatch ("X" + name) P.UnidentifiedCode)

            testCase "non-ASCII high-bit characters with matching low byte never alias ASCII keys" (fun () ->
                for _, name in allCases do
                    if name <> "Unidentified" then
                        for idx = 0 to name.Length - 1 do
                            for highBits in [ 0x0100us; 0x0080us; 0x0200us; 0xFF00us ] do
                                let chars = name.ToCharArray ()
                                chars[idx] <- char (uint16 chars[idx] ||| highBits)
                                let spoofed = String chars
                                assertAllParsersMatch spoofed P.UnidentifiedCode)

            testCase "inputs of every length 0..64 return UnidentifiedCode unless exact match" (fun () ->
                for len = 0 to 64 do
                    let s = String ('a', len)
                    assertAllParsersMatch s P.UnidentifiedCode
                    let s2 = String ('F', len)
                    assertAllParsersMatch s2 P.UnidentifiedCode)

            testCase "USB HID Usage Page 0x07 mappings cover letters, digits, modifiers, function keys, and unmapped" (fun () ->
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x04us) P.KeyA "0x04 -> KeyA"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x1Dus) P.KeyZ "0x1D -> KeyZ"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x1Eus) P.Digit1 "0x1E -> Digit1"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x27us) P.Digit0 "0x27 -> Digit0"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x28us) P.Enter "0x28 -> Enter"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x29us) P.Escape "0x29 -> Escape"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x2Cus) P.Space "0x2C -> Space"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x3Aus) P.F1 "0x3A -> F1"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x45us) P.F12 "0x45 -> F12"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x68us) P.F13 "0x68 -> F13"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x73us) P.F24 "0x73 -> F24"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x64us) P.IntlBackslash "0x64 -> IntlBackslash"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x87us) P.IntlRo "0x87 -> IntlRo"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x89us) P.IntlYen "0x89 -> IntlYen"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0xE0us) P.ControlLeft "0xE0 -> ControlLeft"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0xE7us) P.MetaRight "0xE7 -> MetaRight"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0x00us) P.UnidentifiedCode "0x00 -> UnidentifiedCode"
                Expect.equal (PhysicalKeyCode.ofUsbHidUsage 0xFFFFus) P.UnidentifiedCode "0xFFFF -> UnidentifiedCode")

            testCase "ISO/IEC 9995-1 grid coordinates map standard rows and return ValueNone for unmapped keys" (fun () ->
                Expect.equal
                    (PhysicalKeyCode.toIso9995 P.IntlBackslash)
                    (ValueSome
                        {
                            Section = Alphanumeric
                            Row = RowB
                            Column = 0uy
                        })
                    "IntlBackslash is B00"

                Expect.equal
                    (PhysicalKeyCode.toIso9995 P.KeyA)
                    (ValueSome
                        {
                            Section = Alphanumeric
                            Row = RowC
                            Column = 1uy
                        })
                    "KeyA is C01"

                Expect.equal
                    (PhysicalKeyCode.toIso9995 P.Escape)
                    (ValueSome
                        {
                            Section = FunctionSection
                            Row = RowK
                            Column = 0uy
                        })
                    "Escape is K00"

                Expect.equal
                    (PhysicalKeyCode.toIso9995 P.ArrowDown)
                    (ValueSome
                        {
                            Section = EditingAndNavigation
                            Row = RowA
                            Column = 1uy
                        })
                    "ArrowDown is Nav A01"

                Expect.equal (PhysicalKeyCode.toIso9995 P.F13) ValueNone "F13 has no ISO 9995-1 grid slot"
                Expect.equal (PhysicalKeyCode.toIso9995 P.UnidentifiedCode) ValueNone "Unidentified has no slot")
        ]
