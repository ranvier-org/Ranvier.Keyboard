namespace Ranvier.Keyboard

open System
open System.Text
#if !FABLE_COMPILER
open System.Buffers
open System.Runtime.Intrinsics
#endif

/// <summary>ISO/IEC 9995-3 / W3C UI Events composing dead-key accents.</summary>
[<Struct>]
type ComposingAccent =
    | Grave
    | Acute
    | Circumflex
    | Tilde
    | Diaeresis
    | RingAbove
    | Cedilla
    | Caron
    | Macron
    | Breve
    | DoubleAcute
    | Ogonek
    | Stroke
    | DotAbove

/// <summary>ISO/IEC 9995-3 dead-key composition helpers.</summary>
[<RequireQualifiedAccess>]
module DeadKey =

#if FABLE_COMPILER
    [<Fable.Core.Emit("$0.normalize('NFC')")>]
    let private normalizeNfc (s: string) : string =
        Fable.Core.Util.jsNative
#else
    let private normalizeNfc (s: string) : string =
        s.Normalize (NormalizationForm.FormC)
#endif

    let private combiningMarkOf (accent: ComposingAccent) : string =
        match accent with
        | Grave -> "\u0300"
        | Acute -> "\u0301"
        | Circumflex -> "\u0302"
        | Tilde -> "\u0303"
        | Macron -> "\u0304"
        | Breve -> "\u0306"
        | DotAbove -> "\u0307"
        | Diaeresis -> "\u0308"
        | RingAbove -> "\u030A"
        | DoubleAcute -> "\u030B"
        | Caron -> "\u030C"
        | Cedilla -> "\u0327"
        | Ogonek -> "\u0328"
        | Stroke -> "\u0335"

    let private spacingMarkOf (accent: ComposingAccent) : string =
        match accent with
        | Grave -> "`"
        | Acute -> "\u00B4"
        | Circumflex -> "^"
        | Tilde -> "~"
        | Diaeresis -> "\u00A8"
        | RingAbove -> "\u02DA"
        | Cedilla -> "\u00B8"
        | Caron -> "\u02C7"
        | Macron -> "\u00AF"
        | Breve -> "\u02D8"
        | DoubleAcute -> "\u02DD"
        | Ogonek -> "\u02DB"
        | Stroke -> "/"
        | DotAbove -> "\u02D9"

    /// <summary>Composes an ISO/IEC 9995-3 dead-key accent with a base Rune via Unicode NFC normalization (plus stroked letters and space).</summary>
    let compose (accent: ComposingAccent) (baseRune: Rune) : string =
        if baseRune = Rune ' ' then
            spacingMarkOf accent
        elif accent = Stroke then
            match baseRune.Value with
            | 0x006C -> "ł"
            | 0x004C -> "Ł"
            | 0x006F -> "ø"
            | 0x004F -> "Ø"
            | 0x0064 -> "đ"
            | 0x0044 -> "Đ"
            | _ -> normalizeNfc (baseRune.ToString () + combiningMarkOf accent)
        else
            normalizeNfc (baseRune.ToString () + combiningMarkOf accent)

