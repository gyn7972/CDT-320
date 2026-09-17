$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$parent = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler')).TrimEnd('\') + '\'
$verifyRoot = [IO.Path]::GetFullPath((Join-Path $parent ('verify-generated-frame-projection-' + [Guid]::NewGuid().ToString('N'))))
if (-not $verifyRoot.StartsWith($parent, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output escaped dedicated build directory.' }
New-Item -ItemType Directory -Path $verifyRoot | Out-Null
$utf8 = New-Object Text.UTF8Encoding($true)
$recipeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'QMC.CDT-320\Equipment\Recipes\RecipeStore.cs'))
$modelIndex = $recipeSource.IndexOf('[DataContract]' + "`r`n" + '    public class RecipeProject', [StringComparison]::Ordinal)
if ($modelIndex -lt 0) { throw 'Recipe POCO marker missing.' }
$modelPath = Join-Path $verifyRoot 'RecipeModels.cs'
[IO.File]::WriteAllText($modelPath, "using System;`r`nusing System.Runtime.Serialization;`r`nnamespace QMC.CDT320.Recipes`r`n{`r`n" + $recipeSource.Substring($modelIndex), $utf8)
$storageTest = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verify_wafer_map_storage.cs'))
$stubIndex = $storageTest.IndexOf('// Hardware/store/log boundaries only.', [StringComparison]::Ordinal)
if ($stubIndex -lt 0) { throw 'Shared test boundary stubs marker missing.' }
$stubsPath = Join-Path $verifyRoot 'BoundaryStubs.cs'
$stubs = $storageTest.Substring($stubIndex).Replace('public static class MaterialStateService', 'public static partial class MaterialStateService')
[IO.File]::WriteAllText($stubsPath, "using System;`r`nusing System.IO;`r`nusing System.Runtime.Serialization;`r`n" + $stubs, $utf8)
# Extract the actual one-method MaterialSpecs adapter and its grid calculation helper only.
$materialSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'QMC.CDT-320\Equipment\Materials\MaterialStateService.CassetteMapping.cs'))
$start = $materialSource.IndexOf('        private static void EnsureTapeFrameSpecFromFrame(', [StringComparison]::Ordinal)
$end = $materialSource.IndexOf('        private static void EnsureDieSpecFromRecipe(', $start, [StringComparison]::Ordinal)
if ($start -lt 0 -or $end -le $start) { throw 'MaterialSpecs adapter boundaries missing.' }
$adapterPath = Join-Path $verifyRoot 'ActualMaterialAdapter.cs'
[IO.File]::WriteAllText($adapterPath, "using System;`r`nusing QMC.CDT320.DieMaps;`r`nusing QMC.CDT320.Recipes;`r`nnamespace QMC.CDT320.Materials { public static partial class MaterialStateService {`r`n" + $materialSource.Substring($start, $end - $start) + "} }`r`n", $utf8)
$syncSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'QMC.CDT-320\Equipment\Materials\MaterialStateService.RecipeSpec.cs'))
$syncStart = $syncSource.IndexOf('        public static string SyncRecipeTapeFrameSpec(', [StringComparison]::Ordinal)
$syncEnd = $syncSource.IndexOf('        public static string ResolveRecipeDieSpecName(', $syncStart, [StringComparison]::Ordinal)
if ($syncStart -lt 0 -or $syncEnd -le $syncStart) { throw 'MaterialSpecs sync boundaries missing.' }
$syncPath = Join-Path $verifyRoot 'ActualMaterialSync.cs'
[IO.File]::WriteAllText($syncPath, "using System;`r`nusing QMC.Common;`r`nusing QMC.CDT320.Recipes;`r`nnamespace QMC.CDT320.Materials { public static partial class MaterialStateService {`r`n" + $syncSource.Substring($syncStart, $syncEnd - $syncStart) + "} }`r`n", $utf8)
$sources = @(
    'QMC.CDT-320\Equipment\DieMaps\WaferMapGeneration.cs',
    'QMC.CDT-320\Equipment\DieMaps\GeneratedWaferMapCodec.cs',
    'QMC.CDT-320\Equipment\DieMaps\DieMap.cs',
    'QMC.CDT-320\Equipment\DieMaps\DieMapGenerator.cs',
    'QMC.CDT-320\Equipment\DieMaps\WaferMapProcessService.cs',
    'QMC.CDT-320\Equipment\Recipes\GeneratedWaferFrameProjection.cs',
    'QMC.CDT-320\Equipment\Recipes\RecipeMapPaths.cs',
    'QMC.CDT-320\Equipment\Recipes\RecipeDieMapResolver.cs',
    'QMC.CDT-320\Equipment\Recipes\RecipeProjectConsistencyService.cs',
    'QMC.Common\Data\Store\JsonPrettySerializer.cs',
    'tools\verify_generated_frame_projection.cs'
) | ForEach-Object { Join-Path $repoRoot $_ }
$exe = Join-Path $verifyRoot 'verify_generated_frame_projection.exe'
& $compiler /nologo /target:exe /platform:anycpu "/out:$exe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll $modelPath $stubsPath $adapterPath $syncPath $sources
if ($LASTEXITCODE -ne 0) { throw "Projection verification compile failed: $LASTEXITCODE" }
& $exe | Tee-Object -FilePath (Join-Path $verifyRoot 'verification.log')
$exitCode = $LASTEXITCODE
# Retain isolated artifacts. No app runtime is loaded and no cleanup/deletion is performed.
Write-Output "Verification artifacts: $verifyRoot"
exit $exitCode
