# Grab-gated verification:
#  PHASE1: client connected, NO grab -> expect NO frame (PASS = transmit only on grab)
#  PHASE2: drive GRAB via command port -> expect frames
param([string]$Server="127.0.0.1", [int]$ViewerPort=5200, [int]$CmdPort=5100, [string]$Module="WaferVision")
Add-Type -AssemblyName System.Drawing

function Read-Exactly([System.IO.Stream]$s,[int]$n){
    $buf=New-Object byte[] $n; $off=0
    while($off -lt $n){ $r=$s.Read($buf,$off,$n-$off); if($r -le 0){return $null}; $off+=$r }
    return ,$buf
}
function Read-Frame([System.IO.Stream]$s){
    $sz=Read-Exactly $s 4; if($sz -eq $null){return $null}
    $len=[BitConverter]::ToInt32($sz,0); if($len -le 0 -or $len -gt 10485760){return @{Err="badlen"}}
    $data=Read-Exactly $s $len; if($data -eq $null){return @{Err="short"}}
    $soi=($data[0] -eq 0xFF -and $data[1] -eq 0xD8); $dec=$false;$w=0;$h=0
    try{$ms=New-Object System.IO.MemoryStream(,$data);$img=[System.Drawing.Image]::FromStream($ms);$dec=$true;$w=$img.Width;$h=$img.Height;$img.Dispose();$ms.Dispose()}catch{}
    return @{Len=$len;Soi=$soi;Decode=$dec;W=$w;H=$h}
}

$v=New-Object System.Net.Sockets.TcpClient;$v.NoDelay=$true;$v.Connect($Server,$ViewerPort)
$vs=$v.GetStream();$vs.ReadTimeout=1300

# PHASE 1
$p1=$null; try{$p1=Read-Frame $vs}catch{$p1="timeout"}
if($p1 -eq "timeout" -or $p1 -eq $null){ "PHASE1 [connected, NO grab]: PASS - no frame streamed" }
else{ "PHASE1 [connected, NO grab]: FAIL - frame received without grab (len=$($p1.Len))" }

# PHASE 2
$c=New-Object System.Net.Sockets.TcpClient;$c.NoDelay=$true;$c.Connect($Server,$CmdPort)
$cs=$c.GetStream();$cs.ReadTimeout=2000
$enc=[System.Text.Encoding]::UTF8
$vs.ReadTimeout=3000
$got=0
for($i=1;$i -le 4;$i++){
    $b=$enc.GetBytes("$Module|GRAB`n");$cs.Write($b,0,$b.Length);$cs.Flush()
    Start-Sleep -Milliseconds 60
    try{
        $f=Read-Frame $vs
        if($f -and $f.Soi -and $f.Decode){ $got++; "  GRAB#$i -> frame len=$($f.Len) $($f.W)x$($f.H)" }
        else { "  GRAB#$i -> invalid frame" }
    }catch{ "  GRAB#$i -> timeout (no frame)" }
    Start-Sleep -Milliseconds 200
}
if($got -ge 1){ "PHASE2 [grab occurred]: PASS - $got frames received" } else { "PHASE2 [grab occurred]: FAIL - no frames after grabs" }
try{$c.Close()}catch{}; try{$v.Close()}catch{}
