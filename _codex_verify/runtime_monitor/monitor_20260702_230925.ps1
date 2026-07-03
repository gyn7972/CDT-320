param([string]$OutPath)
$ErrorActionPreference='SilentlyContinue'
$logRoot='D:\CDT-320\Log'
$eventDir=Join-Path $logRoot 'Event'
$today=Get-Date -Format 'yyyy-MM-dd'
$durationSec=1200
$patterns=@(
 'Bottom/Side 재시작 안전 진입',
 'Bottom X 이동 전에 PickerY를 Avoid',
 'Side 단독 재개 방지',
 '기존 Bottom 결과가 있어도 Bottom shot',
 'Place 재시작 안전 진입',
 'Place 재시작 첫 접근',
 'Place 재시작 OutputStageY 수령 위치',
 'Place 재시작 Picker X/T',
 'Place 재시작 PickerY 전진',
 'Picker phase 진입 대기',
 'Picker phase 전환 대기',
 'Picker phase 진입 대기/차단',
 'Picker phase 점유 완료',
 'Picker phase 전환 완료',
 'Bottom/Side 통합 Bottom',
 'Bottom shot',
 'Side 검사',
 'Place',
 'SHARED-RAIL-X',
 'INTERLOCK',
 'Critical',
 'Error',
 'Failed',
 'Alarm',
 '자동 시퀀스 실패',
 'collision',
 'clearance is too close'
)
function Get-WatchFiles {
    $files=@()
    if(Test-Path $eventDir){
        $latest=Get-ChildItem -LiteralPath $eventDir -Filter '*.csv' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if($latest){ $files += $latest.FullName }
    }
    foreach($name in @("Event_$today.log","FrontHeadSeq_$today.log","RearHeadSeq_$today.log","SharedRailX_$today.log","Alarm_$today.log","Warning_$today.log")){
        $p=Join-Path $logRoot $name
        if(Test-Path -LiteralPath $p){ $files += $p }
    }
    return $files | Select-Object -Unique
}
function Write-Line([string]$text){
    Add-Content -LiteralPath $OutPath -Value $text -Encoding UTF8
}
$positions=@{}
foreach($f in Get-WatchFiles){
    $positions[$f]=(Get-Item -LiteralPath $f).Length
}
Write-Line ("MONITOR_START " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff') + " files=" + (($positions.Keys | ForEach-Object { $_ }) -join '|'))
$deadline=(Get-Date).AddSeconds($durationSec)
while((Get-Date) -lt $deadline){
    foreach($f in Get-WatchFiles){
        if(-not $positions.ContainsKey($f)){ $positions[$f]=0; Write-Line ("MONITOR_NEW_FILE " + $f) }
    }
    foreach($f in @($positions.Keys)){
        if(-not (Test-Path -LiteralPath $f)){ continue }
        $fs=$null
        try{
            $fs=[System.IO.File]::Open($f,[System.IO.FileMode]::Open,[System.IO.FileAccess]::Read,[System.IO.FileShare]::ReadWrite)
            if($fs.Length -lt [long]$positions[$f]){ $positions[$f]=0 }
            if($fs.Length -gt [long]$positions[$f]){
                [void]$fs.Seek([long]$positions[$f],[System.IO.SeekOrigin]::Begin)
                $sr=New-Object System.IO.StreamReader($fs,[System.Text.Encoding]::UTF8,$true,8192,$true)
                $text=$sr.ReadToEnd()
                $positions[$f]=$fs.Position
                $sr.Dispose()
                foreach($line in ($text -split "`r?`n")){
                    if([string]::IsNullOrWhiteSpace($line)){ continue }
                    $hit=$false
                    foreach($pat in $patterns){ if($line.IndexOf($pat,[System.StringComparison]::OrdinalIgnoreCase) -ge 0){ $hit=$true; break } }
                    if($hit){ Write-Line ((Get-Date -Format 'HH:mm:ss.fff') + "\t" + (Split-Path $f -Leaf) + "\t" + $line) }
                }
            }
        } catch {
        } finally {
            if($fs){ $fs.Dispose() }
        }
    }
    Start-Sleep -Milliseconds 500
}
Write-Line ("MONITOR_END " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'))