/// <summary>IETF RFC 1345 character mnemonics (standardized two-character digraphs used by Vim &lt;C-k&gt;{c1}{c2}).</summary>
[<RequireQualifiedAccess>]
module Rfc1345Digraph =

    let private tryComposeSingleRune (accent: ComposingAccent) (baseCh: char) : Rune voption =
        let s = DeadKey.compose accent (Rune baseCh)
        let mutable r = Rune.ReplacementChar

        if
            s.Length > 0
            && Rune.TryGetRuneAt (s, 0, &r)
            && s.Length = r.Utf16SequenceLength
        then
            ValueSome r
        else
            ValueNone

    /// <summary>Looks up a two-character RFC 1345 digraph mnemonic into a single Unicode Rune.</summary>
    let lookup (c1: char) (c2: char) : Rune voption =
        match c1, c2 with
        | '-', '>' -> ValueSome (Rune 0x2192)
        | '<', '-' -> ValueSome (Rune 0x2190)
        | '<', '>' -> ValueSome (Rune 0x2194)
        | '=', '>' -> ValueSome (Rune 0x21D2)
        | '=', '<' -> ValueSome (Rune 0x2264)
        | '<', '=' -> ValueSome (Rune 0x2264)
        | '>', '=' -> ValueSome (Rune 0x2265)
        | '!', '=' -> ValueSome (Rune 0x2260)
        | '=', '=' -> ValueSome (Rune 0x2261)
        | '?', '=' -> ValueSome (Rune 0x2248)
        | 'F', 'A' -> ValueSome (Rune 0x2200)
        | 'T', 'E' -> ValueSome (Rune 0x2203)
        | 'A', 'N' -> ValueSome (Rune 0x2227)
        | 'O', 'R' -> ValueSome (Rune 0x2228)
        | 'N', 'O' -> ValueSome (Rune 0x00AC)
        | '(', '-' -> ValueSome (Rune 0x2208)
        | '0', '0' -> ValueSome (Rune 0x221E)
        | 'R', 'T' -> ValueSome (Rune 0x221A)
        | '+', '-' -> ValueSome (Rune 0x00B1)
        | '*', 'X' -> ValueSome (Rune 0x00D7)
        | '-', ':' -> ValueSome (Rune 0x00F7)
        | 'a', '*' -> ValueSome (Rune 0x03B1)
        | 'b', '*' -> ValueSome (Rune 0x03B2)
        | 'g', '*' -> ValueSome (Rune 0x03B3)
        | 'd', '*' -> ValueSome (Rune 0x03B4)
        | 'e', '*' -> ValueSome (Rune 0x03B5)
        | 'z', '*' -> ValueSome (Rune 0x03B6)
        | 'y', '*' -> ValueSome (Rune 0x03B7)
        | 'h', '*' -> ValueSome (Rune 0x03B8)
        | 'i', '*' -> ValueSome (Rune 0x03B9)
        | 'k', '*' -> ValueSome (Rune 0x03BA)
        | 'l', '*' -> ValueSome (Rune 0x03BB)
        | 'm', '*' -> ValueSome (Rune 0x03BC)
        | 'n', '*' -> ValueSome (Rune 0x03BD)
        | 'p', '*' -> ValueSome (Rune 0x03C0)
        | 'r', '*' -> ValueSome (Rune 0x03C1)
        | 's', '*' -> ValueSome (Rune 0x03C3)
        | 't', '*' -> ValueSome (Rune 0x03C4)
        | 'f', '*' -> ValueSome (Rune 0x03C6)
        | 'x', '*' -> ValueSome (Rune 0x03C7)
        | 'q', '*' -> ValueSome (Rune 0x03C8)
        | 'w', '*' -> ValueSome (Rune 0x03C9)
        | 'D', '*' -> ValueSome (Rune 0x0394)
        | 'L', '*' -> ValueSome (Rune 0x039B)
        | 'P', '*' -> ValueSome (Rune 0x03A0)
        | 'S', '*' -> ValueSome (Rune 0x03A3)
        | 'F', '*' -> ValueSome (Rune 0x03A6)
        | 'W', '*' -> ValueSome (Rune 0x03A9)
        | '<', '<' -> ValueSome (Rune 0x00AB)
        | '>', '>' -> ValueSome (Rune 0x00BB)
        | 'E', 'u'
        | '=', 'e' -> ValueSome (Rune 0x20AC)
        | 'P', 'd' -> ValueSome (Rune 0x00A3)
        | 'Y', 'e' -> ValueSome (Rune 0x00A5)
        | 'C', 'o' -> ValueSome (Rune 0x00A9)
        | 'R', 'g' -> ValueSome (Rune 0x00AE)
        | 'D', 'g' -> ValueSome (Rune 0x00B0)
        | 's', 's' -> ValueSome (Rune 0x00DF)
        | 'a', 'e' -> ValueSome (Rune 0x00E6)
        | 'A', 'E' -> ValueSome (Rune 0x00C6)
        | 'o', 'e' -> ValueSome (Rune 0x0153)
        | 'O', 'E' -> ValueSome (Rune 0x0152)
        | '!', 'I' -> ValueSome (Rune 0x00A1)
        | '?', 'I' -> ValueSome (Rune 0x00BF)
        | baseCh, '\'' -> tryComposeSingleRune Acute baseCh
        | baseCh, '!' -> tryComposeSingleRune Grave baseCh
        | baseCh, '>' -> tryComposeSingleRune Circumflex baseCh
        | baseCh, ':' -> tryComposeSingleRune Diaeresis baseCh
        | baseCh, '?' -> tryComposeSingleRune Tilde baseCh
        | baseCh, ',' -> tryComposeSingleRune Cedilla baseCh
        | baseCh, '0' -> tryComposeSingleRune RingAbove baseCh
        | baseCh, '-' -> tryComposeSingleRune Macron baseCh
        | baseCh, '/' -> tryComposeSingleRune Stroke baseCh
        | _ -> ValueNone

/// <summary>W3C UI Events KeyboardEvent.key navigation keys.</summary>
[<Struct>]
type NavigationKey =
    | ArrowLeft
    | ArrowRight
    | ArrowUp
    | ArrowDown
    | Home
    | End
    | PageUp
    | PageDown

/// <summary>W3C UI Events KeyboardEvent.key editing keys.</summary>
[<RequireQualifiedAccess; Struct>]
type EditingKey =
    | Backspace
    | Delete
    | Insert
    | Clear

/// <summary>W3C UI Events KeyboardEvent.key whitespace keys.</summary>
[<Struct>]
type WhitespaceKey =
    | Space
    | Tab
    | Enter

/// <summary>W3C UI Events KeyboardEvent.key UI / modal keys.</summary>
[<Struct>]
type UiKey =
    | Escape
    | ContextMenu
    | Pause
    | Cancel

