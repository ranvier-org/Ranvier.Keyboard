namespace Ranvier.Keyboard

open System
open System.Text
#if !FABLE_COMPILER
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Intrinsics
#endif

/// <summary>
/// 30-bit packed key representation:
/// bits 0..20  (21 bits): payload (Rune.Value for Character, case ordinal, or 1..24 for FunctionKey)
/// bits 21..24 (4 bits) : LogicalKey kind tag (0..11)
/// bits 25..29 (5 bits) : KeyModifiers (Ctrl=25, Alt=26, Shift=27, Meta=28, AltGraph=29)
/// </summary>
#if FABLE_COMPILER
[<Fable.Core.Erase>]
#else
[<Struct>]
#endif
type PackedKey = PackedKey of int

module internal PackedKeyLayout =

    [<Literal>]
    let PayloadMask = 0x001FFFFF

    [<Literal>]
    let KindShift = 21

    [<Literal>]
    let KindMask = 0xF

    [<Literal>]
    let ModShift = 25

    [<Literal>]
    let ModMask = 0x1F

    [<Literal>]
    let KindCharacter = 0

    [<Literal>]
    let KindWhitespace = 1

    [<Literal>]
    let KindNavigation = 2

    [<Literal>]
    let KindEditing = 3

    [<Literal>]
    let KindUi = 4

    [<Literal>]
    let KindFunction = 5

    [<Literal>]
    let KindModifier = 6

    [<Literal>]
    let KindDead = 7

    [<Literal>]
    let KindIme = 8

    [<Literal>]
    let KindLeader = 9

    [<Literal>]
    let KindUnidentified = 10

    [<Literal>]
    let KindGraphemeText = 11

    /// Mask retaining payload (0..20), kind (21..24), Ctrl (25), Alt (26), and Meta (28), while masking out Shift (27) and AltGraph (29).
    [<Literal>]
    let PlainCharComparisonMask = 0x17FFFFFF

    let pack (logical: LogicalKey) (mods: KeyModifiers) : struct (int * string) =
        let modBits =
            (KeyModifiers.toBits mods &&& ModMask)
            <<< ModShift

        match logical with
        | Character r ->
            let packed =
                (r.Value &&& PayloadMask)
                ||| (KindCharacter <<< KindShift)
                ||| modBits

            struct (packed, null)
        | Whitespace ws ->
            let p =
                match ws with
                | Space -> 0
                | Tab -> 1
                | Enter -> 2

            struct (p ||| (KindWhitespace <<< KindShift) ||| modBits, null)
        | Navigation nav ->
            let p =
                match nav with
                | ArrowLeft -> 0
                | ArrowRight -> 1
                | ArrowUp -> 2
                | ArrowDown -> 3
                | Home -> 4
                | End -> 5
                | PageUp -> 6
                | PageDown -> 7

            struct (p ||| (KindNavigation <<< KindShift) ||| modBits, null)
        | Editing ed ->
            let p =
                match ed with
                | EditingKey.Backspace -> 0
                | EditingKey.Delete -> 1
                | EditingKey.Insert -> 2
                | EditingKey.Clear -> 3

            struct (p ||| (KindEditing <<< KindShift) ||| modBits, null)
        | Ui ui ->
            let p =
                match ui with
                | Escape -> 0
                | ContextMenu -> 1
                | Pause -> 2
                | Cancel -> 3

            struct (p ||| (KindUi <<< KindShift) ||| modBits, null)
        | FunctionKey n ->
            let p = if n < 0 then 0 else (n &&& PayloadMask)
            struct (p ||| (KindFunction <<< KindShift) ||| modBits, null)
        | Modifier m ->
            let p =
                match m with
                | Shift -> 0
                | Control -> 1
                | Alt -> 2
                | AltGraph -> 3
                | Meta -> 4
                | CapsLock -> 5
                | NumLock -> 6
                | ScrollLock -> 7
                | Symbol -> 8
                | Fn -> 9

            struct (p ||| (KindModifier <<< KindShift) ||| modBits, null)
        | Dead acc ->
            let p =
                match acc with
                | Grave -> 0
                | Acute -> 1
                | Circumflex -> 2
                | Tilde -> 3
                | Diaeresis -> 4
                | RingAbove -> 5
                | Cedilla -> 6
                | Caron -> 7
                | Macron -> 8
                | Breve -> 9
                | DoubleAcute -> 10
                | Ogonek -> 11
                | Stroke -> 12
                | DotAbove -> 13

            struct (p ||| (KindDead <<< KindShift) ||| modBits, null)
        | Ime ime ->
            let p =
                match ime with
                | Process -> 0
                | Convert -> 1
                | NonConvert -> 2
                | ModeChange -> 3
                | KanjiMode -> 4
                | KanaMode -> 5
                | HangulMode -> 6
                | HanjaMode -> 7
                | Eisu -> 8
                | ZenkakuHankaku -> 9

            struct (p ||| (KindIme <<< KindShift) ||| modBits, null)
        | LeaderVirtual -> struct ((KindLeader <<< KindShift) ||| modBits, null)
        | Unidentified raw ->
            if String.IsNullOrEmpty raw then
                struct (0 ||| (KindUnidentified <<< KindShift) ||| modBits, null)
            elif raw = "Unidentified" then
                struct (1 ||| (KindUnidentified <<< KindShift) ||| modBits, null)
            else
                struct (2 ||| (KindUnidentified <<< KindShift) ||| modBits, raw)
        | GraphemeText text ->
            let nonNull = if isNull text then "" else text
            struct ((KindGraphemeText <<< KindShift) ||| modBits, nonNull)

    let unpackLogical (packed: int) (overflowText: string) : LogicalKey =
        let kind = (packed >>> KindShift) &&& KindMask
        let payload = packed &&& PayloadMask

        match kind with
        | 0 ->
            let mutable r = Rune.ReplacementChar

            if Rune.TryCreate (payload, &r) then
                Character r
            else
                Character Rune.ReplacementChar
        | 1 ->
            match payload with
            | 1 -> Whitespace Tab
            | 2 -> Whitespace Enter
            | _ -> Whitespace Space
        | 2 ->
            match payload with
            | 0 -> Navigation ArrowLeft
            | 1 -> Navigation ArrowRight
            | 2 -> Navigation ArrowUp
            | 3 -> Navigation ArrowDown
            | 4 -> Navigation Home
            | 5 -> Navigation End
            | 6 -> Navigation PageUp
            | _ -> Navigation PageDown
        | 3 ->
            match payload with
            | 0 -> Editing EditingKey.Backspace
            | 1 -> Editing EditingKey.Delete
            | 2 -> Editing EditingKey.Insert
            | _ -> Editing EditingKey.Clear
        | 4 ->
            match payload with
            | 0 -> Ui Escape
            | 1 -> Ui ContextMenu
            | 2 -> Ui Pause
            | _ -> Ui Cancel
        | 5 -> FunctionKey payload
        | 6 ->
            match payload with
            | 0 -> Modifier Shift
            | 1 -> Modifier Control
            | 2 -> Modifier Alt
            | 3 -> Modifier AltGraph
            | 4 -> Modifier Meta
            | 5 -> Modifier CapsLock
            | 6 -> Modifier NumLock
            | 7 -> Modifier ScrollLock
            | 8 -> Modifier Symbol
            | _ -> Modifier Fn
        | 7 ->
            match payload with
            | 0 -> Dead Grave
            | 1 -> Dead Acute
            | 2 -> Dead Circumflex
            | 3 -> Dead Tilde
            | 4 -> Dead Diaeresis
            | 5 -> Dead RingAbove
            | 6 -> Dead Cedilla
            | 7 -> Dead Caron
            | 8 -> Dead Macron
            | 9 -> Dead Breve
            | 10 -> Dead DoubleAcute
            | 11 -> Dead Ogonek
            | 12 -> Dead Stroke
            | _ -> Dead DotAbove
        | 8 ->
            match payload with
            | 0 -> Ime ImeKey.Process
            | 1 -> Ime ImeKey.Convert
            | 2 -> Ime ImeKey.NonConvert
            | 3 -> Ime ImeKey.ModeChange
            | 4 -> Ime ImeKey.KanjiMode
            | 5 -> Ime ImeKey.KanaMode
            | 6 -> Ime ImeKey.HangulMode
            | 7 -> Ime ImeKey.HanjaMode
            | 8 -> Ime ImeKey.Eisu
            | _ -> Ime ImeKey.ZenkakuHankaku
        | 9 -> LeaderVirtual
        | 10 ->
            match payload with
            | 0 -> Unidentified ""
            | 1 -> Unidentified "Unidentified"
            | _ -> Unidentified (if isNull overflowText then "" else overflowText)
        | _ -> GraphemeText (if isNull overflowText then "" else overflowText)

