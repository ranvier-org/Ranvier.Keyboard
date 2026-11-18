namespace Ranvier.Keyboard

open System
open System.Globalization
open System.Text
#if !FABLE_COMPILER
open System.Buffers
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Intrinsics
#endif

/// <summary>ECMA-48 / ISO/IEC 6429 C0 control characters (0x00..0x1F) plus ISO/IEC 646 DEL (0x7F).</summary>
[<Struct>]
type C0Control =
    | Nul
    | Soh
    | Stx
    | Etx
    | Eot
    | Enq
    | Ack
    | Bel
    | Bs
    | Ht
    | Lf
    | Vt
    | Ff
    | Cr
    | So
    | Si
    | Dle
    | Dc1
    | Dc2
    | Dc3
    | Dc4
    | Nak
    | Syn
    | Etb
    | Can
    | Em
    | Sub
    | Esc
    | Fs
    | Gs
    | Rs
    | Us
    | Del

/// <summary>C0 control operations and SIMD span scanners.</summary>
[<RequireQualifiedAccess>]
module C0Control =

    let private caretTable =
        [|
            "^@"
            "^A"
            "^B"
            "^C"
            "^D"
            "^E"
            "^F"
            "^G"
            "^H"
            "^I"
            "^J"
            "^K"
            "^L"
            "^M"
            "^N"
            "^O"
            "^P"
            "^Q"
            "^R"
            "^S"
            "^T"
            "^U"
            "^V"
            "^W"
            "^X"
            "^Y"
            "^Z"
            "^["
            "^\\"
            "^]"
            "^^"
            "^_"
        |]

    /// <summary>Attempts to classify a byte as an ECMA-48 C0 control or DEL (0x7F).</summary>
    let ofByte (b: byte) : C0Control voption =
        match b with
        | 0x00uy -> ValueSome Nul
        | 0x01uy -> ValueSome Soh
        | 0x02uy -> ValueSome Stx
        | 0x03uy -> ValueSome Etx
        | 0x04uy -> ValueSome Eot
        | 0x05uy -> ValueSome Enq
        | 0x06uy -> ValueSome Ack
        | 0x07uy -> ValueSome Bel
        | 0x08uy -> ValueSome Bs
        | 0x09uy -> ValueSome Ht
        | 0x0Auy -> ValueSome Lf
        | 0x0Buy -> ValueSome Vt
        | 0x0Cuy -> ValueSome Ff
        | 0x0Duy -> ValueSome Cr
        | 0x0Euy -> ValueSome So
        | 0x0Fuy -> ValueSome Si
        | 0x10uy -> ValueSome Dle
        | 0x11uy -> ValueSome Dc1
        | 0x12uy -> ValueSome Dc2
        | 0x13uy -> ValueSome Dc3
        | 0x14uy -> ValueSome Dc4
        | 0x15uy -> ValueSome Nak
        | 0x16uy -> ValueSome Syn
        | 0x17uy -> ValueSome Etb
        | 0x18uy -> ValueSome Can
        | 0x19uy -> ValueSome Em
        | 0x1Auy -> ValueSome Sub
        | 0x1Buy -> ValueSome Esc
        | 0x1Cuy -> ValueSome Fs
        | 0x1Duy -> ValueSome Gs
        | 0x1Euy -> ValueSome Rs
        | 0x1Fuy -> ValueSome Us
        | 0x7Fuy -> ValueSome Del
        | _ -> ValueNone

    /// <summary>Converts a C0Control to its raw ASCII byte value.</summary>
    let toByte (ctrl: C0Control) : byte =
        match ctrl with
        | Nul -> 0x00uy
        | Soh -> 0x01uy
        | Stx -> 0x02uy
        | Etx -> 0x03uy
        | Eot -> 0x04uy
        | Enq -> 0x05uy
        | Ack -> 0x06uy
        | Bel -> 0x07uy
        | Bs -> 0x08uy
        | Ht -> 0x09uy
        | Lf -> 0x0Auy
        | Vt -> 0x0Buy
        | Ff -> 0x0Cuy
        | Cr -> 0x0Duy
        | So -> 0x0Euy
        | Si -> 0x0Fuy
        | Dle -> 0x10uy
        | Dc1 -> 0x11uy
        | Dc2 -> 0x12uy
        | Dc3 -> 0x13uy
        | Dc4 -> 0x14uy
        | Nak -> 0x15uy
        | Syn -> 0x16uy
        | Etb -> 0x17uy
        | Can -> 0x18uy
        | Em -> 0x19uy
        | Sub -> 0x1Auy
        | Esc -> 0x1Buy
        | Fs -> 0x1Cuy
        | Gs -> 0x1Duy
        | Rs -> 0x1Euy
        | Us -> 0x1Fuy
        | Del -> 0x7Fuy

    /// <summary>Maps Ctrl + printable ASCII key (ISO/IEC 646 caret notation, e.g. 'a'/'A' -> Soh, '[' -> Esc, '?' -> Del).</summary>
    let ofModifiedAscii (ch: char) : C0Control voption =
        let upper = if ch >= 'a' && ch <= 'z' then char (int ch - 0x20) else ch

        if upper >= '@' && upper <= '_' then
            ofByte (byte (int upper - 0x40))
        elif ch = '?' then
            ValueSome Del
        elif ch = ' ' then
            ValueSome Nul
        else
            ValueNone

    /// <summary>Formats a C0Control in standard ISO/IEC 646 caret notation (^@ through ^_ and ^?).</summary>
    let toCaretNotation (ctrl: C0Control) : string =
        match ctrl with
        | Del -> "^?"
        | other -> caretTable[int (toByte other)]

