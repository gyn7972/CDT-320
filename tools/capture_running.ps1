param([string]$OutDir = "")
if ([string]::IsNullOrEmpty($OutDir)) { $OutDir = Split-Path $MyInvocation.MyCommand.Path -Parent }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32R {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
}
"@

function Capture-WindowByProcess {
    param([string]$ProcessName, [string]$FileName)
    $proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $proc) { Write-Output "[$ProcessName] not running"; return }
    $hwnd = $proc.MainWindowHandle
    if ([Win32R]::IsIconic($hwnd)) { [Win32R]::ShowWindowAsync($hwnd, 9) | Out-Null }
    [Win32R]::ShowWindowAsync($hwnd, 5) | Out-Null
    [Win32R]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 250
    $rect = New-Object Win32R+RECT
    [Win32R]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { Write-Output "[$ProcessName] invalid rect"; return }
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $path = Join-Path $OutDir $FileName
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output ("[$ProcessName] -> $path  ($w x $h)")
}

Capture-WindowByProcess -ProcessName "QMC.CDT-320"     -FileName "demo_running_handler.png"
Capture-WindowByProcess -ProcessName "QMC.Vision"      -FileName "demo_running_vision.png"
Capture-WindowByProcess -ProcessName "CDT320Simulator" -FileName "demo_running_simulator.png"
