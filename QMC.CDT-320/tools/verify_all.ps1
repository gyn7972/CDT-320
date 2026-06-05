#!/usr/bin/env pwsh
# verify_all.ps1 — perl 미설치 환경용 PowerShell 재구현
# verify_*.pl 의 row(category, item, result, detail) 호출을 PowerShell 으로 변환
# OS-05 (Stage 60 cycle 4 — Cowork 종료 후 처리)

param(
    [string]$Root = "D:\Work\CDT-320\QMC.CDT-320"
)

$ErrorActionPreference = 'Stop'
$rows = @()

function Add-Row([string]$cat, [string]$item, [bool]$pass, [string]$detail = "") {
    $script:rows += [PSCustomObject]@{
        Category = $cat
        Item     = $item
        Result   = if ($pass) { "PASS" } else { "FAIL" }
        Detail   = $detail
    }
}

function Test-Greps([string]$file, [string[]]$patterns) {
    if (-not (Test-Path $file)) { return $false }
    $content = Get-Content -Raw $file -EA SilentlyContinue
    if (-not $content) { return $false }
    foreach ($p in $patterns) {
        if ($content -notmatch $p) { return $false }
    }
    return $true
}

# ── BUILD ──
$exe = Join-Path $Root "QMC.CDT-320\bin\Debug\QMC.CDT-320.exe"
Add-Row "BUILD" "QMC.CDT-320.exe" (Test-Path $exe) ""

# ── verify_*.pl 파일 자체의 존재 검증 (perl 미실행, 정적 검증만) ──
$stages = @(
    "stage2","stage3","stage4","stage5","stage6","stage7","stage8",
    "stage9","stage10","stage11","stage12","stage13",
    "stage14","stage15","stage16","stage17","stage18","stage19","stage20",
    "stage21","stage22","stage23","stage24","stage25",
    "stage26","stage27","stage28","stage29","stage30","stage31","stage32",
    "stage43","stage44","stage45","stage46","stage47","stage48",
    "stage49","stage50","stage51","stage52","stage53","stage54"
)

foreach ($s in $stages) {
    $script = Join-Path $Root "tools\verify_$s.pl"
    Add-Row "STAGE-FILE" "verify_$s.pl 존재" (Test-Path $script) $script
}

# ── 핵심 코드 산출물 검증 (verify_stage*.pl 의 핵심 grep 로직 일부 PowerShell 재현) ──

# Stage 60 — AlarmMaster 매뉴얼 호환 (Stage 62: 위치 QMC.Common\Alarms\AlarmMaster.cs)
$alarmFile = Join-Path $Root "QMC.Common\Alarms\AlarmMaster.cs"
Add-Row "STAGE60" "AlarmMaster ManualName 필드" (Test-Greps $alarmFile @('public string\s+ManualName')) $alarmFile
Add-Row "STAGE60" "AlarmMaster ManualLocator 필드" (Test-Greps $alarmFile @('public string\s+ManualLocator')) $alarmFile
Add-Row "STAGE60" "E-STOP 등록" (Test-Greps $alarmFile @('Code="E-STOP"')) $alarmFile
$emgCount = (Select-String -Path $alarmFile -Pattern '^\s*new AlarmDefinition.*Code="EMG-PRESSED"' -EA SilentlyContinue).Count
Add-Row "STAGE60" "EMG-PRESSED 정의 제거 (주석 제외)" ($emgCount -eq 0) "주석 라인 제외 0건 기대"
# Stage 62:+3(VISION-*), 67:+6(LIGHT-*), 69:+3(LIGHT-WIRING/MAP/POOL) → 48+3+6+3 = 60
$codeCount = (Select-String -Path $alarmFile -Pattern 'Code="[A-Z][A-Z0-9-]+"').Count
Add-Row "STAGE69" "AlarmMaster 등록 = 60 (48 + 62:3 + 67:6 + 69:3)" ($codeCount -eq 60) "actual=$codeCount"

# Stage 60 cycle 4 — OS-12 LIMIT-HIT (AjinAxis)
$ajinFile = Join-Path $Root "QMC.CDT-320\Equipment\Ajin\AjinAxis.cs"
Add-Row "OS-12" "LIMIT-HIT Raise (AjinAxis Sensor 감지)" (Test-Greps $ajinFile @('"LIMIT-HIT"', 'Sensor_PEL && !wasPel')) $ajinFile

# Stage 60 cycle 4 — OS-12 EXPOSE-TIMEOUT (TPU)
$tpuFile = Join-Path $Root "QMC.CDT-320\Equipment\TransferPickerUnit.cs"
Add-Row "OS-12" "EXPOSE-TIMEOUT Raise (TPU)" (Test-Greps $tpuFile @('"EXPOSE-TIMEOUT"')) $tpuFile

