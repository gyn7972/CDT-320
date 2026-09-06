param([Parameter(Mandatory=$true)][string]$HandlerAssemblyPath)
$ErrorActionPreference = 'Stop'
$assemblyPath = (Resolve-Path -LiteralPath $HandlerAssemblyPath).Path
$testOutput = Split-Path -Parent $assemblyPath
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$harness = Join-Path $PSScriptRoot 'InputStageReviewMaterialGeometryTests.cs'
$exe = Join-Path $testOutput 'InputStageReviewMaterialGeometryTests.exe'
& $compiler /nologo /target:exe /langversion:7.3 /warnaserror+ /reference:System.Runtime.Serialization.dll "/reference:$assemblyPath" "/out:$exe" $harness
if ($LASTEXITCODE -ne 0) { throw "Material geometry test compile failed: $LASTEXITCODE" }
& $exe
if ($LASTEXITCODE -ne 0) { throw "Material geometry test failed: $LASTEXITCODE" }
