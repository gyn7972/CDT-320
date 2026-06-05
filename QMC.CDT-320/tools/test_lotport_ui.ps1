# test_lotport_ui.ps1
# Click LIFT WAFER MAPPING button → wait → capture screenshot showing green slot LEDs.
param([string]$OutDir = ".")

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W4 {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
}
"@

$proc = Get-Process -Name "QMC.CDT-320" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$h = $proc.MainWindowHandle
if ([W4]::IsIconic($h)) { [W4]::ShowWindow($h, 9) | Out-Null; Start-Sleep -Milliseconds 400 }
[W4]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 800

$wr = New-Object W4+RECT
[W4]::GetWindowRect($h, [ref]$wr) | Out-Null

function Click($x, $y) {
    [W4]::SetCursorPos($x, $y)
    Start-Sleep -Milliseconds 250
    [W4]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
    [W4]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
}

function Capture($name) {
    $ww = $wr.R - $wr.L
    $wh = $wr.B - $wr.T
    $bmp = New-Object System.Drawing.Bitmap $ww, $wh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($wr.L, $wr.T, 0, 0, (New-Object System.Drawing.Size $ww, $wh))
    $out = Join-Path $OutDir $name
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Saved $out"
}

# 1) Click WorkInfo tab (bottom nav second button)
Write-Output "[1] Click WorkInfo tab"
Click ($wr.L + 222) ($wr.B - 50)
Start-Sleep -Milliseconds 800

# 2) Click INPUT CASSETTE sidebar (default selected)
Write-Output "[2] Already on INPUT CASSETTE"

# 3) Click LIFT WAFER MAPPING (button at approx x=130, y=1278 in 2576x1408 window)
Write-Output "[3] Click LIFT WAFER MAPPING"
Click ($wr.L + 130) ($wr.B - 130)
Start-Sleep -Milliseconds 9000  # ScanCassetteAsync takes ~6.6s + buffer

Capture "tab_workinfo_after_map.png"

# 4) Click LIFT WAFER LOADING (button at x=320, y=1278)
Write-Output "[4] Click LIFT WAFER LOADING"
Click ($wr.L + 320) ($wr.B - 130)
Start-Sleep -Milliseconds 5000  # MoveToTarget + Exchange = ~3-4s

Capture "tab_workinfo_after_load.png"

Write-Output "Done"
