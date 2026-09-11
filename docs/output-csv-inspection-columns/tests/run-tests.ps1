param(
    [Parameter(Mandatory = $true)]
    [string]$BuildOut
)

$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
$testSource = Join-Path $repoRoot 'docs\output-csv-inspection-columns\tests\VisionInspectionResultFileWriterTests.cs'
$resolvedBuildOut = (Resolve-Path -LiteralPath $BuildOut).Path
$handlerAssembly = Join-Path $resolvedBuildOut 'QMC.CDT-320.exe'
$commonAssembly = Join-Path $resolvedBuildOut 'QMC.Common.dll'
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$testExe = Join-Path $resolvedBuildOut 'VisionInspectionResultFileWriterTests.exe'

if (-not (Test-Path -LiteralPath $compiler))
{
    throw "C# compiler not found: $compiler"
}
if (-not (Test-Path -LiteralPath $handlerAssembly))
{
    throw "Handler assembly not found: $handlerAssembly"
}
if (-not (Test-Path -LiteralPath $commonAssembly))
{
    throw "Common assembly not found: $commonAssembly"
}

& $compiler /nologo /target:exe "/out:$testExe" "/reference:$handlerAssembly" "/reference:$commonAssembly" $testSource
if ($LASTEXITCODE -ne 0)
{
    throw "Test compilation failed: exit=$LASTEXITCODE"
}

& $testExe
if ($LASTEXITCODE -ne 0)
{
    throw "Test execution failed: exit=$LASTEXITCODE"
}