/// <summary>Helpers for PackedKey.</summary>
[<RequireQualifiedAccess>]
module PackedKey =

    /// <summary>Extracts the underlying 30-bit integer value.</summary>
    let inline value (PackedKey v) : int = v

    /// <summary>Packs a single-unit LogicalKey and KeyModifiers into a 30-bit PackedKey.</summary>
    let pack (logical: LogicalKey) (mods: KeyModifiers) : PackedKey =
        let struct (bits, _) = PackedKeyLayout.pack logical mods
        PackedKey bits

    /// <summary>Unpacks KeyModifiers from a 30-bit PackedKey.</summary>
    let inline unpackModifiers (PackedKey bits) : KeyModifiers =
        KeyModifiers.ofBits (
            (bits >>> PackedKeyLayout.ModShift)
            &&& PackedKeyLayout.ModMask
        )

    /// <summary>Unpacks LogicalKey from a 30-bit PackedKey (with optional overflow text for multi-scalar GraphemeText).</summary>
    let unpackLogical (overflowText: string) (PackedKey bits) : LogicalKey =
        PackedKeyLayout.unpackLogical bits overflowText

/// <summary>Compact 30-bit packed key chord with null OverflowText for all single-scalar and named W3C keys.</summary>
[<Struct>]
type KeyChord =
    {
        Packed: int
        OverflowText: string
    }

    /// <summary>Decoded W3C LogicalKey.</summary>
    member this.Logical: LogicalKey =
        PackedKeyLayout.unpackLogical this.Packed this.OverflowText

    /// <summary>Decoded KeyModifiers.</summary>
    member inline this.Modifiers: KeyModifiers =
        KeyModifiers.ofBits ((this.Packed >>> 25) &&& 0x1F)

    /// <summary>True when Control is active.</summary>
    member inline this.Ctrl: bool = (this.Packed &&& (1 <<< 25)) <> 0

    /// <summary>True when Alt is active.</summary>
    member inline this.Alt: bool = (this.Packed &&& (1 <<< 26)) <> 0

    /// <summary>True when Shift is active.</summary>
    member inline this.Shift: bool = (this.Packed &&& (1 <<< 27)) <> 0

    /// <summary>True when Meta / Super / Command is active.</summary>
    member inline this.Meta: bool = (this.Packed &&& (1 <<< 28)) <> 0

    /// <summary>True when ISO/IEC 9995-3 AltGraph is active.</summary>
    member inline this.AltGraph: bool = (this.Packed &&& (1 <<< 29)) <> 0

    /// <summary>Canonical W3C KeyboardEvent.key string for this chord's LogicalKey.</summary>
    member this.W3cKey: string = LogicalKey.toW3cKey this.Logical