#if !FABLE_COMPILER
    /// <summary>Scalar reference scanner returning the index of the first C0 control or DEL byte, or -1.</summary>
    let indexOfControlByteScalar (span: ReadOnlySpan<byte>) : int =
        let mutable idx = 0
        let mutable found = -1

        while idx < span.Length && found < 0 do
            let b = span[idx]

            if b <= 0x1Fuy || b = 0x7Fuy then
                found <- idx

            idx <- idx + 1

        found

    /// <summary>Returns the index of the first C0 control (0x00..0x1F) or DEL (0x7F) byte using three-tier SIMD, or -1.</summary>
    let indexOfControlByte (span: ReadOnlySpan<byte>) : int =
        let length = unativeint span.Length

        if length = 0un then
            -1
        else
            let src = &MemoryMarshal.GetReference (span)
            let mutable i = 0un
            let mutable found = -1

            if
                Vector512.IsHardwareAccelerated
                && Vector512<byte>.IsSupported
            then
                let vMaxC0 = Vector512.Create (0x1Fuy)
                let vDel = Vector512.Create (0x7Fuy)

                while i + 64un <= length && found < 0 do
                    let vec = Vector512.LoadUnsafe (&src, i)

                    let m =
                        Vector512.LessThanOrEqual (vec, vMaxC0)
                        ||| Vector512.Equals (vec, vDel)

                    let mask = m.ExtractMostSignificantBits ()

                    if mask <> 0UL then
                        found <- int i + BitOperations.TrailingZeroCount (mask)
                    else
                        i <- i + 64un
            elif
                Vector256.IsHardwareAccelerated
                && Vector256<byte>.IsSupported
            then
                let vMaxC0 = Vector256.Create (0x1Fuy)
                let vDel = Vector256.Create (0x7Fuy)

                while i + 32un <= length && found < 0 do
                    let vec = Vector256.LoadUnsafe (&src, i)

                    let m =
                        Vector256.LessThanOrEqual (vec, vMaxC0)
                        ||| Vector256.Equals (vec, vDel)

                    let mask = m.ExtractMostSignificantBits ()

                    if mask <> 0u then
                        found <- int i + BitOperations.TrailingZeroCount (mask)
                    else
                        i <- i + 32un
            elif
                Vector128.IsHardwareAccelerated
                && Vector128<byte>.IsSupported
            then
                let vMaxC0 = Vector128.Create (0x1Fuy)
                let vDel = Vector128.Create (0x7Fuy)

                while i + 16un <= length && found < 0 do
                    let vec = Vector128.LoadUnsafe (&src, i)

                    let m =
                        Vector128.LessThanOrEqual (vec, vMaxC0)
                        ||| Vector128.Equals (vec, vDel)

                    let mask = m.ExtractMostSignificantBits ()

                    if mask <> 0u then
                        found <- int i + BitOperations.TrailingZeroCount (mask)
                    else
                        i <- i + 16un

            while i < length && found < 0 do
                let b = Unsafe.Add (&src, i)

                if b <= 0x1Fuy || b = 0x7Fuy then
                    found <- int i
                else
                    i <- i + 1un

            found

    /// <summary>Scalar reference scanner returning the index of the first C0 control or DEL char, or -1.</summary>
    let indexOfControlCharScalar (span: ReadOnlySpan<char>) : int =
        let mutable idx = 0
        let mutable found = -1

        while idx < span.Length && found < 0 do
            let u = uint16 span[idx]

            if u <= 0x001Fus || u = 0x007Fus then
                found <- idx

            idx <- idx + 1

        found

    /// <summary>Returns the index of the first C0 control (U+0000..U+001F) or DEL (U+007F) char using three-tier SIMD, or -1.</summary>
    let indexOfControlChar (span: ReadOnlySpan<char>) : int =
        let length = unativeint span.Length

        if length = 0un then
            -1
        else
            let u16Span = MemoryMarshal.Cast<char, uint16>(span)
            let src = &MemoryMarshal.GetReference (u16Span)
            let mutable i = 0un
            let mutable found = -1

            if
                Vector512.IsHardwareAccelerated
                && Vector512<uint16>.IsSupported
            then
                let vMaxC0 = Vector512.Create (0x001Fus)
                let vDel = Vector512.Create (0x007Fus)

                while i + 32un <= length && found < 0 do
                    let vec = Vector512.LoadUnsafe (&src, i)

                    let m =
                        Vector512.LessThanOrEqual (vec, vMaxC0)
                        ||| Vector512.Equals (vec, vDel)

                    let mask = m.ExtractMostSignificantBits ()

                    if mask <> 0UL then
                        found <- int i + BitOperations.TrailingZeroCount (mask)
                    else
                        i <- i + 32un
            elif
                Vector256.IsHardwareAccelerated
                && Vector256<uint16>.IsSupported
            then
                let vMaxC0 = Vector256.Create (0x001Fus)
                let vDel = Vector256.Create (0x007Fus)

                while i + 16un <= length && found < 0 do
                    let vec = Vector256.LoadUnsafe (&src, i)

                    let m =
                        Vector256.LessThanOrEqual (vec, vMaxC0)
                        ||| Vector256.Equals (vec, vDel)

                    let mask = m.ExtractMostSignificantBits ()

                    if mask <> 0u then
                        found <- int i + BitOperations.TrailingZeroCount (mask)
                    else
                        i <- i + 16un
            elif
                Vector128.IsHardwareAccelerated
                && Vector128<uint16>.IsSupported
            then
                let vMaxC0 = Vector128.Create (0x001Fus)
                let vDel = Vector128.Create (0x007Fus)

                while i + 8un <= length && found < 0 do
                    let vec = Vector128.LoadUnsafe (&src, i)

                    let m =
                        Vector128.LessThanOrEqual (vec, vMaxC0)
                        ||| Vector128.Equals (vec, vDel)

                    let mask = m.ExtractMostSignificantBits ()

                    if mask <> 0u then
                        found <- int i + BitOperations.TrailingZeroCount (mask)
                    else
                        i <- i + 8un

            while i < length && found < 0 do
                let u = Unsafe.Add (&src, i)

                if u <= 0x001Fus || u = 0x007Fus then
                    found <- int i
                else
                    i <- i + 1un

            found
