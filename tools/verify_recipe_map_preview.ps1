param(
    [Parameter(Mandatory=$true)][string[]]$MapPaths,
    [string]$BuildDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) '_build_check_handler/map-create-preview/out'),
    [Parameter(Mandatory=$true)][string]$ArtifactDirectory
)
$ErrorActionPreference = 'Stop'
$taskBuild = [IO.Path]::GetFullPath($BuildDirectory)
$taskOutput = [IO.Path]::GetFullPath($ArtifactDirectory)
foreach ($taskPath in @($taskBuild, $taskOutput)) {
    if ($taskPath.StartsWith('D:\CDT-320\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Verification must run outside equipment deployment.' }
}
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskCompiler = 'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe'
$taskExe = Join-Path $taskBuild 'VerifyRecipeMapPreview.exe'
& $taskCompiler /nologo /debug:full /target:exe /platform:anycpu "/out:$taskExe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll "/r:$taskBuild/QMC.CDT-320.exe" "/r:$taskBuild/QMC.Common.dll" (Join-Path $PSScriptRoot 'verify_recipe_map_preview.cs')
if ($LASTEXITCODE -ne 0) { throw 'Recipe preview verification compilation failed.' }
& $taskExe $taskOutput $MapPaths | Tee-Object -FilePath (Join-Path $taskOutput 'results.log')
if ($LASTEXITCODE -ne 0) { throw 'Recipe preview verification failed.' }
