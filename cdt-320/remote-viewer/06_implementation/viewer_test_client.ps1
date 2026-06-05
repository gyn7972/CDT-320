# 헤드리스 검증: 뷰어 포트에 접속해 [4B len][JPEG] 프레임 2장을 받아 유효성 확인.
param([string]$Server = "127.0.0.1", [int[]]$Ports = @(5200,5201,5203,5205,5206))

Add-Type -AssemblyName System.Drawing

function Read-Exactly([System.IO.Stream]$s, [int]$n) {
    $buf = New-Object byte[] $n
    $off = 0
    while ($off -lt $n) {
        $r = $s.Read($buf, $off, $n - $off)
        if ($r -le 0) { return $null }
        $off += $r
    }
    return ,$buf
}

function Read-Frame([System.IO.Stream]$s) {
    $sz = Read-Exactly $s 4
    if ($sz -eq $null) { return $null }
    $len = [BitConverter]::ToInt32($sz, 0)
    if ($len -le 0 -or $len -gt (10*1024*1024)) { return @{ Err = "bad len $len" } }
    $data = Read-Exactly $s $len
    if ($data -eq $null) { return @{ Err = "short read" } }
    $soi = ($data[0] -eq 0xFF -and $data[1] -eq 0xD8)
    $decodeOk = $false; $w=0; $h=0
    try {
        $ms = New-Object System.IO.MemoryStream(,$data)
        $img = [System.Drawing.Image]::FromStream($ms)
        $decodeOk = $true; $w = $img.Width; $h = $img.Height
        $img.Dispose(); $ms.Dispose()
    } catch { }
    return @{ Len = $len; Soi = $soi; Decode = $decodeOk; W = $w; H = $h }
}

foreach ($port in $Ports) {
    try {
        $cli = New-Object System.Net.Sockets.TcpClient
        $cli.NoDelay = $true
        $iar = $cli.BeginConnect($Server, $port, $null, $null)
        if (-not $iar.AsyncWaitHandle.WaitOne(3000)) { throw "connect timeout" }
        $cli.EndConnect($iar)
        $st = $cli.GetStream()
        $st.ReadTimeout = 5000
        $f1 = Read-Frame $st
        $f2 = Read-Frame $st
        $cli.Close()
        if ($f1.Err -or $f2.Err) { Write-Output "PORT $port : FAIL ($($f1.Err)$($f2.Err))" }
        else {
            $ok = $f1.Soi -and $f1.Decode -and $f2.Soi -and $f2.Decode
            Write-Output ("PORT {0} : {1}  f1[len={2} soi={3} dec={4} {5}x{6}] f2[len={7} soi={8} dec={9}]" -f `
                $port, ($(if($ok){'PASS'}else{'FAIL'})), $f1.Len, $f1.Soi, $f1.Decode, $f1.W, $f1.H, $f2.Len, $f2.Soi, $f2.Decode)
        }
    } catch {
        Write-Output "PORT $port : FAIL (exception: $($_.Exception.Message))"
    }
}
