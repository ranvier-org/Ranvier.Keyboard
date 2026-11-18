module Ranvier.Keyboard.Tests.KeyboardEventTests

open System
open System.Text
open Expecto
open Ranvier.Keyboard
open Ranvier.Keyboard.Dom

type FakeKeyboardEvent(key: string, code: string, ctrl: bool, alt: bool, shift: bool, meta: bool, onGetModifierState: string -> bool) =
    let mutable modifierQueries = ResizeArray<string>()
    let mutable cancelBubble = false
    let mutable returnValue = true

    member _.ModifierQueryCount = modifierQueries.Count
    member _.ModifierQueries = modifierQueries.ToArray ()

    interface Ranvier.Dom.KeyboardEvent with
        member _.``type`` = "keydown"
        member _.target = null
        member _.currentTarget = null
        member _.eventPhase = 2us
        member _.stopPropagation() = ()

        member _.cancelBubble
            with get () = cancelBubble
            and set v = cancelBubble <- v

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
            and set v = returnValue <- v

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
            modifierQueries.Add (keyArg)
            onGetModifierState keyArg

        member _.keyIdentifier = ""
        member _.charCode = 0u
        member _.keyCode = 0u

[<Tests>]
let tests =
    testList
        "KeyboardEvent"
        [
            testCase "modifiers does NOT call getModifierState when ctrlKey=false and altKey=false" (fun () ->
                let plainEv =
                    FakeKeyboardEvent ("j", "KeyJ", false, false, false, false, fun _ -> true)

                let m1 = KeyboardEvent.modifiers plainEv
                Expect.equal plainEv.ModifierQueryCount 0 "plain key must not query getModifierState"
                Expect.equal m1 KeyModifiers.None "plain modifiers"

                let shiftMetaEv =
                    FakeKeyboardEvent ("G", "KeyG", false, false, true, true, fun _ -> true)

                let m2 = KeyboardEvent.modifiers shiftMetaEv
                Expect.equal shiftMetaEv.ModifierQueryCount 0 "Shift+Meta without Ctrl/Alt must not query getModifierState"

                Expect.isTrue
                    (m2.Shift
                     && m2.Meta
                     && not m2.Ctrl
                     && not m2.Alt
                     && not m2.AltGraph)
                    "Shift+Meta flags")

            testCase "modifiers calls getModifierState('AltGraph') when ctrlKey or altKey is true and handles exceptions safely" (fun () ->
                let altEv =
                    FakeKeyboardEvent ("€", "KeyE", false, true, false, false, fun k -> k = "AltGraph")

                let mAlt = KeyboardEvent.modifiers altEv
                Expect.equal altEv.ModifierQueries [| "AltGraph" |] "Alt triggers AltGraph query"
                Expect.isTrue mAlt.AltGraph "AltGraph reported true"

                let ctrlEv =
                    FakeKeyboardEvent ("c", "KeyC", true, false, false, false, fun _ -> false)

                let mCtrl = KeyboardEvent.modifiers ctrlEv
                Expect.equal ctrlEv.ModifierQueries [| "AltGraph" |] "Ctrl triggers AltGraph query"
                Expect.isTrue (mCtrl.Ctrl && not mCtrl.AltGraph) "Ctrl without AltGraph"

                let throwingEv =
                    FakeKeyboardEvent ("x", "KeyX", true, false, false, false, fun _ -> invalidOp "unsupported")

                let mSafe = KeyboardEvent.modifiers throwingEv
                Expect.equal throwingEv.ModifierQueryCount 1 "queried once"
                Expect.isFalse mSafe.AltGraph "exception falls back to AltGraph=false")

            testCase "physicalCode, logicalKey, toKeyChord, and toKeyPress decode W3C events, OSLeft/OSRight, shifted symbols, and AltGr" (fun () ->
                let colonEv =
                    FakeKeyboardEvent (":", "Semicolon", false, false, true, false, fun _ -> false)

                Expect.equal (KeyboardEvent.physicalCode colonEv) PhysicalKeyCode.Semicolon "physical Semicolon"
                Expect.equal (KeyboardEvent.logicalKey colonEv) (Character (Rune ':')) "logical ':'"
                let colonChord = KeyboardEvent.toKeyChord colonEv
                Expect.equal colonEv.ModifierQueryCount 0 "no getModifierState on Shift+; -> ':'"
                Expect.isTrue (KeyChord.isPlainChar ':' colonChord) "isPlainChar ':'"
                Expect.equal colonChord (KeyChord.ofChar ':') "matches KeyChord.ofChar ':'"

                let osLeftEv =
                    FakeKeyboardEvent ("Meta", "OSLeft", false, false, false, true, fun _ -> false)

                let osRightEv =
                    FakeKeyboardEvent ("Meta", "OSRight", false, false, false, true, fun _ -> false)

                Expect.equal (KeyboardEvent.physicalCode osLeftEv) PhysicalKeyCode.MetaLeft "OSLeft -> MetaLeft"
                Expect.equal (KeyboardEvent.physicalCode osRightEv) PhysicalKeyCode.MetaRight "OSRight -> MetaRight"

                let winAltGrBrace =
                    FakeKeyboardEvent ("{", "Digit7", true, true, false, false, fun _ -> false)

                let press = KeyboardEvent.toKeyPress winAltGrBrace
                Expect.equal press.Physical PhysicalKeyCode.Digit7 "Digit7 physical"
                Expect.equal press.Logical (Character (Rune '{')) "logical '{'"
                Expect.isFalse press.Modifiers.Ctrl "Ctrl cleared by AltGraph normalization"
                Expect.isFalse press.Modifiers.Alt "Alt cleared by AltGraph normalization"
                Expect.isTrue press.Modifiers.AltGraph "AltGraph set"
                Expect.isTrue (KeyChord.isPlainChar '{' press.Chord) "AltGr '{' matches isPlainChar '{'")

            testCase "isEscape detects unmodified Escape/Esc without calling getModifierState and rejects modified/null events" (fun () ->
                let esc =
                    FakeKeyboardEvent ("Escape", "Escape", false, false, false, false, fun _ -> true)

                let legacyEsc =
                    FakeKeyboardEvent ("Esc", "Escape", false, false, false, false, fun _ -> true)

                let ctrlEsc =
                    FakeKeyboardEvent ("Escape", "Escape", true, false, false, false, fun _ -> true)

                let shiftEsc =
                    FakeKeyboardEvent ("Escape", "Escape", false, false, true, false, fun _ -> true)

                let enter =
                    FakeKeyboardEvent ("Enter", "Enter", false, false, false, false, fun _ -> true)

                Expect.isTrue (KeyboardEvent.isEscape esc) "Escape"
                Expect.equal esc.ModifierQueryCount 0 "isEscape never calls getModifierState"
                Expect.isTrue (KeyboardEvent.isEscape legacyEsc) "legacy Esc"
                Expect.isFalse (KeyboardEvent.isEscape ctrlEsc) "Ctrl+Escape is not plain Escape"
                Expect.equal ctrlEsc.ModifierQueryCount 0 "Ctrl+Escape short-circuits without getModifierState"
                Expect.isFalse (KeyboardEvent.isEscape shiftEsc) "Shift+Escape is not plain Escape"
                Expect.isFalse (KeyboardEvent.isEscape enter) "Enter is not Escape"
                Expect.isFalse (KeyboardEvent.isEscape null) "null is not Escape"

                Expect.equal (KeyboardEvent.physicalCode null) PhysicalKeyCode.UnidentifiedCode "null physicalCode"
                Expect.equal (KeyboardEvent.modifiers null) KeyModifiers.None "null modifiers"
                Expect.equal (KeyboardEvent.logicalKey null) (Unidentified "") "null logicalKey"
                Expect.equal (KeyboardEvent.toKeyChord null).Logical (Unidentified "") "null toKeyChord"
                Expect.equal (KeyboardEvent.toKeyPress null).Physical PhysicalKeyCode.UnidentifiedCode "null toKeyPress")
        ]
