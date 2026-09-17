param([string]$SourceRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath($SourceRoot)
$taskOut = Join-Path $taskRepo ('_build_check_handler/verify-wafer-map-processing-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskOut | Out-Null
$taskCompiler = 'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe'
$taskUtf8 = New-Object Text.UTF8Encoding($true)
$taskRecipeSource = [IO.File]::ReadAllText((Join-Path $taskRepo 'QMC.CDT-320/Equipment/Recipes/RecipeStore.cs'))
$taskMatch = [regex]::Match($taskRecipeSource, '\[DataContract\]\r?\n    public class RecipeProject')
if (-not $taskMatch.Success) { throw 'Recipe POCO marker missing.' }
$taskModelPath = Join-Path $taskOut 'RecipeModels.cs'
[IO.File]::WriteAllText($taskModelPath, "using System;`r`nusing System.Runtime.Serialization;`r`nnamespace QMC.CDT320.Recipes`r`n{`r`n" + $taskRecipeSource.Substring($taskMatch.Index), $taskUtf8)
$taskStubSource = [IO.File]::ReadAllText((Join-Path $taskRepo 'tools/verify_wafer_map_storage.cs'))
$taskStubIndex = $taskStubSource.IndexOf('// Hardware/store/log boundaries only.', [StringComparison]::Ordinal)
if ($taskStubIndex -lt 0) { throw 'Boundary stub marker missing.' }
$taskStubs = $taskStubSource.Substring($taskStubIndex).Replace('public static class MaterialStateService', 'public static partial class MaterialStateService').Replace('enum EventKind { Warning, Event }', 'enum EventKind { Warning, Event, Alarm }')
$taskStubPath = Join-Path $taskOut 'BoundaryStubs.cs'
[IO.File]::WriteAllText($taskStubPath, "using System;`r`nusing System.IO;`r`nusing System.Runtime.Serialization;`r`n" + $taskStubs, $taskUtf8)
# Compile the exact runtime output-cache method, with only store/notification/hardware boundaries replaced.
$taskOutput = [IO.File]::ReadAllText((Join-Path $taskRepo 'QMC.CDT-320/Equipment/Materials/MaterialStateService.OutputReceive.cs'))
$taskStart = $taskOutput.IndexOf('        private static readonly System.Collections.Generic.Dictionary<QMC.CDT320.BinSide', [StringComparison]::Ordinal)
$taskEnd = $taskOutput.IndexOf('        private static WaferMaterial ResolveCurrentInputSourceForOutputTargetNoLock()', $taskStart, [StringComparison]::Ordinal)
if ($taskStart -lt 0 -or $taskEnd -le $taskStart) { throw 'Output cache method boundaries missing.' }
$taskOutputPath = Join-Path $taskOut 'ActualOutputCache.cs'
[IO.File]::WriteAllText($taskOutputPath, "using System; using System.IO; using System.Linq; using System.Collections.Generic; using QMC.CDT320.DieMaps; using QMC.CDT320.Recipes; namespace QMC.CDT320.Materials { public static partial class MaterialStateService {`r`n" + $taskOutput.Substring($taskStart, $taskEnd - $taskStart) + "} }", $taskUtf8)
$taskSnapshot = [IO.File]::ReadAllText((Join-Path $taskRepo 'QMC.CDT-320/Equipment/Materials/MaterialSnapshotStore.cs'))
$taskCloneStart = $taskSnapshot.IndexOf('        private static List<int> CloneIntList(', [StringComparison]::Ordinal)
$taskCloneEnd = $taskSnapshot.IndexOf('        private static ', $taskSnapshot.IndexOf('        private static MaterialSnapshot CloneSnapshotTyped(', [StringComparison]::Ordinal) + 25, [StringComparison]::Ordinal)
if ($taskCloneStart -lt 0 -or $taskCloneEnd -le $taskCloneStart) { throw 'Typed snapshot clone boundaries missing.' }
$taskClonePath = Join-Path $taskOut 'ActualSnapshotClone.cs'
$taskCloneCode = $taskSnapshot.Substring($taskCloneStart, $taskCloneEnd - $taskCloneStart).Replace('#endregion', '')
[IO.File]::WriteAllText($taskClonePath, "using System; using System.Collections.Generic; namespace QMC.CDT320.Materials { public static class ActualSnapshotClone { public static MaterialSnapshot Copy(MaterialSnapshot value) { return CloneSnapshotTyped(value, true); }`r`n" + $taskCloneCode + "} }", $taskUtf8)
$taskNames = @(
 'QMC.CDT-320/Equipment/DieMaps/WaferMapGeneration.cs',
 'QMC.CDT-320/Equipment/DieMaps/GeneratedWaferMapCodec.cs',
 'QMC.CDT-320/Equipment/DieMaps/DieMap.cs',
 'QMC.CDT-320/Equipment/DieMaps/DieMapGenerator.cs',
 'QMC.CDT-320/Equipment/DieMaps/PickupSequenceGenerator.cs',
 'QMC.CDT-320/Equipment/DieMaps/WaferMapProcessService.cs',
 'QMC.CDT-320/Equipment/DieMaps/WaferMapParserRegistry.cs',
 'QMC.CDT-320/Equipment/Recipes/RecipeMapBuildService.cs',
 'QMC.CDT-320/Equipment/Recipes/RecipeMapPaths.cs',
 'QMC.CDT-320/Equipment/Recipes/RecipeDieMapResolver.cs',
 'QMC.CDT-320/Equipment/Recipes/RecipeProjectConsistencyService.cs',
 'QMC.CDT-320/Equipment/Recipes/RecipeMapProcessSettingsService.cs',
 'QMC.CDT-320/Equipment/Recipes/RecipeInputMapSource.cs',
 'QMC.CDT-320/Equipment/Materials/MaterialModel.cs',
 'QMC.CDT-320/Equipment/Materials/MaterialStateService.WaferMapProcessing.cs',
 'QMC.CDT-320/Equipment/Lots/LotWaferMapFetchService.cs',
 'QMC.CDT-320/Ui/Common/WaferMaps/PickupOrderDraft.cs',
 'QMC.Common/Data/Store/JsonPrettySerializer.cs'
)
$taskSources = $taskNames | ForEach-Object { Join-Path $taskRepo $_ }
$taskExe = Join-Path $taskOut 'VerifyProcessMaps.exe'
& $taskCompiler /nologo /target:exe /platform:anycpu "/out:$taskExe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll $taskModelPath $taskStubPath $taskOutputPath $taskClonePath $taskSources (Join-Path $PSScriptRoot 'verify_wafer_map_processing.cs')
if ($LASTEXITCODE -ne 0) { throw 'Process map verification compilation failed.' }
& $taskExe | Tee-Object -FilePath (Join-Path $taskOut 'results.log')
if ($LASTEXITCODE -ne 0) { throw 'Process map verification failed.' }
[IO.File]::WriteAllText((Join-Path $taskRepo '_build_check_handler/latest-wafer-map-processing-verification.txt'), $taskOut, $taskUtf8)
Write-Output "Verification artifacts: $taskOut"
