param([string]$AssemblyDirectory = '')

$ErrorActionPreference = 'Stop'

$assemblyDir = if ([string]::IsNullOrWhiteSpace($AssemblyDirectory)) {
    Join-Path $PSScriptRoot 'recipe-map-final'
} else {
    $AssemblyDirectory
}
[void][Reflection.Assembly]::LoadFrom((Join-Path $assemblyDir 'QMC.Common.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $assemblyDir 'QMC.CDT-320.exe'))

$testRoot = Join-Path $PSScriptRoot ('recipe-map-test-data-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testRoot -Force)
[QMC.Common.Data.Store.DataPaths]::Root = $testRoot

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) {
        throw "ASSERT FAILED: $message"
    }
}

function New-MapEntry([int]$index, [int]$x, [int]$y, [bool]$target, [string]$uid) {
    $entry = New-Object QMC.CDT320.DieMaps.DieMapEntry
    $entry.Index = $index
    $entry.DieMapX = $x
    $entry.DieMapY = $y
    $entry.OriginalMapX = $x
    $entry.OriginalMapY = $y
    $entry.IsTarget = $target
    $entry.BinCode = $(if ($target) { 1 } else { 0 })
    $entry.DieUid = $uid
    return $entry
}

function Get-AddressFingerprint($map) {
    return (($map.Entries | ForEach-Object { "$($_.OriginalMapX),$($_.OriginalMapY)" } | Sort-Object -Unique) -join '|')
}

function Get-EntryByAddress($map, [int]$x, [int]$y) {
    return $map.Entries | Where-Object { $_.OriginalMapX -eq $x -and $_.OriginalMapY -eq $y } | Select-Object -First 1
}

function Assert-Near([double]$actual, [double]$expected, [string]$message) {
    Assert-True ([Math]::Abs($actual - $expected) -le 0.000001) "$message actual=$actual expected=$expected"
}

# 1) Legacy JSON without OriginalMapX/Y must retain DieMapX/Y.
$legacyJsonPath = Join-Path $testRoot 'legacy-no-original.json'
$legacyJson = '{"DieMapX":3,"DieMapY":1,"PitchX":1,"PitchY":1,"Entries":[{"Index":0,"DieMapX":1,"DieMapY":0,"IsTarget":true},{"Index":1,"DieMapX":2,"DieMapY":0,"IsTarget":true}]}'
[IO.File]::WriteAllText($legacyJsonPath, $legacyJson)
$legacyMap = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($legacyJsonPath)
Assert-True ($null -ne $legacyMap) 'Legacy JSON load'
Assert-True ((Get-AddressFingerprint $legacyMap) -eq '1,0|2,0') 'Legacy JSON Original address fallback'

# 2) CSV round-trip must preserve Original addresses and quoted UID.
$csvRoundTripMap = New-Object QMC.CDT320.DieMaps.DieMap
$csvRoundTripMap.FrameObjId = "CSV,FRAME`r`nTWO"
$csvRoundTripMap.DieMapX = 3
$csvRoundTripMap.DieMapY = 1
$csvRoundTripMap.PitchX = 1.25
$csvRoundTripMap.PitchY = 2.5
$csvRoundTripMap.DieSizeX = 1.0
$csvRoundTripMap.DieSizeY = 2.0
$csvRoundTripMap.Entries.Add((New-MapEntry 0 1 0 $true "UID,WITH,COMMA`r`nTWO"))
$csvRoundTripPath = Join-Path $testRoot 'roundtrip.csv'
[QMC.CDT320.DieMaps.DieMapGenerator]::SaveCsv($csvRoundTripMap, $csvRoundTripPath)
$csvRoundTripLoaded = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadCsv($csvRoundTripPath)
Assert-True ($csvRoundTripLoaded.FrameObjId -eq "CSV,FRAME`r`nTWO") 'CSV escaped multiline FrameObjId'
Assert-True ($csvRoundTripLoaded.Entries[0].DieUid -eq "UID,WITH,COMMA`r`nTWO") 'CSV escaped multiline DieUid'

# 3) Import one sparse Base CSV and derive all roles from one Original address domain.
$sourceMap = New-Object QMC.CDT320.DieMaps.DieMap
$sourceMap.FrameObjId = 'RAW-SOURCE'
$sourceMap.DieMapX = 201
$sourceMap.DieMapY = 228
$sourceMap.PitchX = 1.0
$sourceMap.PitchY = 1.0
$sourceMap.DieSizeX = 1.0
$sourceMap.DieSizeY = 1.0
$sourceMap.OuterDiameterMm = 300.0
$sourceMap.EdgeSkipMode = 'ExternalMap'
$sourceMap.Entries.Add((New-MapEntry 0 166 181 $true 'RAW-1'))
$sourceMap.Entries.Add((New-MapEntry 1 167 181 $false 'RAW-2'))
$sourceMap.Entries.Add((New-MapEntry 2 200 227 $true 'RAW-3'))
$sourcePath = Join-Path $testRoot 'source.csv'
[QMC.CDT320.DieMaps.DieMapGenerator]::SaveCsv($sourceMap, $sourcePath)

$project = New-Object QMC.CDT320.Recipes.RecipeProject
$project.FileName = 'CodexMapHarness'
[QMC.CDT320.Recipes.RecipeProjectConsistencyService]::EnsureStructure($project) | Out-Null
$project.Die.DieSpecName = 'HARNESS-DIE'
$project.Die.WidthMm = 1.2
$project.Die.HeightMm = 2.4
$project.Die.ThicknessMm = 0.15
$project.InputFrame.PitchX = 2.0
$project.InputFrame.PitchY = 3.0
$project.OutputFrame.PitchX = 4.0
$project.OutputFrame.PitchY = 5.0

$importResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::ImportBaseAndBuildAll($project, $sourcePath)
Assert-True $importResult.Success ('Import result: ' + $importResult.Message)

$baseMap = [QMC.CDT320.DieMaps.DieMapGenerator]::Load($importResult.BaseMapPath)
$inputMap = [QMC.CDT320.DieMaps.DieMapGenerator]::Load($importResult.InputMapPath)
$goodMap = [QMC.CDT320.DieMaps.DieMapGenerator]::Load($importResult.GoodMapPath)
$ngMap = [QMC.CDT320.DieMaps.DieMapGenerator]::Load($importResult.NgMapPath)
$baseFingerprint = Get-AddressFingerprint $baseMap
Assert-True ($baseFingerprint -eq (Get-AddressFingerprint $inputMap)) 'Input address domain equals Base'
Assert-True ($baseFingerprint -eq (Get-AddressFingerprint $goodMap)) 'Good address domain equals Base'
Assert-True ($baseFingerprint -eq (Get-AddressFingerprint $ngMap)) 'NG address domain equals Base'

$inputFirst = Get-EntryByAddress $inputMap 166 181
$goodFirst = Get-EntryByAddress $goodMap 166 181
Assert-Near $inputFirst.PosX -34.0 'Input X role pitch projection'
Assert-Near $inputFirst.PosY -69.0 'Input Y role pitch projection'
Assert-Near $goodFirst.PosX -68.0 'Output X role pitch projection'
Assert-Near $goodFirst.PosY -115.0 'Output Y role pitch projection'
Assert-Near $baseMap.DieSizeX 1.2 'Base canonical DieSizeX'
Assert-Near $goodMap.DieSizeY 2.4 'Derived canonical DieSizeY'

$canonicalBaseCsv = [IO.Path]::ChangeExtension($importResult.BaseMapPath, '.csv')
$canonicalBaseCsvMap = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadCsv($canonicalBaseCsv)
Assert-True ($canonicalBaseCsvMap.FrameObjId -like '*-BASE-WAFER-MAP') 'Canonical Base CSV was not overwritten by raw CSV'
$sourceSidecar = Join-Path ([IO.Path]::GetDirectoryName($importResult.BaseMapPath)) (([IO.Path]::GetFileNameWithoutExtension($importResult.BaseMapPath)) + '.source.csv')
Assert-True (Test-Path $sourceSidecar) 'Raw CSV source sidecar uses .source.csv'

# 4) Role masks stay independent when pitch/Die size changes.
$goodEdited = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($importResult.GoodMapPath)
$goodEditedEntry = Get-EntryByAddress $goodEdited 166 181
$goodEditedEntry.IsTarget = $false
$goodEditedEntry.BinCode = 0
[QMC.CDT320.DieMaps.DieMapGenerator]::SaveJson($goodEdited, $importResult.GoodMapPath)
[QMC.CDT320.DieMaps.DieMapGenerator]::SaveCsv($goodEdited, [IO.Path]::ChangeExtension($importResult.GoodMapPath, '.csv'))

$project.Die.WidthMm = 1.25
$project.Die.HeightMm = 2.5
$project.OutputFrame.PitchX = 6.0
$project.OutputFrame.PitchY = 7.0
$rebuildResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::RebuildDerivedMaps($project, $true, $true, $true)
Assert-True $rebuildResult.Success ('Rebuild result: ' + $rebuildResult.Message)

$rebuiltBase = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuildResult.BaseMapPath)
$rebuiltInput = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuildResult.InputMapPath)
$rebuiltGood = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuildResult.GoodMapPath)
$rebuiltNg = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuildResult.NgMapPath)
Assert-True (-not (Get-EntryByAddress $rebuiltGood 166 181).IsTarget) 'Good mask preserved'
Assert-True ((Get-EntryByAddress $rebuiltNg 166 181).IsTarget) 'NG mask remains independent'
Assert-True ((Get-EntryByAddress $rebuiltInput 167 181).IsTarget -eq $false) 'Input mask preserved'
Assert-Near $rebuiltBase.DieSizeX 1.25 'Rebuilt Base DieSizeX persisted'
Assert-Near $rebuiltGood.DieSizeY 2.5 'Rebuilt role DieSizeY persisted'
$rebuiltGoodFirst = Get-EntryByAddress $rebuiltGood 166 181
Assert-Near $rebuiltGoodFirst.PosX -102.0 'Rebuilt Output X pitch'
Assert-Near $rebuiltGoodFirst.PosY -161.0 'Rebuilt Output Y pitch'