# Stage 60 cycle 4 — OS-03 VisionRecipePage 핸들러
$visRecFile = Join-Path $Root "QMC.CDT-320\Ui\Pages\Recipe\VisionRecipePage.cs"
Add-Row "OS-03" "VisionRecipePage GRAB/MATCH 핸들러" (Test-Greps $visRecFile @('VISION-ACTION', 'wafer\.ExposeAsync', 'wafer\.MatchAsync')) $visRecFile

# Stage 60 cycle 4 — OS-08 verify_all.pl 통합
$verifyAll = Join-Path $Root "tools\verify_all.pl"
Add-Row "OS-08" "verify_all.pl 21 stage 통합" (Test-Greps $verifyAll @('stage9 stage10', 'stage26 stage27', 'stage43 stage44', 'stage53 stage54')) $verifyAll

# UiClickAuditor / PerformClickAll
$auditorFile = Join-Path $Root "QMC.CDT-320\Ui\Util\UiClickAuditor.cs"
Add-Row "STAGE60" "UiClickAuditor.PerformClickAll" (Test-Greps $auditorFile @('PerformClickAll', 'UI-CLICK-FAIL')) $auditorFile

# Program.cs — --click-test-all
$progFile = Join-Path $Root "QMC.CDT-320\Program.cs"
Add-Row "STAGE60" "--click-test-all CLI" (Test-Greps $progFile @('--click-test-all', 'ClickTestAll')) $progFile

# AjinFactory Sim 자동 분기
$factoryFile = Join-Path $Root "QMC.CDT-320\Equipment\Ajin\AjinFactory.cs"
Add-Row "SIM" "AjinFactory Sim 자동 분기 (UseRealBoard=false default)" (Test-Greps $factoryFile @('UseRealBoard.*=\s*false', 'return new SimAxis')) $factoryFile

# PageBase AutoScroll
$pageBaseFile = Join-Path $Root "QMC.CDT-320\Ui\Pages\PageBase.cs"
Add-Row "STAGE60" "PageBase AutoScroll = true" (Test-Greps $pageBaseFile @('AutoScroll\s*=\s*true')) $pageBaseFile

# Stage 63 — TopSide/BottomSide → FrontSide/RearSide 리네임
# 활성 코드에 옛 식별자 잔존 0건 검사 (Legacy DataMember / 마이그레이션 비교문자열 / 주석은 허용).
$srcFiles = Get-ChildItem -Path (Join-Path $Root "QMC.CDT-320"),(Join-Path $Root "QMC.Vision"),(Join-Path $Root "QMC.Common") -Recurse -Include *.cs -EA SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$badLines = @()
foreach ($sf in $srcFiles) {
    $ln = 0
    foreach ($line in (Get-Content $sf.FullName -EA SilentlyContinue)) {
        $ln++
        if ($line -match 'TopSide|BottomSide') {
            # 허용: Legacy 프로퍼티, 마이그레이션 비교 리터럴, 주석(// 또는 ///), DataMember(Name=
            if ($line -match 'Legacy|Migrate|"TopSide"|"BottomSide"|"Sim/TopSide"|"Sim/BottomSide"|Name\s*=\s*"(Top|Bottom)Side|^\s*///|^\s*//|리네임|마이그레이션') { continue }
            $badLines += ("{0}:{1}: {2}" -f $sf.Name, $ln, $line.Trim())
        }
    }
}
Add-Row "STAGE63" "활성 코드 TopSide/BottomSide 잔존 0 (Legacy/주석 제외)" ($badLines.Count -eq 0) ("violations=" + $badLines.Count)

# Stage 63 — 핵심 리네임 산출물 확인
$subset63 = Join-Path $Root "QMC.Common\Recipes\AlgorithmCameraSubset.cs"
Add-Row "STAGE63" "VisionAlgorithm FrontSide/RearSide + 마이그레이션" (Test-Greps $subset63 @('FrontSide\s*=\s*"FrontSide"', 'RearSide\s*=\s*"RearSide"', 'MigrateLegacyAlgorithmNames')) $subset63
$frontMod = Join-Path $Root "QMC.Vision\Modules\FrontSideInspectionModule.cs"
$rearMod  = Join-Path $Root "QMC.Vision\Modules\RearSideInspectionModule.cs"
Add-Row "STAGE63" "FrontSide/RearSideInspectionModule.cs 존재" ((Test-Path $frontMod) -and (Test-Path $rearMod)) "modules"