/// <summary>W3C UI Events KeyboardEvent.key modifier and lock keys.</summary>
[<Struct>]
type ModifierKey =
    | Shift
    | Control
    | Alt
    | AltGraph
    | Meta
    | CapsLock
    | NumLock
    | ScrollLock
    | Symbol
    | Fn

/// <summary>W3C UI Events KeyboardEvent.key IME / East Asian input keys.</summary>
[<Struct>]
type ImeKey =
    | Process
    | Convert
    | NonConvert
    | ModeChange
    | KanjiMode
    | KanaMode
    | HangulMode
    | HanjaMode
    | Eisu
    | ZenkakuHankaku

/// <summary>W3C UI Events KeyboardEvent.key logical key value as a value-type discriminated union.</summary>
[<Struct>]
type LogicalKey =
    | Character of character: Rune
    | GraphemeText of text: string
    | Whitespace of whitespace: WhitespaceKey
    | Navigation of navigation: NavigationKey
    | Editing of editing: EditingKey
    | Ui of ui: UiKey
    | FunctionKey of number: int
    | Modifier of modifier: ModifierKey
    | Dead of accent: ComposingAccent
    | Ime of ime: ImeKey
    | LeaderVirtual
    | Unidentified of raw: string

/// <summary>Standardized keyboard modifier flags including ISO/IEC 9995-3 AltGraph (Level 3 shift).</summary>
[<Struct>]
type KeyModifiers =
    {
        Ctrl: bool
        Alt: bool
        Shift: bool
        Meta: bool
        AltGraph: bool
    }

    /// <summary>No modifiers active.</summary>
    static member None =
        {
            Ctrl = false
            Alt = false
            Shift = false
            Meta = false
            AltGraph = false
        }

/// <summary>5-bit integer encoding (0..31) for KeyModifiers.</summary>
[<RequireQualifiedAccess>]
module KeyModifiers =

    /// <summary>Packs KeyModifiers into a 5-bit integer in 0..31.</summary>
    let inline toBits (m: KeyModifiers) : int =
        (if m.Ctrl then 1 else 0)
        ||| (if m.Alt then 2 else 0)
        ||| (if m.Shift then 4 else 0)
        ||| (if m.Meta then 8 else 0)
        ||| (if m.AltGraph then 16 else 0)

    /// <summary>Unpacks a 5-bit integer in 0..31 into KeyModifiers.</summary>
    let inline ofBits (bits: int) : KeyModifiers =
        {
            Ctrl = (bits &&& 1) <> 0
            Alt = (bits &&& 2) <> 0
            Shift = (bits &&& 4) <> 0
            Meta = (bits &&& 8) <> 0
            AltGraph = (bits &&& 16) <> 0
        }

/// <summary>LogicalKey conversions, SIMD parsers, and ISO/IEC 9995-3 AltGraph normalization.</summary>
[<RequireQualifiedAccess>]
module LogicalKey =

    let private functionKeyNames =
        [|
            "F1"
            "F2"
            "F3"
            "F4"
            "F5"
            "F6"
            "F7"
            "F8"
            "F9"
            "F10"
            "F11"
            "F12"
            "F13"
            "F14"
            "F15"
            "F16"
            "F17"
            "F18"
            "F19"
            "F20"
            "F21"
            "F22"
            "F23"
            "F24"
        |]

    let inline private tryParseFunctionNumber2 (c1: char) : int =
        if c1 >= '1' && c1 <= '9' then int c1 - int '0' else 0

    let inline private tryParseFunctionNumber3 (c1: char) (c2: char) : int =
        if c1 = '1' && c2 >= '0' && c2 <= '9' then
            10 + (int c2 - int '0')
        elif c1 = '2' && c2 >= '0' && c2 <= '4' then
            20 + (int c2 - int '0')
        else
            0