/// <summary>Standards-typed keyboard event combining normalized KeyChord and W3C/ISO-9995 PhysicalKeyCode.</summary>
[<Struct>]
type KeyPress =
    {
        Chord: KeyChord
        Physical: PhysicalKeyCode
    }

    /// <summary>Decoded W3C LogicalKey.</summary>
    member inline this.Logical: LogicalKey = this.Chord.Logical

    /// <summary>Decoded KeyModifiers.</summary>
    member inline this.Modifiers: KeyModifiers = this.Chord.Modifiers

/// <summary>KeyChord constructors, O(1) integer predicates, active patterns, and SIMD Vim chord parser.</summary>
module KeyChord =

    let private shiftAsciiChar (c: char) : char =
        if c >= 'a' && c <= 'z' then
            char (int c - 0x20)
        else
            match c with
            | '`' -> '~'
            | '1' -> '!'
            | '2' -> '@'
            | '3' -> '#'
            | '4' -> '$'
            | '5' -> '%'
            | '6' -> '^'
            | '7' -> '&'
            | '8' -> '*'
            | '9' -> '('
            | '0' -> ')'
            | '-' -> '_'
            | '=' -> '+'
            | '[' -> '{'
            | ']' -> '}'
            | '\\' -> '|'
            | ';' -> ':'
            | '\'' -> '"'
            | ',' -> '<'
            | '.' -> '>'
            | '/' -> '?'
            | other -> other

    /// <summary>Packs LogicalKey and KeyModifiers directly without Shift or AltGraph normalization.</summary>
    let rawOfLogical (logical: LogicalKey) (mods: KeyModifiers) : KeyChord =
        let struct (packed, overflow) = PackedKeyLayout.pack logical mods

        {
            Packed = packed
            OverflowText = overflow
        }

    /// <summary>Packs LogicalKey and KeyModifiers after normalizing ISO 9995-3 AltGraph and Shift + printable Character.</summary>
    let ofLogical (logical: LogicalKey) (mods: KeyModifiers) : KeyChord =
        let normMods = LogicalKey.normalizeAltGraph logical mods

        if
            not normMods.Ctrl
            && not normMods.Alt
            && not normMods.Meta
        then
            match logical with
            | Character r ->
                let rNorm =
                    if normMods.Shift then
                        if r.IsAscii then Rune (shiftAsciiChar (char r.Value))
                        elif Rune.IsLower r then Rune.ToUpperInvariant r
                        else r
                    else
                        r

                let canonicalShift = Rune.IsUpper rNorm
                rawOfLogical (Character rNorm) { normMods with Shift = canonicalShift }
            | _ -> rawOfLogical logical normMods
        else
            rawOfLogical logical normMods

    /// <summary>Normalizes a KeyChord (idempotent on chords constructed via ofLogical, ofChar, ofW3c, or parse).</summary>
    let normalize (chord: KeyChord) : KeyChord =
        ofLogical chord.Logical chord.Modifiers

    /// <summary>Constructs a normalized KeyChord from W3C key string and Ctrl/Alt/Shift flags.</summary>
    let ofW3c (key: string, ctrl: bool, alt: bool, shift: bool) : KeyChord =
        ofLogical
            (LogicalKey.ofW3cKey key)
            {
                Ctrl = ctrl
                Alt = alt
                Shift = shift
                Meta = false
                AltGraph = false
            }

    /// <summary>Constructs a normalized KeyPress from W3C KeyboardEvent fields.</summary>
    let ofW3cEvent (key: string, code: string, ctrl: bool, alt: bool, shift: bool, meta: bool, altGraph: bool) : KeyPress =
        let logical = LogicalKey.ofW3cKey key
        let physical = PhysicalKeyCode.ofW3cCode code

        let chord =
            ofLogical
                logical
                {
                    Ctrl = ctrl
                    Alt = alt
                    Shift = shift
                    Meta = meta
                    AltGraph = altGraph
                }

        { Chord = chord; Physical = physical }

    /// <summary>Constructs a normalized unmodified KeyChord for char c.</summary>
    let ofChar (c: char) : KeyChord =
        if c = ' ' then
            rawOfLogical (Whitespace Space) KeyModifiers.None
        elif Char.IsSurrogate c then
            rawOfLogical (GraphemeText (string c)) KeyModifiers.None
        else
            let r = Rune c

            rawOfLogical
                (Character r)
                { KeyModifiers.None with
                    Shift = Rune.IsUpper r
                }

    /// <summary>Constructs a normalized unmodified KeyChord for Rune r.</summary>
    let ofRune (r: Rune) : KeyChord =
        if r.Value = 0x20 then
            rawOfLogical (Whitespace Space) KeyModifiers.None
        else
            rawOfLogical
                (Character r)
                { KeyModifiers.None with
                    Shift = Rune.IsUpper r
                }

    /// <summary>Constructs a normalized unmodified KeyChord from a W3C key name or text.</summary>
    let ofKey (key: string) : KeyChord =
        ofLogical (LogicalKey.ofW3cKey key) KeyModifiers.None

    /// <summary>Constructs a Ctrl-modified KeyChord.</summary>
    let ctrl (key: string) : KeyChord =
        ofLogical (LogicalKey.ofW3cKey key) { KeyModifiers.None with Ctrl = true }

    /// <summary>Constructs an Alt-modified KeyChord.</summary>
    let alt (key: string) : KeyChord =
        ofLogical (LogicalKey.ofW3cKey key) { KeyModifiers.None with Alt = true }

    /// <summary>Constructs a Shift-modified KeyChord.</summary>
    let shift (key: string) : KeyChord =
        ofLogical (LogicalKey.ofW3cKey key) { KeyModifiers.None with Shift = true }

    /// <summary>The virtual Vim &lt;leader&gt; chord.</summary>
    let leader: KeyChord = rawOfLogical LeaderVirtual KeyModifiers.None

    /// <summary>Single-comparison check testing whether chord is the plain (no Ctrl/Alt/Meta) character c.</summary>
    let inline isPlainChar (c: char) (chord: KeyChord) : bool =
        let target =
            if c = ' ' then (1 <<< 21)
            elif uint16 c >= 0xD800us && uint16 c <= 0xDFFFus then -1
            else int c

        (chord.Packed &&& 0x17FFFFFF) = target

    /// <summary>Returns ValueSome char when chord is an unmodified BMP character or Space.</summary>
    let tryPlainChar (chord: KeyChord) : char voption =
        let masked =
            chord.Packed
            &&& PackedKeyLayout.PlainCharComparisonMask

        let kind = (masked >>> PackedKeyLayout.KindShift) &&& 0xFF

        if kind = PackedKeyLayout.KindCharacter then
            let cp = masked &&& PackedKeyLayout.PayloadMask
            if cp <= 0xFFFF then ValueSome (char cp) else ValueNone
        elif
            masked =
                (PackedKeyLayout.KindWhitespace
                 <<< PackedKeyLayout.KindShift)
        then
            ValueSome ' '
        else
            ValueNone

    /// <summary>Returns ValueSome Rune when chord is an unmodified single Unicode scalar or Space.</summary>
    let tryPlainRune (chord: KeyChord) : Rune voption =
        let masked =
            chord.Packed
            &&& PackedKeyLayout.PlainCharComparisonMask

        let kind = (masked >>> PackedKeyLayout.KindShift) &&& 0xFF

        if kind = PackedKeyLayout.KindCharacter then
            let cp = masked &&& PackedKeyLayout.PayloadMask
            let mutable r = Rune.ReplacementChar
            if Rune.TryCreate (cp, &r) then ValueSome r else ValueNone
        elif
            masked =
                (PackedKeyLayout.KindWhitespace
                 <<< PackedKeyLayout.KindShift)
        then
            ValueSome (Rune ' ')
        else
            ValueNone

    /// <summary>Returns ValueSome string when chord is an unmodified printable character, multi-scalar grapheme, or Space.</summary>
    let tryPlainText (chord: KeyChord) : string voption =
        if not chord.Ctrl && not chord.Alt && not chord.Meta then
            LogicalKey.tryPrintedText chord.Logical
        else
            ValueNone

    /// <summary>True when chord is a pure modifier, IME Process event, or Unidentified key.</summary>
    let isModifierOnly (chord: KeyChord) : bool =
        LogicalKey.isNonActionableModifier chord.Logical

    /// <summary>Wraps a KeyChord into a KeyPress with PhysicalKeyCode.UnidentifiedCode.</summary>
    let toKeyPress (chord: KeyChord) : KeyPress =
        {
            Chord = normalize chord
            Physical = PhysicalKeyCode.UnidentifiedCode
        }

    /// <summary>Extracts the normalized KeyChord from a KeyPress without string round-tripping.</summary>
    let ofKeyPress (press: KeyPress) : KeyChord =
        normalize press.Chord

    /// <summary>Returns the ECMA-48 C0Control corresponding to a Ctrl-modified chord, if any.</summary>
    let c0Control (chord: KeyChord) : C0Control voption =
        let norm = normalize chord

        if norm.Ctrl && not norm.Alt && not norm.Meta then
            match norm.Logical with
            | Character r when r.IsAscii -> C0Control.ofModifiedAscii (char r.Value)
            | Whitespace Space -> ValueSome C0Control.Nul
            | Whitespace Enter -> ValueSome C0Control.Cr
            | Whitespace Tab -> ValueSome C0Control.Ht
            | Editing EditingKey.Backspace -> ValueSome C0Control.Bs
            | Ui Escape -> ValueSome C0Control.Esc
            | _ -> ValueNone
        else
            ValueNone

    /// <summary>Decomposes a normalized KeyChord into its LogicalKey, KeyModifiers, and optional C0Control.</summary>
    let (|ClassifiedKey|) (chord: KeyChord) : LogicalKey * KeyModifiers * C0Control voption =
        let norm = normalize chord
        (norm.Logical, norm.Modifiers, c0Control norm)

    /// <summary>Matches an unmodified printable Unicode grapheme (Character, GraphemeText, or Space).</summary>
    let (|PlainText|_|) (chord: KeyChord) : string option =
        match tryPlainText (normalize chord) with
        | ValueSome s -> Some s
        | ValueNone -> None

    /// <summary>Matches an unmodified single Unicode Rune (BMP, non-ASCII, astral emoji, or Space).</summary>
    let (|PlainRune|_|) (chord: KeyChord) : Rune option =
        match tryPlainRune (normalize chord) with
        | ValueSome r -> Some r
        | ValueNone -> None

    /// <summary>Matches an unmodified BMP char or Space.</summary>
    let (|PlainChar|_|) (chord: KeyChord) : char option =
        match tryPlainChar (normalize chord) with
        | ValueSome c -> Some c
        | ValueNone -> None

    /// <summary>Matches an ECMA-48 C0Control Ctrl key combination.</summary>
    let (|CtrlC0|_|) (chord: KeyChord) : C0Control option =
        match c0Control chord with
        | ValueSome c0 -> Some c0
        | ValueNone -> None

    let private asciiChordLut: int array =
        Array.init 128 (fun code ->
            if code >= 0x20 && code <= 0x7E then
                (ofChar (char code)).Packed
            else
                0)

    let private resolveTagKeyName (keyName: string) : LogicalKey =
        let lower = keyName.ToLowerInvariant ()

        match lower with
        | "lt" -> Character (Rune '<')
        | "space" -> Whitespace Space
        | "bar" -> Character (Rune '|')
        | "bslash" -> Character (Rune '\\')
        | "leader" -> LeaderVirtual
        | "esc"
        | "escape" -> Ui Escape
        | "cr"
        | "return"
        | "enter" -> Whitespace Enter
        | "bs"
        | "backspace" -> Editing EditingKey.Backspace
        | "tab" -> Whitespace Tab
        | "up"
        | "arrowup" -> Navigation ArrowUp
        | "down"
        | "arrowdown" -> Navigation ArrowDown
        | "left"
        | "arrowleft" -> Navigation ArrowLeft
        | "right"
        | "arrowright" -> Navigation ArrowRight
        | "home" -> Navigation Home
        | "end" -> Navigation End
        | "pageup" -> Navigation PageUp
        | "pagedown" -> Navigation PageDown
        | "del"
        | "delete" -> Editing EditingKey.Delete
        | "ins"
        | "insert" -> Editing EditingKey.Insert
        | _ -> LogicalKey.ofW3cKey keyName

    let private parseTagString (tag: string) : KeyChord =
        let len = tag.Length

        if len = 0 then
            rawOfLogical (Unidentified "") KeyModifiers.None
        else
            let inline lo (c: char) =
                if c >= 'A' && c <= 'Z' then char (int c + 0x20) else c

            if len > 4 && tag[1] = '-' && tag[3] = '-' then
                let m1 = lo tag[0]
                let m2 = lo tag[2]
                let k = resolveTagKeyName (tag.Substring (4))

                if (m1 = 'c' && m2 = 's') || (m1 = 's' && m2 = 'c') then
                    ofLogical
                        k
                        { KeyModifiers.None with
                            Ctrl = true
                            Shift = true
                        }
                elif
                    (m1 = 'c' && (m2 = 'a' || m2 = 'm'))
                    || ((m1 = 'a' || m1 = 'm') && m2 = 'c')
                then
                    ofLogical
                        k
                        { KeyModifiers.None with
                            Ctrl = true
                            Alt = true
                        }
                elif
                    (m1 = 'a' || m1 = 'm') && m2 = 's'
                    || (m1 = 's' && (m2 = 'a' || m2 = 'm'))
                then
                    ofLogical
                        k
                        { KeyModifiers.None with
                            Alt = true
                            Shift = true
                        }
                else
                    ofLogical (resolveTagKeyName tag) KeyModifiers.None
            elif len > 2 && tag[1] = '-' then
                let m1 = lo tag[0]
                let k = resolveTagKeyName (tag.Substring (2))

                match m1 with
                | 'c' -> ofLogical k { KeyModifiers.None with Ctrl = true }
                | 'a'
                | 'm' -> ofLogical k { KeyModifiers.None with Alt = true }
                | 's' -> ofLogical k { KeyModifiers.None with Shift = true }
                | 'd' -> ofLogical k { KeyModifiers.None with Meta = true }
                | _ -> ofLogical (resolveTagKeyName tag) KeyModifiers.None
            else
                ofLogical (resolveTagKeyName tag) KeyModifiers.None