#endif

/// <summary>ISO/IEC 10646 / Unicode letter case classification.</summary>
[<RequireQualifiedAccess; Struct>]
type LetterCase =
    | Uppercase
    | Lowercase
    | Titlecase
    | ModifierLetter
    | CaselessLetter

/// <summary>Unicode Bidi / paired delimiter direction.</summary>
[<Struct>]
type BracketRole =
    | OpeningBracket
    | ClosingBracket

/// <summary>International quotation mark families.</summary>
[<Struct>]
type QuoteStyle =
    | DoubleAscii
    | SingleAscii
    | BacktickAscii
    | CurvedDoubleOpen
    | CurvedDoubleClose
    | CurvedSingleOpen
    | CurvedSingleClose

/// <summary>ISO/IEC 10646 / Unicode Standard character classification over System.Text.Rune.</summary>
[<Struct>]
type UnicodeCharClass =
    | UnicodeLetter of letterCase: LetterCase
    | UnicodeDigit of decimalValue: int voption
    | UnicodeConnector
    | UnicodeBracket of role: BracketRole * partner: Rune
    | UnicodeQuote of quoteStyle: QuoteStyle
    | UnicodeMathOrSymbol
    | UnicodePunctuation
    | UnicodeWhitespace
    | UnicodeControl of c0: C0Control voption
    | UnicodeMarkOrOther

/// <summary>Unicode character classification and Vim word boundary helpers.</summary>
[<RequireQualifiedAccess>]
module UnicodeChar =

#if FABLE_COMPILER
    let private decimalZeroBases =
        [|
            0x0030
            0x0660
            0x06F0
            0x07C0
            0x0966
            0x09E6
            0x0A66
            0x0AE6
            0x0B66
            0x0BE6
            0x0C66
            0x0CE6
            0x0D66
            0x0DE6
            0x0E50
            0x0ED0
            0x0F20
            0x1040
            0x1090
            0x17E0
            0x1810
            0x1946
            0x19D0
            0x1A80
            0x1A90
            0x1B50
            0x1BB0
            0x1C40
            0x1C50
            0xA620
            0xA8D0
            0xA900
            0xA9D0
            0xA9F0
            0xAA50
            0xABF0
            0xFF10
            0x1D7CE
            0x1D7D8
            0x1D7E2
            0x1D7EC
            0x1D7F6
        |]

    let private tryDecimalDigitValue (codePoint: int) : int voption =
        if codePoint >= 0x30 && codePoint <= 0x39 then
            ValueSome (codePoint - 0x30)
        else
            let mutable idx = 0
            let mutable found = ValueNone

            while idx < decimalZeroBases.Length && found.IsNone do
                let z = decimalZeroBases[idx]

                if codePoint >= z && codePoint <= z + 9 then
                    found <- ValueSome (codePoint - z)

                idx <- idx + 1

            found
#else
    let private tryDecimalDigitValue (r: Rune) : int voption =
        let cp = r.Value

        if cp >= 0x30 && cp <= 0x39 then
            ValueSome (cp - 0x30)
        else
            let d = CharUnicodeInfo.GetDecimalDigitValue (r.ToString (), 0)
            if d >= 0 then ValueSome d else ValueNone
#endif

    /// <summary>Paired bracket table spanning ASCII, Latin-1 guillemets, Mathematical brackets, and CJK/fullwidth brackets.</summary>
    let tryBracketPartner (r: Rune) : struct (BracketRole * Rune) voption =
        match r.Value with
        | 0x0028 -> ValueSome (struct (OpeningBracket, Rune 0x0029))
        | 0x0029 -> ValueSome (struct (ClosingBracket, Rune 0x0028))
        | 0x005B -> ValueSome (struct (OpeningBracket, Rune 0x005D))
        | 0x005D -> ValueSome (struct (ClosingBracket, Rune 0x005B))
        | 0x007B -> ValueSome (struct (OpeningBracket, Rune 0x007D))
        | 0x007D -> ValueSome (struct (ClosingBracket, Rune 0x007B))
        | 0x00AB -> ValueSome (struct (OpeningBracket, Rune 0x00BB))
        | 0x00BB -> ValueSome (struct (ClosingBracket, Rune 0x00AB))
        | 0x2039 -> ValueSome (struct (OpeningBracket, Rune 0x203A))
        | 0x203A -> ValueSome (struct (ClosingBracket, Rune 0x2039))
        | 0x27E8 -> ValueSome (struct (OpeningBracket, Rune 0x27E9))
        | 0x27E9 -> ValueSome (struct (ClosingBracket, Rune 0x27E8))
        | 0x3008 -> ValueSome (struct (OpeningBracket, Rune 0x3009))
        | 0x3009 -> ValueSome (struct (ClosingBracket, Rune 0x3008))
        | 0x300A -> ValueSome (struct (OpeningBracket, Rune 0x300B))
        | 0x300B -> ValueSome (struct (ClosingBracket, Rune 0x300A))
        | 0x300C -> ValueSome (struct (OpeningBracket, Rune 0x300D))
        | 0x300D -> ValueSome (struct (ClosingBracket, Rune 0x300C))
        | 0x300E -> ValueSome (struct (OpeningBracket, Rune 0x300F))
        | 0x300F -> ValueSome (struct (ClosingBracket, Rune 0x300E))
        | 0x3010 -> ValueSome (struct (OpeningBracket, Rune 0x3011))
        | 0x3011 -> ValueSome (struct (ClosingBracket, Rune 0x3010))
        | 0xFF08 -> ValueSome (struct (OpeningBracket, Rune 0xFF09))
        | 0xFF09 -> ValueSome (struct (ClosingBracket, Rune 0xFF08))
        | 0xFF3B -> ValueSome (struct (OpeningBracket, Rune 0xFF3D))
        | 0xFF3D -> ValueSome (struct (ClosingBracket, Rune 0xFF3B))
        | 0xFF5B -> ValueSome (struct (OpeningBracket, Rune 0xFF5D))
        | 0xFF5D -> ValueSome (struct (ClosingBracket, Rune 0xFF5B))
        | _ -> ValueNone

    /// <summary>Classifies any Unicode scalar value according to Unicode General Category and Bidi Bracket tables.</summary>
    let classify (r: Rune) : UnicodeCharClass =
        if r.Value <= 0x1F || r.Value = 0x7F then
            UnicodeControl (C0Control.ofByte (byte r.Value))
        else
            match r.Value with
            | 0x0022 -> UnicodeQuote DoubleAscii
            | 0x0027 -> UnicodeQuote SingleAscii
            | 0x0060 -> UnicodeQuote BacktickAscii
            | 0x201C -> UnicodeQuote CurvedDoubleOpen
            | 0x201D -> UnicodeQuote CurvedDoubleClose
            | 0x2018 -> UnicodeQuote CurvedSingleOpen
            | 0x2019 -> UnicodeQuote CurvedSingleClose
            | _ ->
                match tryBracketPartner r with
                | ValueSome (struct (role, partner)) -> UnicodeBracket (role, partner)
                | ValueNone ->
                    if Rune.IsWhiteSpace r then
                        UnicodeWhitespace
                    else
                        match Rune.GetUnicodeCategory r with
                        | UnicodeCategory.UppercaseLetter -> UnicodeLetter LetterCase.Uppercase
                        | UnicodeCategory.LowercaseLetter -> UnicodeLetter LetterCase.Lowercase
                        | UnicodeCategory.TitlecaseLetter -> UnicodeLetter LetterCase.Titlecase
                        | UnicodeCategory.ModifierLetter -> UnicodeLetter LetterCase.ModifierLetter
                        | UnicodeCategory.OtherLetter -> UnicodeLetter LetterCase.CaselessLetter
                        | UnicodeCategory.DecimalDigitNumber ->
