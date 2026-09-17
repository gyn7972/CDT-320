param(
 [Parameter(Mandatory=$true)][string[]]$MapPaths,
 [string]$SourceRoot = (Split-Path -Parent $PSScriptRoot),
 [string]$ArtifactDirectory = ''
)
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath($SourceRoot)
$taskBuild = Join-Path $taskRepo '_build_check_handler/viewer-unification/out'
if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
 $ArtifactDirectory = Join-Path $taskRepo ('_build_check_handler/verify-map-view-style-' + [Guid]::NewGuid().ToString('N'))
}
$taskOutput = [IO.Path]::GetFullPath($ArtifactDirectory)
if ($taskOutput.StartsWith('D:\CDT-320\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Verification must not write to the equipment deployment directory.' }
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskCompiler = 'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe'
$taskExe = Join-Path $taskBuild 'VerifyWaferMapViewStyle.exe'
& $taskCompiler /nologo /target:exe /platform:anycpu "/out:$taskExe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll "/r:$taskBuild/QMC.CDT-320.exe" "/r:$taskBuild/QMC.Common.dll" (Join-Path $PSScriptRoot 'verify_wafer_map_view_style.cs')
if ($LASTEXITCODE -ne 0) { throw 'Viewer verification compilation failed.' }
& $taskExe $taskOutput $MapPaths | Tee-Object -FilePath (Join-Path $taskOutput 'results.log')
if ($LASTEXITCODE -ne 0) { throw 'Viewer verification failed.' }
Write-Output "Verification artifacts: $taskOutput"
