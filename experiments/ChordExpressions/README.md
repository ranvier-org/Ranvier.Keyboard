# Typed chord expressions

Use `chord` to declare a shortcut. It accumulates modifier bits and finishes with a typed `LogicalKey`.

```fsharp
let extendLeft = chord { ctrl; shift; key (Navigation ArrowLeft) }
let undo = chord { ctrl; key (Character (System.Text.Rune 'z')) }
```

The final operation calls `KeyChord.ofLogical` once. That preserves Keyboard's handling of case, printable characters, AltGraph and overflow text. Supply the logical character reported by the event. For Ctrl+Shift+Z that is an uppercase `'Z'`.

## Published package

**Measured here** against Ranvier.Keyboard 0.2.0 restored from NuGet on Windows x64 using the Release probe and loop described below. The median was 9.666 ns/chord for the public `chord` expression, 9.644 for direct `ofBits` and 9.548 for the full record literal. Each allocated zero managed bytes per loop. All three wrappers emitted the same 18 instructions, including the call to normalize the logical key.

## Prototype measurements

**Measured here** with a Release F# consumer in a separate assembly on Windows x64, .NET SDK 10.0.404, tiering disabled, tail calls disabled for the consumer. Each loop constructed 2,000,000 chords from eight preconstructed logical keys, including a supplementary scalar and an unidentified vendor key. One warmup loop per case preceded nine timed loops per case in rotating order. The stopwatch covers dispatch through the benchmark function, construction and a checksum. The allocation counter covers the loop on the executing thread. These are steady-state construction figures.

| Construction | Median ns/chord | Managed bytes per loop | x64 wrapper bytes |
| --- | ---: | ---: | ---: |
| Direct record update from `KeyModifiers.None` | 16.92 | 0 | 162 |
| Inline CE updating a modifier record per operation | 12.80 | 0 | 121 |
| Inline CE accumulating bits | 9.60 | 0 | 69 |
| Direct full modifier record literal | 11.52 | 0 | 69 |
| Direct `KeyModifiers.ofBits` with the constant mask | 10.41 | 0 | 69 |

**Measured here:** the last three wrappers had identical instruction listings, including the call to the common normalization function. The different timing medians for those wrappers illustrate run variability. Instruction parity is the evidence for zero added construction work in this case. The common normalization callee is excluded from wrapper sizes and included in the timed loops.

**Measured here:** the NativeAOT Windows x64 executable passed the semantic checks and recorded zero managed bytes in every construction loop. Fickl Release output passed the same parity checks under Node. In emitted JS the record CE constructed intermediate modifier records. The bits CE emitted constant bit operations, one final modifier record and the normalization call. The JS check establishes output shape and semantic parity. Browser latency, JS heap allocation, cold initialization and other architectures remain unmeasured.

**Measured here:** F# rejected a missing key, a second key, a modifier after the key and a string passed as the key. Repeating a modifier is idempotent. The type checker enforces the order because modifier operations accept an accumulator and the terminal operation returns a chord. The builder has no delay, bind, loop or sequence machinery. The language permits builders to supply only the operations required by their syntax. [F# computation expressions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)

## Other APIs

| Surface | Decision |
| --- | --- |
| `KeyPress` | The prototype's `press { stroke value; physical code }` produced the same x64 instructions and the same JS constructor as the record literal. Both fields are already required and typed in F#. Prefer the literal unless a consumer use case makes the CE easier to read. |
| Plume multi-stroke bindings | Pass an ordinary F# list of typed chords to `Keymap.bindChords`. It is already the representation Plume stores. Define static bindings once. A sequence builder would need to justify its own collection or conversion costs. |
| Terminal commands | Use a chord expression for `Commands.Binding.Chord`. Keep the display label independent and leave event decoding in the transport adapter. |
| Native and Fable DOM packages | Both consume the same adapter source. `KeyboardEvent.toKeyChord` already translates the event through the shared normalizer. Builder syntax belongs at shortcut declaration sites. |
| Dead keys, digraphs and control-code lookup | Keep the direct operations. They consume an accent and text, a pair of characters, or a control value. There is no accumulating declaration here. |

## Reproduce

Run `pwsh -File experiments/ChordExpressions/measure-packaged.ps1 -Version 0.2.0` on Windows x64 to benchmark the NuGet API in an isolated package cache. The script verifies package provenance, checks allocations and compares the JIT instruction listing with both direct baselines.

Run from the Ranvier.Keyboard root in PowerShell. The probe prints case, ns/chord, current-thread allocation delta and checksum.

```powershell
dotnet build experiments/ChordExpressions/Probe.fsproj -c Release
$env:DOTNET_TieredCompilation = '0'
$env:DOTNET_JitDisasm = 'ChordExperiments.Probe:*'
$env:DOTNET_JitStdOutFile = "$PWD/artifacts/chord-ce.asm"
dotnet experiments/ChordExpressions/bin/Release/net10.0/Probe.dll

dotnet fickl experiments/ChordExpressions/Probe.fsproj -e .fs.js -o artifacts/chord-ce-js -c Release --noCache
node artifacts/chord-ce-js/Probe.fs.js
```

For NativeAOT, use an x64 Visual Studio developer shell.

```powershell
dotnet publish experiments/ChordExpressions/Probe.fsproj -c Release -r win-x64 -p:PublishAot=true --artifacts-path artifacts/chord-ce-aot-build -o artifacts/chord-ce-aot
./artifacts/chord-ce-aot/Probe.exe
```