#if FABLE_COMPILER
                            UnicodeDigit (tryDecimalDigitValue r.Value)
#else
                            UnicodeDigit (tryDecimalDigitValue r)
#endif
                        | UnicodeCategory.LetterNumber
                        | UnicodeCategory.OtherNumber -> UnicodeDigit ValueNone
                        | UnicodeCategory.ConnectorPunctuation -> UnicodeConnector
                        | UnicodeCategory.MathSymbol
                        | UnicodeCategory.CurrencySymbol
                        | UnicodeCategory.ModifierSymbol
                        | UnicodeCategory.OtherSymbol -> UnicodeMathOrSymbol
                        | UnicodeCategory.DashPunctuation
                        | UnicodeCategory.OpenPunctuation
                        | UnicodeCategory.ClosePunctuation
                        | UnicodeCategory.InitialQuotePunctuation
                        | UnicodeCategory.FinalQuotePunctuation
                        | UnicodeCategory.OtherPunctuation -> UnicodePunctuation
                        | UnicodeCategory.Control
                        | UnicodeCategory.Format -> UnicodeControl ValueNone
                        | _ -> UnicodeMarkOrOther

    /// <summary>Vim keyword-character classification extended across Unicode scripts.</summary>
    let isVimWordRune (r: Rune) : bool =
        match classify r with
        | UnicodeLetter _
        | UnicodeDigit _
        | UnicodeConnector -> true
        | _ -> false

    /// <summary>Vim character class: 0 = whitespace, 1 = keyword (letter/digit/connector), 2 = punctuation/symbol/other graphic.</summary>
    let vimWordClass (r: Rune) : int =
        if Rune.IsWhiteSpace r then 0
        elif isVimWordRune r then 1
        else 2

/// <summary>Unicode UAX #29 Extended Grapheme Cluster boundaries and full-Unicode Rune case transformations.</summary>
[<RequireQualifiedAccess>]
module Grapheme =

    let inline private clampIndex (idx: int) (len: int) : int =
        if idx < 0 then 0
        elif idx > len then len
        else idx