# 5) Missing configured JSON must use its valid CSV sidecar without losing the role mask.
[IO.File]::Delete($rebuildResult.GoodMapPath)
$sidecarRebuild = [QMC.CDT320.Recipes.RecipeMapBuildService]::RebuildDerivedMaps($project, $true, $true, $true)
Assert-True $sidecarRebuild.Success ('CSV sidecar rebuild result: ' + $sidecarRebuild.Message)
$sidecarGood = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($sidecarRebuild.GoodMapPath)
Assert-True (-not (Get-EntryByAddress $sidecarGood 166 181).IsTarget) 'Good mask preserved from CSV sidecar'

# 6) Project commit callback failure must roll back already replaced map files.
$beforeProjectFailBase = [Convert]::ToBase64String([IO.File]::ReadAllBytes($sidecarRebuild.BaseMapPath))
$beforeProjectFailGood = [Convert]::ToBase64String([IO.File]::ReadAllBytes($sidecarRebuild.GoodMapPath))
$rejectProjectSave = [Func[QMC.CDT320.Recipes.RecipeProject,bool]] { param($value) return $false }
$project.OutputFrame.PitchX = 7.5
$projectFailResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::RebuildDerivedMaps($project, $true, $true, $true, $rejectProjectSave)
Assert-True (-not $projectFailResult.Success) 'Rejected Project commit must fail map transaction'
Assert-True ($beforeProjectFailBase -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($sidecarRebuild.BaseMapPath))) 'Base rollback after Project failure'
Assert-True ($beforeProjectFailGood -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($sidecarRebuild.GoodMapPath))) 'Good rollback after Project failure'

