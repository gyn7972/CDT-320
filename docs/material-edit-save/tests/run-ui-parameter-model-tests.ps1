param([Parameter(Mandatory=$true)][string]$HandlerAssembly, [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoPath = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$testOutput = [IO.Path]::GetFullPath($OutputDirectory)
if ($testOutput.StartsWith('D:\CDT-320', [StringComparison]::OrdinalIgnoreCase)) { throw 'Operational path is forbidden.' }
[IO.Directory]::CreateDirectory($testOutput) | Out-Null
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$sourcePaths = @(
    (Join-Path $PSScriptRoot 'UiParameterCaptureModelTests.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\JsonDataSaveCoordinator.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\JsonDataStore.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\JsonPrettySerializer.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\DataStoreResult.cs')
)
$testExe = Join-Path $testOutput 'UiParameterCaptureModelTests.exe'
& $compiler /nologo /target:exe "/out:$testExe" /r:System.Runtime.Serialization.dll /r:System.Core.dll $sourcePaths
if ($LASTEXITCODE -ne 0) { throw 'Model test compile failed.' }
& $testExe ([IO.Path]::GetFullPath($HandlerAssembly)) (Join-Path $testOutput 'data')
if ($LASTEXITCODE -ne 0) { throw 'UI parameter model tests failed.' }
