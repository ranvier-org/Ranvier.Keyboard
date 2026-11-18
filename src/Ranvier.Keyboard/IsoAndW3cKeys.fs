namespace Ranvier.Keyboard

open System
#if !FABLE_COMPILER
open System.Buffers.Binary
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Intrinsics
#endif

/// <summary>ISO/IEC 9995-1 keyboard functional sections.</summary>
[<Struct>]
type IsoSection =
    | Alphanumeric
    | EditingAndNavigation
    | NumericKeypad
    | FunctionSection
    | Auxiliary

/// <summary>ISO/IEC 9995-1 physical row designators (Row A bottom modifier row through Row E digit row, Row K function row).</summary>
[<Struct>]
type IsoRow =
    | RowA
    | RowB
    | RowC
    | RowD
    | RowE
    | RowK

/// <summary>ISO/IEC 9995-1 physical grid coordinate (for example Alphanumeric RowB Column 0 = B00 ISO IntlBackslash key).</summary>
[<Struct>]
type Iso9995Coordinate =
    {
        Section: IsoSection
        Row: IsoRow
        Column: byte
    }

/// <summary>W3C UI Events KeyboardEvent.code physical scan positions aligned with USB HID Usage Page 0x07 and ISO/IEC 9995.</summary>
#if FABLE_COMPILER
[<RequireQualifiedAccess; Fable.Core.StringEnum(Fable.Core.CaseRules.None)>]
#else
[<RequireQualifiedAccess; Struct>]
#endif
type PhysicalKeyCode =
    // Alphanumeric Section — Row E (ISO E00..E14)
    | Backquote
    | Digit1
    | Digit2
    | Digit3
    | Digit4
    | Digit5
    | Digit6
    | Digit7
    | Digit8
    | Digit9
    | Digit0
    | Minus
    | Equal
    | IntlYen
    | Backspace
    // Alphanumeric Section — Row D (ISO D00..D13)
    | Tab
    | KeyQ
    | KeyW
    | KeyE
    | KeyR
    | KeyT
    | KeyY
    | KeyU
    | KeyI
    | KeyO
    | KeyP
    | BracketLeft
    | BracketRight
    | Backslash
    // Alphanumeric Section — Row C (ISO C00..C13)
    | CapsLock
    | KeyA
    | KeyS
    | KeyD
    | KeyF
    | KeyG
    | KeyH
    | KeyJ
    | KeyK
    | KeyL
    | Semicolon
    | Quote
    | IntlHash
    | Enter
    // Alphanumeric Section — Row B (ISO B00..B12)
    | ShiftLeft
    | IntlBackslash
    | KeyZ
    | KeyX
    | KeyC
    | KeyV
    | KeyB
    | KeyN
    | KeyM
    | Comma
    | Period
    | Slash
    | IntlRo
    | ShiftRight
    // Alphanumeric Section — Row A & IME (ISO A99..A12, JIS X 6002, KS X 5002)
    | ControlLeft
    | MetaLeft
    | AltLeft
    | Space
    | AltRight
    | MetaRight
    | ContextMenu
    | ControlRight
    | Convert
    | NonConvert
    | KanaMode
    | Lang1
    | Lang2
    | Fn
    // Function Section — Row K (ISO/IEC 9995-6)
    | Escape
    | F1
    | F2
    | F3
    | F4
    | F5
    | F6
    | F7
    | F8
    | F9
    | F10
    | F11
    | F12
    | F13
    | F14
    | F15
    | F16
    | F17
    | F18
    | F19
    | F20
    | F21
    | F22
    | F23
    | F24
    // Control & Arrow Pads (ISO/IEC 9995-5)
    | PrintScreen
    | ScrollLock
    | Pause
    | Insert
    | Home
    | PageUp
    | Delete
    | End
    | PageDown
    | ArrowUp
    | ArrowDown
    | ArrowLeft
    | ArrowRight
    // Numeric Keypad (ISO/IEC 9995-4)
    | NumLock
    | NumpadDivide
    | NumpadMultiply
    | NumpadSubtract
    | NumpadAdd
    | NumpadEnter
    | NumpadDecimal
    | NumpadComma
    | NumpadEqual
    | Numpad0
    | Numpad1
    | Numpad2
    | Numpad3
    | Numpad4
    | Numpad5
    | Numpad6
    | Numpad7
    | Numpad8
    | Numpad9
#if FABLE_COMPILER
    | [<CompiledName("Unidentified")>] UnidentifiedCode
#else
    | UnidentifiedCode
#endif

