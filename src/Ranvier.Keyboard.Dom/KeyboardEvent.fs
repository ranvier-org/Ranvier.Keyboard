namespace Ranvier.Keyboard.Dom

open System
open Ranvier.Keyboard

/// <summary>Zero-overhead adapters decoding Ranvier.Dom.KeyboardEvent into Ranvier.Keyboard values.</summary>
[<RequireQualifiedAccess>]
module KeyboardEvent =

#if FABLE_COMPILER
    /// <summary>Reads ev.code as a PhysicalKeyCode with zero string-switch overhead in JavaScript.</summary>
    let inline physicalCode (ev: Ranvier.Dom.KeyboardEvent) : PhysicalKeyCode =
        if isNull ev then
            PhysicalKeyCode.UnidentifiedCode
        else
            let code = ev.code

            if String.IsNullOrEmpty code then
                PhysicalKeyCode.UnidentifiedCode
            elif code = "OSLeft" then
                PhysicalKeyCode.MetaLeft
            elif code = "OSRight" then
                PhysicalKeyCode.MetaRight
            else
                unbox<PhysicalKeyCode> code
#else
    /// <summary>Parses ev.code into a PhysicalKeyCode using SIMD W3C code lookup.</summary>
    let physicalCode (ev: Ranvier.Dom.KeyboardEvent) : PhysicalKeyCode =
        if isNull ev then
            PhysicalKeyCode.UnidentifiedCode
        else
            PhysicalKeyCode.ofW3cCode ev.code
#endif

    /// <summary>
    /// Reads modifier flags from ev, querying getModifierState("AltGraph") only when altKey or ctrlKey is active
    /// so unmodified keystrokes avoid interop / string-pinning overhead on native DOM hosts.
    /// </summary>
    let modifiers (ev: Ranvier.Dom.KeyboardEvent) : KeyModifiers =
        if isNull ev then
            KeyModifiers.None
        else
            let ctrl = ev.ctrlKey
            let alt = ev.altKey
            let shift = ev.shiftKey
            let meta = ev.metaKey

            let altGraph =
                if alt || ctrl then
                    try
                        ev.getModifierState "AltGraph"
                    with _ ->
                        false
                else
                    false

            {
                Ctrl = ctrl
                Alt = alt
                Shift = shift
                Meta = meta
                AltGraph = altGraph
            }

    /// <summary>Decodes ev.key into a LogicalKey.</summary>
    let logicalKey (ev: Ranvier.Dom.KeyboardEvent) : LogicalKey =
        if isNull ev then
            LogicalKey.Unidentified ""
        else
            LogicalKey.ofW3cKey ev.key

    /// <summary>Decodes ev into a normalized 30-bit packed KeyChord.</summary>
    let toKeyChord (ev: Ranvier.Dom.KeyboardEvent) : KeyChord =
        if isNull ev then
            KeyChord.rawOfLogical (LogicalKey.Unidentified "") KeyModifiers.None
        else
            let logical = LogicalKey.ofW3cKey ev.key
            let mods = modifiers ev
            KeyChord.ofLogical logical mods

    /// <summary>Decodes ev into a normalized KeyPress combining KeyChord and PhysicalKeyCode.</summary>
    let toKeyPress (ev: Ranvier.Dom.KeyboardEvent) : KeyPress =
        if isNull ev then
            {
                Chord = KeyChord.rawOfLogical (LogicalKey.Unidentified "") KeyModifiers.None
                Physical = PhysicalKeyCode.UnidentifiedCode
            }
        else
            let logical = LogicalKey.ofW3cKey ev.key
            let mods = modifiers ev
            let physical = physicalCode ev

            {
                Chord = KeyChord.ofLogical logical mods
                Physical = physical
            }

    /// <summary>True when ev is an unmodified Escape or legacy Esc key press (without calling getModifierState).</summary>
    let isEscape (ev: Ranvier.Dom.KeyboardEvent) : bool =
        if isNull ev then
            false
        elif
            ev.ctrlKey
            || ev.altKey
            || ev.shiftKey
            || ev.metaKey
        then
            false
        else
            let k = ev.key
            k = "Escape" || k = "Esc"