# 7) Lock a late transaction file: earlier replaced files must roll back byte-for-byte.
$beforeBase = [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.BaseMapPath))
$beforeInput = [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.InputMapPath))
$beforeGood = [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.GoodMapPath))
$goodCsvPath = [IO.Path]::ChangeExtension($rebuildResult.GoodMapPath, '.csv')
$lockStream = [IO.File]::Open($goodCsvPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
try {
    $project.OutputFrame.PitchX = 8.0
    $failedResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::RebuildDerivedMaps($project, $true, $true, $true)
    Assert-True (-not $failedResult.Success) 'Locked transaction must fail'
}
finally {
    $lockStream.Dispose()
}
Assert-True ($beforeBase -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.BaseMapPath))) 'Base rollback byte equality'
Assert-True ($beforeInput -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.InputMapPath))) 'Input rollback byte equality'
Assert-True ($beforeGood -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.GoodMapPath))) 'Good rollback byte equality'

# 8) Duplicate OriginalMap address must fail instead of silently dropping one entry.
$duplicateMap = New-Object QMC.CDT320.DieMaps.DieMap
$duplicateMap.FrameObjId = 'DUPLICATE-SOURCE'
$duplicateMap.DieMapX = 2
$duplicateMap.DieMapY = 1
$duplicateMap.PitchX = 1
$duplicateMap.PitchY = 1
$duplicateMap.DieSizeX = 1
$duplicateMap.DieSizeY = 1
$duplicateMap.OuterDiameterMm = 200
$duplicateMap.Entries.Add((New-MapEntry 0 1 0 $true 'DUP-A'))
$duplicateMap.Entries.Add((New-MapEntry 1 1 0 $false 'DUP-B'))
$duplicatePath = Join-Path $testRoot 'duplicate.json'
[QMC.CDT320.DieMaps.DieMapGenerator]::SaveJson($duplicateMap, $duplicatePath)
$duplicateProject = New-Object QMC.CDT320.Recipes.RecipeProject
$duplicateProject.FileName = 'DuplicateGuard'
[QMC.CDT320.Recipes.RecipeProjectConsistencyService]::EnsureStructure($duplicateProject) | Out-Null
$duplicateResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::ImportBaseAndBuildAll($duplicateProject, $duplicatePath)
Assert-True (-not $duplicateResult.Success) 'Duplicate OriginalMap address must be rejected'

