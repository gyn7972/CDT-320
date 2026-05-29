#!/usr/bin/env pwsh
# verify_stage67.ps1 - LFine light controller (Stage 67)

param([string]$Root = "D:\Work\CDT-320\QMC.CDT-320")

$ErrorActionPreference = 'Stop'
$rows = @()
function Add-Row([string]$cat,[string]$item,[bool]$pass,[string]$detail=""){ $script:rows += [PSCustomObject]@{Category=$cat;Item=$item;Result=if($pass){"PASS"}else{"FAIL"};Detail=$detail} }
function Test-Greps([string]$file,[string[]]$patterns){ if(-not(Test-Path $file)){return $false}; $c=Get-Content -Raw $file -EA SilentlyContinue; if(-not $c){return $false}; foreach($p in $patterns){ if($c -notmatch $p){return $false} }; return $true }

# A. interface / sim / factory
$ifc = Join-Path $Root "QMC.Vision\Optics\ILightController.cs"
Add-Row "S67-A" "ILightController 6 methods" (Test-Greps $ifc @('ConnectAsync','SetPowerAsync','SetStrobeTimeAsync','SetOnOffAsync','GetPowerAsync','CheckPowerOnAsync')) $ifc
$sim = Join-Path $Root "QMC.Vision\Optics\Sim\SimLightController.cs"
Add-Row "S67-A" "SimLightController (cache, IsConnected=true)" (Test-Greps $sim @('class SimLightController','IsConnected')) $sim
$fac = Join-Path $Root "QMC.Vision\Optics\LightControllerFactory.cs"
Add-Row "S67-A" "LightControllerFactory.Create(cfg, useSim)" (Test-Greps $fac @('class LightControllerFactory','Create\(.*useSim')) $fac

# B. config (2 controllers)
$cfg = Join-Path $Root "QMC.Vision\Optics\LFine\LFineLightConfig.cs"
Add-Row "S67-B" "LFineLightConfig + LFineChannel + Setup(2 controllers)" (Test-Greps $cfg @('class LFineLightConfig','class LFineChannel','class LFineLightSetup','CreateDefault')) $cfg
Add-Row "S67-B" "Store lfine_light.json" (Test-Greps $cfg @('lfine_light.json','class LFineLightConfigStore')) $cfg

# C. protocol
$proto = Join-Path $Root "QMC.Vision\Optics\LFine\LFineProtocol.cs"
Add-Row "S67-C" "Protocol Stx=0x02 Etx=0x03 + Build commands" (Test-Greps $proto @('Stx = 0x02','Etx = 0x03','BuildPowerCommand','BuildStrobeTimeCommand','WrapFrame')) $proto

# D. real controller
$ctrl = Join-Path $Root "QMC.Vision\Optics\LFine\LFineLightController.cs"
Add-Row "S67-D" "LFineLightController SerialPort + alarms" (Test-Greps $ctrl @('class LFineLightController','SerialPort','LIGHT-OPEN-FAIL','LIGHT-PWR-RANGE','LIGHT-TX-FAIL')) $ctrl

# E. alarms in AlarmMaster
$am = Join-Path $Root "QMC.Common\Alarms\AlarmMaster.cs"
Add-Row "S67-E" "AlarmMaster 6 LIGHT-* codes" (Test-Greps $am @('LIGHT-OPEN-FAIL','LIGHT-TIMEOUT','LIGHT-NAK','LIGHT-INVALID-RESP','LIGHT-TX-FAIL','LIGHT-PWR-RANGE')) $am

# csproj
$csproj = Join-Path $Root "QMC.Vision\QMC.Vision.csproj"
Add-Row "S67-H" "csproj System.IO.Ports + Optics compiles" (Test-Greps $csproj @('System.IO.Ports','Optics\\ILightController.cs','Optics\\LFine\\LFineLightController.cs')) $csproj

$bar = "=" * 100
Write-Output $bar
Write-Output "Stage 67 - LFine light controller - verify"
Write-Output $bar
Write-Output ("{0,-8} {1,-56} {2,-6} {3}" -f "CAT","ITEM","RESULT","DETAIL")
Write-Output ("-"*100)
$pass=0;$fail=0
foreach($r in $rows){ Write-Output ("{0,-8} {1,-56} {2,-6} {3}" -f $r.Category,$r.Item,$r.Result,$r.Detail); if($r.Result -eq "PASS"){$pass++}else{$fail++} }
Write-Output ("-"*100)
Write-Output ("TOTAL {0}  PASS {1}  FAIL {2}" -f $rows.Count,$pass,$fail)
Write-Output $bar
if($fail -gt 0){exit 1}else{exit 0}
