# remote_viewer_client.ps1 — 외부 PC 에서 Handler RemoteViewer 화면 보기
# 사용: powershell -NoProfile -ExecutionPolicy Bypass -File remote_viewer_client.ps1 [-Host 127.0.0.1] [-Port 5099]
# Handler 측에서 Settings → "원격 뷰어" → Start 후 본 스크립트 실행.

Param(
    [string]$VHost = "127.0.0.1",
    [int]$VPort    = 5099
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Form 생성
$form = New-Object System.Windows.Forms.Form
$form.Text = "Remote Viewer Client — $VHost`:$VPort"
$form.Size = New-Object System.Drawing.Size(1024, 768)
$form.StartPosition = "CenterScreen"
$form.BackColor = [System.Drawing.Color]::Black

$picture = New-Object System.Windows.Forms.PictureBox
$picture.Dock = [System.Windows.Forms.DockStyle]::Fill
$picture.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::Zoom
$picture.BackColor = [System.Drawing.Color]::Black
$form.Controls.Add($picture)

$status = New-Object System.Windows.Forms.StatusStrip
$lbl = New-Object System.Windows.Forms.ToolStripStatusLabel
$lbl.Text = "Connecting to $VHost`:$VPort..."
$status.Items.Add($lbl) | Out-Null
$form.Controls.Add($status)

# TCP 연결 + 백그라운드 수신
$client = $null
$stream = $null
$reader = $null

$tryConnect = {
    try {
        $script:client = New-Object System.Net.Sockets.TcpClient
        $script:client.Connect($VHost, $VPort)
        $script:stream = $script:client.GetStream()
        $script:reader = New-Object System.IO.StreamReader($script:stream)
        $lbl.Text = "Connected: $VHost`:$VPort"
        return $true
    } catch {
        $lbl.Text = "Connect fail: $($_.Exception.Message)"
        return $false
    }
}

# 화면 갱신 타이머 (300ms — 1fps 송신이지만 polling 더 빠르게)
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 300
$timer.Add_Tick({
    if ($script:client -eq $null -or -not $script:client.Connected) {
        & $tryConnect | Out-Null
        return
    }
    try {
        if ($script:stream.DataAvailable) {
            while ($script:stream.DataAvailable) {
                $line = $script:reader.ReadLine()
                if ($line -match "^FRAME\|(.+)$") {
                    try {
                        $bytes = [Convert]::FromBase64String($matches[1])
                        $ms = New-Object System.IO.MemoryStream(,$bytes)
                        $img = [System.Drawing.Image]::FromStream($ms)
                        if ($picture.Image) { $picture.Image.Dispose() }
                        $picture.Image = $img
                        $lbl.Text = "$VHost`:$VPort  frame=$($bytes.Length)B  $(Get-Date -Format 'HH:mm:ss')"
                    } catch {
                        $lbl.Text = "Decode err: $($_.Exception.Message)"
                    }
                }
            }
        }
    } catch {
        $lbl.Text = "Recv err: $($_.Exception.Message)"
        try { $script:client.Close() } catch {}
        $script:client = $null
    }
})

$form.Add_Load({
    & $tryConnect | Out-Null
    $timer.Start()
})

$form.Add_FormClosing({
    $timer.Stop()
    try { $script:reader.Close() } catch {}
    try { $script:stream.Close() } catch {}
    try { $script:client.Close() } catch {}
})

[System.Windows.Forms.Application]::Run($form)
