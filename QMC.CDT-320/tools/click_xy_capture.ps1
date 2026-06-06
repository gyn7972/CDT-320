param([int]$X = 0, [int]$Y = 0, [string]$OutFile = "demo.png", [int]$WaitMs = 700)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$src = @'
using System;
using System.Runtime.InteropServices;
public class W2 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint a, uint b, uint c, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
'@
Add-Type -TypeDefinition $src
[W2]::SetProcessDPIAware() | Out-Null

$proc = Get-Process -Name 'QMC.CDT-320' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$hwnd = $proc.MainWindowHandle

if ([W2]::IsIconic($hwnd)) { [W2]::ShowWindowAsync($hwnd, 9) | Out-Null }
[W2]::ShowWindowAsync($hwnd, 5) | Out-Null
[W2]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 400

# Get window rect
$rect = New-Object W2+RECT
[W2]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
$winW = $rect.Right - $rect.Left
$winH = $rect.Bottom - $rect.Top
Write-Output ("Window rect: {0},{1} {2}x{3}" -f $rect.Left, $rect.Top, $winW, $winH)

if ($X -gt 0 -and $Y -gt 0) {
    # X/Y are window-relative pixels (in capture space). Convert to screen.
    $scrX = $rect.Left + $X
    $scrY = $rect.Top + $Y
    Write-Output ("Click at screen: $scrX,$scrY")
    [W2]::SetCursorPos($scrX, $scrY)
    Start-Sleep -Milliseconds 200
    [W2]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [W2]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds $WaitMs
}

# Capture via PrintWindow
$bmp = New-Object System.Drawing.Bitmap $winW, $winH
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[W2]::PrintWindow($hwnd, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$path = "D:\Work\CDT-320\$OutFile"
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output ("Captured -> $path")