# 9) Map editor save may change only one role Target/Skip mask and never paths/address domains.
$project.OutputFrame.PitchX = 6.0
$project.OutputFrame.PitchY = 7.0
$beforeInputMaskSave = [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.InputMapPath))
$beforeNgMaskSave = [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.NgMapPath))
$beforeInputConfigured = $project.InputDieMapFileName
$beforeGoodConfigured = $project.GoodBinDieMapFileName
$beforeNgConfigured = $project.NgBinDieMapFileName
$maskEdited = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuildResult.GoodMapPath)
(Get-EntryByAddress $maskEdited 200 227).IsTarget = $false
$maskSaveResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::SaveRoleTargetMask(
    $project,
    [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin,
    $maskEdited)
Assert-True $maskSaveResult.Success ('Mask save result: ' + $maskSaveResult.Message)
$maskSavedGood = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuildResult.GoodMapPath)
Assert-True (-not (Get-EntryByAddress $maskSavedGood 200 227).IsTarget) 'Good Target/Skip mask changed'
Assert-True ($beforeInputConfigured -eq $project.InputDieMapFileName) 'Input configured path unchanged'
Assert-True ($beforeGoodConfigured -eq $project.GoodBinDieMapFileName) 'Good configured path unchanged'
Assert-True ($beforeNgConfigured -eq $project.NgBinDieMapFileName) 'NG configured path unchanged'
Assert-True ($beforeInputMaskSave -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.InputMapPath))) 'Input map untouched by Good mask save'
Assert-True ($beforeNgMaskSave -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.NgMapPath))) 'NG map untouched by Good mask save'
Assert-True ((Get-AddressFingerprint $maskSavedGood) -eq (Get-AddressFingerprint $rebuiltBase)) 'Good mask save retains Base address domain'

$beforeBadMaskSave = [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.GoodMapPath))
$badMask = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($rebuildResult.GoodMapPath)
$badMask.Entries.RemoveAt(0)
$badMaskResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::SaveRoleTargetMask(
    $project,
    [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin,
    $badMask)
Assert-True (-not $badMaskResult.Success) 'Address-domain mismatch mask save must fail'
Assert-True ($beforeBadMaskSave -eq [Convert]::ToBase64String([IO.File]::ReadAllBytes($rebuildResult.GoodMapPath))) 'Rejected mask save leaves Good map unchanged'

# 10) Base import must reject invalid role pitch instead of using a silent fallback.
$invalidPitchProject = New-Object QMC.CDT320.Recipes.RecipeProject
$invalidPitchProject.FileName = 'InvalidPitchGuard'
[QMC.CDT320.Recipes.RecipeProjectConsistencyService]::EnsureStructure($invalidPitchProject) | Out-Null
$invalidPitchProject.InputFrame.PitchX = 0.0
$invalidPitchResult = [QMC.CDT320.Recipes.RecipeMapBuildService]::ImportBaseAndBuildAll($invalidPitchProject, $sourcePath)
Assert-True (-not $invalidPitchResult.Success) 'Invalid Input Pitch must reject Base import'

Write-Output ('PASS root=' + $testRoot)