#if !FABLE_COMPILER
    /// Counts leading printable ASCII chars (0x0020..0x007E) using three-tier SIMD.
    let internal asciiPrintablePrefixLength (span: ReadOnlySpan<char>) : int =
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

                while i + 32un <= length && stop < 0 do
                    let vec = Vector512.LoadUnsafe (&src, i)
                    let failMask = Vector512.GreaterThan(vec - vLo, vRange).ExtractMostSignificantBits()

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

                while i + 16un <= length && stop < 0 do
                    let vec = Vector256.LoadUnsafe (&src, i)
                    let failMask = Vector256.GreaterThan(vec - vLo, vRange).ExtractMostSignificantBits()

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

                while i + 8un <= length && stop < 0 do
                    let vec = Vector128.LoadUnsafe (&src, i)
                    let failMask = Vector128.GreaterThan(vec - vLo, vRange).ExtractMostSignificantBits()

                    if failMask <> 0u then
                        stop <- int i + BitOperations.TrailingZeroCount (failMask)
                    else
                        i <- i + 8un

            if stop >= 0 then
                stop
            else
                while i < length
                      && (Unsafe.Add (&src, i) - 0x0020us) <= 0x005Eus do
                    i <- i + 1un

                int i

    /// Safe count of 1-char UAX #29 clusters at the start of span (accounting for trailing combining mark >= U+0300).
    let internal asciiSimpleClusterPrefix (span: ReadOnlySpan<char>) : int =
        let n = asciiPrintablePrefixLength span

        if n > 0 && n < span.Length && span[n] >= '\u0300' then
            n - 1
        else
            n

    /// Counts trailing printable ASCII chars (0x0020..0x007E) ending at span.Length using three-tier SIMD.
    let internal asciiPrintableSuffixLength (span: ReadOnlySpan<char>) : int =
        let len = span.Length

        if len = 0 then
            0
        else
            let u16Span = MemoryMarshal.Cast<char, uint16>(span)
            let src = &MemoryMarshal.GetReference (u16Span)
            let mutable pos = len
            let mutable stopped = false

            if
                Vector512.IsHardwareAccelerated
                && Vector512<uint16>.IsSupported
            then
                let vLo = Vector512.Create (0x0020us)
                let vRange = Vector512.Create (0x005Eus)

                while pos >= 32 && not stopped do
                    let start = pos - 32
                    let vec = Vector512.LoadUnsafe (&src, unativeint start)
                    let failMask = Vector512.GreaterThan(vec - vLo, vRange).ExtractMostSignificantBits()

                    if failMask <> 0UL then
                        let highestFailBit = 63 - BitOperations.LeadingZeroCount (failMask)
                        pos <- start + highestFailBit + 1
                        stopped <- true
                    else
                        pos <- start
            elif
                Vector256.IsHardwareAccelerated
                && Vector256<uint16>.IsSupported
            then
                let vLo = Vector256.Create (0x0020us)
                let vRange = Vector256.Create (0x005Eus)

                while pos >= 16 && not stopped do
                    let start = pos - 16
                    let vec = Vector256.LoadUnsafe (&src, unativeint start)
                    let failMask = Vector256.GreaterThan(vec - vLo, vRange).ExtractMostSignificantBits()

                    if failMask <> 0u then
                        let highestFailBit = 31 - BitOperations.LeadingZeroCount (failMask)
                        pos <- start + highestFailBit + 1
                        stopped <- true
                    else
                        pos <- start
            elif
                Vector128.IsHardwareAccelerated
                && Vector128<uint16>.IsSupported
            then
                let vLo = Vector128.Create (0x0020us)
                let vRange = Vector128.Create (0x005Eus)

                while pos >= 8 && not stopped do
                    let start = pos - 8
                    let vec = Vector128.LoadUnsafe (&src, unativeint start)
                    let failMask = Vector128.GreaterThan(vec - vLo, vRange).ExtractMostSignificantBits()

                    if failMask <> 0u then
                        let highestFailBit = 31 - BitOperations.LeadingZeroCount (failMask)
                        pos <- start + highestFailBit + 1
                        stopped <- true
                    else
                        pos <- start

            if not stopped then
                while pos > 0
                      && (Unsafe.Add (&src, unativeint (pos - 1)) - 0x0020us)
                         <= 0x005Eus do
                    pos <- pos - 1

            len - pos

    /// Safe count of 1-char UAX #29 clusters ending at span.Length (reserving 1 char if preceded by >= 0x80 Prepend candidate).
    let internal asciiSimpleClusterSuffix (span: ReadOnlySpan<char>) : int =
        let n = asciiPrintableSuffixLength span
        let start = span.Length - n

        if n > 0 && start > 0 && span[start - 1] >= '\u0080' then
            n - 1
        else
            n

    let private isAllAscii (span: ReadOnlySpan<char>) : bool =
        let length = unativeint span.Length

        if length = 0un then
            true
        else
            let u16Span = MemoryMarshal.Cast<char, uint16>(span)
            let src = &MemoryMarshal.GetReference (u16Span)
            let mutable i = 0un
            let mutable ok = true

            if
                Vector512.IsHardwareAccelerated
                && Vector512<uint16>.IsSupported
            then
                let vMask = Vector512.Create (0xFF80us)

                while i + 32un <= length && ok do
                    let vec = Vector512.LoadUnsafe (&src, i)

                    if (vec &&& vMask) <> Vector512<uint16>.Zero then
                        ok <- false
                    else
                        i <- i + 32un
            elif
                Vector256.IsHardwareAccelerated
                && Vector256<uint16>.IsSupported
            then
                let vMask = Vector256.Create (0xFF80us)

                while i + 16un <= length && ok do
                    let vec = Vector256.LoadUnsafe (&src, i)

                    if (vec &&& vMask) <> Vector256<uint16>.Zero then
                        ok <- false
                    else
                        i <- i + 16un
            elif
                Vector128.IsHardwareAccelerated
                && Vector128<uint16>.IsSupported
            then
                let vMask = Vector128.Create (0xFF80us)

                while i + 8un <= length && ok do
                    let vec = Vector128.LoadUnsafe (&src, i)

                    if (vec &&& vMask) <> Vector128<uint16>.Zero then
                        ok <- false
                    else
                        i <- i + 8un

            while i < length && ok do
                if (Unsafe.Add (&src, i) &&& 0xFF80us) <> 0us then
                    ok <- false
                else
                    i <- i + 1un

            ok
#else
    let private isExtendingRune (r: Rune) : bool =
        let cp = r.Value

        if cp < 0x0300 then
            false
        elif
            (cp >= 0xFE00 && cp <= 0xFE0F)
            || (cp >= 0xE0100 && cp <= 0xE01EF)
            || (cp >= 0x1F3FB && cp <= 0x1F3FF)
        then
            true
        else
            match Rune.GetUnicodeCategory r with
            | UnicodeCategory.NonSpacingMark
            | UnicodeCategory.SpacingCombiningMark
            | UnicodeCategory.EnclosingMark -> true
            | _ -> false

    let private isRegionalIndicator (r: Rune) : bool =
        r.Value >= 0x1F1E6 && r.Value <= 0x1F1FF
