param(
 [string]$SourceRoot = (Split-Path -Parent $PSScriptRoot),
 [string]$BuildOutput = '',
 [string]$ArtifactDirectory = ''
)
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath($SourceRoot)
if ([string]::IsNullOrWhiteSpace($BuildOutput)) { $BuildOutput = Join-Path $taskRepo '_build_check_handler/map-coordinates/out' }
$taskBuild = [IO.Path]::GetFullPath($BuildOutput)
if (-not $taskBuild.StartsWith((Join-Path $taskRepo '_build_check_handler') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
 throw 'Use an isolated build beneath the source _build_check_handler directory.'
}
if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) { $ArtifactDirectory = Join-Path $taskRepo ('_build_check_handler/map-coordinate-results-' + [Guid]::NewGuid().ToString('N')) }
$taskArtifacts = [IO.Path]::GetFullPath($ArtifactDirectory)
if ($taskArtifacts.StartsWith('D:\CDT-320\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Operational output is read-only during verification.' }
New-Item -ItemType Directory -Force -Path $taskArtifacts | Out-Null
$taskCompiler = 'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe'
$taskExe = Join-Path $taskBuild 'VerifyMapCoordinates.exe'
& $taskCompiler /nologo /target:exe /platform:anycpu "/out:$taskExe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll "/r:$taskBuild/QMC.CDT-320.exe" "/r:$taskBuild/QMC.Common.dll" (Join-Path $PSScriptRoot 'verify_map_coordinates.cs')
if ($LASTEXITCODE -ne 0) { throw 'Map coordinate verification compilation failed.' }
& $taskExe $taskArtifacts | Tee-Object -FilePath (Join-Path $taskArtifacts 'results.log')
if ($LASTEXITCODE -ne 0) { throw 'Map coordinate verification failed.' }
Write-Output "Verification artifacts: $taskArtifacts"