#if !FABLE_COMPILER
    /// Counts contiguous plain ASCII chars (0x0020 <= c <= 0x007E && c <> '<') from index 0 using three-tier SIMD.
    let private plainAsciiNoAngleRun (span: ReadOnlySpan<char>) : int =
        let length = unativeint span.Length

        if length = 0un then
            0
        else
            let u16Span = MemoryMarshal.Cast<char, uint16>(span)
            let src = &MemoryMarshal.GetReference (u16Span)
            let mutable i = 0un
            let mutable stop = -1

            if
                Vector512.IsHardwareAccelerated
                && Vector512<uint16>.IsSupported
            then
                let vLo = Vector512.Create (0x0020us)
                let vRange = Vector512.Create (0x005Eus)
                let vAngle = Vector512.Create (uint16 '<')

                while i + 32un <= length && stop < 0 do
                    let vec = Vector512.LoadUnsafe (&src, i)

                    let failMask =
                        (Vector512.GreaterThan (vec - vLo, vRange)
                         ||| Vector512.Equals (vec, vAngle))
                            .ExtractMostSignificantBits()

                    if failMask <> 0UL then
                        stop <- int i + BitOperations.TrailingZeroCount (failMask)
                    else
                        i <- i + 32un
            elif
                Vector256.IsHardwareAccelerated
                && Vector256<uint16>.IsSupported
            then
                let vLo = Vector256.Create (0x0020us)
                let vRange = Vector256.Create (0x005Eus)
                let vAngle = Vector256.Create (uint16 '<')

                while i + 16un <= length && stop < 0 do
                    let vec = Vector256.LoadUnsafe (&src, i)

                    let failMask =
                        (Vector256.GreaterThan (vec - vLo, vRange)
                         ||| Vector256.Equals (vec, vAngle))
                            .ExtractMostSignificantBits()

                    if failMask <> 0u then
                        stop <- int i + BitOperations.TrailingZeroCount (failMask)
                    else
                        i <- i + 16un
            elif
                Vector128.IsHardwareAccelerated
                && Vector128<uint16>.IsSupported
            then
                let vLo = Vector128.Create (0x0020us)
                let vRange = Vector128.Create (0x005Eus)
                let vAngle = Vector128.Create (uint16 '<')

                while i + 8un <= length && stop < 0 do
                    let vec = Vector128.LoadUnsafe (&src, i)

                    let failMask =
                        (Vector128.GreaterThan (vec - vLo, vRange)
                         ||| Vector128.Equals (vec, vAngle))
                            .ExtractMostSignificantBits()

                    if failMask <> 0u then
                        stop <- int i + BitOperations.TrailingZeroCount (failMask)
                    else
                        i <- i + 8un

            let rawRun =
                if stop >= 0 then
                    stop
                else
                    while i < length
                          && (let c = Unsafe.Add (&src, i) in (c - 0x0020us) <= 0x005Eus && c <> uint16 '<') do
                        i <- i + 1un

                    int i

            if
                rawRun > 0
                && rawRun < span.Length
                && span[rawRun] >= '\u0300'
            then
                rawRun - 1
            else
                rawRun

    let private nextClusterLenSpan (span: ReadOnlySpan<char>) (idx: int) : int =
        let c = span[idx]

        if
            c >= '\u0020'
            && c <= '\u007E'
            && (idx + 1 >= span.Length || span[idx + 1] < '\u0300')
        then
            1
        elif
            c = '\r'
            && idx + 1 < span.Length
            && span[idx + 1] = '\n'
        then
            2
        else
            // Slice minimal window around idx for StringInfo on non-ASCII cluster
            let rem = span.Slice (idx)
            let mutable window = 2

            while window < rem.Length
                  && (rem[window] >= '\u0300'
                      || Char.IsSurrogate rem[window]
                      || rem[window - 1] = '\u200D') do
                window <- min rem.Length (window + 4)

            let sub = rem.Slice(0, min rem.Length (window + 2)).ToString()
            Grapheme.nextClusterLength sub 0

    /// <summary>Scalar reference implementation of Vim chord sequence parsing into an array.</summary>
    let parseArrayScalar (seq: ReadOnlySpan<char>) : KeyChord array =
        if seq.Length = 0 then
            [||]
        else
            let chords = ResizeArray<KeyChord>(seq.Length)
            let mutable i = 0

            while i < seq.Length do
                if seq[i] = '<' then
                    let relClose = seq.Slice(i + 1).IndexOf('>')

                    if relClose >= 0 then
                        let tag = seq.Slice(i + 1, relClose).ToString()
                        chords.Add (parseTagString tag)
                        i <- i + relClose + 2
                    else
                        let clusterLen = nextClusterLenSpan seq i

                        if clusterLen = 1 then
                            chords.Add (ofChar seq[i])
                        else
                            chords.Add (ofKey (seq.Slice(i, clusterLen).ToString()))

                        i <- i + clusterLen
                else
                    let clusterLen = nextClusterLenSpan seq i

                    if clusterLen = 1 then
                        let c = seq[i]

                        if c >= '\u0020' && c <= '\u007E' then
                            chords.Add (
                                {
                                    Packed = asciiChordLut[int c]
                                    OverflowText = null
                                }
                            )
                        else
                            chords.Add (ofChar c)
                    else
                        chords.Add (ofKey (seq.Slice(i, clusterLen).ToString()))

                    i <- i + clusterLen

            chords.ToArray ()

    /// <summary>Parses a Vim-style key chord sequence span into a KeyChord array using three-tier SIMD for plain ASCII runs.</summary>
    let parseArray (seq: ReadOnlySpan<char>) : KeyChord array =
        let totalLen = seq.Length

        if totalLen = 0 then
            [||]
        else
            let firstRun = plainAsciiNoAngleRun seq

            if firstRun = totalLen then
                let arr = Array.zeroCreate<KeyChord> totalLen
                let lut = asciiChordLut

                for k = 0 to totalLen - 1 do
                    arr[k] <-
                        {
                            Packed = lut[int seq[k]]
                            OverflowText = null
                        }

                arr
            else
                let chords = ResizeArray<KeyChord>(totalLen)
                let lut = asciiChordLut
                let mutable i = 0

                for k = 0 to firstRun - 1 do
                    chords.Add (
                        {
                            Packed = lut[int seq[k]]
                            OverflowText = null
                        }
                    )

                i <- firstRun

                while i < totalLen do
                    let asciiRun = plainAsciiNoAngleRun (seq.Slice (i))

                    if asciiRun > 0 then
                        for k = i to i + asciiRun - 1 do
                            chords.Add (
                                {
                                    Packed = lut[int seq[k]]
                                    OverflowText = null
                                }
                            )

                        i <- i + asciiRun
                    elif seq[i] = '<' then
                        let relClose = seq.Slice(i + 1).IndexOf('>')

                        if relClose >= 0 then
                            let tag = seq.Slice(i + 1, relClose).ToString()
                            chords.Add (parseTagString tag)
                            i <- i + relClose + 2
                        else
                            let clusterLen = nextClusterLenSpan seq i

                            if clusterLen = 1 then
                                chords.Add (ofChar seq[i])
                            else
                                chords.Add (ofKey (seq.Slice(i, clusterLen).ToString()))

                            i <- i + clusterLen
                    else
                        let clusterLen = nextClusterLenSpan seq i

                        if clusterLen = 1 then
                            chords.Add (ofChar seq[i])
                        else
                            chords.Add (ofKey (seq.Slice(i, clusterLen).ToString()))

                        i <- i + clusterLen

                chords.ToArray ()
