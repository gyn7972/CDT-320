# run_smoke.ps1 — Vision 스모크를 "샌드박스" 폴더에서 컴파일·실행한다.
#
# ★ 왜 샌드박스인가 (사고 이력):
#   스모크의 Cleanup() 은 AppDomain.CurrentDomain.BaseDirectory 의 EquipmentData / Recipes\default 를
#   재귀 삭제한다. 과거에 스모크 exe 를 QMC.Vision\bin\Debug 에 두고 실행해서 BaseDirectory 가
#   실제 앱 데이터 폴더가 됐고, 저장된 레시피/장비설정이 통째로 날아갔다 (2026-06-12).
#   → 스모크는 반드시 이 스크립트로 smoke-sandbox 에서 실행한다. bin\Debug 에서 직접 실행 금지.
#
# 사용:
#   powershell -File cdt-320\redesign-r2d-test\run_smoke.ps1                       # *.cs 전부
#   powershell -File cdt-320\redesign-r2d-test\run_smoke.ps1 HwModeSmoke CamLightSmoke
param([string[]]$Names)

$ErrorActionPreference = "Stop"
$root    = "D:\Work\source"
$testDir = Join-Path $root "cdt-320\redesign-r2d-test"
$binDir  = Join-Path $root "QMC.Vision\bin\Debug"
$sandbox = Join-Path $root "cdt-320\smoke-sandbox"

# 0) 빌드 산출물 확인
if (-not (Test-Path (Join-Path $binDir "QMC.Vision.exe"))) { Write-Output "QMC.Vision.exe 없음 — 먼저 빌드"; exit 2 }

# 1) 샌드박스 준비 — 앱 바이너리(exe/dll/pdb)만 복사. 데이터 폴더(EquipmentData/Recipes/Config)는 복사하지 않는다.
New-Item -ItemType Directory -Force $sandbox | Out-Null
Get-ChildItem $binDir -File | Where-Object { $_.Extension -in ".exe", ".dll", ".pdb", ".config" -and $_.Name -notlike "*Smoke*" } |
    ForEach-Object { Copy-Item $_.FullName (Join-Path $sandbox $_.Name) -Force }

# 2) 안전 가드 — 샌드박스가 bin\Debug 가 아님을 확인 (실수 방지)
if ((Resolve-Path $sandbox).Path -like "*bin\Debug*") { Write-Output "FATAL: sandbox 가 bin\Debug 안 — 중단"; exit 2 }

# 3) 대상 스모크 결정
if (-not $Names -or $Names.Count -eq 0) {
    $Names = Get-ChildItem $testDir -Filter *Smoke.cs | ForEach-Object { $_.BaseName }
}

# 4) 컴파일(→ 샌드박스) + 실행(작업폴더 = 샌드박스 → BaseDirectory = 샌드박스)
$csc = (Get-ChildItem "C:\Program Files\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\Roslyn\csc.exe" | Select-Object -First 1).FullName
$failTotal = 0
foreach ($n in $Names) {
    $src = Join-Path $testDir "$n.cs"
    if (-not (Test-Path $src)) { Write-Output ("{0,-28}: (소스 없음 — skip)" -f $n); continue }
    $out = Join-Path $sandbox "$n.exe"
    # WinForms 참조는 항상 포함(콘솔 스모크에도 무해)
    & $csc /nologo /langversion:7.3 /platform:x64 /target:winexe "/out:$out" `
        "/r:$sandbox\QMC.Vision.exe" "/r:$sandbox\QMC.Common.dll" `
        "/r:System.Windows.Forms.dll" "/r:System.Drawing.dll" $src
    if ($LASTEXITCODE -ne 0) { Write-Output ("{0,-28}: COMPILE FAIL" -f $n); $failTotal++; continue }
    $p = Start-Process -FilePath $out -WorkingDirectory $sandbox -Wait -PassThru -RedirectStandardOutput "$sandbox\$n.out.txt" -WindowStyle Hidden
    $last = (Get-Content "$sandbox\$n.out.txt" -ErrorAction SilentlyContinue | Select-Object -Last 1)
    Write-Output ("{0,-28}: {1}" -f $n, $last)
    if ($p.ExitCode -ne 0) { $failTotal++ }
}
Write-Output ("=" * 50)
Write-Output ($(if ($failTotal -eq 0) { "SMOKE ALL PASS" } else { "SMOKE FAIL x$failTotal" }))
exit $(if ($failTotal -eq 0) { 0 } else { 1 })
