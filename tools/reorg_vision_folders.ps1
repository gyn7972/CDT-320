<#
  reorg_vision_folders.ps1
  ---------------------------------------------------------------------------
  QMC.Vision 폴더를 핸들러(QMC.CDT-320)식 Equipment/ 하위로 정렬한다.

  설계 원칙:
   - 네임스페이스는 변경하지 않는다. 핸들러처럼 "폴더 ≠ 네임스페이스" 로 둔다.
     → .cs 코드/using 변경이 전혀 없다(컴파일 영향 최소).
   - git mv 로 폴더를 이동해 이력을 보존하고, QMC.Vision.csproj 의
     <... Include="Folder\..."> 경로만 Equipment\Folder\ 로 일괄 갱신한다(UTF-8 BOM 보존).

  대상 폴더: Backends, Cameras, Comm, Config, Core, Modules, Optics, Tools
  (Ui 는 핸들러도 루트 직속이라 이동하지 않는다.)

  사용:
     pwsh .\tools\reorg_vision_folders.ps1 -DryRun     # 미리보기(변경 없음)
     pwsh .\tools\reorg_vision_folders.ps1             # 실제 수행

  실행 후 반드시 Visual Studio 2022 에서 QMC.Vision 빌드로 검증할 것.
  되돌리기: git checkout -- . ; git clean -fd QMC.Vision\Equipment
  ---------------------------------------------------------------------------
#>
param([switch]$DryRun)
$ErrorActionPreference = "Stop"

# 리포 루트 = 이 스크립트(tools/)의 상위 폴더
$root = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $root "QMC.Vision"
if (-not (Test-Path $proj)) { throw "QMC.Vision 폴더를 찾을 수 없음: $proj" }

$folders = @("Backends","Cameras","Comm","Config","Core","Modules","Optics","Tools")
$csproj  = "QMC.Vision.csproj"

Push-Location $proj
try {
    Write-Host "=== QMC.Vision 폴더 정렬 (DryRun=$DryRun) ===" -ForegroundColor Cyan

    $inGit = $false
    git rev-parse --is-inside-work-tree 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $inGit = $true }

    if (-not (Test-Path "Equipment")) {
        Write-Host "[mkdir] Equipment"
        if (-not $DryRun) { New-Item -ItemType Directory "Equipment" | Out-Null }
    }

    foreach ($f in $folders) {
        if (-not (Test-Path $f)) { Write-Host "[skip ] $f (없음)"; continue }
        $dest = "Equipment\$f"
        Write-Host "[move ] $f  ->  $dest"
        if (-not $DryRun) {
            if ($inGit) { git mv $f $dest } else { Move-Item -Path $f -Destination $dest }
        }
    }

    Write-Host "[csproj] include 경로 갱신: $csproj"
    if (-not $DryRun) {
        $full   = (Resolve-Path $csproj).Path
        $bytes  = [System.IO.File]::ReadAllBytes($full)
        $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        $text   = [System.Text.Encoding]::UTF8.GetString($bytes)
        if ($hasBom) { $text = $text.TrimStart([char]0xFEFF) }
        foreach ($f in $folders) {
            $pattern     = 'Include="' + [regex]::Escape($f) + '\\'
            $replacement = 'Include="Equipment\' + $f + '\'
            $text = [regex]::Replace($text, $pattern, $replacement)
        }
        $enc = New-Object System.Text.UTF8Encoding($hasBom)
        [System.IO.File]::WriteAllText($full, $text, $enc)
    }

    Write-Host ""
    Write-Host "완료. Visual Studio 에서 QMC.Vision 빌드로 검증하세요." -ForegroundColor Green
    Write-Host "되돌리기: git checkout -- . ; git clean -fd Equipment"
}
finally { Pop-Location }