#if !FABLE_COMPILER
module internal SimdKeyTable =
    /// Safely packs 2..15 UTF-16 chars into a 16-byte zero-padded Vector128<byte> if all chars are ASCII (< 0x80).
    /// Never reads before index 0 or past span.Length - 1.
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let tryPackAsciiChars16 (span: ReadOnlySpan<char>) (packed: outref<Vector128<byte>>) : bool =
        let len = span.Length

        if
            len < 2
            || len > 15
            || not BitConverter.IsLittleEndian
        then
            packed <- Vector128<byte>.Zero
            false
        elif len <= 3 then
            let c0 = uint16 span[0]
            let c1 = uint16 span[1]
            let c2 = if len = 3 then uint16 span[2] else 0us

            if ((c0 ||| c1 ||| c2) &&& 0xFF80us) <> 0us then
                packed <- Vector128<byte>.Zero
                false
            else
                let lo =
                    uint64 (byte c0)
                    ||| (uint64 (byte c1) <<< 8)
                    ||| (uint64 (byte c2) <<< 16)

                packed <- Vector128.Create(lo, 0UL).AsByte()
                true
        elif len <= 7 then
            let bytes = MemoryMarshal.AsBytes (span)
            let lo64 = BinaryPrimitives.ReadUInt64LittleEndian (bytes)

            let hi64 =
                if len = 4 then
                    0UL
                else
                    let tail64 = BinaryPrimitives.ReadUInt64LittleEndian (bytes.Slice ((len - 4) * 2))
                    tail64 >>> ((8 - len) * 16)

            let vLo = Vector128.Create(lo64, hi64).AsUInt16()

            if
                (vLo &&& Vector128.Create (0xFF80us))
                <> Vector128<uint16>.Zero
            then
                packed <- Vector128<byte>.Zero
                false
            else
                packed <- Vector128.Narrow (vLo, Vector128<uint16>.Zero)
                true
        else
            let u16Span = MemoryMarshal.Cast<char, uint16>(span)
            let vLo = Vector128.LoadUnsafe (&MemoryMarshal.GetReference (u16Span))
            let bytes = MemoryMarshal.AsBytes (span)

            let vHi =
                if len = 8 then
                    Vector128<uint16>.Zero
                elif len <= 11 then
                    let tail64 = BinaryPrimitives.ReadUInt64LittleEndian (bytes.Slice ((len - 4) * 2))
                    let lo64 = tail64 >>> ((12 - len) * 16)
                    Vector128.Create(lo64, 0UL).AsUInt16()
                elif len = 12 then
                    let lo64 = BinaryPrimitives.ReadUInt64LittleEndian (bytes.Slice (16))
                    Vector128.Create(lo64, 0UL).AsUInt16()
                else
                    let lo64 = BinaryPrimitives.ReadUInt64LittleEndian (bytes.Slice (16))
                    let tail64 = BinaryPrimitives.ReadUInt64LittleEndian (bytes.Slice ((len - 4) * 2))
                    let hi64 = tail64 >>> ((16 - len) * 16)
                    Vector128.Create(lo64, hi64).AsUInt16()

            if
                ((vLo ||| vHi) &&& Vector128.Create (0xFF80us))
                <> Vector128<uint16>.Zero
            then
                packed <- Vector128<byte>.Zero
                false
            else
                packed <- Vector128.Narrow (vLo, vHi)
                true

    /// Safely packs 2..15 UTF-8 bytes into a 16-byte zero-padded Vector128<byte> if all bytes are ASCII (< 0x80).
    /// Never reads before index 0 or past utf8.Length - 1.
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let tryPackAsciiBytes16 (utf8: ReadOnlySpan<byte>) (packed: outref<Vector128<byte>>) : bool =
        let len = utf8.Length

        if
            len < 2
            || len > 15
            || not BitConverter.IsLittleEndian
        then
            packed <- Vector128<byte>.Zero
            false
        else
            let lo, hi =
                if len <= 3 then
                    let b01 = uint64 (BinaryPrimitives.ReadUInt16LittleEndian (utf8))
                    let v = if len = 2 then b01 else b01 ||| (uint64 utf8[2] <<< 16)
                    v, 0UL
                elif len <= 7 then
                    let lo32 = uint64 (BinaryPrimitives.ReadUInt32LittleEndian (utf8))

                    if len = 4 then
                        lo32, 0UL
                    else
                        let tail32 = uint64 (BinaryPrimitives.ReadUInt32LittleEndian (utf8.Slice (len - 4)))
                        let hiPart = (tail32 >>> ((8 - len) * 8)) <<< 32
                        (lo32 ||| hiPart), 0UL
                elif len = 8 then
                    BinaryPrimitives.ReadUInt64LittleEndian (utf8), 0UL
                else
                    let lo64 = BinaryPrimitives.ReadUInt64LittleEndian (utf8)
                    let tail64 = BinaryPrimitives.ReadUInt64LittleEndian (utf8.Slice (len - 8))
                    let hi64 = tail64 >>> ((16 - len) * 8)
                    lo64, hi64

            let vec = Vector128.Create(lo, hi).AsByte()

            if vec.ExtractMostSignificantBits () <> 0u then
                packed <- Vector128<byte>.Zero
                false
            else
                packed <- vec
                true

    type FlatIndex =
        {
            Counts: byte array
            SigFirst: byte array
            SigMid: byte array
            SigLast: byte array
            FullKeys: byte array
        }

    type Table<'T> = { Index: FlatIndex; Values: 'T array }

    let build (entries: (string * 'T) array) (fallbackValue: 'T) : Table<'T> =
        let counts = Array.zeroCreate<byte> 16
        let sigFirst = Array.create 1024 0xFFuy
        let sigMid = Array.zeroCreate<byte> 1024
        let sigLast = Array.zeroCreate<byte> 1024
        let fullKeys = Array.zeroCreate<byte>(1024 * 16)
        let values = Array.create 1024 fallbackValue

        for idx = 0 to entries.Length - 1 do
            let key, value = entries[idx]
            let n = key.Length
            let localSlot = int counts[n]
            counts[n] <- byte (localSlot + 1)
            let globalSlot = (n <<< 6) + localSlot
            sigFirst[globalSlot] <- byte key[0]
            sigMid[globalSlot] <- byte key[n >>> 1]
            sigLast[globalSlot] <- byte key[n - 1]
            values[globalSlot] <- value
            let baseOff = globalSlot * 16

            for k = 0 to n - 1 do
                fullKeys[baseOff + k] <- byte key[k]

        {
            Index =
                {
                    Counts = counts
                    SigFirst = sigFirst
                    SigMid = sigMid
                    SigLast = sigLast
                    FullKeys = fullKeys
                }
            Values = values
        }

    /// Non-generic three-tier Vector512 / Vector256 / Vector128 SIMD lookup returning global slot in 0..1023 or -1.
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let lookupSlot (index: FlatIndex) (len: int) (first: byte) (mid: byte) (last: byte) (keyVec: Vector128<byte>) : int =
        let count = int index.Counts[len]

        if count = 0 then
            -1
        else
            let baseSlot = len <<< 6
            let sigFirstRef = &MemoryMarshal.GetArrayDataReference (index.SigFirst)
            let sigMidRef = &MemoryMarshal.GetArrayDataReference (index.SigMid)
            let sigLastRef = &MemoryMarshal.GetArrayDataReference (index.SigLast)
            let fullKeysRef = &MemoryMarshal.GetArrayDataReference (index.FullKeys)
            let mutable found = -1

            if
                Vector512.IsHardwareAccelerated
                && Vector512<byte>.IsSupported
            then
                let bOff = unativeint baseSlot

                let m =
                    Vector512.Equals (Vector512.LoadUnsafe (&sigFirstRef, bOff), Vector512.Create (first))
                    &&& Vector512.Equals (Vector512.LoadUnsafe (&sigMidRef, bOff), Vector512.Create (mid))
                    &&& Vector512.Equals (Vector512.LoadUnsafe (&sigLastRef, bOff), Vector512.Create (last))

                let mutable mask = m.ExtractMostSignificantBits ()

                while mask <> 0UL && found < 0 do
                    let bit = BitOperations.TrailingZeroCount (mask)
                    let slot = baseSlot + bit

                    if Vector128.LoadUnsafe (&fullKeysRef, unativeint (slot * 16)) = keyVec then
                        found <- slot

                    mask <- mask &&& (mask - 1UL)
            elif
                Vector256.IsHardwareAccelerated
                && Vector256<byte>.IsSupported
            then
                let vFirst = Vector256.Create (first)
                let vMid = Vector256.Create (mid)
                let vLast = Vector256.Create (last)
                let mutable localOff = 0

                while localOff < count && found < 0 do
                    let bOff = unativeint (baseSlot + localOff)

                    let m =
                        Vector256.Equals (Vector256.LoadUnsafe (&sigFirstRef, bOff), vFirst)
                        &&& Vector256.Equals (Vector256.LoadUnsafe (&sigMidRef, bOff), vMid)
                        &&& Vector256.Equals (Vector256.LoadUnsafe (&sigLastRef, bOff), vLast)

                    let mutable mask = m.ExtractMostSignificantBits ()

                    while mask <> 0u && found < 0 do
                        let bit = BitOperations.TrailingZeroCount (mask)
                        let slot = baseSlot + localOff + bit

                        if Vector128.LoadUnsafe (&fullKeysRef, unativeint (slot * 16)) = keyVec then
                            found <- slot

                        mask <- mask &&& (mask - 1u)

                    localOff <- localOff + 32
            elif
                Vector128.IsHardwareAccelerated
                && Vector128<byte>.IsSupported
            then
                let vFirst = Vector128.Create (first)
                let vMid = Vector128.Create (mid)
                let vLast = Vector128.Create (last)
                let mutable localOff = 0

                while localOff < count && found < 0 do
                    let bOff = unativeint (baseSlot + localOff)

                    let m =
                        Vector128.Equals (Vector128.LoadUnsafe (&sigFirstRef, bOff), vFirst)
                        &&& Vector128.Equals (Vector128.LoadUnsafe (&sigMidRef, bOff), vMid)
                        &&& Vector128.Equals (Vector128.LoadUnsafe (&sigLastRef, bOff), vLast)

                    let mutable mask = m.ExtractMostSignificantBits ()

                    while mask <> 0u && found < 0 do
                        let bit = BitOperations.TrailingZeroCount (mask)
                        let slot = baseSlot + localOff + bit

                        if Vector128.LoadUnsafe (&fullKeysRef, unativeint (slot * 16)) = keyVec then
                            found <- slot

                        mask <- mask &&& (mask - 1u)

                    localOff <- localOff + 16

            found
#endif

/// <summary>Conversions for W3C KeyboardEvent.code, USB HID Usage Page 0x07, and ISO/IEC 9995-1 coordinates.</summary>
[<RequireQualifiedAccess>]
module PhysicalKeyCode =

    type private P = PhysicalKeyCode

#if FABLE_COMPILER
    /// <summary>Parses a W3C UI Events KeyboardEvent.code string into a PhysicalKeyCode.</summary>
    let ofW3cCode (code: string) : PhysicalKeyCode =
        if String.IsNullOrEmpty code then
            P.UnidentifiedCode
        else
            match code with
            | "Backquote" -> P.Backquote
            | "Digit1" -> P.Digit1
            | "Digit2" -> P.Digit2
            | "Digit3" -> P.Digit3
            | "Digit4" -> P.Digit4
            | "Digit5" -> P.Digit5
            | "Digit6" -> P.Digit6
            | "Digit7" -> P.Digit7
            | "Digit8" -> P.Digit8
            | "Digit9" -> P.Digit9
            | "Digit0" -> P.Digit0
            | "Minus" -> P.Minus
            | "Equal" -> P.Equal
            | "IntlYen" -> P.IntlYen
            | "Backspace" -> P.Backspace
            | "Tab" -> P.Tab
            | "KeyQ" -> P.KeyQ
            | "KeyW" -> P.KeyW
            | "KeyE" -> P.KeyE
            | "KeyR" -> P.KeyR
            | "KeyT" -> P.KeyT
            | "KeyY" -> P.KeyY
            | "KeyU" -> P.KeyU
            | "KeyI" -> P.KeyI
            | "KeyO" -> P.KeyO
            | "KeyP" -> P.KeyP
            | "BracketLeft" -> P.BracketLeft
            | "BracketRight" -> P.BracketRight
            | "Backslash" -> P.Backslash
            | "CapsLock" -> P.CapsLock
            | "KeyA" -> P.KeyA
            | "KeyS" -> P.KeyS
            | "KeyD" -> P.KeyD
            | "KeyF" -> P.KeyF
            | "KeyG" -> P.KeyG
            | "KeyH" -> P.KeyH
            | "KeyJ" -> P.KeyJ
            | "KeyK" -> P.KeyK
            | "KeyL" -> P.KeyL
            | "Semicolon" -> P.Semicolon
            | "Quote" -> P.Quote
            | "IntlHash" -> P.IntlHash
            | "Enter" -> P.Enter
            | "ShiftLeft" -> P.ShiftLeft
            | "IntlBackslash" -> P.IntlBackslash
            | "KeyZ" -> P.KeyZ
            | "KeyX" -> P.KeyX
            | "KeyC" -> P.KeyC
            | "KeyV" -> P.KeyV
            | "KeyB" -> P.KeyB
            | "KeyN" -> P.KeyN
            | "KeyM" -> P.KeyM
            | "Comma" -> P.Comma
            | "Period" -> P.Period
            | "Slash" -> P.Slash
            | "IntlRo" -> P.IntlRo
            | "ShiftRight" -> P.ShiftRight
            | "ControlLeft" -> P.ControlLeft
            | "MetaLeft"
            | "OSLeft" -> P.MetaLeft
            | "AltLeft" -> P.AltLeft
            | "Space" -> P.Space
            | "AltRight" -> P.AltRight
            | "MetaRight"
            | "OSRight" -> P.MetaRight
            | "ContextMenu" -> P.ContextMenu
            | "ControlRight" -> P.ControlRight
            | "Convert" -> P.Convert
            | "NonConvert" -> P.NonConvert
            | "KanaMode" -> P.KanaMode
            | "Lang1" -> P.Lang1
            | "Lang2" -> P.Lang2
            | "Fn" -> P.Fn
            | "Escape" -> P.Escape
            | "F1" -> P.F1
            | "F2" -> P.F2
            | "F3" -> P.F3
            | "F4" -> P.F4
            | "F5" -> P.F5
            | "F6" -> P.F6
            | "F7" -> P.F7
            | "F8" -> P.F8
            | "F9" -> P.F9
            | "F10" -> P.F10
            | "F11" -> P.F11
            | "F12" -> P.F12
            | "F13" -> P.F13
            | "F14" -> P.F14
            | "F15" -> P.F15
            | "F16" -> P.F16
            | "F17" -> P.F17
            | "F18" -> P.F18
            | "F19" -> P.F19
            | "F20" -> P.F20
            | "F21" -> P.F21
            | "F22" -> P.F22
            | "F23" -> P.F23
            | "F24" -> P.F24
            | "PrintScreen" -> P.PrintScreen
            | "ScrollLock" -> P.ScrollLock
            | "Pause" -> P.Pause
            | "Insert" -> P.Insert
            | "Home" -> P.Home
            | "PageUp" -> P.PageUp
            | "Delete" -> P.Delete
            | "End" -> P.End
            | "PageDown" -> P.PageDown
            | "ArrowUp" -> P.ArrowUp
            | "ArrowDown" -> P.ArrowDown
            | "ArrowLeft" -> P.ArrowLeft
            | "ArrowRight" -> P.ArrowRight
            | "NumLock" -> P.NumLock
            | "NumpadDivide" -> P.NumpadDivide
            | "NumpadMultiply" -> P.NumpadMultiply
            | "NumpadSubtract" -> P.NumpadSubtract
            | "NumpadAdd" -> P.NumpadAdd
            | "NumpadEnter" -> P.NumpadEnter
            | "NumpadDecimal" -> P.NumpadDecimal
            | "NumpadComma" -> P.NumpadComma
            | "NumpadEqual" -> P.NumpadEqual
            | "Numpad0" -> P.Numpad0
            | "Numpad1" -> P.Numpad1
            | "Numpad2" -> P.Numpad2
            | "Numpad3" -> P.Numpad3
            | "Numpad4" -> P.Numpad4
            | "Numpad5" -> P.Numpad5
            | "Numpad6" -> P.Numpad6
            | "Numpad7" -> P.Numpad7
            | "Numpad8" -> P.Numpad8
            | "Numpad9" -> P.Numpad9
            | _ -> P.UnidentifiedCode

    /// <summary>Formats a PhysicalKeyCode back to its canonical W3C KeyboardEvent.code string.</summary>
    let inline toW3cCode (code: PhysicalKeyCode) : string =
        unbox<string> code
#else
    let private w3cEntries: (string * PhysicalKeyCode) array =
        [|
            "Backquote", P.Backquote
            "Digit1", P.Digit1
            "Digit2", P.Digit2
            "Digit3", P.Digit3
            "Digit4", P.Digit4
            "Digit5", P.Digit5
            "Digit6", P.Digit6
            "Digit7", P.Digit7
            "Digit8", P.Digit8
            "Digit9", P.Digit9
            "Digit0", P.Digit0
            "Minus", P.Minus
            "Equal", P.Equal
            "IntlYen", P.IntlYen
            "Backspace", P.Backspace
            "Tab", P.Tab
            "KeyQ", P.KeyQ
            "KeyW", P.KeyW
            "KeyE", P.KeyE
            "KeyR", P.KeyR
            "KeyT", P.KeyT
            "KeyY", P.KeyY
            "KeyU", P.KeyU
            "KeyI", P.KeyI
            "KeyO", P.KeyO
            "KeyP", P.KeyP
            "BracketLeft", P.BracketLeft
            "BracketRight", P.BracketRight
            "Backslash", P.Backslash
            "CapsLock", P.CapsLock
            "KeyA", P.KeyA
            "KeyS", P.KeyS
            "KeyD", P.KeyD
            "KeyF", P.KeyF
            "KeyG", P.KeyG
            "KeyH", P.KeyH
            "KeyJ", P.KeyJ
            "KeyK", P.KeyK
            "KeyL", P.KeyL
            "Semicolon", P.Semicolon
            "Quote", P.Quote
            "IntlHash", P.IntlHash
            "Enter", P.Enter
            "ShiftLeft", P.ShiftLeft
            "IntlBackslash", P.IntlBackslash
            "KeyZ", P.KeyZ
            "KeyX", P.KeyX
            "KeyC", P.KeyC
            "KeyV", P.KeyV
            "KeyB", P.KeyB
            "KeyN", P.KeyN
            "KeyM", P.KeyM
            "Comma", P.Comma
            "Period", P.Period
            "Slash", P.Slash
            "IntlRo", P.IntlRo
            "ShiftRight", P.ShiftRight
            "ControlLeft", P.ControlLeft
            "MetaLeft", P.MetaLeft
            "OSLeft", P.MetaLeft
            "AltLeft", P.AltLeft
            "Space", P.Space
            "AltRight", P.AltRight
            "MetaRight", P.MetaRight
            "OSRight", P.MetaRight
            "ContextMenu", P.ContextMenu
            "ControlRight", P.ControlRight
            "Convert", P.Convert
            "NonConvert", P.NonConvert
            "KanaMode", P.KanaMode
            "Lang1", P.Lang1
            "Lang2", P.Lang2
            "Fn", P.Fn
            "Escape", P.Escape
            "F1", P.F1
            "F2", P.F2
            "F3", P.F3
            "F4", P.F4
            "F5", P.F5
            "F6", P.F6
            "F7", P.F7
            "F8", P.F8
            "F9", P.F9
            "F10", P.F10
            "F11", P.F11
            "F12", P.F12
            "F13", P.F13
            "F14", P.F14
            "F15", P.F15
            "F16", P.F16
            "F17", P.F17
            "F18", P.F18
            "F19", P.F19
            "F20", P.F20
            "F21", P.F21
            "F22", P.F22
            "F23", P.F23
            "F24", P.F24
            "PrintScreen", P.PrintScreen
            "ScrollLock", P.ScrollLock
            "Pause", P.Pause
            "Insert", P.Insert
            "Home", P.Home
            "PageUp", P.PageUp
            "Delete", P.Delete
            "End", P.End
            "PageDown", P.PageDown
            "ArrowUp", P.ArrowUp
            "ArrowDown", P.ArrowDown
            "ArrowLeft", P.ArrowLeft
            "ArrowRight", P.ArrowRight
            "NumLock", P.NumLock
            "NumpadDivide", P.NumpadDivide
            "NumpadMultiply", P.NumpadMultiply
            "NumpadSubtract", P.NumpadSubtract
            "NumpadAdd", P.NumpadAdd
            "NumpadEnter", P.NumpadEnter
            "NumpadDecimal", P.NumpadDecimal
            "NumpadComma", P.NumpadComma
            "NumpadEqual", P.NumpadEqual
            "Numpad0", P.Numpad0
            "Numpad1", P.Numpad1
            "Numpad2", P.Numpad2
            "Numpad3", P.Numpad3
            "Numpad4", P.Numpad4
            "Numpad5", P.Numpad5
            "Numpad6", P.Numpad6
            "Numpad7", P.Numpad7
            "Numpad8", P.Numpad8
            "Numpad9", P.Numpad9
        |]

    let private simdTable = SimdKeyTable.build w3cEntries P.UnidentifiedCode

    /// <summary>Scalar fallback and parity oracle for parsing W3C KeyboardEvent.code spans.</summary>
    let ofW3cCodeScalar (code: ReadOnlySpan<char>) : PhysicalKeyCode =
        let len = code.Length

        if len < 2 || len > 14 then
            P.UnidentifiedCode
        else
            match len with
            | 2 ->
                if code[0] <> 'F' then
                    P.UnidentifiedCode
                else
                    match code[1] with
                    | 'n' -> P.Fn
                    | '1' -> P.F1
                    | '2' -> P.F2
                    | '3' -> P.F3
                    | '4' -> P.F4
                    | '5' -> P.F5
                    | '6' -> P.F6
                    | '7' -> P.F7
                    | '8' -> P.F8
                    | '9' -> P.F9
                    | _ -> P.UnidentifiedCode
            | 3 ->
                if code.SequenceEqual ("Tab".AsSpan()) then
                    P.Tab
                elif code.SequenceEqual ("End".AsSpan()) then
                    P.End
                elif code[0] = 'F' && code[1] = '1' then
                    match code[2] with
                    | '0' -> P.F10
                    | '1' -> P.F11
                    | '2' -> P.F12
                    | '3' -> P.F13
                    | '4' -> P.F14
                    | '5' -> P.F15
                    | '6' -> P.F16
                    | '7' -> P.F17
                    | '8' -> P.F18
                    | '9' -> P.F19
                    | _ -> P.UnidentifiedCode
                elif code[0] = 'F' && code[1] = '2' then
                    match code[2] with
                    | '0' -> P.F20
                    | '1' -> P.F21
                    | '2' -> P.F22
                    | '3' -> P.F23
                    | '4' -> P.F24
                    | _ -> P.UnidentifiedCode
                else
                    P.UnidentifiedCode
            | 4 ->
                if code[0] = 'K' && code[1] = 'e' && code[2] = 'y' then
                    match code[3] with
                    | 'A' -> P.KeyA
                    | 'B' -> P.KeyB
                    | 'C' -> P.KeyC
                    | 'D' -> P.KeyD
                    | 'E' -> P.KeyE
                    | 'F' -> P.KeyF
                    | 'G' -> P.KeyG
                    | 'H' -> P.KeyH
                    | 'I' -> P.KeyI
                    | 'J' -> P.KeyJ
                    | 'K' -> P.KeyK
                    | 'L' -> P.KeyL
                    | 'M' -> P.KeyM
                    | 'N' -> P.KeyN
                    | 'O' -> P.KeyO
                    | 'P' -> P.KeyP
                    | 'Q' -> P.KeyQ
                    | 'R' -> P.KeyR
                    | 'S' -> P.KeyS
                    | 'T' -> P.KeyT
                    | 'U' -> P.KeyU
                    | 'V' -> P.KeyV
                    | 'W' -> P.KeyW
                    | 'X' -> P.KeyX
                    | 'Y' -> P.KeyY
                    | 'Z' -> P.KeyZ
                    | _ -> P.UnidentifiedCode
                elif code.SequenceEqual ("Home".AsSpan()) then
                    P.Home
                else
                    P.UnidentifiedCode
            | 5 ->
                if code.SequenceEqual ("Minus".AsSpan()) then P.Minus
                elif code.SequenceEqual ("Equal".AsSpan()) then P.Equal
                elif code.SequenceEqual ("Quote".AsSpan()) then P.Quote
                elif code.SequenceEqual ("Enter".AsSpan()) then P.Enter
                elif code.SequenceEqual ("Comma".AsSpan()) then P.Comma
                elif code.SequenceEqual ("Slash".AsSpan()) then P.Slash
                elif code.SequenceEqual ("Space".AsSpan()) then P.Space
                elif code.SequenceEqual ("Lang1".AsSpan()) then P.Lang1
                elif code.SequenceEqual ("Lang2".AsSpan()) then P.Lang2
                elif code.SequenceEqual ("Pause".AsSpan()) then P.Pause
                else P.UnidentifiedCode
            | 6 ->
                if code.StartsWith ("Digit".AsSpan()) then
                    match code[5] with
                    | '0' -> P.Digit0
                    | '1' -> P.Digit1
                    | '2' -> P.Digit2
                    | '3' -> P.Digit3
                    | '4' -> P.Digit4
                    | '5' -> P.Digit5
                    | '6' -> P.Digit6
                    | '7' -> P.Digit7
                    | '8' -> P.Digit8
                    | '9' -> P.Digit9
                    | _ -> P.UnidentifiedCode
                elif code.SequenceEqual ("Period".AsSpan()) then
                    P.Period
                elif code.SequenceEqual ("IntlRo".AsSpan()) then
                    P.IntlRo
                elif code.SequenceEqual ("OSLeft".AsSpan()) then
                    P.MetaLeft
                elif code.SequenceEqual ("Escape".AsSpan()) then
                    P.Escape
                elif code.SequenceEqual ("Insert".AsSpan()) then
                    P.Insert
                elif code.SequenceEqual ("PageUp".AsSpan()) then
                    P.PageUp
                elif code.SequenceEqual ("Delete".AsSpan()) then
                    P.Delete
                else
                    P.UnidentifiedCode
            | 7 ->
                if code.StartsWith ("Numpad".AsSpan()) then
                    match code[6] with
                    | '0' -> P.Numpad0
                    | '1' -> P.Numpad1
                    | '2' -> P.Numpad2
                    | '3' -> P.Numpad3
                    | '4' -> P.Numpad4
                    | '5' -> P.Numpad5
                    | '6' -> P.Numpad6
                    | '7' -> P.Numpad7
                    | '8' -> P.Numpad8
                    | '9' -> P.Numpad9
                    | _ -> P.UnidentifiedCode
                elif code.SequenceEqual ("IntlYen".AsSpan()) then
                    P.IntlYen
                elif code.SequenceEqual ("AltLeft".AsSpan()) then
                    P.AltLeft
                elif code.SequenceEqual ("OSRight".AsSpan()) then
                    P.MetaRight
                elif code.SequenceEqual ("Convert".AsSpan()) then
                    P.Convert
                elif code.SequenceEqual ("ArrowUp".AsSpan()) then
                    P.ArrowUp
                elif code.SequenceEqual ("NumLock".AsSpan()) then
                    P.NumLock
                else
                    P.UnidentifiedCode
            | 8 ->
                if code.SequenceEqual ("CapsLock".AsSpan()) then
                    P.CapsLock
                elif code.SequenceEqual ("IntlHash".AsSpan()) then
                    P.IntlHash
                elif code.SequenceEqual ("MetaLeft".AsSpan()) then
                    P.MetaLeft
                elif code.SequenceEqual ("AltRight".AsSpan()) then
                    P.AltRight
                elif code.SequenceEqual ("KanaMode".AsSpan()) then
                    P.KanaMode
                elif code.SequenceEqual ("PageDown".AsSpan()) then
                    P.PageDown
                else
                    P.UnidentifiedCode
            | 9 ->
                if code.SequenceEqual ("Backquote".AsSpan()) then
                    P.Backquote
                elif code.SequenceEqual ("Backspace".AsSpan()) then
                    P.Backspace
                elif code.SequenceEqual ("Backslash".AsSpan()) then
                    P.Backslash
                elif code.SequenceEqual ("Semicolon".AsSpan()) then
                    P.Semicolon
                elif code.SequenceEqual ("ShiftLeft".AsSpan()) then
                    P.ShiftLeft
                elif code.SequenceEqual ("MetaRight".AsSpan()) then
                    P.MetaRight
                elif code.SequenceEqual ("ArrowDown".AsSpan()) then
                    P.ArrowDown
                elif code.SequenceEqual ("ArrowLeft".AsSpan()) then
                    P.ArrowLeft
                elif code.SequenceEqual ("NumpadAdd".AsSpan()) then
                    P.NumpadAdd
                else
                    P.UnidentifiedCode
            | 10 ->
                if code.SequenceEqual ("ShiftRight".AsSpan()) then
                    P.ShiftRight
                elif code.SequenceEqual ("NonConvert".AsSpan()) then
                    P.NonConvert
                elif code.SequenceEqual ("ScrollLock".AsSpan()) then
                    P.ScrollLock
                elif code.SequenceEqual ("ArrowRight".AsSpan()) then
                    P.ArrowRight
                else
                    P.UnidentifiedCode
            | 11 ->
                if code.SequenceEqual ("BracketLeft".AsSpan()) then
                    P.BracketLeft
                elif code.SequenceEqual ("ControlLeft".AsSpan()) then
                    P.ControlLeft
                elif code.SequenceEqual ("ContextMenu".AsSpan()) then
                    P.ContextMenu
                elif code.SequenceEqual ("PrintScreen".AsSpan()) then
                    P.PrintScreen
                elif code.SequenceEqual ("NumpadEnter".AsSpan()) then
                    P.NumpadEnter
                elif code.SequenceEqual ("NumpadComma".AsSpan()) then
                    P.NumpadComma
                elif code.SequenceEqual ("NumpadEqual".AsSpan()) then
                    P.NumpadEqual
                else
                    P.UnidentifiedCode
            | 12 ->
                if code.SequenceEqual ("BracketRight".AsSpan()) then
                    P.BracketRight
                elif code.SequenceEqual ("ControlRight".AsSpan()) then
                    P.ControlRight
                elif code.SequenceEqual ("NumpadDivide".AsSpan()) then
                    P.NumpadDivide
                else
                    P.UnidentifiedCode
            | 13 ->
                if code.SequenceEqual ("IntlBackslash".AsSpan()) then
                    P.IntlBackslash
                elif code.SequenceEqual ("NumpadDecimal".AsSpan()) then
                    P.NumpadDecimal
                else
                    P.UnidentifiedCode
            | 14 ->
                if code.SequenceEqual ("NumpadMultiply".AsSpan()) then
                    P.NumpadMultiply
                elif code.SequenceEqual ("NumpadSubtract".AsSpan()) then
                    P.NumpadSubtract
                else
                    P.UnidentifiedCode
            | _ -> P.UnidentifiedCode

    /// <summary>Parses a UTF-16 ReadOnlySpan W3C KeyboardEvent.code using three-tier SIMD with scalar fallback.</summary>
    let ofW3cCodeSpan (code: ReadOnlySpan<char>) : PhysicalKeyCode =
        let len = code.Length

        if len < 2 || len > 14 then
            P.UnidentifiedCode
        elif
            (Vector512.IsHardwareAccelerated
             && Vector512<byte>.IsSupported)
            || (Vector256.IsHardwareAccelerated
                && Vector256<byte>.IsSupported)
            || (Vector128.IsHardwareAccelerated
                && Vector128<byte>.IsSupported)
        then
            let mutable keyVec = Vector128<byte>.Zero

            if not (SimdKeyTable.tryPackAsciiChars16 code &keyVec) then
                P.UnidentifiedCode
            else
                let idx =
                    SimdKeyTable.lookupSlot simdTable.Index len (byte code[0]) (byte code[len >>> 1]) (byte code[len - 1]) keyVec

                if idx >= 0 then
                    simdTable.Values[idx]
                else
                    P.UnidentifiedCode
        else
            ofW3cCodeScalar code

    /// <summary>Parses a UTF-8 ReadOnlySpan W3C KeyboardEvent.code using three-tier SIMD with scalar fallback.</summary>
    let ofW3cCodeUtf8 (utf8: ReadOnlySpan<byte>) : PhysicalKeyCode =
        let len = utf8.Length

        if len < 2 || len > 14 then
            P.UnidentifiedCode
        else
            let mutable keyVec = Vector128<byte>.Zero

            if not (SimdKeyTable.tryPackAsciiBytes16 utf8 &keyVec) then
                P.UnidentifiedCode
            elif
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
                    P.UnidentifiedCode
            else
                let chars = Array.zeroCreate<char> len

                for i = 0 to len - 1 do
                    chars[i] <- char utf8[i]

                ofW3cCodeScalar (ReadOnlySpan<char>(chars))

    /// <summary>Parses a W3C UI Events KeyboardEvent.code string into a PhysicalKeyCode.</summary>
    let ofW3cCode (code: string) : PhysicalKeyCode =
        if String.IsNullOrEmpty code then
            P.UnidentifiedCode
        else
            ofW3cCodeSpan (code.AsSpan ())

    /// <summary>Formats a PhysicalKeyCode back to its canonical W3C KeyboardEvent.code string.</summary>
    let toW3cCode (code: PhysicalKeyCode) : string =
        match code with
        | P.Backquote -> "Backquote"
        | P.Digit1 -> "Digit1"
        | P.Digit2 -> "Digit2"
        | P.Digit3 -> "Digit3"
        | P.Digit4 -> "Digit4"
        | P.Digit5 -> "Digit5"
        | P.Digit6 -> "Digit6"
        | P.Digit7 -> "Digit7"
        | P.Digit8 -> "Digit8"
        | P.Digit9 -> "Digit9"
        | P.Digit0 -> "Digit0"
        | P.Minus -> "Minus"
        | P.Equal -> "Equal"
        | P.IntlYen -> "IntlYen"
        | P.Backspace -> "Backspace"
        | P.Tab -> "Tab"
        | P.KeyQ -> "KeyQ"
        | P.KeyW -> "KeyW"
        | P.KeyE -> "KeyE"
        | P.KeyR -> "KeyR"
        | P.KeyT -> "KeyT"
        | P.KeyY -> "KeyY"
        | P.KeyU -> "KeyU"
        | P.KeyI -> "KeyI"
        | P.KeyO -> "KeyO"
        | P.KeyP -> "KeyP"
        | P.BracketLeft -> "BracketLeft"
        | P.BracketRight -> "BracketRight"
        | P.Backslash -> "Backslash"
        | P.CapsLock -> "CapsLock"
        | P.KeyA -> "KeyA"
        | P.KeyS -> "KeyS"
        | P.KeyD -> "KeyD"
        | P.KeyF -> "KeyF"
        | P.KeyG -> "KeyG"
        | P.KeyH -> "KeyH"
        | P.KeyJ -> "KeyJ"
        | P.KeyK -> "KeyK"
        | P.KeyL -> "KeyL"
        | P.Semicolon -> "Semicolon"
        | P.Quote -> "Quote"
        | P.IntlHash -> "IntlHash"
        | P.Enter -> "Enter"
        | P.ShiftLeft -> "ShiftLeft"
        | P.IntlBackslash -> "IntlBackslash"
        | P.KeyZ -> "KeyZ"
        | P.KeyX -> "KeyX"
        | P.KeyC -> "KeyC"
        | P.KeyV -> "KeyV"
        | P.KeyB -> "KeyB"
        | P.KeyN -> "KeyN"
        | P.KeyM -> "KeyM"
        | P.Comma -> "Comma"
        | P.Period -> "Period"
        | P.Slash -> "Slash"
        | P.IntlRo -> "IntlRo"
        | P.ShiftRight -> "ShiftRight"
        | P.ControlLeft -> "ControlLeft"
        | P.MetaLeft -> "MetaLeft"
        | P.AltLeft -> "AltLeft"
        | P.Space -> "Space"
        | P.AltRight -> "AltRight"
        | P.MetaRight -> "MetaRight"
        | P.ContextMenu -> "ContextMenu"
        | P.ControlRight -> "ControlRight"
        | P.Convert -> "Convert"
        | P.NonConvert -> "NonConvert"
        | P.KanaMode -> "KanaMode"
        | P.Lang1 -> "Lang1"
        | P.Lang2 -> "Lang2"
        | P.Fn -> "Fn"
        | P.Escape -> "Escape"
        | P.F1 -> "F1"
        | P.F2 -> "F2"
        | P.F3 -> "F3"
        | P.F4 -> "F4"
        | P.F5 -> "F5"
        | P.F6 -> "F6"
        | P.F7 -> "F7"
        | P.F8 -> "F8"
        | P.F9 -> "F9"
        | P.F10 -> "F10"
        | P.F11 -> "F11"
        | P.F12 -> "F12"
        | P.F13 -> "F13"
        | P.F14 -> "F14"
        | P.F15 -> "F15"
        | P.F16 -> "F16"
        | P.F17 -> "F17"
        | P.F18 -> "F18"
        | P.F19 -> "F19"
        | P.F20 -> "F20"
        | P.F21 -> "F21"
        | P.F22 -> "F22"
        | P.F23 -> "F23"
        | P.F24 -> "F24"
        | P.PrintScreen -> "PrintScreen"
        | P.ScrollLock -> "ScrollLock"
        | P.Pause -> "Pause"
        | P.Insert -> "Insert"
        | P.Home -> "Home"
        | P.PageUp -> "PageUp"
        | P.Delete -> "Delete"
        | P.End -> "End"
        | P.PageDown -> "PageDown"
        | P.ArrowUp -> "ArrowUp"
        | P.ArrowDown -> "ArrowDown"
        | P.ArrowLeft -> "ArrowLeft"
        | P.ArrowRight -> "ArrowRight"
        | P.NumLock -> "NumLock"
        | P.NumpadDivide -> "NumpadDivide"
        | P.NumpadMultiply -> "NumpadMultiply"
        | P.NumpadSubtract -> "NumpadSubtract"
        | P.NumpadAdd -> "NumpadAdd"
        | P.NumpadEnter -> "NumpadEnter"
        | P.NumpadDecimal -> "NumpadDecimal"
        | P.NumpadComma -> "NumpadComma"
        | P.NumpadEqual -> "NumpadEqual"
        | P.Numpad0 -> "Numpad0"
        | P.Numpad1 -> "Numpad1"
        | P.Numpad2 -> "Numpad2"
        | P.Numpad3 -> "Numpad3"
        | P.Numpad4 -> "Numpad4"
        | P.Numpad5 -> "Numpad5"
        | P.Numpad6 -> "Numpad6"
        | P.Numpad7 -> "Numpad7"
        | P.Numpad8 -> "Numpad8"
        | P.Numpad9 -> "Numpad9"
        | P.UnidentifiedCode -> "Unidentified"
#endif

    /// <summary>Maps USB HID Usage Page 0x07 (Keyboard/Keypad) usage IDs to PhysicalKeyCode.</summary>
    let ofUsbHidUsage (usage: uint16) : PhysicalKeyCode =
        match usage with
        | 0x04us -> P.KeyA
        | 0x05us -> P.KeyB
        | 0x06us -> P.KeyC
        | 0x07us -> P.KeyD
        | 0x08us -> P.KeyE
        | 0x09us -> P.KeyF
        | 0x0Aus -> P.KeyG
        | 0x0Bus -> P.KeyH
        | 0x0Cus -> P.KeyI
        | 0x0Dus -> P.KeyJ
        | 0x0Eus -> P.KeyK
        | 0x0Fus -> P.KeyL
        | 0x10us -> P.KeyM
        | 0x11us -> P.KeyN
        | 0x12us -> P.KeyO
        | 0x13us -> P.KeyP
        | 0x14us -> P.KeyQ
        | 0x15us -> P.KeyR
        | 0x16us -> P.KeyS
        | 0x17us -> P.KeyT
        | 0x18us -> P.KeyU
        | 0x19us -> P.KeyV
        | 0x1Aus -> P.KeyW
        | 0x1Bus -> P.KeyX
        | 0x1Cus -> P.KeyY
        | 0x1Dus -> P.KeyZ
        | 0x1Eus -> P.Digit1
        | 0x1Fus -> P.Digit2
        | 0x20us -> P.Digit3
        | 0x21us -> P.Digit4
        | 0x22us -> P.Digit5
        | 0x23us -> P.Digit6
        | 0x24us -> P.Digit7
        | 0x25us -> P.Digit8
        | 0x26us -> P.Digit9
        | 0x27us -> P.Digit0
        | 0x28us -> P.Enter
        | 0x29us -> P.Escape
        | 0x2Aus -> P.Backspace
        | 0x2Bus -> P.Tab
        | 0x2Cus -> P.Space
        | 0x2Dus -> P.Minus
        | 0x2Eus -> P.Equal
        | 0x2Fus -> P.BracketLeft
        | 0x30us -> P.BracketRight
        | 0x31us -> P.Backslash
        | 0x32us -> P.IntlHash
        | 0x33us -> P.Semicolon
        | 0x34us -> P.Quote
        | 0x35us -> P.Backquote
        | 0x36us -> P.Comma
        | 0x37us -> P.Period
        | 0x38us -> P.Slash
        | 0x39us -> P.CapsLock
        | 0x3Aus -> P.F1
        | 0x3Bus -> P.F2
        | 0x3Cus -> P.F3
        | 0x3Dus -> P.F4
        | 0x3Eus -> P.F5
        | 0x3Fus -> P.F6
        | 0x40us -> P.F7
        | 0x41us -> P.F8
        | 0x42us -> P.F9
        | 0x43us -> P.F10
        | 0x44us -> P.F11
        | 0x45us -> P.F12
        | 0x46us -> P.PrintScreen
        | 0x47us -> P.ScrollLock
        | 0x48us -> P.Pause
        | 0x49us -> P.Insert
        | 0x4Aus -> P.Home
        | 0x4Bus -> P.PageUp
        | 0x4Cus -> P.Delete
        | 0x4Dus -> P.End
        | 0x4Eus -> P.PageDown
        | 0x4Fus -> P.ArrowRight
        | 0x50us -> P.ArrowLeft
        | 0x51us -> P.ArrowDown
        | 0x52us -> P.ArrowUp
        | 0x53us -> P.NumLock
        | 0x54us -> P.NumpadDivide
        | 0x55us -> P.NumpadMultiply
        | 0x56us -> P.NumpadSubtract
        | 0x57us -> P.NumpadAdd
        | 0x58us -> P.NumpadEnter
        | 0x59us -> P.Numpad1
        | 0x5Aus -> P.Numpad2
        | 0x5Bus -> P.Numpad3
        | 0x5Cus -> P.Numpad4
        | 0x5Dus -> P.Numpad5
        | 0x5Eus -> P.Numpad6
        | 0x5Fus -> P.Numpad7
        | 0x60us -> P.Numpad8
        | 0x61us -> P.Numpad9
        | 0x62us -> P.Numpad0
        | 0x63us -> P.NumpadDecimal
        | 0x64us -> P.IntlBackslash
        | 0x65us -> P.ContextMenu
        | 0x67us -> P.NumpadEqual
        | 0x68us -> P.F13
        | 0x69us -> P.F14
        | 0x6Aus -> P.F15
        | 0x6Bus -> P.F16
        | 0x6Cus -> P.F17
        | 0x6Dus -> P.F18
        | 0x6Eus -> P.F19
        | 0x6Fus -> P.F20
        | 0x70us -> P.F21
        | 0x71us -> P.F22
        | 0x72us -> P.F23
        | 0x73us -> P.F24
        | 0x85us -> P.NumpadComma
        | 0x87us -> P.IntlRo
        | 0x88us -> P.KanaMode
        | 0x89us -> P.IntlYen
        | 0x8Aus -> P.Convert
        | 0x8Bus -> P.NonConvert
        | 0x90us -> P.Lang1
        | 0x91us -> P.Lang2
        | 0xE0us -> P.ControlLeft
        | 0xE1us -> P.ShiftLeft
        | 0xE2us -> P.AltLeft
        | 0xE3us -> P.MetaLeft
        | 0xE4us -> P.ControlRight
        | 0xE5us -> P.ShiftRight
        | 0xE6us -> P.AltRight
        | 0xE7us -> P.MetaRight
        | _ -> P.UnidentifiedCode

    /// <summary>Maps PhysicalKeyCode to its ISO/IEC 9995-1/2 physical grid coordinate where standardized.</summary>
    let toIso9995 (code: PhysicalKeyCode) : Iso9995Coordinate voption =
        let inline alpha row col =
            ValueSome
                {
                    Section = Alphanumeric
                    Row = row
                    Column = byte col
                }

        let inline nav row col =
            ValueSome
                {
                    Section = EditingAndNavigation
                    Row = row
                    Column = byte col
                }

        let inline fn col =
            ValueSome
                {
                    Section = FunctionSection
                    Row = RowK
                    Column = byte col
                }

        match code with
        | P.Backquote -> alpha RowE 0
        | P.Digit1 -> alpha RowE 1
        | P.Digit2 -> alpha RowE 2
        | P.Digit3 -> alpha RowE 3
        | P.Digit4 -> alpha RowE 4
        | P.Digit5 -> alpha RowE 5
        | P.Digit6 -> alpha RowE 6
        | P.Digit7 -> alpha RowE 7
        | P.Digit8 -> alpha RowE 8
        | P.Digit9 -> alpha RowE 9
        | P.Digit0 -> alpha RowE 10
        | P.Minus -> alpha RowE 11
        | P.Equal -> alpha RowE 12
        | P.IntlYen -> alpha RowE 13
        | P.Backspace -> alpha RowE 14
        | P.Tab -> alpha RowD 0
        | P.KeyQ -> alpha RowD 1
        | P.KeyW -> alpha RowD 2
        | P.KeyE -> alpha RowD 3
        | P.KeyR -> alpha RowD 4
        | P.KeyT -> alpha RowD 5
        | P.KeyY -> alpha RowD 6
        | P.KeyU -> alpha RowD 7
        | P.KeyI -> alpha RowD 8
        | P.KeyO -> alpha RowD 9
        | P.KeyP -> alpha RowD 10
        | P.BracketLeft -> alpha RowD 11
        | P.BracketRight -> alpha RowD 12
        | P.Backslash -> alpha RowD 13
        | P.CapsLock -> alpha RowC 0
        | P.KeyA -> alpha RowC 1
        | P.KeyS -> alpha RowC 2
        | P.KeyD -> alpha RowC 3
        | P.KeyF -> alpha RowC 4
        | P.KeyG -> alpha RowC 5
        | P.KeyH -> alpha RowC 6
        | P.KeyJ -> alpha RowC 7
        | P.KeyK -> alpha RowC 8
        | P.KeyL -> alpha RowC 9
        | P.Semicolon -> alpha RowC 10
        | P.Quote -> alpha RowC 11
        | P.IntlHash -> alpha RowC 12
        | P.Enter -> alpha RowC 13
        | P.ShiftLeft -> alpha RowB 99
        | P.IntlBackslash -> alpha RowB 0
        | P.KeyZ -> alpha RowB 1
        | P.KeyX -> alpha RowB 2
        | P.KeyC -> alpha RowB 3
        | P.KeyV -> alpha RowB 4
        | P.KeyB -> alpha RowB 5
        | P.KeyN -> alpha RowB 6
        | P.KeyM -> alpha RowB 7
        | P.Comma -> alpha RowB 8
        | P.Period -> alpha RowB 9
        | P.Slash -> alpha RowB 10
        | P.IntlRo -> alpha RowB 11
        | P.ShiftRight -> alpha RowB 12
        | P.ControlLeft -> alpha RowA 99
        | P.MetaLeft -> alpha RowA 0
        | P.AltLeft -> alpha RowA 1
        | P.Space -> alpha RowA 3
        | P.AltRight -> alpha RowA 8
        | P.MetaRight -> alpha RowA 10
        | P.ContextMenu -> alpha RowA 11
        | P.ControlRight -> alpha RowA 12
        | P.Escape -> fn 0
        | P.F1 -> fn 1
        | P.F2 -> fn 2
        | P.F3 -> fn 3
        | P.F4 -> fn 4
        | P.F5 -> fn 5
        | P.F6 -> fn 6
        | P.F7 -> fn 7
        | P.F8 -> fn 8
        | P.F9 -> fn 9
        | P.F10 -> fn 10
        | P.F11 -> fn 11
        | P.F12 -> fn 12
        | P.Insert -> nav RowE 0
        | P.Home -> nav RowE 1
        | P.PageUp -> nav RowE 2
        | P.Delete -> nav RowD 0
        | P.End -> nav RowD 1
        | P.PageDown -> nav RowD 2
        | P.ArrowUp -> nav RowB 1
        | P.ArrowLeft -> nav RowA 0
        | P.ArrowDown -> nav RowA 1
        | P.ArrowRight -> nav RowA 2
        | _ -> ValueNone
