$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$sources = @(
    'QMC.CDT-320\Equipment\DieMaps\WaferMapGeneration.cs',
    'QMC.CDT-320\Equipment\DieMaps\GeneratedWaferMapCodec.cs',
    'QMC.CDT-320\Equipment\DieMaps\WaferMapProcessService.cs',
    'QMC.CDT-320\Equipment\DieMaps\DieMap.cs',
    'QMC.CDT-320\Equipment\DieMaps\DieMapGenerator.cs',
    'QMC.CDT-320\Equipment\Materials\Die.cs',
    'QMC.CDT-320\Equipment\Materials\DieTapeFrame.cs',
    'QMC.Common\Data\Store\JsonPrettySerializer.cs',
    'tools\verify_generated_wafer_persistence.cs'
) | ForEach-Object { Join-Path $repoRoot $_ }
$allowedParent = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler')).TrimEnd('\') + '\'
$verifyRoot = [IO.Path]::GetFullPath((Join-Path $allowedParent ('verify-generated-wafer-persistence-' + [Guid]::NewGuid().ToString('N'))))
if (-not $verifyRoot.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Verification output escaped this repository _build_check_handler directory.'
}
New-Item -ItemType Directory -Path $verifyRoot | Out-Null
$verificationExe = Join-Path $verifyRoot 'verify_generated_wafer_persistence.exe'
# Actual POCOs and serializers only; no Handler app, settings, GUI, machine or operational data.
# Keep artifacts for review. This verification script deliberately performs no file deletion.
& $compiler /nologo /target:exe /platform:anycpu "/out:$verificationExe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll $sources
if ($LASTEXITCODE -ne 0) { throw "Standalone persistence verification compilation failed: $LASTEXITCODE" }
& $verificationExe $verifyRoot | Tee-Object -FilePath (Join-Path $verifyRoot 'verification.log')
$exitCode = $LASTEXITCODE
Write-Output "Verification artifacts: $verifyRoot"
exit $exitCode
