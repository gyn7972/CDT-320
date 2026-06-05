#!/usr/bin/env pwsh
# verify_stage62.ps1 — Vision 알고리즘별 카메라 설정 (Stage 62)

param([string]$Root = "D:\Work\CDT-320\QMC.CDT-320")

$ErrorActionPreference = 'Stop'
$rows = @()

function Add-Row([string]$cat, [string]$item, [bool]$pass, [string]$detail = "") {
    $script:rows += [PSCustomObject]@{ Category = $cat; Item = $item; Result = if ($pass) { "PASS" } else { "FAIL" }; Detail = $detail }
}

function Test-Greps([string]$file, [string[]]$patterns) {
    if (-not (Test-Path $file)) { return $false }
    $content = Get-Content -Raw $file -EA SilentlyContinue
    if (-not $content) { return $false }
    foreach ($p in $patterns) { if ($content -notmatch $p) { return $false } }
    return $true
}

# 1. QMC.Common 으로 알람 이동
$alarmMgr = Join-Path $Root "QMC.Common\Alarms\AlarmManager.cs"
Add-Row "S62-E" "AlarmManager moved to QMC.Common.Alarms" (Test-Greps $alarmMgr @('namespace QMC\.Common\.Alarms', 'LanguageProvider')) $alarmMgr

$alarmMaster = Join-Path $Root "QMC.Common\Alarms\AlarmMaster.cs"
Add-Row "S62-E" "AlarmMaster has VISION-MAPMISS/PARAMFAIL/CAMOPEN" (Test-Greps $alarmMaster @('VISION-MAPMISS', 'VISION-PARAMFAIL', 'VISION-CAMOPEN')) $alarmMaster

# 2. AlgorithmCameraSubset in QMC.Common
$subset = Join-Path $Root "QMC.Common\Recipes\AlgorithmCameraSubset.cs"
Add-Row "S62-G" "AlgorithmCameraSubset model in QMC.Common.Recipes" (Test-Greps $subset @('namespace QMC\.Common\.Recipes', 'class AlgorithmCameraSubset', 'class AlgorithmCameraMapping', 'class VisionAlgorithm|static class VisionAlgorithm')) $subset

# 3. ROI fields
Add-Row "S62-A" "AlgorithmCameraMapping has ROI 4 fields" (Test-Greps $subset @('RoiOffsetX', 'RoiOffsetY', 'RoiWidth', 'RoiHeight', 'IsRoiFull', 'ToRectangle')) $subset

# 4. Vision Form1 alarm wiring
$visionForm = Join-Path $Root "QMC.Vision\Form1.cs"
Add-Row "S62-D" "Vision Form1 raises VISION-MAPMISS/CAMOPEN/PARAMFAIL" (Test-Greps $visionForm @('VISION-MAPMISS', 'VISION-CAMOPEN', 'VISION-PARAMFAIL', 'using QMC\.Common\.Alarms')) $visionForm

# 5. Binder ROI + TryApplyParameters
$binder = Join-Path $Root "QMC.Vision\Config\AlgorithmCameraBinder.cs"
Add-Row "S62-B" "Binder TryApplyParameters + ROI" (Test-Greps $binder @('TryApplyParameters', 'IsRoiFull', 'ToRectangle')) $binder

# 6. CameraMappingPanel ROI / Reset / Cancel / Validate
$panel = Join-Path $Root "QMC.Vision\Ui\Pages\CameraMappingPanel.cs"
Add-Row "S62-C" "CameraMappingPanel ROI + Reset/Cancel/Validate" (Test-Greps $panel @('_numRoiX', '_numRoiY', '_numRoiW', '_numRoiH', '_btnReset', '_btnCancel', 'Validate\(')) $panel

# 7. RecipeProject VisionCameras
$recipe = Join-Path $Root "QMC.CDT-320\Equipment\Recipes\RecipeStore.cs"
Add-Row "S62-G" "RecipeProject.VisionCameras (per-project)" (Test-Greps $recipe @('AlgorithmCameraSubset', 'VisionCameras', 'using QMC\.Common\.Recipes')) $recipe

# 8. QMC.Common.csproj has new files
$commonProj = Join-Path $Root "QMC.Common\QMC.Common.csproj"
Add-Row "S62-E" "QMC.Common.csproj registers Alarms + Recipes" (Test-Greps $commonProj @('Alarms\\AlarmManager\.cs', 'Recipes\\AlgorithmCameraSubset\.cs', 'System\.Drawing', 'System\.Runtime\.Serialization')) $commonProj

# 9. Vision csproj references Common
$visionProj = Join-Path $Root "QMC.Vision\QMC.Vision.csproj"
Add-Row "S62-E" "QMC.Vision.csproj references QMC.Common" (Test-Greps $visionProj @('QMC\.Common\.csproj')) $visionProj

# 10. Handler csproj no longer compiles old Alarm files
Add-Row "S62-E" "Handler csproj removed old Alarm Compile entries" (-not (Test-Greps (Join-Path $Root "QMC.CDT-320\QMC.CDT-320.csproj") @('Equipment\\Alarms\\AlarmManager\.cs'))) "csproj"

# 11. Handler Form1 sets LanguageProvider
Add-Row "S62-E" "Handler Form1 sets AlarmManager.LanguageProvider" (Test-Greps (Join-Path $Root "QMC.CDT-320\Form1.cs") @('AlarmManager\.LanguageProvider')) "Form1"

# ── 출력 ──
$bar = "=" * 110
Write-Output $bar
Write-Output "Stage 62 — Vision Algorithm-Camera Config — verify"
Write-Output $bar
Write-Output ("{0,-10} {1,-55} {2,-6} {3}" -f "CATEGORY", "ITEM", "RESULT", "DETAIL")
Write-Output ("-" * 110)

$pass = 0; $fail = 0
foreach ($r in $rows) {
    Write-Output ("{0,-10} {1,-55} {2,-6} {3}" -f $r.Category, $r.Item, $r.Result, $r.Detail)
    if ($r.Result -eq "PASS") { $pass++ } else { $fail++ }
}
Write-Output ("-" * 110)
Write-Output ("TOTAL {0}   PASS {1}   FAIL {2}" -f $rows.Count, $pass, $fail)
Write-Output $bar

if ($fail -gt 0) { exit 1 } else { exit 0 }
