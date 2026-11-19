module ChordExperiments.Probe

open System
open System.Text
open System.Runtime.CompilerServices
open Ranvier.Keyboard
open ChordExperiments.Builders

[<MethodImpl(MethodImplOptions.NoInlining)>]
let direct logical =
    KeyChord.ofLogical
        logical
        { KeyModifiers.None with
            Ctrl = true
            Shift = true
        }

[<MethodImpl(MethodImplOptions.NoInlining)>]
let recordCE logical =
    recordChord {
        ctrl
        shift
        key logical
    }

[<MethodImpl(MethodImplOptions.NoInlining)>]
let bitsCE logical =
    chord {
        ctrl
        shift
        key logical
    }

[<MethodImpl(MethodImplOptions.NoInlining)>]
let literal logical =
    KeyChord.ofLogical
        logical
        {
            Ctrl = true
            Alt = false
            Shift = true
            Meta = false
            AltGraph = false
        }

[<MethodImpl(MethodImplOptions.NoInlining)>]
let directBits logical =
    KeyChord.ofLogical logical (KeyModifiers.ofBits 5)

[<MethodImpl(MethodImplOptions.NoInlining)>]
let directPress chord code =
    { Chord = chord; Physical = code }

[<MethodImpl(MethodImplOptions.NoInlining)>]
let pressCE chord code =
    press {
        stroke chord
        physical code
    }

let keys =
    [|
        Navigation ArrowLeft
        Character (Rune 'Z')
        FunctionKey 5
        Character (Rune 0x1F642)
        Whitespace Space
        Ui Escape
        Character (Rune 'é')
        Unidentified "vendor-key"
    |]

let verify () =
    for logical in keys do
        let value = direct logical

        if
            pressCE value PhysicalKeyCode.KeyZ
            <> directPress value PhysicalKeyCode.KeyZ
        then
            failwith "physical code"

        if
            value <> literal logical
            || value <> directBits logical
        then
            failwith "baseline"

        if
            recordCE logical <> direct logical
            || bitsCE logical <> direct logical
        then
            failwith "modified chord"

        if
            recordChord { key logical }
            <> KeyChord.ofLogical logical KeyModifiers.None
        then
            failwith "plain chord"

        let mods =
            { KeyModifiers.None with
                Ctrl = true
                Alt = true
                AltGraph = true
                Meta = true
            }

        let expected = KeyChord.ofLogical logical mods

        if
            recordChord {
                ctrl
                alt
                altGraph
                meta
                key logical
            }
            <> expected
        then
            failwith "AltGraph normalization"

        if
            chord {
                ctrl
                alt
                altGraph
                meta
                key logical
            }
            <> expected
        then
            failwith "packed AltGraph normalization"

#if !FABLE_COMPILER
let measure (construct: LogicalKey -> KeyChord) =
    let timer = Diagnostics.Stopwatch.StartNew ()
    let before = GC.GetAllocatedBytesForCurrentThread ()
    let mutable checksum = 0L

    for i = 0 to 1999999 do
        checksum <- checksum + int64 (construct keys[i &&& 7]).Packed

    let bytes = GC.GetAllocatedBytesForCurrentThread () - before
    timer.Stop ()
    timer.Elapsed.TotalMilliseconds / 2., bytes, checksum

[<EntryPoint>]
let main _ =
    verify ()

    let cases =
        [|
            "direct", direct
            "record", recordCE
            "bits", bitsCE
            "literal", literal
            "directBits", directBits
        |]

    for _, construct in cases do
        measure construct |> ignore

    for round = 0 to 8 do
        for offset = 0 to cases.Length - 1 do
            let name, construct = cases[(round + offset) % cases.Length]
            let ns, bytes, checksum = measure construct
            Console.WriteLine ("{0} {1:F3} {2} {3}", name, ns, bytes, checksum)

    0
#else
verify ()
#endif
