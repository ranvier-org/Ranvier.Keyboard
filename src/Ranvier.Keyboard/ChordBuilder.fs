namespace Ranvier.Keyboard

/// <summary>Builds a normalized chord from modifier flags and a final logical key.</summary>
[<Sealed>]
type ChordBuilder() =
    member inline _.Yield(()) = 0

    [<CustomOperation("ctrl")>]
    member inline _.Ctrl(bits: int) =
        bits ||| KeyModifiers.CtrlMask

    [<CustomOperation("alt")>]
    member inline _.Alt(bits: int) =
        bits ||| KeyModifiers.AltMask

    [<CustomOperation("shift")>]
    member inline _.Shift(bits: int) =
        bits ||| KeyModifiers.ShiftMask

    [<CustomOperation("meta")>]
    member inline _.Meta(bits: int) =
        bits ||| KeyModifiers.MetaMask

    [<CustomOperation("altGraph")>]
    member inline _.AltGraph(bits: int) =
        bits ||| KeyModifiers.AltGraphMask

    [<CustomOperation("key")>]
    member inline _.Key(bits: int, logical: LogicalKey) =
        KeyChord.ofLogical logical (KeyModifiers.ofBits bits)

    member inline _.Run(value: KeyChord) = value

[<AutoOpen>]
module ChordExpressions =
    /// <summary>Declare a shortcut, for example <c>chord { ctrl; key (Navigation ArrowLeft) }</c>.</summary>
    let chord = ChordBuilder ()