#endif

    /// <summary>Parses a Vim-style key chord sequence string (e.g. "&lt;leader&gt;ff", "&lt;C-w&gt;v", "3dd") into a KeyChord list.</summary>
    let parse (seq: string) : KeyChord list =
        if String.IsNullOrEmpty seq then
            []
        else
#if !FABLE_COMPILER
            parseArray (seq.AsSpan ()) |> Array.toList
#else
            let chords = ResizeArray<KeyChord>(seq.Length)
            let lut = asciiChordLut
            let mutable i = 0

            while i < seq.Length do
                let c = seq[i]

                if c = '<' then
                    let closeIdx = seq.IndexOf ('>', i + 1)

                    if closeIdx >= 0 then
                        let tag = seq.Substring (i + 1, closeIdx - i - 1)
                        chords.Add (parseTagString tag)
                        i <- closeIdx + 1
                    else
                        let clusterLen = Grapheme.nextClusterLength seq i

                        if clusterLen = 1 then
                            chords.Add (ofChar c)
                        else
                            chords.Add (ofKey (seq.Substring (i, clusterLen)))

                        i <- i + clusterLen
                elif
                    c >= '\u0020'
                    && c <= '\u007E'
                    && (i + 1 >= seq.Length || seq[i + 1] < '\u0300')
                then
                    chords.Add (
                        {
                            Packed = lut[int c]
                            OverflowText = null
                        }
                    )

                    i <- i + 1
                else
                    let clusterLen = Grapheme.nextClusterLength seq i

                    if clusterLen = 1 then
                        chords.Add (ofChar c)
                    else
                        chords.Add (ofKey (seq.Substring (i, clusterLen)))

                    i <- i + clusterLen

            chords |> Seq.toList
#endif
