param(
    [Parameter(Mandatory = $true)]
    [string]$HandlerAssemblyPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.Serialization

$assemblyPath = (Resolve-Path -LiteralPath $HandlerAssemblyPath).Path
$assemblyFolder = Split-Path -Parent $assemblyPath
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $assemblyFolder 'QMC.Common.dll'))
$handlerAssembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$configType = $handlerAssembly.GetType('QMC.CDT320.InputStageConfig', $true)
$serializer = New-Object System.Runtime.Serialization.Json.DataContractJsonSerializer($configType)
$script:assertions = 0

function Read-InputStageConfig([string]$json) {
    $stream = New-Object System.IO.MemoryStream(,[System.Text.Encoding]::UTF8.GetBytes($json))
    try { return $serializer.ReadObject($stream) }
    finally { $stream.Dispose() }
}

function Write-InputStageConfig($config) {
    $stream = New-Object System.IO.MemoryStream
    try {
        $serializer.WriteObject($stream, $config)
        return [System.Text.Encoding]::UTF8.GetString($stream.ToArray())
    }
    finally { $stream.Dispose() }
}

function Assert-Value([double]$actual, [double]$expected, [string]$label) {
    if ($actual -ne $expected) {
        throw "$label expected=$expected actual=$actual"
    }
    $script:assertions++
}

# Only the actual configuration data class is instantiated. No Controller, Unit, IO, or Vision is created.
$legacy = Read-InputStageConfig '{}'
Assert-Value $legacy.ManualDieDetectCumulativeOffsetLimitX 20 'Legacy missing cumulative X'
Assert-Value $legacy.ManualDieDetectCumulativeOffsetLimitY 20 'Legacy missing cumulative Y'

$wide = Read-InputStageConfig '{"ManualDieDetectOffsetLimitX":30,"ManualDieDetectOffsetLimitY":40,"ManualDieDetectCumulativeOffsetLimitX":300,"ManualDieDetectCumulativeOffsetLimitY":400}'
Assert-Value $wide.ManualDieDetectOffsetLimitX 30 'Single X is independent'
Assert-Value $wide.ManualDieDetectOffsetLimitY 40 'Single Y is independent'
Assert-Value $wide.ManualDieDetectCumulativeOffsetLimitX 300 'Wide cumulative X retained'
Assert-Value $wide.ManualDieDetectCumulativeOffsetLimitY 400 'Wide cumulative Y retained'
$reloaded = Read-InputStageConfig (Write-InputStageConfig $wide)
Assert-Value $reloaded.ManualDieDetectCumulativeOffsetLimitX 300 'Cumulative X roundtrip'
Assert-Value $reloaded.ManualDieDetectCumulativeOffsetLimitY 400 'Cumulative Y roundtrip'

$partial = Read-InputStageConfig '{"ManualDieDetectCumulativeOffsetLimitX":0.2}'
Assert-Value $partial.ManualDieDetectCumulativeOffsetLimitX 0.2 'Small cumulative X retained'
Assert-Value $partial.ManualDieDetectCumulativeOffsetLimitY 20 'Missing Y default independent of X'

$invalid = Read-InputStageConfig '{"ManualDieDetectCumulativeOffsetLimitX":0,"ManualDieDetectCumulativeOffsetLimitY":-1}'
Assert-Value $invalid.ManualDieDetectCumulativeOffsetLimitX 0 'Explicit zero preserved for policy rejection'
Assert-Value $invalid.ManualDieDetectCumulativeOffsetLimitY -1 'Explicit negative preserved for policy rejection'

Write-Output "PASS: $script:assertions actual InputStageConfig serialization assertions; no hardware instantiated."
