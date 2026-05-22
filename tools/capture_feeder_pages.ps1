# capture_feeder_pages.ps1 - UIA navigation to InputFeeder + OutputFeeder pages.
param([string]$OutDir = ".")

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W6 {
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
$hwnd = $proc.MainWindowHandle
[W6]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 800

function Click($x, $y) {
    [W6]::SetCursorPos($x, $y)
    Start-Sleep -Milliseconds 250
    [W6]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
    [W6]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
}

function Capture($name) {
    $wr = New-Object W6+RECT
    [W6]::GetWindowRect($hwnd, [ref]$wr) | Out-Null
    $ww = $wr.R - $wr.L; $wh = $wr.B - $wr.T
    $bmp = New-Object System.Drawing.Bitmap $ww, $wh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($wr.L, $wr.T, 0, 0, (New-Object System.Drawing.Size $ww, $wh))
    $out = Join-Path $OutDir $name
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Saved $out"
}

$wr = New-Object W6+RECT
[W6]::GetWindowRect($hwnd, [ref]$wr) | Out-Null

# 1) WorkInfo tab (i icon, 2nd from left)
Write-Output "[1] WorkInfo tab"
Click ($wr.L + 222) ($wr.B - 50)
Start-Sleep -Milliseconds 1200

# Sidebar items at right side. From screenshot, items are at:
# y=210 INPUT CASSETTE (selected/header)
# y=258 INPUT FEEDER     ← 2nd
# y=306 INPUT STAGE
# y=354 FRONT HEAD
# y=402 REAR HEAD
# y=450 OUTPUT STAGE
# y=498 OUTPUT FEEDER    ← 7th
# y=546 OUTPUT CASSETTE
# Sidebar x-center: window right edge - 105 (sidebar width 210, half=105)

Write-Output "[2] Click INPUT FEEDER sidebar"
Click ($wr.R - 105) 258
Start-Sleep -Milliseconds 1000
Capture "tab_inputfeeder.png"

# 3) Click OUTPUT FEEDER sidebar
Write-Output "[3] Click OUTPUT FEEDER sidebar"
Click ($wr.R - 105) 498
Start-Sleep -Milliseconds 800
Capture "tab_outputfeeder.png"

Write-Output "Done"