#endif

    /// <summary>Length in UTF-16 code units of the UAX #29 extended grapheme cluster starting at utf16Index.</summary>
    let nextClusterLength (line: string) (utf16Index: int) : int =
        if
            String.IsNullOrEmpty line
            || utf16Index < 0
            || utf16Index >= line.Length
        then
            0
        else
            let c = line[utf16Index]

            if
                c >= '\u0020'
                && c <= '\u007E'
                && (utf16Index + 1 >= line.Length
                    || line[utf16Index + 1] < '\u0300')
            then
                1
            elif
                c = '\r'
                && utf16Index + 1 < line.Length
                && line[utf16Index + 1] = '\n'
            then
                2
            else
#if FABLE_COMPILER
                let mutable r0 = Rune.ReplacementChar

                let len0 =
                    if Rune.TryGetRuneAt (line, utf16Index, &r0) then
                        r0.Utf16SequenceLength
                    else
                        1

                if r0.Value < 0x20 || r0.Value = 0x7F then
                    1
                else
                    let mutable pos = utf16Index + len0
                    let mutable keepGoing = true

                    if isRegionalIndicator r0 && pos < line.Length then
                        let mutable rNext = Rune.ReplacementChar

                        if
                            Rune.TryGetRuneAt (line, pos, &rNext)
                            && isRegionalIndicator rNext
                        then
                            pos <- pos + rNext.Utf16SequenceLength

                    while keepGoing && pos < line.Length do
                        let mutable rn = Rune.ReplacementChar

                        let lenN =
                            if Rune.TryGetRuneAt (line, pos, &rn) then
                                rn.Utf16SequenceLength
                            else
                                1

                        if isExtendingRune rn then
                            pos <- pos + lenN
                        elif rn.Value = 0x200D then
                            pos <- pos + lenN

                            if pos < line.Length then
                                let mutable rJoined = Rune.ReplacementChar

                                let lenJ =
                                    if Rune.TryGetRuneAt (line, pos, &rJoined) then
                                        rJoined.Utf16SequenceLength
                                    else
                                        1

                                pos <- pos + lenJ
                        else
                            keepGoing <- false

                    max 1 (pos - utf16Index)
#else
                max 1 (StringInfo.GetNextTextElementLength (line, utf16Index))
#endif

    /// <summary>Start UTF-16 index of the extended grapheme cluster immediately preceding utf16Index.</summary>
    let prevClusterStart (line: string) (utf16Index: int) : int =
        if String.IsNullOrEmpty line || utf16Index <= 0 then
            0
        else
            let clamped = if utf16Index > line.Length then line.Length else utf16Index
            let lastChar = line[clamped - 1]

            if
                lastChar >= '\u0020'
                && lastChar <= '\u007E'
                && (clamped = 1 || line[clamped - 2] < '\u0080')
            then
                clamped - 1
            else
                // Scan backward to the nearest guaranteed UAX #29 ASCII cluster boundary rather than 0.
                let mutable anchor = clamped - 1

                while anchor > 0
                      && not (
                          line[anchor] >= '\u0020'
                          && line[anchor] <= '\u007E'
                          && line[anchor - 1] < '\u0080'
                      ) do
                    anchor <- anchor - 1

                let mutable pos = anchor
                let mutable prev = anchor

                while pos < clamped do
                    prev <- pos
                    pos <- pos + nextClusterLength line pos

                prev

#if !FABLE_COMPILER
    /// <summary>Advances utf16Index forward by count extended grapheme clusters.</summary>
    let stepForward (count: int) (line: string) (utf16Index: int) : int =
        if String.IsNullOrEmpty line then
            0
        else
            let mutable idx = clampIndex utf16Index line.Length
            let mutable rem = count

            while rem > 0 && idx < line.Length do
                let safeAscii = asciiSimpleClusterPrefix (line.AsSpan (idx))

                if safeAscii >= rem then
                    idx <- idx + rem
                    rem <- 0
                elif safeAscii > 0 then
                    idx <- idx + safeAscii
                    rem <- rem - safeAscii
                else
                    idx <- idx + nextClusterLength line idx
                    rem <- rem - 1

            idx

    /// <summary>Moves utf16Index backward by count extended grapheme clusters.</summary>
    let stepBackward (count: int) (line: string) (utf16Index: int) : int =
        if String.IsNullOrEmpty line then
            0
        else
            let mutable idx = clampIndex utf16Index line.Length
            let mutable rem = count

            while rem > 0 && idx > 0 do
                let safeAscii = asciiSimpleClusterSuffix (line.AsSpan (0, idx))

                if safeAscii >= rem then
                    idx <- idx - rem
                    rem <- 0
                elif safeAscii > 0 then
                    idx <- idx - safeAscii
                    rem <- rem - safeAscii
                else
                    idx <- prevClusterStart line idx
                    rem <- rem - 1

            idx
#else
    /// <summary>Advances utf16Index forward by count extended grapheme clusters.</summary>
    let stepForward (count: int) (line: string) (utf16Index: int) : int =
        if String.IsNullOrEmpty line then
            0
        else
            let mutable idx = clampIndex utf16Index line.Length
            let mutable rem = count

            while rem > 0 && idx < line.Length do
                idx <- idx + nextClusterLength line idx
                rem <- rem - 1

            idx

    /// <summary>Moves utf16Index backward by count extended grapheme clusters.</summary>
    let stepBackward (count: int) (line: string) (utf16Index: int) : int =
        if String.IsNullOrEmpty line then
            0
        else
            let mutable idx = clampIndex utf16Index line.Length
            let mutable rem = count

            while rem > 0 && idx > 0 do
                idx <- prevClusterStart line idx
                rem <- rem - 1

            idx
#endif

    /// <summary>Decodes the Rune at utf16Index (returning Rune.ReplacementChar on unpaired surrogates or out-of-range indices).</summary>
    let runeAt (line: string) (utf16Index: int) : Rune =
        if
            String.IsNullOrEmpty line
            || utf16Index < 0
            || utf16Index >= line.Length
        then
            Rune.ReplacementChar
        else
            let mutable r = Rune.ReplacementChar

            if Rune.TryGetRuneAt (line, utf16Index, &r) then
                r
            else
                Rune.ReplacementChar

