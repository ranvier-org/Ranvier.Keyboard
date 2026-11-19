module ChordExperiments.Builders

open Ranvier.Keyboard

type RecordChordBuilder() =
    member inline _.Yield(()) =
        KeyModifiers.None

    [<CustomOperation("ctrl")>]
    member inline _.Ctrl(mods: KeyModifiers) =
        { mods with Ctrl = true }

    [<CustomOperation("alt")>]
    member inline _.Alt(mods: KeyModifiers) =
        { mods with Alt = true }

    [<CustomOperation("shift")>]
    member inline _.Shift(mods: KeyModifiers) =
        { mods with Shift = true }

    [<CustomOperation("meta")>]
    member inline _.Meta(mods: KeyModifiers) =
        { mods with Meta = true }

    [<CustomOperation("altGraph")>]
    member inline _.AltGraph(mods: KeyModifiers) =
        { mods with AltGraph = true }

    [<CustomOperation("key")>]
    member inline _.Key(mods: KeyModifiers, logical: LogicalKey) =
        KeyChord.ofLogical logical mods

    member inline _.Run(chord: KeyChord) = chord

let recordChord = RecordChordBuilder ()

type PressBuilder() =
    member inline _.Yield(()) = ()

    [<CustomOperation("stroke")>]
    member inline _.Stroke((), value: KeyChord) = value

    [<CustomOperation("physical")>]
    member inline _.Physical(value: KeyChord, code: PhysicalKeyCode) =
        { Chord = value; Physical = code }

    member inline _.Run(value: KeyPress) = value

let press = PressBuilder ()
