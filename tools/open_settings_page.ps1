param([string]$PageName = "위치 티칭", [string]$OutFile = "demo_page.png")

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$src = @'
using System;
using System.Runtime.InteropServices;
public class W {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint a, uint b, uint c, uint d, IntPtr e);
}
'@
Add-Type -TypeDefinition $src

$proc = Get-Process -Name 'QMC.CDT-320' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$hwnd = $proc.MainWindowHandle

if ([W]::IsIconic($hwnd)) { [W]::ShowWindowAsync($hwnd, 9) | Out-Null }
[W]::ShowWindowAsync($hwnd, 5) | Out-Null
[W]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 400

$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Output "UIA window missing"; exit 2 }

# 1) 하단 "설정" 탭 클릭 — Name=설정
$settingsTabName = [char]0xC124 + [char]0xC815   # 설정
$cSettings = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $settingsTabName)
$btnSettings = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cSettings)
if ($btnSettings) {
    $r = $btnSettings.Current.BoundingRectangle
    [W]::SetCursorPos([int]($r.Left + $r.Width/2), [int]($r.Top + $r.Height/2))
    Start-Sleep -Milliseconds 200
    [W]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [W]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
    Write-Output "Clicked: Settings tab"
    Start-Sleep -Milliseconds 500
} else {
    Write-Output "Settings tab not found"
}

# 2) 사이드바에서 PageName 클릭
$cPage = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $PageName)
$btnPage = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cPage)
if ($btnPage) {
    $r = $btnPage.Current.BoundingRectangle
    [W]::SetCursorPos([int]($r.Left + $r.Width/2), [int]($r.Top + $r.Height/2))
    Start-Sleep -Milliseconds 200
    [W]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [W]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
    Write-Output "Clicked: $PageName"
    Start-Sleep -Milliseconds 700
} else {
    Write-Output "Page not found: $PageName"
}

# 3) PrintWindow 캡처
$rect = New-Object W+RECT
[W]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[W]::PrintWindow($hwnd, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$path = "D:\Work\CDT-320\$OutFile"
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output ("Captured -> $path  ($w x $h)")
