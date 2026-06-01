#!/usr/bin/env pwsh
# verify_stage69.ps1 - per-inspection light mapping (Stage 69)

param([string]$Root = "D:\Work\CDT-320\QMC.CDT-320")

$ErrorActionPreference = 'Stop'
$rows = @()
function Add-Row([string]$cat,[string]$item,[bool]$pass,[string]$detail=""){ $script:rows += [PSCustomObject]@{Category=$cat;Item=$item;Result=if($pass){"PASS"}else{"FAIL"};Detail=$detail} }
function Test-Greps([string]$file,[string[]]$patterns){ if(-not(Test-Path $file)){return $false}; $c=Get-Content -Raw $file -EA SilentlyContinue; if(-not $c){return $false}; foreach($p in $patterns){ if($c -notmatch $p){return $false} }; return $true }

# A. data model
$setup = Join-Path $Root "QMC.Common\Recipes\LightSystemSetup.cs"
Add-Row "S69-A" "LightSystemSetup + Entry + Wiring + RenamePort" (Test-Greps $setup @('class LightSystemSetup','class LightControllerEntry','class AlgorithmLightWiring','RenamePort','Validate')) $setup
$sub = Join-Path $Root "QMC.Common\Recipes\InspectionLightSubset.cs"
Add-Row "S69-A" "InspectionLightSetting/Override + StabilizeDelayMs" (Test-Greps $sub @('class InspectionLightSetting','class InspectionLightOverride','StabilizeDelayMs')) $sub
$acs = Join-Path $Root "QMC.Common\Recipes\AlgorithmCameraSubset.cs"
Add-Row "S69-A" "AlgorithmCameraMapping.InspectionLights (option A)" (Test-Greps $acs @('List<InspectionLightOverride>\s+InspectionLights','GetOrCreateLightOverride')) $acs

# B. migrator
$mig = Join-Path $Root "QMC.Common\Recipes\LightSystemMigrator.cs"
Add-Row "S69-B" "LightSystemMigrator (legacy + heuristic + backup)" (Test-Greps $mig @('MigrateFromLegacy','GuessAlgorithm','BackupLegacy')) $mig

# C. LightHub + SwitchPageAsync
$hub = Join-Path $Root "QMC.Vision\Comm\LightHub.cs"
Add-Row "S69-C" "LightHub Dictionary + Initialize + Get" (Test-Greps $hub @('class LightHub','Initialize\(','Get\(')) $hub
$ifc = Join-Path $Root "QMC.Vision\Optics\ILightController.cs"
Add-Row "S69-C" "ILightController.SwitchPageAsync" (Test-Greps $ifc @('SwitchPageAsync')) $ifc

# D/E. UI
$sp = Join-Path $Root "QMC.Vision\Ui\Pages\SettingsPage.cs"
Add-Row "S69-D" "SettingsPage sys:light + tabs [camera][light]" (Test-Greps $sp @('sys:light','_inspTabs','_lightSetupPage','ShowLightSystemSetup')) $sp
$setupPage = Join-Path $Root "QMC.Vision\Ui\Pages\LightSystemSetupPage.cs"
Add-Row "S69-D" "LightSystemSetupPage (inventory+wiring+rename)" (Test-Greps $setupPage @('class LightSystemSetupPage','RenamePort','Migrate')) $setupPage
$lp = Join-Path $Root "QMC.Vision\Ui\Pages\InspectionLightPanel.cs"
Add-Row "S69-E" "InspectionLightPanel (pool combo + Apply/Save)" (Test-Greps $lp @('class InspectionLightPanel','SelectInspection','LightHub.Get')) $lp

# F. alarms
$am = Join-Path $Root "QMC.Common\Alarms\AlarmMaster.cs"
Add-Row "S69-F" "AlarmMaster 3 new (WIRING-MISS/MAP-INVALID/OUT-OF-POOL)" (Test-Greps $am @('LIGHT-WIRING-MISS','LIGHT-MAP-INVALID','LIGHT-CHANNEL-OUT-OF-POOL')) $am
$vform = Join-Path $Root "QMC.Vision\Form1.cs"
Add-Row "S69-F" "Form1 LightHub.Initialize" (Test-Greps $vform @('LightHub.Initialize','LightSystemSetupStore.Load')) $vform

$bar = "=" * 100
Write-Output $bar
Write-Output "Stage 69 - Per-inspection light mapping - verify"
Write-Output $bar
Write-Output ("{0,-8} {1,-58} {2,-6} {3}" -f "CAT","ITEM","RESULT","DETAIL")
Write-Output ("-"*100)
$pass=0;$fail=0
foreach($r in $rows){ Write-Output ("{0,-8} {1,-58} {2,-6} {3}" -f $r.Category,$r.Item,$r.Result,$r.Detail); if($r.Result -eq "PASS"){$pass++}else{$fail++} }
Write-Output ("-"*100)
Write-Output ("TOTAL {0}  PASS {1}  FAIL {2}" -f $rows.Count,$pass,$fail)
Write-Output $bar
if($fail -gt 0){exit 1}else{exit 0}
