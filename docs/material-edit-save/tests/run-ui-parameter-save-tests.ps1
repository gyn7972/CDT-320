param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoPath = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$testOutput = [IO.Path]::GetFullPath($OutputDirectory)
if ($testOutput.StartsWith('D:\CDT-320', [StringComparison]::OrdinalIgnoreCase)) { throw 'Operational path is forbidden.' }
[IO.Directory]::CreateDirectory($testOutput) | Out-Null
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sourcePaths = @(
    (Join-Path $PSScriptRoot 'UiParameterSaveTests.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\JsonDataSaveCoordinator.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\JsonDataStore.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\JsonPrettySerializer.cs'),
    (Join-Path $repoPath 'QMC.Common\Data\Store\DataStoreResult.cs')
)
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$testExe = Join-Path $testOutput 'UiParameterSaveTests.exe'
& $compiler /nologo /target:exe "/out:$testExe" /r:System.Runtime.Serialization.dll /r:System.Core.dll $sourcePaths
if ($LASTEXITCODE -ne 0) { throw 'Test compile failed.' }
& $testExe (Join-Path $testOutput 'data')
if ($LASTEXITCODE -ne 0) { throw 'UI parameter save tests failed.' }