#if FABLE_COMPILER
    /// <summary>Parses a W3C UI Events KeyboardEvent.key value (and common aliases) into a LogicalKey.</summary>
    let ofW3cKey (raw: string) : LogicalKey =
        if isNull raw || raw.Length = 0 then
            Unidentified ""
        elif raw.Length = 1 then
            let c = raw[0]

            if c = ' ' then Whitespace Space
            elif Char.IsSurrogate c then GraphemeText raw
            else Character (Rune c)
        else
            match raw with
            | "Spacebar" -> Whitespace Space
            | "Tab" -> Whitespace Tab
            | "Enter"
            | "Return" -> Whitespace Enter
            | "ArrowLeft"
            | "Left" -> Navigation ArrowLeft
            | "ArrowRight"
            | "Right" -> Navigation ArrowRight
            | "ArrowUp"
            | "Up" -> Navigation ArrowUp
            | "ArrowDown"
            | "Down" -> Navigation ArrowDown
            | "Home" -> Navigation Home
            | "End" -> Navigation End
            | "PageUp" -> Navigation PageUp
            | "PageDown" -> Navigation PageDown
            | "Backspace" -> Editing EditingKey.Backspace
            | "Delete"
            | "Del" -> Editing EditingKey.Delete
            | "Insert" -> Editing EditingKey.Insert
            | "Clear" -> Editing EditingKey.Clear
            | "Escape"
            | "Esc" -> Ui Escape
            | "ContextMenu" -> Ui ContextMenu
            | "Pause" -> Ui Pause
            | "Cancel" -> Ui Cancel
            | "Shift"
            | "ShiftLeft"
            | "ShiftRight" -> Modifier Shift
            | "Control"
            | "ControlLeft"
            | "ControlRight" -> Modifier Control
            | "Alt"
            | "AltLeft"
            | "AltRight" -> Modifier Alt
            | "AltGraph" -> Modifier AltGraph
            | "Meta"
            | "MetaLeft"
            | "MetaRight"
            | "OS"
            | "Super"
            | "Hyper" -> Modifier Meta
            | "CapsLock" -> Modifier CapsLock
            | "NumLock" -> Modifier NumLock
            | "ScrollLock" -> Modifier ScrollLock
            | "Symbol" -> Modifier Symbol
            | "Fn" -> Modifier Fn
            | "Dead"
            | "DeadAcute" -> Dead Acute
            | "DeadGrave" -> Dead Grave
            | "DeadCircumflex" -> Dead Circumflex
            | "DeadTilde" -> Dead Tilde
            | "DeadDiaeresis" -> Dead Diaeresis
            | "DeadRingAbove" -> Dead RingAbove
            | "DeadCedilla" -> Dead Cedilla
            | "DeadCaron" -> Dead Caron
            | "DeadMacron" -> Dead Macron
            | "DeadBreve" -> Dead Breve
            | "DeadDoubleAcute" -> Dead DoubleAcute
            | "DeadOgonek" -> Dead Ogonek
            | "DeadStroke" -> Dead Stroke
            | "DeadAboveDot"
            | "DeadDotAbove" -> Dead DotAbove
            | "Process" -> Ime Process
            | "Convert" -> Ime Convert
            | "NonConvert" -> Ime NonConvert
            | "ModeChange" -> Ime ModeChange
            | "KanjiMode" -> Ime KanjiMode
            | "KanaMode" -> Ime KanaMode
            | "HangulMode" -> Ime HangulMode
            | "HanjaMode" -> Ime HanjaMode
            | "Eisu" -> Ime Eisu
            | "ZenkakuHankaku" -> Ime ZenkakuHankaku
            | "<leader>" -> LeaderVirtual
            | "Unidentified" -> Unidentified "Unidentified"
            | s when s.Length = 2 && (s[0] = 'F' || s[0] = 'f') ->
                let n = tryParseFunctionNumber2 s[1]
                if n > 0 then FunctionKey n else GraphemeText s
            | s when s.Length = 3 && (s[0] = 'F' || s[0] = 'f') ->
                let n = tryParseFunctionNumber3 s[1] s[2]
                if n > 0 then FunctionKey n else GraphemeText s
            | s ->
                let mutable r = Rune.ReplacementChar

                if
                    s.Length = 2
                    && Rune.TryGetRuneAt (s, 0, &r)
                    && r.Utf16SequenceLength = 2
                then
                    Character r
                else
                    GraphemeText s
