#!/usr/bin/env pwsh
# verify_stage64.ps1 - per-inspection camera parameter override (Stage 64)

param([string]$Root = "D:\Work\CDT-320\QMC.CDT-320")

$ErrorActionPreference = 'Stop'
$rows = @()
function Add-Row([string]$cat,[string]$item,[bool]$pass,[string]$detail=""){ $script:rows += [PSCustomObject]@{Category=$cat;Item=$item;Result=if($pass){"PASS"}else{"FAIL"};Detail=$detail} }
function Test-Greps([string]$file,[string[]]$patterns){ if(-not(Test-Path $file)){return $false}; $c=Get-Content -Raw $file -EA SilentlyContinue; if(-not $c){return $false}; foreach($p in $patterns){ if($c -notmatch $p){return $false} }; return $true }

# A. data model
$subset = Join-Path $Root "QMC.Common\Recipes\AlgorithmCameraSubset.cs"
Add-Row "S64-A" "InspectionCameraOverride class + helpers" (Test-Greps $subset @('class InspectionCameraOverride','bool IsEmpty\(\)','ApplyOver\(','HasRoiOverride')) $subset
Add-Row "S64-A" "AlgorithmCameraMapping.Inspections + EffectiveFor/GetOrCreate" (Test-Greps $subset @('List<InspectionCameraOverride>\s+Inspections','EffectiveFor\(','GetOrCreateOverride\(')) $subset
Add-Row "S64-A" "InspectionLabel dict + InspectionsOf" (Test-Greps $subset @('class InspectionLabel','InspectionsOf\(','FrontSide\|TopSurfaceInspector','RearSide\|BottomSurfaceInspector')) $subset
Add-Row "S64-A" "EmitDefaultValue=false on override fields" (Test-Greps $subset @('EmitDefaultValue = false\)\] public double\? ExposureUs')) $subset

# B. SettingsPage 3-level tree
$sp = Join-Path $Root "QMC.Vision\Ui\Pages\SettingsPage.cs"
Add-Row "S64-B" "TreeView 3-level cam:alg:insp + routing + marker" (Test-Greps $sp @('InspectionLabel.InspectionsOf','ShowInspectionOverride','InspNodeText','_inspPanel')) $sp

# C. InspectionOverridePanel
$ip = Join-Path $Root "QMC.Vision\Ui\Pages\InspectionOverridePanel.cs"
Add-Row "S64-C" "InspectionOverridePanel 7 checkbox fields + ROI group" (Test-Greps $ip @('class InspectionOverridePanel','_ckExposure','_ckRoi','SelectInspection\(','TestGrab')) $ip

# D. persistence
$store = Join-Path $Root "QMC.Vision\Config\AlgorithmCameraMap.cs"
Add-Row "S64-D" "Store.Save prunes empty overrides" (Test-Greps $store @('PruneEmptyOverrides','IsEmpty\(\)')) $store

# csproj
$csproj = Join-Path $Root "QMC.Vision\QMC.Vision.csproj"
Add-Row "S64-C" "csproj registers InspectionOverridePanel" (Test-Greps $csproj @('Ui\\Pages\\InspectionOverridePanel.cs')) $csproj

$bar = "=" * 100
Write-Output $bar
Write-Output "Stage 64 - Per-inspection camera override - verify"
Write-Output $bar
Write-Output ("{0,-8} {1,-58} {2,-6} {3}" -f "CAT","ITEM","RESULT","DETAIL")
Write-Output ("-"*100)
$pass=0;$fail=0
foreach($r in $rows){ Write-Output ("{0,-8} {1,-58} {2,-6} {3}" -f $r.Category,$r.Item,$r.Result,$r.Detail); if($r.Result -eq "PASS"){$pass++}else{$fail++} }
Write-Output ("-"*100)
Write-Output ("TOTAL {0}  PASS {1}  FAIL {2}" -f $rows.Count,$pass,$fail)
Write-Output $bar
if($fail -gt 0){exit 1}else{exit 0}
