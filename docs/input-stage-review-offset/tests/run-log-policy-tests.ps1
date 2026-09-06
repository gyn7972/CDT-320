param([Parameter(Mandatory = $true)][string]$CommonAssemblyPath)

$ErrorActionPreference = 'Stop'
$assemblyPath = (Resolve-Path -LiteralPath $CommonAssemblyPath).Path
$testOutput = Split-Path -Parent $assemblyPath
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$harness = Join-Path $PSScriptRoot 'InputStageReviewLogPolicyTests.cs'
$testExe = Join-Path $testOutput 'InputStageReviewLogPolicyTests.exe'

# The harness loads only the supplied compiled Common assembly in its own process.
# It never calls EventLogger or public log-policy mode/configuration APIs.
& $compiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$testExe" $harness
if ($LASTEXITCODE -ne 0) { throw "Log policy test compile failed: $LASTEXITCODE" }
& $testExe $assemblyPath
if ($LASTEXITCODE -ne 0) { throw "Log policy test failed: $LASTEXITCODE" }