#else
    let private namedKeyEntries: (string * LogicalKey) array =
        let baseEntries =
            [|
                "Spacebar", Whitespace Space
                "Tab", Whitespace Tab
                "Enter", Whitespace Enter
                "Return", Whitespace Enter
                "ArrowLeft", Navigation ArrowLeft
                "Left", Navigation ArrowLeft
                "ArrowRight", Navigation ArrowRight
                "Right", Navigation ArrowRight
                "ArrowUp", Navigation ArrowUp
                "Up", Navigation ArrowUp
                "ArrowDown", Navigation ArrowDown
                "Down", Navigation ArrowDown
                "Home", Navigation Home
                "End", Navigation End
                "PageUp", Navigation PageUp
                "PageDown", Navigation PageDown
                "Backspace", Editing EditingKey.Backspace
                "Delete", Editing EditingKey.Delete
                "Del", Editing EditingKey.Delete
                "Insert", Editing EditingKey.Insert
                "Clear", Editing EditingKey.Clear
                "Escape", Ui Escape
                "Esc", Ui Escape
                "ContextMenu", Ui ContextMenu
                "Pause", Ui Pause
                "Cancel", Ui Cancel
                "Shift", Modifier Shift
                "ShiftLeft", Modifier Shift
                "ShiftRight", Modifier Shift
                "Control", Modifier Control
                "ControlLeft", Modifier Control
                "ControlRight", Modifier Control
                "Alt", Modifier Alt
                "AltLeft", Modifier Alt
                "AltRight", Modifier Alt
                "AltGraph", Modifier AltGraph
                "Meta", Modifier Meta
                "MetaLeft", Modifier Meta
                "MetaRight", Modifier Meta
                "OS", Modifier Meta
                "Super", Modifier Meta
                "Hyper", Modifier Meta
                "CapsLock", Modifier CapsLock
                "NumLock", Modifier NumLock
                "ScrollLock", Modifier ScrollLock
                "Symbol", Modifier Symbol
                "Fn", Modifier Fn
                "Dead", Dead Acute
                "DeadAcute", Dead Acute
                "DeadGrave", Dead Grave
                "DeadCircumflex", Dead Circumflex
                "DeadTilde", Dead Tilde
                "DeadDiaeresis", Dead Diaeresis
                "DeadRingAbove", Dead RingAbove
                "DeadCedilla", Dead Cedilla
                "DeadCaron", Dead Caron
                "DeadMacron", Dead Macron
                "DeadBreve", Dead Breve
                "DeadDoubleAcute", Dead DoubleAcute
                "DeadOgonek", Dead Ogonek
                "DeadStroke", Dead Stroke
                "DeadAboveDot", Dead DotAbove
                "DeadDotAbove", Dead DotAbove
                "Process", Ime Process
                "Convert", Ime Convert
                "NonConvert", Ime NonConvert
                "ModeChange", Ime ModeChange
                "KanjiMode", Ime KanjiMode
                "KanaMode", Ime KanaMode
                "HangulMode", Ime HangulMode
                "HanjaMode", Ime HanjaMode
                "Eisu", Ime Eisu
                "ZenkakuHankaku", Ime ZenkakuHankaku
                "<leader>", LeaderVirtual
                "Unidentified", Unidentified "Unidentified"
            |]

        let fnEntries =
            Array.init 48 (fun idx ->
                let n = (idx / 2) + 1
                let prefix = if idx % 2 = 0 then "F" else "f"
                prefix + string n, FunctionKey n)

        Array.append baseEntries fnEntries

    let private simdTable = SimdKeyTable.build namedKeyEntries (Unidentified "")

    let inline private scalarFallback (raw: ReadOnlySpan<char>) : LogicalKey =
        if
            raw.Length = 2
            && Char.IsSurrogatePair (raw[0], raw[1])
        then
            Character (Rune (raw[0], raw[1]))
        else
            GraphemeText (raw.ToString ())

    /// <summary>Scalar reference parser for W3C KeyboardEvent.key spans.</summary>
    let ofW3cKeyScalar (raw: ReadOnlySpan<char>) : LogicalKey =
        let len = raw.Length

        if len = 0 then
            Unidentified ""
        elif len = 1 then
            let c = raw[0]

            if c = ' ' then Whitespace Space
            elif Char.IsSurrogate c then GraphemeText (raw.ToString ())
            else Character (Rune c)
        else
            match len with
            | 2 ->
                if raw.SequenceEqual ("Up".AsSpan()) then
                    Navigation ArrowUp
                elif raw.SequenceEqual ("OS".AsSpan()) then
                    Modifier Meta
                elif raw.SequenceEqual ("Fn".AsSpan()) then
                    Modifier Fn
                elif raw[0] = 'F' || raw[0] = 'f' then
                    let n = tryParseFunctionNumber2 raw[1]
                    if n > 0 then FunctionKey n else scalarFallback raw
                else
                    scalarFallback raw
            | 3 ->
                if raw.SequenceEqual ("Tab".AsSpan()) then
                    Whitespace Tab
                elif raw.SequenceEqual ("End".AsSpan()) then
                    Navigation End
                elif raw.SequenceEqual ("Del".AsSpan()) then
                    Editing EditingKey.Delete
                elif raw.SequenceEqual ("Esc".AsSpan()) then
                    Ui Escape
                elif raw.SequenceEqual ("Alt".AsSpan()) then
                    Modifier Alt
                elif raw[0] = 'F' || raw[0] = 'f' then
                    let n = tryParseFunctionNumber3 raw[1] raw[2]
                    if n > 0 then FunctionKey n else scalarFallback raw
                else
                    scalarFallback raw
            | 4 ->
                if raw.SequenceEqual ("Left".AsSpan()) then
                    Navigation ArrowLeft
                elif raw.SequenceEqual ("Down".AsSpan()) then
                    Navigation ArrowDown
                elif raw.SequenceEqual ("Home".AsSpan()) then
                    Navigation Home
                elif raw.SequenceEqual ("Meta".AsSpan()) then
                    Modifier Meta
                elif raw.SequenceEqual ("Dead".AsSpan()) then
                    Dead Acute
                elif raw.SequenceEqual ("Eisu".AsSpan()) then
                    Ime Eisu
                else
                    scalarFallback raw
            | 5 ->
                if raw.SequenceEqual ("Enter".AsSpan()) then
                    Whitespace Enter
                elif raw.SequenceEqual ("Right".AsSpan()) then
                    Navigation ArrowRight
                elif raw.SequenceEqual ("Clear".AsSpan()) then
                    Editing EditingKey.Clear
                elif raw.SequenceEqual ("Pause".AsSpan()) then
                    Ui Pause
                elif raw.SequenceEqual ("Shift".AsSpan()) then
                    Modifier Shift
                elif raw.SequenceEqual ("Super".AsSpan()) then
                    Modifier Meta
                elif raw.SequenceEqual ("Hyper".AsSpan()) then
                    Modifier Meta
                else
                    scalarFallback raw
            | 6 ->
                if raw.SequenceEqual ("Return".AsSpan()) then
                    Whitespace Enter
                elif raw.SequenceEqual ("PageUp".AsSpan()) then
                    Navigation PageUp
                elif raw.SequenceEqual ("Delete".AsSpan()) then
                    Editing EditingKey.Delete
                elif raw.SequenceEqual ("Insert".AsSpan()) then
                    Editing EditingKey.Insert
                elif raw.SequenceEqual ("Escape".AsSpan()) then
                    Ui Escape
                elif raw.SequenceEqual ("Cancel".AsSpan()) then
                    Ui Cancel
                elif raw.SequenceEqual ("Symbol".AsSpan()) then
                    Modifier Symbol
                else
                    scalarFallback raw
            | 7 ->
                if raw.SequenceEqual ("ArrowUp".AsSpan()) then
                    Navigation ArrowUp
                elif raw.SequenceEqual ("Control".AsSpan()) then
                    Modifier Control
                elif raw.SequenceEqual ("AltLeft".AsSpan()) then
                    Modifier Alt
                elif raw.SequenceEqual ("NumLock".AsSpan()) then
                    Modifier NumLock
                elif raw.SequenceEqual ("Process".AsSpan()) then
                    Ime Process
                elif raw.SequenceEqual ("Convert".AsSpan()) then
                    Ime Convert
                else
                    scalarFallback raw
            | 8 ->
                if raw.SequenceEqual ("Spacebar".AsSpan()) then
                    Whitespace Space
                elif raw.SequenceEqual ("PageDown".AsSpan()) then
                    Navigation PageDown
                elif raw.SequenceEqual ("AltRight".AsSpan()) then
                    Modifier Alt
                elif raw.SequenceEqual ("AltGraph".AsSpan()) then
                    Modifier AltGraph
                elif raw.SequenceEqual ("MetaLeft".AsSpan()) then
                    Modifier Meta
                elif raw.SequenceEqual ("CapsLock".AsSpan()) then
                    Modifier CapsLock
                elif raw.SequenceEqual ("KanaMode".AsSpan()) then
                    Ime KanaMode
                elif raw.SequenceEqual ("<leader>".AsSpan()) then
                    LeaderVirtual
                else
                    scalarFallback raw
            | 9 ->
                if raw.SequenceEqual ("ArrowLeft".AsSpan()) then
                    Navigation ArrowLeft
                elif raw.SequenceEqual ("ArrowDown".AsSpan()) then
                    Navigation ArrowDown
                elif raw.SequenceEqual ("Backspace".AsSpan()) then
                    Editing EditingKey.Backspace
                elif raw.SequenceEqual ("ShiftLeft".AsSpan()) then
                    Modifier Shift
                elif raw.SequenceEqual ("MetaRight".AsSpan()) then
                    Modifier Meta
                elif raw.SequenceEqual ("DeadAcute".AsSpan()) then
                    Dead Acute
                elif raw.SequenceEqual ("DeadGrave".AsSpan()) then
                    Dead Grave
                elif raw.SequenceEqual ("DeadTilde".AsSpan()) then
                    Dead Tilde
                elif raw.SequenceEqual ("DeadCaron".AsSpan()) then
                    Dead Caron
                elif raw.SequenceEqual ("DeadBreve".AsSpan()) then
                    Dead Breve
                elif raw.SequenceEqual ("KanjiMode".AsSpan()) then
                    Ime KanjiMode
                elif raw.SequenceEqual ("HanjaMode".AsSpan()) then
                    Ime HanjaMode
                else
                    scalarFallback raw
            | 10 ->
                if raw.SequenceEqual ("ArrowRight".AsSpan()) then
                    Navigation ArrowRight
                elif raw.SequenceEqual ("ShiftRight".AsSpan()) then
                    Modifier Shift
                elif raw.SequenceEqual ("ScrollLock".AsSpan()) then
                    Modifier ScrollLock
                elif raw.SequenceEqual ("DeadMacron".AsSpan()) then
                    Dead Macron
                elif raw.SequenceEqual ("DeadOgonek".AsSpan()) then
                    Dead Ogonek
                elif raw.SequenceEqual ("DeadStroke".AsSpan()) then
                    Dead Stroke
                elif raw.SequenceEqual ("NonConvert".AsSpan()) then
                    Ime NonConvert
                elif raw.SequenceEqual ("ModeChange".AsSpan()) then
                    Ime ModeChange
                elif raw.SequenceEqual ("HangulMode".AsSpan()) then
                    Ime HangulMode
                else
                    scalarFallback raw
            | 11 ->
                if raw.SequenceEqual ("ContextMenu".AsSpan()) then
                    Ui ContextMenu
                elif raw.SequenceEqual ("ControlLeft".AsSpan()) then
                    Modifier Control
                elif raw.SequenceEqual ("DeadCedilla".AsSpan()) then
                    Dead Cedilla
                else
                    scalarFallback raw
            | 12 ->
                if raw.SequenceEqual ("ControlRight".AsSpan()) then
                    Modifier Control
                elif raw.SequenceEqual ("DeadAboveDot".AsSpan()) then
                    Dead DotAbove
                elif raw.SequenceEqual ("DeadDotAbove".AsSpan()) then
                    Dead DotAbove
                elif raw.SequenceEqual ("Unidentified".AsSpan()) then
                    Unidentified "Unidentified"
                else
                    scalarFallback raw
            | 13 ->
                if raw.SequenceEqual ("DeadDiaeresis".AsSpan()) then
                    Dead Diaeresis
                elif raw.SequenceEqual ("DeadRingAbove".AsSpan()) then
                    Dead RingAbove
                else
                    scalarFallback raw
            | 14 ->
                if raw.SequenceEqual ("DeadCircumflex".AsSpan()) then
                    Dead Circumflex
                elif raw.SequenceEqual ("ZenkakuHankaku".AsSpan()) then
                    Ime ZenkakuHankaku
                else
                    scalarFallback raw
            | 15 ->
                if raw.SequenceEqual ("DeadDoubleAcute".AsSpan()) then
                    Dead DoubleAcute
                else
                    scalarFallback raw
            | _ -> GraphemeText (raw.ToString ())

    /// <summary>Parses a UTF-16 ReadOnlySpan W3C KeyboardEvent.key using three-tier SIMD with scalar fallback.</summary>
    let ofW3cKeySpan (raw: ReadOnlySpan<char>) : LogicalKey =
        let len = raw.Length

        if len = 0 then
            Unidentified ""
        elif len = 1 then
            let c = raw[0]

            if c = ' ' then Whitespace Space
            elif Char.IsSurrogate c then GraphemeText (raw.ToString ())
            else Character (Rune c)
        elif len > 15 then
            GraphemeText (raw.ToString ())
        elif
            (Vector512.IsHardwareAccelerated
             && Vector512<byte>.IsSupported)
            || (Vector256.IsHardwareAccelerated
                && Vector256<byte>.IsSupported)
            || (Vector128.IsHardwareAccelerated
                && Vector128<byte>.IsSupported)
        then
            let mutable keyVec = Vector128<byte>.Zero

            if SimdKeyTable.tryPackAsciiChars16 raw &keyVec then
                let idx =
                    SimdKeyTable.lookupSlot simdTable.Index len (byte raw[0]) (byte raw[len >>> 1]) (byte raw[len - 1]) keyVec

                if idx >= 0 then
                    simdTable.Values[idx]
                else
                    GraphemeText (raw.ToString ())
            elif len = 2 && Char.IsSurrogatePair (raw[0], raw[1]) then
                Character (Rune (raw[0], raw[1]))
            else
                GraphemeText (raw.ToString ())
        else
            ofW3cKeyScalar raw

    /// <summary>Parses a UTF-8 ReadOnlySpan W3C KeyboardEvent.key using three-tier SIMD with scalar fallback.</summary>
    let ofW3cKeyUtf8 (utf8: ReadOnlySpan<byte>) : LogicalKey =
        let len = utf8.Length

        if len = 0 then
            Unidentified ""
        elif len = 1 then
            let b = utf8[0]

            if b = 0x20uy then Whitespace Space
            elif b < 0x80uy then Character (Rune (int b))
            else ofW3cKeySpan (Encoding.UTF8.GetString(utf8).AsSpan())
        elif len <= 15 then
            let mutable keyVec = Vector128<byte>.Zero

            if SimdKeyTable.tryPackAsciiBytes16 utf8 &keyVec then
                if
                    (Vector512.IsHardwareAccelerated
                     && Vector512<byte>.IsSupported)
                    || (Vector256.IsHardwareAccelerated
                        && Vector256<byte>.IsSupported)
                    || (Vector128.IsHardwareAccelerated
                        && Vector128<byte>.IsSupported)
                then
                    let idx =
                        SimdKeyTable.lookupSlot simdTable.Index len utf8[0] utf8[len >>> 1] utf8[len - 1] keyVec

                    if idx >= 0 then
                        simdTable.Values[idx]
                    else
                        GraphemeText (Encoding.UTF8.GetString (utf8))
                else
                    ofW3cKeyScalar (Encoding.UTF8.GetString(utf8).AsSpan())
            elif len <= 4 then
                let mutable r = Rune.ReplacementChar
                let mutable consumed = 0

                if
                    Rune.DecodeFromUtf8 (utf8, &r, &consumed) = OperationStatus.Done
                    && consumed = len
                then
                    Character r
                else
                    GraphemeText (Encoding.UTF8.GetString (utf8))
            else
                GraphemeText (Encoding.UTF8.GetString (utf8))
        else
            GraphemeText (Encoding.UTF8.GetString (utf8))

    /// <summary>Parses a W3C UI Events KeyboardEvent.key string (and common aliases) into a LogicalKey.</summary>
    let ofW3cKey (raw: string) : LogicalKey =
        if isNull raw || raw.Length = 0 then
            Unidentified ""
        elif raw.Length = 1 then
            let c = raw[0]

            if c = ' ' then Whitespace Space
            elif Char.IsSurrogate c then GraphemeText raw
            else Character (Rune c)
        else
            ofW3cKeySpan (raw.AsSpan ())
