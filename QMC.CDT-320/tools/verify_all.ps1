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

# Stage 60 — AlarmMaster 매뉴얼 호환
$alarmFile = Join-Path $Root "QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs"
Add-Row "STAGE60" "AlarmMaster ManualName 필드" (Test-Greps $alarmFile @('public string\s+ManualName')) $alarmFile
Add-Row "STAGE60" "AlarmMaster ManualLocator 필드" (Test-Greps $alarmFile @('public string\s+ManualLocator')) $alarmFile
Add-Row "STAGE60" "E-STOP 등록" (Test-Greps $alarmFile @('Code="E-STOP"')) $alarmFile
$emgCount = (Select-String -Path $alarmFile -Pattern '^\s*new AlarmDefinition.*Code="EMG-PRESSED"' -EA SilentlyContinue).Count
Add-Row "STAGE60" "EMG-PRESSED 정의 제거 (주석 제외)" ($emgCount -eq 0) "주석 라인 제외 0건 기대"
$codeCount = (Select-String -Path $alarmFile -Pattern 'Code="[A-Z][A-Z0-9-]+"').Count
Add-Row "STAGE60" "AlarmMaster 등록 = 48" ($codeCount -eq 48) "actual=$codeCount"

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