#if !FABLE_COMPILER
    let private fillToggleAscii (dst: Span<char>) (srcStr: string) : unit =
        let length = unativeint srcStr.Length
        let srcU16 = MemoryMarshal.Cast<char, uint16>(srcStr.AsSpan ())
        let dstU16 = MemoryMarshal.Cast<char, uint16>(dst)
        let src = &MemoryMarshal.GetReference (srcU16)
        let out = &MemoryMarshal.GetReference (dstU16)
        let mutable i = 0un

        if
            Vector512.IsHardwareAccelerated
            && Vector512<uint16>.IsSupported
        then
            let vUpperA = Vector512.Create (uint16 'A')
            let vLowerA = Vector512.Create (uint16 'a')
            let v25 = Vector512.Create (25us)
            let vFlip = Vector512.Create (0x0020us)

            while i + 32un <= length do
                let vec = Vector512.LoadUnsafe (&src, i)
                let isU = Vector512.LessThanOrEqual (vec - vUpperA, v25)
                let isL = Vector512.LessThanOrEqual (vec - vLowerA, v25)
                let res = vec ^^^ ((isU ||| isL) &&& vFlip)
                res.StoreUnsafe (&out, i)
                i <- i + 32un
        elif
            Vector256.IsHardwareAccelerated
            && Vector256<uint16>.IsSupported
        then
            let vUpperA = Vector256.Create (uint16 'A')
            let vLowerA = Vector256.Create (uint16 'a')
            let v25 = Vector256.Create (25us)
            let vFlip = Vector256.Create (0x0020us)

            while i + 16un <= length do
                let vec = Vector256.LoadUnsafe (&src, i)
                let isU = Vector256.LessThanOrEqual (vec - vUpperA, v25)
                let isL = Vector256.LessThanOrEqual (vec - vLowerA, v25)
                let res = vec ^^^ ((isU ||| isL) &&& vFlip)
                res.StoreUnsafe (&out, i)
                i <- i + 16un
        elif
            Vector128.IsHardwareAccelerated
            && Vector128<uint16>.IsSupported
        then
            let vUpperA = Vector128.Create (uint16 'A')
            let vLowerA = Vector128.Create (uint16 'a')
            let v25 = Vector128.Create (25us)
            let vFlip = Vector128.Create (0x0020us)

            while i + 8un <= length do
                let vec = Vector128.LoadUnsafe (&src, i)
                let isU = Vector128.LessThanOrEqual (vec - vUpperA, v25)
                let isL = Vector128.LessThanOrEqual (vec - vLowerA, v25)
                let res = vec ^^^ ((isU ||| isL) &&& vFlip)
                res.StoreUnsafe (&out, i)
                i <- i + 8un

        while i < length do
            let c = Unsafe.Add (&src, i)

            let toggled =
                if c - uint16 'A' <= 25us || c - uint16 'a' <= 25us then
                    c ^^^ 0x0020us
                else
                    c

            Unsafe.Add(&out, i) <- toggled
            i <- i + 1un

    let private fillLowerAscii (dst: Span<char>) (srcStr: string) : unit =
        let length = unativeint srcStr.Length
        let srcU16 = MemoryMarshal.Cast<char, uint16>(srcStr.AsSpan ())
        let dstU16 = MemoryMarshal.Cast<char, uint16>(dst)
        let src = &MemoryMarshal.GetReference (srcU16)
        let out = &MemoryMarshal.GetReference (dstU16)
        let mutable i = 0un

        if
            Vector512.IsHardwareAccelerated
            && Vector512<uint16>.IsSupported
        then
            let vUpperA = Vector512.Create (uint16 'A')
            let v25 = Vector512.Create (25us)
            let vBit = Vector512.Create (0x0020us)

            while i + 32un <= length do
                let vec = Vector512.LoadUnsafe (&src, i)
                let isU = Vector512.LessThanOrEqual (vec - vUpperA, v25)
                let res = vec + (isU &&& vBit)
                res.StoreUnsafe (&out, i)
                i <- i + 32un
        elif
            Vector256.IsHardwareAccelerated
            && Vector256<uint16>.IsSupported
        then
            let vUpperA = Vector256.Create (uint16 'A')
            let v25 = Vector256.Create (25us)
            let vBit = Vector256.Create (0x0020us)

            while i + 16un <= length do
                let vec = Vector256.LoadUnsafe (&src, i)
                let isU = Vector256.LessThanOrEqual (vec - vUpperA, v25)
                let res = vec + (isU &&& vBit)
                res.StoreUnsafe (&out, i)
                i <- i + 16un
        elif
            Vector128.IsHardwareAccelerated
            && Vector128<uint16>.IsSupported
        then
            let vUpperA = Vector128.Create (uint16 'A')
            let v25 = Vector128.Create (25us)
            let vBit = Vector128.Create (0x0020us)

            while i + 8un <= length do
                let vec = Vector128.LoadUnsafe (&src, i)
                let isU = Vector128.LessThanOrEqual (vec - vUpperA, v25)
                let res = vec + (isU &&& vBit)
                res.StoreUnsafe (&out, i)
                i <- i + 8un

        while i < length do
            let c = Unsafe.Add (&src, i)
            let lo = if c - uint16 'A' <= 25us then c + 0x0020us else c
            Unsafe.Add(&out, i) <- lo
            i <- i + 1un

    let private fillUpperAscii (dst: Span<char>) (srcStr: string) : unit =
        let length = unativeint srcStr.Length
        let srcU16 = MemoryMarshal.Cast<char, uint16>(srcStr.AsSpan ())
        let dstU16 = MemoryMarshal.Cast<char, uint16>(dst)
        let src = &MemoryMarshal.GetReference (srcU16)
        let out = &MemoryMarshal.GetReference (dstU16)
        let mutable i = 0un

        if
            Vector512.IsHardwareAccelerated
            && Vector512<uint16>.IsSupported
        then
            let vLowerA = Vector512.Create (uint16 'a')
            let v25 = Vector512.Create (25us)
            let vBit = Vector512.Create (0x0020us)

            while i + 32un <= length do
                let vec = Vector512.LoadUnsafe (&src, i)
                let isL = Vector512.LessThanOrEqual (vec - vLowerA, v25)
                let res = vec - (isL &&& vBit)
                res.StoreUnsafe (&out, i)
                i <- i + 32un
        elif
            Vector256.IsHardwareAccelerated
            && Vector256<uint16>.IsSupported
        then
            let vLowerA = Vector256.Create (uint16 'a')
            let v25 = Vector256.Create (25us)
            let vBit = Vector256.Create (0x0020us)

            while i + 16un <= length do
                let vec = Vector256.LoadUnsafe (&src, i)
                let isL = Vector256.LessThanOrEqual (vec - vLowerA, v25)
                let res = vec - (isL &&& vBit)
                res.StoreUnsafe (&out, i)
                i <- i + 16un
        elif
            Vector128.IsHardwareAccelerated
            && Vector128<uint16>.IsSupported
        then
            let vLowerA = Vector128.Create (uint16 'a')
            let v25 = Vector128.Create (25us)
            let vBit = Vector128.Create (0x0020us)

            while i + 8un <= length do
                let vec = Vector128.LoadUnsafe (&src, i)
                let isL = Vector128.LessThanOrEqual (vec - vLowerA, v25)
                let res = vec - (isL &&& vBit)
                res.StoreUnsafe (&out, i)
                i <- i + 8un

        while i < length do
            let c = Unsafe.Add (&src, i)
            let up = if c - uint16 'a' <= 25us then c - 0x0020us else c
            Unsafe.Add(&out, i) <- up
            i <- i + 1un

    /// <summary>Toggles case of every Unicode scalar in text.</summary>
    let toggleCase (text: string) : string =
        if String.IsNullOrEmpty text then
            text
        elif isAllAscii (text.AsSpan ()) then
            String.Create (text.Length, text, SpanAction<char, string>(fillToggleAscii))
        else
            let sb = StringBuilder (text.Length)

            for r in text.EnumerateRunes () do
                let toggled =
                    if Rune.IsUpper r then Rune.ToLowerInvariant r
                    elif Rune.IsLower r then Rune.ToUpperInvariant r
                    else r

                sb.Append (toggled.ToString ()) |> ignore

            sb.ToString ()

    /// <summary>Converts every Unicode scalar in text to lowercase.</summary>
    let toLower (text: string) : string =
        if String.IsNullOrEmpty text then
            text
        elif isAllAscii (text.AsSpan ()) then
            String.Create (text.Length, text, SpanAction<char, string>(fillLowerAscii))
        else
            let sb = StringBuilder (text.Length)

            for r in text.EnumerateRunes () do
                sb.Append ((Rune.ToLowerInvariant r).ToString())
                |> ignore

            sb.ToString ()

    /// <summary>Converts every Unicode scalar in text to uppercase.</summary>
    let toUpper (text: string) : string =
        if String.IsNullOrEmpty text then
            text
        elif isAllAscii (text.AsSpan ()) then
            String.Create (text.Length, text, SpanAction<char, string>(fillUpperAscii))
        else
            let sb = StringBuilder (text.Length)

            for r in text.EnumerateRunes () do
                sb.Append ((Rune.ToUpperInvariant r).ToString())
                |> ignore

            sb.ToString ()