#endif

    /// <summary>Formats a LogicalKey back to its canonical W3C KeyboardEvent.key string representation.</summary>
    let toW3cKey (k: LogicalKey) : string =
        match k with
        | Character r -> r.ToString ()
        | GraphemeText s -> s
        | Whitespace Space -> " "
        | Whitespace Tab -> "Tab"
        | Whitespace Enter -> "Enter"
        | Navigation ArrowLeft -> "ArrowLeft"
        | Navigation ArrowRight -> "ArrowRight"
        | Navigation ArrowUp -> "ArrowUp"
        | Navigation ArrowDown -> "ArrowDown"
        | Navigation Home -> "Home"
        | Navigation End -> "End"
        | Navigation PageUp -> "PageUp"
        | Navigation PageDown -> "PageDown"
        | Editing EditingKey.Backspace -> "Backspace"
        | Editing EditingKey.Delete -> "Delete"
        | Editing EditingKey.Insert -> "Insert"
        | Editing EditingKey.Clear -> "Clear"
        | Ui Escape -> "Escape"
        | Ui ContextMenu -> "ContextMenu"
        | Ui Pause -> "Pause"
        | Ui Cancel -> "Cancel"
        | FunctionKey n ->
            if n >= 1 && n <= 24 then
                functionKeyNames[n - 1]
            else
                "F" + string n
        | Modifier Shift -> "Shift"
        | Modifier Control -> "Control"
        | Modifier Alt -> "Alt"
        | Modifier AltGraph -> "AltGraph"
        | Modifier Meta -> "Meta"
        | Modifier CapsLock -> "CapsLock"
        | Modifier NumLock -> "NumLock"
        | Modifier ScrollLock -> "ScrollLock"
        | Modifier Symbol -> "Symbol"
        | Modifier Fn -> "Fn"
        | Dead Grave -> "DeadGrave"
        | Dead Acute -> "DeadAcute"
        | Dead Circumflex -> "DeadCircumflex"
        | Dead Tilde -> "DeadTilde"
        | Dead Diaeresis -> "DeadDiaeresis"
        | Dead RingAbove -> "DeadRingAbove"
        | Dead Cedilla -> "DeadCedilla"
        | Dead Caron -> "DeadCaron"
        | Dead Macron -> "DeadMacron"
        | Dead Breve -> "DeadBreve"
        | Dead DoubleAcute -> "DeadDoubleAcute"
        | Dead Ogonek -> "DeadOgonek"
        | Dead Stroke -> "DeadStroke"
        | Dead DotAbove -> "DeadDotAbove"
        | Ime Process -> "Process"
        | Ime Convert -> "Convert"
        | Ime NonConvert -> "NonConvert"
        | Ime ModeChange -> "ModeChange"
        | Ime KanjiMode -> "KanjiMode"
        | Ime KanaMode -> "KanaMode"
        | Ime HangulMode -> "HangulMode"
        | Ime HanjaMode -> "HanjaMode"
        | Ime Eisu -> "Eisu"
        | Ime ZenkakuHankaku -> "ZenkakuHankaku"
        | LeaderVirtual -> "<leader>"
        | Unidentified s -> if String.IsNullOrEmpty s then "Unidentified" else s

    /// <summary>Returns the printable text produced by k when it is a Character, non-empty GraphemeText, or Space.</summary>
    let tryPrintedText (k: LogicalKey) : string voption =
        match k with
        | Character r -> ValueSome (r.ToString ())
        | GraphemeText s when not (String.IsNullOrEmpty s) -> ValueSome s
        | Whitespace Space -> ValueSome " "
        | _ -> ValueNone

    /// <summary>True when k is a pure modifier, IME Process event, or Unidentified key that should not advance modal state.</summary>
    let isNonActionableModifier (k: LogicalKey) : bool =
        match k with
        | Modifier _
        | Ime Process
        | Unidentified _ -> true
        | _ -> false

    /// <summary>Normalizes ISO/IEC 9995-3 AltGraph (reported on Windows layouts as Ctrl + Alt alongside a Level-3 or non-ASCII symbol).</summary>
    let normalizeAltGraph (logical: LogicalKey) (mods: KeyModifiers) : KeyModifiers =
        let isIsoLevel3Symbol =
            match logical with
            | Character r ->
                r.Value > 0x7F
                || (match char r.Value with
                    | '{'
                    | '}'
                    | '['
                    | ']'
                    | '\\'
                    | '@'
                    | '~'
                    | '|'
                    | '`'
                    | '^'
                    | '$'
                    | '#' -> true
                    | _ -> false)
            | GraphemeText s -> not (String.IsNullOrEmpty s)
            | _ -> false

        if
            mods.AltGraph
            || (mods.Ctrl && mods.Alt && isIsoLevel3Symbol)
        then
            { mods with
                Ctrl = false
                Alt = false
                AltGraph = true
            }
        else
            mods
