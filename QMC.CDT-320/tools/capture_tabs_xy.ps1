# capture_tabs_xy.ps1 - Click each bottom nav button by coordinate, capture screenshot.
# ASCII-only. Uses window-relative offsets that work for any window size
# (left buttons anchor-left, right buttons anchor-right per Form1.Designer.cs).
param([string]$OutDir = ".")

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W3 {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
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
if ([W3]::IsIconic($h)) { [W3]::ShowWindow($h, 9) | Out-Null; Start-Sleep -Milliseconds 400 }
[W3]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 600

$wr = New-Object W3+RECT
[W3]::GetWindowRect($h, [ref]$wr) | Out-Null
$ww = $wr.R - $wr.L
$wh = $wr.B - $wr.T
Write-Output "Window: $($ww)x$($wh) at ($($wr.L),$($wr.T))"

# Bottom nav y center: Form is maximized; title bar adds ~32 px on top
# Per Form1.Designer.cs btnY ~ form_height - 80 + 12 area; bottom nav height = 80
# Empirically the button centers are at win.bottom - 50 to win.bottom - 30
$navY = $wr.B - 50

# Left buttons: x relative to window-left + ~60/180/300/420 + 50 (button half width)
# Right buttons: x = window-right - (1920 - 1440) + 50 etc.
# After Anchor.Right fix: settings/user/exit are at offsets 480/360/240 from right
# in original 1920 design; with client width 2576 they remain at right-anchor.
# Button width=100, center at offsetFromRight - 50

# TODO: Title bar adds ~32 to top. Bottom nav y_in_client + ~32 from win.top
# But since we measure from win.bottom, the title doesn't matter.

$tabs = @(
    @{ name="work";     x = $wr.L + 60  + 50 ; file="tab_work.png" },
    @{ name="workinfo"; x = $wr.L + 180 + 50 ; file="tab_workinfo.png" },
    @{ name="history";  x = $wr.L + 300 + 50 ; file="tab_history.png" },
    @{ name="recipe";   x = $wr.L + 420 + 50 ; file="tab_recipe.png" },
    @{ name="settings"; x = $wr.R - 480 + 50 ; file="tab_settings.png" },
    @{ name="user";     x = $wr.R - 360 + 50 ; file="tab_user.png" }
)

foreach ($t in $tabs) {
    [W3]::SetCursorPos($t.x, $navY)
    Start-Sleep -Milliseconds 250
    [W3]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
    [W3]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 700

    # Capture
    $bmp = New-Object System.Drawing.Bitmap $ww, $wh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($wr.L, $wr.T, 0, 0, (New-Object System.Drawing.Size $ww, $wh))
    $out = Join-Path $OutDir $t.file
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "[$($t.name)] click x=$($t.x) y=$navY  -> $out"
}
