param([string]$ButtonName = "위치 티칭", [string]$OutFile = "demo_page.png")

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

$src = @'
using System;
using System.Runtime.InteropServices;
public class W3 {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}
'@
Add-Type -TypeDefinition $src

$proc = Get-Process -Name 'QMC.CDT-320' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$hwnd = $proc.MainWindowHandle
[W3]::ShowWindowAsync($hwnd, 5) | Out-Null
[W3]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 300

$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

# Find by Name (works for all language)
$nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $ButtonName)
$el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nameCond)
if (-not $el) { Write-Output "Element not found: $ButtonName"; exit 2 }

Write-Output ("Found '" + $ButtonName + "' at " + $el.Current.BoundingRectangle)

# Try LegacyIAccessible.DoDefaultAction
$ok = $false
try {
    $pat = $el.GetCurrentPattern([System.Windows.Automation.LegacyIAccessiblePattern]::Pattern)
    if ($pat) { $pat.DoDefaultAction(); Write-Output "DoDefaultAction OK"; $ok = $true }
} catch { Write-Output ("Legacy fail: " + $_.Exception.Message) }

# Try InvokePattern
if (-not $ok) {
    try {
        $pat = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        if ($pat) { $pat.Invoke(); Write-Output "Invoke OK"; $ok = $true }
    } catch { Write-Output ("Invoke fail: " + $_.Exception.Message) }
}

# Last resort — SendMessage WM_LBUTTONDOWN/UP via PostMessage to hwnd at relative coords
if (-not $ok) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WM {
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(System.Drawing.Point p);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point p);
    [DllImport("user32.dll")] public static extern int PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@ -ReferencedAssemblies System.Drawing
    $r = $el.Current.BoundingRectangle
    $cx = [int]($r.Left + $r.Width/2)
    $cy = [int]($r.Top + $r.Height/2)
    $pt = New-Object System.Drawing.Point $cx, $cy
    $childHwnd = [WM]::WindowFromPoint($pt)
    Write-Output ("Child hwnd at point: " + $childHwnd)
    $clientPt = $pt
    [WM]::ScreenToClient($childHwnd, [ref]$clientPt) | Out-Null
    $lp = ($clientPt.Y -shl 16) -bor ($clientPt.X -band 0xFFFF)
    [WM]::PostMessage($childHwnd, 0x0201, [IntPtr]1, [IntPtr]$lp) | Out-Null   # WM_LBUTTONDOWN
    Start-Sleep -Milliseconds 50
    [WM]::PostMessage($childHwnd, 0x0202, [IntPtr]0, [IntPtr]$lp) | Out-Null   # WM_LBUTTONUP
    Write-Output ("PostMessage WM_LBUTTONDOWN/UP to " + $childHwnd)
}

Start-Sleep -Milliseconds 1000

# Capture
$rect = New-Object W3+RECT
[W3]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[W3]::PrintWindow($hwnd, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$path = "D:\Work\CDT-320\$OutFile"
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output ("Captured -> $path")