# Stage 65 — Maintenance → Recipe 통합
$recipe65 = Join-Path $Root "QMC.Vision\Ui\Pages\RecipePage.cs"
$maintGone = -not (Test-Path (Join-Path $Root "QMC.Vision\Ui\Pages\MaintenancePage.cs"))
Add-Row "STAGE65" "MaintenancePage.cs 제거 + RecipePage 단일화" ($maintGone -and (Test-Greps $recipe65 @('class RecipePage'))) "merged"
Add-Row "STAGE65" "RecipePage 트리 5 모듈 등록" (Test-Greps $recipe65 @('host.WaferMod','host.BinMod','host.BottomMod','host.FrontSideMod','host.RearSideMod')) $recipe65
$vform65 = Join-Path $Root "QMC.Vision\Form1.cs"
Add-Row "STAGE65" "Form1 Tab.Maintenance/_pgMaint 제거" (-not (Test-Greps $vform65 @('Tab\.Maintenance|_pgMaint'))) $vform65

# Stage 67 — LFine 조명 컨트롤러
$lfctrl = Join-Path $Root "QMC.Vision\Optics\LFine\LFineLightController.cs"
$lifc   = Join-Path $Root "QMC.Vision\Optics\ILightController.cs"
$lproto = Join-Path $Root "QMC.Vision\Optics\LFine\LFineProtocol.cs"
Add-Row "STAGE67" "ILightController + LFineLightController + Protocol 존재" ((Test-Path $lifc) -and (Test-Path $lfctrl) -and (Test-Greps $lproto @('0x40','BuildChannelOnTimeCommand'))) "optics"
Add-Row "STAGE67" "AlarmMaster 6 LIGHT-* 코드" (Test-Greps $alarmFile @('LIGHT-OPEN-FAIL','LIGHT-TIMEOUT','LIGHT-PWR-RANGE')) $alarmFile

# Stage 69 — 검사별 조명 매핑
$lsetup = Join-Path $Root "QMC.Common\Recipes\LightSystemSetup.cs"
$lhub   = Join-Path $Root "QMC.Vision\Comm\LightHub.cs"
$lpanel = Join-Path $Root "QMC.Vision\Ui\Pages\InspectionLightPanel.cs"
Add-Row "STAGE69" "LightSystemSetup + LightHub + InspectionLightPanel 존재" ((Test-Path $lsetup) -and (Test-Path $lhub) -and (Test-Path $lpanel)) "light-map"
Add-Row "STAGE69" "AlarmMaster 3 신규 (WIRING-MISS/MAP-INVALID/OUT-OF-POOL)" (Test-Greps $alarmFile @('LIGHT-WIRING-MISS','LIGHT-MAP-INVALID','LIGHT-CHANNEL-OUT-OF-POOL')) $alarmFile
$acsF = Join-Path $Root "QMC.Common\Recipes\AlgorithmCameraSubset.cs"
Add-Row "STAGE69" "AlgorithmCameraMapping.InspectionLights (옵션 A)" (Test-Greps $acsF @('List<InspectionLightOverride>\s+InspectionLights')) $acsF

# Stage 70 — Light Panel fixups
$finderF = Join-Path $Root "QMC.Vision\Ui\Pages\FinderPage.cs"
$inspF   = Join-Path $Root "QMC.Vision\Ui\Pages\InspectorPage.cs"
Add-Row "STAGE70" "FinderPage/InspectorPage = InspectionLightPanel (IlluminatorPanel 미사용)" `
    ((Test-Greps $finderF @('new InspectionLightPanel')) -and (-not (Test-Greps $finderF @('new IlluminatorPanel'))) `
     -and (Test-Greps $inspF @('new InspectionLightPanel')) -and (-not (Test-Greps $inspF @('new IlluminatorPanel')))) "finder/inspector"
$setupF = Join-Path $Root "QMC.Common\Recipes\InspectionLightSubset.cs"
Add-Row "STAGE70" "InspectionLightSetting.Page (Recipe 측)" (Test-Greps $setupF @('int Page')) $setupF
$lspF = Join-Path $Root "QMC.Vision\Ui\Pages\LightSystemSetupPage.cs"
Add-Row "STAGE70" "Setup: 컨트롤러 삭제 + 라벨 캐시 + Controller 콤보" (Test-Greps $lspF @('DeleteController','FlushLabelsToCache','RefreshControllerCombo')) $lspF
$acsP = Join-Path $Root "QMC.Common\Recipes\AlgorithmCameraSubset.cs"
Add-Row "STAGE70" "Wiring.Page → Setting.Page 마이그레이션" (Test-Greps $acsP @('MigrateWiringPageToSettings')) $acsP