#else
    /// <summary>Toggles case of every Unicode scalar in text.</summary>
    let toggleCase (text: string) : string =
        if String.IsNullOrEmpty text then
            text
        else
            let parts = ResizeArray<string>(text.Length)
            let mutable idx = 0

            while idx < text.Length do
                let mutable r = Rune.ReplacementChar

                if Rune.TryGetRuneAt (text, idx, &r) then
                    let toggled =
                        if Rune.IsUpper r then Rune.ToLowerInvariant r
                        elif Rune.IsLower r then Rune.ToUpperInvariant r
                        else r

                    parts.Add (toggled.ToString ())
                    idx <- idx + r.Utf16SequenceLength
                else
                    parts.Add (string text[idx])
                    idx <- idx + 1

            String.concat "" parts

    /// <summary>Converts every Unicode scalar in text to lowercase.</summary>
    let toLower (text: string) : string =
        if String.IsNullOrEmpty text then
            text
        else
            let parts = ResizeArray<string>(text.Length)
            let mutable idx = 0

            while idx < text.Length do
                let mutable r = Rune.ReplacementChar

                if Rune.TryGetRuneAt (text, idx, &r) then
                    parts.Add ((Rune.ToLowerInvariant r).ToString())
                    idx <- idx + r.Utf16SequenceLength
                else
                    parts.Add (string text[idx])
                    idx <- idx + 1

            String.concat "" parts

    /// <summary>Converts every Unicode scalar in text to uppercase.</summary>
    let toUpper (text: string) : string =
        if String.IsNullOrEmpty text then
            text
        else
            let parts = ResizeArray<string>(text.Length)
            let mutable idx = 0

            while idx < text.Length do
                let mutable r = Rune.ReplacementChar

                if Rune.TryGetRuneAt (text, idx, &r) then
                    parts.Add ((Rune.ToUpperInvariant r).ToString())
                    idx <- idx + r.Utf16SequenceLength
                else
                    parts.Add (string text[idx])
                    idx <- idx + 1

            String.concat "" parts
#endif
