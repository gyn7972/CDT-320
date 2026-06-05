param([string]$OutDir = "")
if ([string]::IsNullOrEmpty($OutDir)) { $OutDir = Split-Path $MyInvocation.MyCommand.Path -Parent }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$src = @'
using System;
using System.Runtime.InteropServices;
using System.Drawing;
public class PW {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
}
'@
Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing

function Capture-WindowPW {
    param([string]$ProcessName, [string]$FileName)
    $proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $proc) { Write-Output "[$ProcessName] not running"; return }
    $hwnd = $proc.MainWindowHandle
    if ([PW]::IsIconic($hwnd)) { [PW]::ShowWindowAsync($hwnd, 9) | Out-Null; Start-Sleep -Milliseconds 300 }
    $rect = New-Object PW+RECT
    [PW]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { Write-Output "[$ProcessName] invalid rect"; return }
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    # PW_RENDERFULLCONTENT = 0x00000002 (Windows 8.1+)
    $ok = [PW]::PrintWindow($hwnd, $hdc, 2)
    $g.ReleaseHdc($hdc)
    if (-not $ok) {
        # Fallback to client-only capture
        [PW]::PrintWindow($hwnd, $g.GetHdc(), 0) | Out-Null
    }
    $path = Join-Path $OutDir $FileName
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output ("[$ProcessName] -> $path  ($w x $h)")
}

Capture-WindowPW -ProcessName "QMC.CDT-320"     -FileName "demo_pw_handler.png"
Capture-WindowPW -ProcessName "QMC.Vision"      -FileName "demo_pw_vision.png"
Capture-WindowPW -ProcessName "CDT320Simulator" -FileName "demo_pw_simulator.png"
