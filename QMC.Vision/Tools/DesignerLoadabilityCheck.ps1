# DesignerLoadabilityCheck.ps1 — VS 디자이너 로드 가능성 정적 검사 (Stage 90 / Phase 1)
#
# InitializeComponent() 메서드 본문 범위 안에서 디자이너 로드를 깨는 5종 패턴을 탐지한다.
# 0건 = 합격(디자이너 직렬화 가능 형태). InitializeComponent 가 없으면 "NO-IC"(생성자 UI → 추출 필요).
#
# 사용: powershell -ExecutionPolicy Bypass -File DesignerLoadabilityCheck.ps1 -Path <폴더 또는 파일> [-Md <출력.md>]
param(
    [Parameter(Mandatory=$true)][string]$Path,
    [string]$Md = $null
)

function Get-Block([string[]]$lines, [int]$startIdx) {
    # startIdx 줄에서 시작해 첫 '{' 부터 중괄호 균형까지의 (시작줄,끝줄) 반환 (0-based inclusive)
    $depth = 0; $started = $false
    for ($i = $startIdx; $i -lt $lines.Count; $i++) {
        foreach ($ch in $lines[$i].ToCharArray()) {
            if ($ch -eq '{') { $depth++; $started = $true }
            elseif ($ch -eq '}') { $depth-- }
        }
        if ($started -and $depth -le 0) { return @($startIdx, $i) }
    }
    return @($startIdx, $lines.Count - 1)
}

$patterns = @(
    @{ Name = 'object-initializer'; Rx = 'new\s+\w[\w\.]*\s*\{' },
    @{ Name = 'lambda-event';       Rx = '=>' },
    @{ Name = 'local-control-var';  Rx = 'var\s+\w+\s*=\s*new\b' },
    @{ Name = 'layout-arithmetic';  Rx = '(\bint\s+\w+\s*=\s*-?\d)|(\b\w+\s*\+=\s*-?\d)' },   # 숫자 산술만 (이벤트 += new EventHandler 제외)
    @{ Name = 'logic-statement';    Rx = '\b(if|for|foreach|while|switch)\b' }
)

$files = @()
if (Test-Path $Path -PathType Container) {
    $files = Get-ChildItem $Path -Filter *.cs -File | Where-Object { $_.Name -notmatch '\.Designer\.cs$' -or $true } | Sort-Object Name
} else {
    $files = @(Get-Item $Path)
}

$report = New-Object System.Collections.Generic.List[string]
$totalViol = 0; $noIc = 0; $okFiles = 0
foreach ($f in $files) {
    $lines = Get-Content $f.FullName
    $icIdx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'void\s+InitializeComponent\s*\(') { $icIdx = $i; break }
    }
    if ($icIdx -lt 0) {
        $report.Add(("{0}: NO-IC (InitializeComponent 없음 — 생성자 UI 추출 필요)" -f $f.Name))
        $noIc++
        continue
    }
    $blk = Get-Block $lines $icIdx
    $fileViol = 0
    for ($i = $blk[0]; $i -le $blk[1]; $i++) {
        $line = $lines[$i]
        $code = ($line -replace '//.*$', '')   # 줄 끝 주석 제거(간이)
        foreach ($p in $patterns) {
            if ($code -match $p.Rx) {
                $report.Add(("{0}:{1}: [{2}] {3}" -f $f.Name, ($i+1), $p.Name, $line.Trim()))
                $fileViol++; $totalViol++
            }
        }
    }
    if ($fileViol -eq 0) { $report.Add(("{0}: OK (위반 0)" -f $f.Name)); $okFiles++ }
}

$summary = "=== DesignerLoadabilityCheck: 파일 $($files.Count) / 위반 $totalViol / NO-IC $noIc / OK $okFiles ==="
Write-Output $summary
$report | ForEach-Object { Write-Output $_ }

if ($Md) {
    $out = New-Object System.Collections.Generic.List[string]
    $out.Add("# Designer Loadability Check"); $out.Add("")
    $out.Add('```'); $out.Add($summary); foreach ($r in $report) { $out.Add($r) }; $out.Add('```')
    Set-Content -Path $Md -Value $out -Encoding utf8
    Write-Output "→ saved: $Md"
}

exit $totalViol
