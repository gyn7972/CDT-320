param([switch]$KeepArtifacts)

$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$allowedParent = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler')).TrimEnd('\') + '\'
$verifyRoot = [IO.Path]::GetFullPath((Join-Path $allowedParent ('verify-wafer-map-storage-' + [Guid]::NewGuid().ToString('N'))))
if (-not $verifyRoot.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase)) { throw 'Verification directory escaped its allowed build parent.' }
$exitCode = 1
try {
    New-Item -ItemType Directory -Path $verifyRoot | Out-Null
    # Extract the actual POCO contracts only. Do not compile RecipeStore's startup constructor,
    # material storage, Handler startup, device classes, or production configuration stores.
    $recipeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'QMC.CDT-320\Equipment\Recipes\RecipeStore.cs'))
    $marker = '[DataContract]' + "`r`n" + '    public class RecipeProject'
    $modelIndex = $recipeSource.IndexOf($marker, [StringComparison]::Ordinal)
    if ($modelIndex -lt 0) { throw 'RecipeProject POCO marker was not found.' }
    $modelPath = Join-Path $verifyRoot 'RecipeModels.cs'
    [IO.File]::WriteAllText($modelPath, "using System;`r`nusing System.Runtime.Serialization;`r`nnamespace QMC.CDT320.Recipes`r`n{`r`n" + $recipeSource.Substring($modelIndex), (New-Object Text.UTF8Encoding($true)))
    $sources = @(
        'QMC.CDT-320\Equipment\DieMaps\WaferMapGeneration.cs',
        'QMC.CDT-320\Equipment\DieMaps\GeneratedWaferMapCodec.cs',
        'QMC.CDT-320\Equipment\DieMaps\WaferMapProcessService.cs',
        'QMC.CDT-320\Equipment\DieMaps\WaferMapParserRegistry.cs',
        'QMC.CDT-320\Equipment\DieMaps\DieMap.cs',
        'QMC.CDT-320\Equipment\DieMaps\DieMapGenerator.cs',
        'QMC.CDT-320\Equipment\DieMaps\PickupSequenceGenerator.cs',
        'QMC.CDT-320\Equipment\Recipes\RecipeMapBuildService.cs',
        'QMC.CDT-320\Equipment\Recipes\RecipeMapPaths.cs',
        'QMC.CDT-320\Equipment\Recipes\RecipeDieMapResolver.cs',
        'QMC.CDT-320\Equipment\Recipes\RecipeProjectConsistencyService.cs',
        'QMC.Common\Data\Store\JsonPrettySerializer.cs',
        'tools\verify_wafer_map_storage.cs'
    ) | ForEach-Object { Join-Path $repoRoot $_ }
    $verificationExe = Join-Path $verifyRoot 'verify_wafer_map_storage.exe'
    & $compiler /nologo /target:exe /platform:anycpu "/out:$verificationExe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll $modelPath $sources
    if ($LASTEXITCODE -ne 0) { throw "Storage verification compile failed: $LASTEXITCODE" }
    & $verificationExe | Tee-Object -FilePath (Join-Path $verifyRoot 'verification.log')
    $exitCode = $LASTEXITCODE
}
finally {
    if ($KeepArtifacts) { Write-Output "Verification artifacts: $verifyRoot" }
    elseif (Test-Path -LiteralPath $verifyRoot) {
        $resolvedVerify = (Resolve-Path -LiteralPath $verifyRoot).Path.TrimEnd('\')
        if (-not $resolvedVerify.Equals($verifyRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not $resolvedVerify.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase) -or
            -not ([IO.Path]::GetFileName($resolvedVerify)).StartsWith('verify-wafer-map-storage-', [StringComparison]::Ordinal)) {
            throw 'Refusing cleanup because the exact dedicated verification directory was not confirmed.'
        }
        Remove-Item -LiteralPath $resolvedVerify -Recurse -Force
    }
}
exit $exitCode
