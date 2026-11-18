module Ranvier.Keyboard.Smoke.Fable.Main

open Fable.Core
open Ranvier.Keyboard
open Ranvier.Keyboard.Dom
open Ranvier.Keyboard.Smoke

[<Emit("process.exitCode = $0")>]
let private setExitCode (code: int) : unit = jsNative

[<Emit("({ key: 'Escape', code: 'Escape', ctrlKey: false, altKey: false, shiftKey: false, metaKey: false })")>]
let private jsEscapeEvent () : Ranvier.Dom.KeyboardEvent = jsNative

[<Emit("({ key: '{', code: 'Digit7', ctrlKey: true, altKey: true, shiftKey: false, metaKey: false, getModifierState: function(k) { this.calls = (this.calls || 0) + 1; return false; } })")>]
let private jsAltGrEvent () : Ranvier.Dom.KeyboardEvent = jsNative

[<Emit("($0.calls || 0)")>]
let private jsCallsOf (ev: Ranvier.Dom.KeyboardEvent) : int = jsNative

[<Emit("({ key: 'Meta', code: 'OSLeft', ctrlKey: false, altKey: false, shiftKey: false, metaKey: true })")>]
let private jsOsLeftEvent () : Ranvier.Dom.KeyboardEvent = jsNative

let private jsDomChecks () : (string * bool) list =
    let esc = jsEscapeEvent ()
    let altGr = jsAltGrEvent ()
    let altGrPress = KeyboardEvent.toKeyPress altGr
    let osLeft = jsOsLeftEvent ()

    [
        "JS StringEnum PhysicalKeyCode erases directly to W3C string literals",
        unbox<string> PhysicalKeyCode.KeyA = "KeyA"
        && unbox<string> PhysicalKeyCode.UnidentifiedCode = "Unidentified"
        "JS plain object KeyboardEvent Escape is decoded without getModifierState",
        KeyboardEvent.isEscape esc
        && KeyboardEvent.physicalCode esc = PhysicalKeyCode.Escape
        && KeyboardEvent.toKeyChord esc = KeyChord.ofKey "Escape"
        "JS plain object KeyboardEvent AltGr calls getModifierState once and normalizes '{'",
        jsCallsOf altGr = 1
        && altGrPress.Physical = PhysicalKeyCode.Digit7
        && altGrPress.Modifiers.AltGraph
        && not altGrPress.Modifiers.Ctrl
        && KeyChord.isPlainChar '{' altGrPress.Chord
        "JS plain object KeyboardEvent OSLeft normalizes to PhysicalKeyCode.MetaLeft", KeyboardEvent.physicalCode osLeft = PhysicalKeyCode.MetaLeft
    ]

let private run () =
    let results = Checks.run () @ jsDomChecks ()
    let mutable failures = 0

    for name, ok in results do
        if ok then
            printfn "ok   %s" name
        else
            failures <- failures + 1
            printfn "FAIL %s" name

    if failures = 0 then
        printfn "all checks passed"
    else
        printfn "%d checks failed" failures
        setExitCode 1

run ()