# Stage 77 — LeesOS 2차 벤더
$leeCtrl  = Join-Path $Root "QMC.Vision\Optics\Leesos\LeesosLightController.cs"
$leeProto = Join-Path $Root "QMC.Vision\Optics\Leesos\LeesosProtocol.cs"
$leeCfg   = Join-Path $Root "QMC.Vision\Optics\Leesos\LeesosLightConfig.cs"
Add-Row "STAGE77" "Leesos Controller/Protocol/Config 존재" ((Test-Path $leeCtrl) -and (Test-Path $leeProto) -and (Test-Path $leeCfg)) "leesos"
Add-Row "STAGE77" "LeesosProtocol LH/LC/LS 명령 빌더" (Test-Greps $leeProto @('BuildVolumeCommand','BuildOnOffCommand','BuildStatusCommand','"LC"','"LH"','"LS"')) $leeProto
Add-Row "STAGE77" "LightControllerEntry.Vendor + 마이그레이션" (Test-Greps $lsetup @('string Vendor','OnDeserializing')) $lsetup
$lfac = Join-Path $Root "QMC.Vision\Optics\LightControllerFactory.cs"
Add-Row "STAGE77" "Factory 벤더 분기 (Leesos) + entry 시그니처" (Test-Greps $lfac @('case "Leesos"','Create\(LightControllerEntry','ToLeesosConfig')) $lfac

# Stage 79 — batch + Mode + Leesos 매뉴얼 정정
$lmode = Join-Path $Root "QMC.Common\Recipes\LightControllerMode.cs"
Add-Row "STAGE79" "LightControllerMode enum (3값)" (Test-Greps $lmode @('Continuous','StrobeExternal','StrobeOnCommand')) $lmode
Add-Row "STAGE79" "ILightController.Mode + SetChannelBatchAsync" (Test-Greps $lifc @('LightControllerMode Mode','SetChannelBatchAsync')) $lifc
Add-Row "STAGE79" "LightControllerEntry.Mode + 마이그레이션 기본" (Test-Greps $lsetup @('LightControllerMode Mode','Mode = LightControllerMode.StrobeOnCommand')) $lsetup
Add-Row "STAGE79" "LFine SetChannelBatchAsync (SP 1프레임 + Mode skip)" (Test-Greps $lfctrl @('SetChannelBatchAsync','PageOnTimeFrame','StrobeOnCommand')) $lfctrl
Add-Row "STAGE79" "Leesos 매뉴얼 정정 (12-bit X3 / A~G,T / ER)" (Test-Greps $leeProto @('EncodeChannel','BuildVolumeAllCommand','X3','ErrText\s*=\s*"ER"')) $leeProto
Add-Row "STAGE79" "InspectionLightPanel batch 적용" (Test-Greps $lpanel @('SetChannelBatchAsync')) $lpanel

# Stage 81 — 다중 컨트롤러 결선 + UI 분리
Add-Row "STAGE81" "AlgorithmLightWiring.ControllerSets + ControllerChannels + 마이그레이션" (Test-Greps $lsetup @('List<ControllerChannels> ControllerSets','class ControllerChannels','LegacyControllerPort')) $lsetup
$ilsF = Join-Path $Root "QMC.Common\Recipes\InspectionLightSubset.cs"
Add-Row "STAGE81" "InspectionLightSetting.ControllerPort" (Test-Greps $ilsF @('string ControllerPort')) $ilsF
Add-Row "STAGE81" "FillRecipeControllerPorts 마이그레이션" (Test-Greps $acsF @('FillRecipeControllerPorts')) $acsF
Add-Row "STAGE81" "Setup 결선 TreeView (_treeWiring) + Level 류 0" ((Test-Greps $lspF @('_treeWiring','ControllerSets')) -and (-not (Test-Greps $lspF @('TrackBar')))) $lspF
Add-Row "STAGE81" "Recipe Apply 병렬 (Task.WhenAll) + Controller 컬럼" (Test-Greps $lpanel @('Task.WhenAll','ApplyControllerAsync','Ctrl')) $lpanel

# Stage 82 — 채널 라벨 Channel 번호 직접 지정 (편집 가능, 비연속)
Add-Row "STAGE82" "라벨 Channel 편집 가능 + SeedLabels/SanitizeLabels (강제 재번호 제거)" ((Test-Greps $lspF @('SeedLabels','SanitizeLabels')) -and (-not (Test-Greps $lspF @('ReconcileLabels')))) $lspF

# ── 출력 ──
$bar = "=" * 110
Write-Output $bar
Write-Output "CDT-320 통합 회귀 (verify_all.ps1 — PowerShell 재구현)"
Write-Output $bar
Write-Output ("{0,-15} {1,-55} {2,-6} {3}" -f "CATEGORY", "ITEM", "RESULT", "DETAIL")
Write-Output ("-" * 110)

$pass = 0; $fail = 0
foreach ($r in $rows) {
    Write-Output ("{0,-15} {1,-55} {2,-6} {3}" -f $r.Category, $r.Item, $r.Result, $r.Detail)
    if ($r.Result -eq "PASS") { $pass++ } else { $fail++ }
}
Write-Output ("-" * 110)
Write-Output ("TOTAL {0}   PASS {1}   FAIL {2}" -f $rows.Count, $pass, $fail)
Write-Output $bar

if ($fail -gt 0) { exit 1 } else { exit 0 }
