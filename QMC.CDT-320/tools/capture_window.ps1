# capture_window.ps1
# Captures the QMC.CDT-320 main window to a PNG file.
# Usage: powershell -File capture_window.ps1 -OutPath screenshot.png

param(
    [string]$ProcessName = "QMC.CDT-320",
    [string]$OutPath     = "screenshot.png"
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Win32 API for window rect
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
}
"@

$proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Error "Process $ProcessName not found"; exit 1 }
$h = $proc.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Error "Main window handle is null"; exit 1 }

# If minimized, restore
if ([W]::IsIconic($h)) { [W]::ShowWindow($h, 9) | Out-Null; Start-Sleep -Milliseconds 400 }
[W]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 250

$rect = New-Object W+RECT
[W]::GetWindowRect($h, [ref]$rect) | Out-Null
$w = $rect.R - $rect.L
$hh = $rect.B - $rect.T
if ($w -le 0 -or $hh -le 0) { Write-Error "Invalid window rect"; exit 1 }

$bmp = New-Object System.Drawing.Bitmap $w, $hh
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($rect.L, $rect.T, 0, 0, (New-Object System.Drawing.Size $w, $hh))
$bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

Write-Output "Saved $OutPath  ($($w)x$($hh))"
