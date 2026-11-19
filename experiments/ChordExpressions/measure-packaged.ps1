param([Parameter(Mandatory)][string]$Version)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$output = Join-Path $root 'artifacts/packaged-chord-probe'
$env:NUGET_PACKAGES = Join-Path $output 'packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $output 'http-cache'
dotnet build "$PSScriptRoot/Probe.fsproj" -c Release "-p:KeyboardPackageVersion=$Version" --artifacts-path $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$binary = Join-Path $output 'bin/Probe/release/Probe.dll'
$deps = Get-Content (Join-Path $output 'bin/Probe/release/Probe.deps.json') -Raw | ConvertFrom-Json -AsHashtable
if ($deps.libraries["Ranvier.Keyboard/$Version"].type -ne 'package') { throw 'Probe did not resolve the requested package' }
$env:DOTNET_TieredCompilation = '0'
$env:DOTNET_JitDisasm = 'ChordExperiments.Probe:*'
$env:DOTNET_JitStdOutFile = Join-Path $output ('jit-' + [guid]::NewGuid().ToString('N') + '.asm')
$timings = @(dotnet $binary)
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$timings | Set-Content (Join-Path $output 'timings.txt')
foreach ($group in ($timings | Group-Object { ($_ -split ' ')[0] })) {
    $ns = @($group.Group | ForEach-Object { [double](($_ -split ' ')[1]) } | Sort-Object)
    $bytes = @($group.Group | ForEach-Object { [long](($_ -split ' ')[2]) } | Sort-Object -Unique)
    if ($ns.Count -ne 9 -or $bytes.Count -ne 1 -or $bytes[0] -ne 0) { throw "Unexpected samples or allocation for $($group.Name)" }
    Write-Output "$($group.Name): median $($ns[4]) ns/chord; $($bytes[0]) bytes per loop"
}
$dump = Get-Content $env:DOTNET_JitStdOutFile -Raw
function Instructions([string]$method) {
    $listing = [regex]::Match($dump, ('(?s)Assembly listing for method ChordExperiments\.Probe:' + $method + '\(.*?; Total bytes'))
    if (!$listing.Success) { throw "Missing listing for $method" }
    return @(($listing.Value -split '\r?\n') | Where-Object { $_ -match '^       [a-z]' } | ForEach-Object { $_.Trim() })
}
$actual = Instructions 'bitsCE'
foreach ($baseline in 'literal','directBits') {
    $expected = Instructions $baseline
    if (!$actual.Count -or ($actual -join '|') -cne ($expected -join '|')) { throw "Instruction mismatch versus $baseline" }
}
Write-Output "Verified $($actual.Count) matching instructions against both direct baselines. Disassembly: $env:DOTNET_JitStdOutFile"
