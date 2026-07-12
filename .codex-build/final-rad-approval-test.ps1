param([string]$AssemblyDirectory = '')

$ErrorActionPreference = 'Stop'

$assemblyDir = if ([string]::IsNullOrWhiteSpace($AssemblyDirectory)) {
    Join-Path $PSScriptRoot 'final-rad-approval'
} else {
    $AssemblyDirectory
}

[void][Reflection.Assembly]::LoadFrom((Join-Path $assemblyDir 'QMC.Common.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $assemblyDir 'QMC.CDT-320.exe'))

$radPath = 'D:\CDT-320\Config\WaferMap\RAD1.txt'
$radHashBefore = (Get-FileHash $radPath -Algorithm SHA256).Hash
$testRoot = Join-Path $PSScriptRoot ('final-rad-approval-data-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testRoot -Force)
[QMC.Common.Data.Store.DataPaths]::Root = $testRoot

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "ASSERT FAILED: $message" }
}

function Assert-Near([double]$actual, [double]$expected, [string]$message) {
    Assert-True ([Math]::Abs($actual - $expected) -le 0.000001) "$message actual=$actual expected=$expected"
}

function Get-ByRaw($map, [int]$x, [int]$y) {
    return $map.Entries | Where-Object { $_.OriginalMapX -eq $x -and $_.OriginalMapY -eq $y } | Select-Object -First 1
}

function Assert-MapRoundTrip($expected, $actual, [string]$label) {
    Assert-True ($null -ne $actual) "$label loaded"
    Assert-True ($actual.DieMapX -eq 35 -and $actual.DieMapY -eq 47) "$label local dimensions"
    Assert-True ($actual.Entries.Count -eq 1257) "$label record count"
    Assert-True ($actual.SourceFileName -eq 'RAD1.txt') "$label source filename"
    Assert-True ($actual.SourceFormat -eq 'RAD TXT') "$label source format"
    Assert-True $actual.SourcePitchFromFile "$label source pitch flag"
    Assert-True ($actual.SourceDeclaredCount -eq 1257) "$label declared count"
    Assert-True ($actual.SourceFirstX -eq 200 -and $actual.SourceFirstY -eq 200) "$label FIRST raw"
    Assert-Near $actual.SourceFirstPosX 138.058 "$label FX"
    Assert-Near $actual.SourceFirstPosY -24.456 "$label FY"
    $center = Get-ByRaw $actual 183 204
    Assert-True ($null -ne $center) "$label raw center exists"
    Assert-True ($center.DieMapX -eq 17 -and $center.DieMapY -eq 23) "$label center local"
    Assert-Near $center.EquipmentGridX 0 "$label center equipment X"
    Assert-Near $center.EquipmentGridY 0 "$label center equipment Y"
}

# 1) Parse the real RAD1 file without ever writing to it.
$radMap = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadWaferMapTextOrThrow($radPath)
Assert-MapRoundTrip $radMap $radMap 'RAD parser'
Assert-Near $radMap.PitchX 8.12 'RAD Pitch X'
Assert-Near $radMap.PitchY 6.12 'RAD Pitch Y'
Assert-Near $radMap.DieSizeX 0 'RAD contains no die body width'
Assert-Near $radMap.DieSizeY 0 'RAD contains no die body height'
Assert-True (($radMap.Entries | Measure-Object OriginalMapX -Minimum -Maximum).Minimum -eq 166) 'raw min X'
Assert-True (($radMap.Entries | Measure-Object OriginalMapX -Minimum -Maximum).Maximum -eq 200) 'raw max X'
Assert-True (($radMap.Entries | Measure-Object OriginalMapY -Minimum -Maximum).Minimum -eq 181) 'raw min Y'
Assert-True (($radMap.Entries | Measure-Object OriginalMapY -Minimum -Maximum).Maximum -eq 227) 'raw max Y'
$first = Get-ByRaw $radMap 200 200
Assert-True ($first.DieMapX -eq 34 -and $first.DieMapY -eq 27) 'FIRST local coordinates'
Assert-Near $first.EquipmentGridX 17 'FIRST equipment grid X'
Assert-Near $first.EquipmentGridY -4 'FIRST equipment grid Y'
Assert-Near $first.PosX 138.04 'FIRST computed X from map pitch'
Assert-Near $first.PosY -24.48 'FIRST computed Y from map pitch'

# 2) JSON/CSV round-trip must retain raw/local/equipment/provenance fields.
$jsonPath = Join-Path $testRoot 'RAD1-roundtrip.json'
$csvPath = Join-Path $testRoot 'RAD1-roundtrip.csv'
[QMC.CDT320.DieMaps.DieMapGenerator]::SaveJson($radMap, $jsonPath)
[QMC.CDT320.DieMaps.DieMapGenerator]::SaveCsv($radMap, $csvPath)
Assert-MapRoundTrip $radMap ([QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($jsonPath)) 'JSON'
Assert-MapRoundTrip $radMap ([QMC.CDT320.DieMaps.DieMapGenerator]::LoadCsv($csvPath)) 'CSV'

# 3) Build one Base and all three role maps. Role pitch comes from Recipe, die body size comes from Recipe Die.
$project = New-Object QMC.CDT320.Recipes.RecipeProject
$project.FileName = 'FinalRadApprovalHarness'
[QMC.CDT320.Recipes.RecipeProjectConsistencyService]::EnsureStructure($project) | Out-Null
$project.Die.DieSpecName = 'RAD-HARNESS-DIE'
$project.Die.WidthMm = 8.0
$project.Die.HeightMm = 6.0
$project.Die.ThicknessMm = 0.25
$project.InputFrame.PitchX = 8.12
$project.InputFrame.PitchY = 6.12
$project.InputFrame.OuterDiameterMm = 300
$project.OutputFrame.PitchX = 8.20
$project.OutputFrame.PitchY = 6.20
$project.OutputFrame.OuterDiameterMm = 300

$built = [QMC.CDT320.Recipes.RecipeMapBuildService]::ImportBaseAndBuildAll($project, $radPath)
Assert-True $built.Success ('RAD Base build: ' + $built.Message)
Assert-True ($project.MapApprovalVersion -eq 1) 'managed approval version enabled'
Assert-True ([string]::IsNullOrWhiteSpace($project.InputMapApprovalHash)) 'Input pending after import'
Assert-True ([string]::IsNullOrWhiteSpace($project.GoodBinMapApprovalHash)) 'Good pending after import'
Assert-True ([string]::IsNullOrWhiteSpace($project.NgBinMapApprovalHash)) 'NG pending after import'

$baseMap = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($built.BaseMapPath)
$inputMap = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($built.InputMapPath)
$goodMap = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($built.GoodMapPath)
$ngMap = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($built.NgMapPath)
foreach ($map in @($baseMap, $inputMap, $goodMap, $ngMap)) {
    Assert-MapRoundTrip $radMap $map 'Built map'
    Assert-Near $map.DieSizeX 8.0 'Recipe die width propagated'
    Assert-Near $map.DieSizeY 6.0 'Recipe die height propagated'
}
$inputFirst = Get-ByRaw $inputMap 200 200
Assert-Near $inputFirst.PosX 138.04 'Input role X is center-relative'
Assert-Near $inputFirst.PosY -24.48 'Input role Y is center-relative'
$goodFirst = Get-ByRaw $goodMap 200 200
$ngFirst = Get-ByRaw $ngMap 200 200
Assert-Near $goodMap.PitchX 8.20 'Good role uses Output Pitch X'
Assert-Near $goodMap.PitchY 6.20 'Good role uses Output Pitch Y'
Assert-Near $goodFirst.PosX 139.40 'Good role X is center-relative with Output pitch'
Assert-Near $goodFirst.PosY -24.80 'Good role Y is center-relative with Output pitch'
Assert-Near $ngFirst.PosX 139.40 'NG role shares Output X pitch projection'
Assert-Near $ngFirst.PosY -24.80 'NG role shares Output Y pitch projection'

# 4) Managed maps are blocked until each role is FINAL APPLY approved.
$sourcePath = ''
$reason = ''
$resolved = [QMC.CDT320.Recipes.RecipeDieMapResolver]::LoadCompatibleMap(
    $project, [QMC.CDT320.Recipes.RecipeMapKind]::Input, [ref]$sourcePath, [ref]$reason)
Assert-True ($null -eq $resolved) 'Input resolver blocked before approval'
Assert-True ($reason -like '*approval*' -or $reason -like '*FINAL APPLY*') ('Input block reason: ' + $reason)

$inputApply = [QMC.CDT320.Recipes.RecipeMapBuildService]::SaveRoleTargetMask(
    $project, [QMC.CDT320.Recipes.RecipeMapKind]::Input, $inputMap)
Assert-True $inputApply.Success ('Input FINAL APPLY: ' + $inputApply.Message)
$resolved = [QMC.CDT320.Recipes.RecipeDieMapResolver]::LoadCompatibleMap(
    $project, [QMC.CDT320.Recipes.RecipeMapKind]::Input, [ref]$sourcePath, [ref]$reason)
Assert-True ($null -ne $resolved) ('Input resolver after approval: ' + $reason)

$goodApply = [QMC.CDT320.Recipes.RecipeMapBuildService]::SaveRoleTargetMask(
    $project, [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin, $goodMap)
Assert-True $goodApply.Success ('Good FINAL APPLY: ' + $goodApply.Message)
$ngApply = [QMC.CDT320.Recipes.RecipeMapBuildService]::SaveRoleTargetMask(
    $project, [QMC.CDT320.Recipes.RecipeMapKind]::NgBin, $ngMap)
Assert-True $ngApply.Success ('NG FINAL APPLY: ' + $ngApply.Message)

foreach ($kind in @(
    [QMC.CDT320.Recipes.RecipeMapKind]::Input,
    [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin,
    [QMC.CDT320.Recipes.RecipeMapKind]::NgBin)) {
    $sourcePath = ''
    $reason = ''
    $resolved = [QMC.CDT320.Recipes.RecipeDieMapResolver]::LoadCompatibleMap($project, $kind, [ref]$sourcePath, [ref]$reason)
    Assert-True ($null -ne $resolved) ("$kind resolver after FINAL APPLY: $reason")
}

# 5) Input-only rebuild invalidates only Input approval and uses changed Input role pitch.
$goodHashBefore = $project.GoodBinMapApprovalHash
$ngHashBefore = $project.NgBinMapApprovalHash
$project.InputFrame.PitchX = 8.13
$project.InputFrame.PitchY = 6.13
$rebuilt = [QMC.CDT320.Recipes.RecipeMapBuildService]::RebuildDerivedMaps($project, $true, $false, $true)
Assert-True $rebuilt.Success ('Input-only rebuild: ' + $rebuilt.Message)
Assert-True ([string]::IsNullOrWhiteSpace($project.InputMapApprovalHash)) 'Input invalidated after Input rebuild'
Assert-True ($project.GoodBinMapApprovalHash -eq $goodHashBefore) 'Good approval retained after Input rebuild'
Assert-True ($project.NgBinMapApprovalHash -eq $ngHashBefore) 'NG approval retained after Input rebuild'
$rebuiltInput = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuilt.InputMapPath)
Assert-Near $rebuiltInput.PitchX 8.13 'rebuilt Input role pitch X'
Assert-Near $rebuiltInput.PitchY 6.13 'rebuilt Input role pitch Y'
Assert-Near (Get-ByRaw $rebuiltInput 200 200).PosX 138.21 'rebuilt Input relative X'
Assert-Near (Get-ByRaw $rebuiltInput 200 200).PosY -24.52 'rebuilt Input relative Y'

$sourcePath = ''
$reason = ''
$resolved = [QMC.CDT320.Recipes.RecipeDieMapResolver]::LoadCompatibleMap(
    $project, [QMC.CDT320.Recipes.RecipeMapKind]::Input, [ref]$sourcePath, [ref]$reason)
Assert-True ($null -eq $resolved) 'Input resolver blocked after pitch rebuild until reapproval'
$resolved = [QMC.CDT320.Recipes.RecipeDieMapResolver]::LoadCompatibleMap(
    $project, [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin, [ref]$sourcePath, [ref]$reason)
Assert-True ($null -ne $resolved) ('Good remains approved after Input rebuild: ' + $reason)

# 6) Parser rejects count mismatch and duplicate raw addresses using temp files only.
$badCountPath = Join-Path $testRoot 'bad-count.txt'
[IO.File]::WriteAllText($badCountPath, "/08120/06120/%2/&2/`r`nX= 0001 Y= 0001 B= 001`r`n")
$threw = $false
try { [void][QMC.CDT320.DieMaps.DieMapGenerator]::LoadWaferMapTextOrThrow($badCountPath) } catch { $threw = $true }
Assert-True $threw 'RAD declared count mismatch rejected'

$duplicatePath = Join-Path $testRoot 'duplicate.txt'
[IO.File]::WriteAllText($duplicatePath, "/08120/06120/%2/&2/`r`nX= 0001 Y= 0001 B= 001`r`nX= 0001 Y= 0001 B= 001`r`n")
$threw = $false
try { [void][QMC.CDT320.DieMaps.DieMapGenerator]::LoadWaferMapTextOrThrow($duplicatePath) } catch { $threw = $true }
Assert-True $threw 'RAD duplicate raw address rejected'

# 7) Real source file must be byte-identical after the whole test.
$radHashAfter = (Get-FileHash $radPath -Algorithm SHA256).Hash
Assert-True ($radHashAfter -eq $radHashBefore) 'RAD1 source was not modified'

Write-Output ('PASS root=' + $testRoot)
Write-Output ('RAD_SHA256=' + $radHashAfter)
