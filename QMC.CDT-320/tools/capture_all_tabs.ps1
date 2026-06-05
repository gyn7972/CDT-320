# capture_all_tabs.ps1
# Cycles through bottom nav tabs (Work/WorkInfo/History/Recipe/Settings/User)
# and captures each one.
# Uses UIAutomation to find and invoke buttons.

param(
    [string]$ProcessName = "QMC.CDT-320",
    [string]$OutDir      = "."
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W2 {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
}
"@

function Capture($outPath) {
    $proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $proc) { return }
    $h = $proc.MainWindowHandle
    if ([W2]::IsIconic($h)) { [W2]::ShowWindow($h, 9) | Out-Null; Start-Sleep -Milliseconds 400 }
    [W2]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 500
    $rect = New-Object W2+RECT
    [W2]::GetWindowRect($h, [ref]$rect) | Out-Null
    $w = $rect.R - $rect.L
    $hh = $rect.B - $rect.T
    if ($w -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap $w, $hh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.L, $rect.T, 0, 0, (New-Object System.Drawing.Size $w, $hh))
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Saved $outPath ($($w)x$($hh))"
}

function FindWindow {
    $proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $proc) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    return [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}

function ClickByName {
    param([string]$Name)
    $win = FindWindow
    if (-not $win) { Write-Output "Window not found"; return }
    # Look for Button or Pane with Name
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    $el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $el) { Write-Output "Element '$Name' not found"; return }
    # Move mouse over and click using location
    $r = $el.Current.BoundingRectangle
    $cx = [int]($r.Left + $r.Width / 2)
    $cy = [int]($r.Top + $r.Height / 2)
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class M2 {
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint d, IntPtr e);
}
"@ -ErrorAction SilentlyContinue
    [M2]::SetCursorPos($cx, $cy)
    Start-Sleep -Milliseconds 200
    [M2]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)  # LDOWN
    [M2]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)  # LUP
    Start-Sleep -Milliseconds 400
}

# Tab labels in current language (Korean)
$tabs = @(
    @{ name="작업";       file="tab_work" },
    @{ name="작업 정보";  file="tab_workinfo" },
    @{ name="이력";       file="tab_history" },
    @{ name="레시피";     file="tab_recipe" },
    @{ name="설정";       file="tab_settings" },
    @{ name="사용자";     file="tab_user" }
)

foreach ($t in $tabs) {
    Write-Output "=> Tab: $($t.name)"
    ClickByName -Name $t.name
    Start-Sleep -Milliseconds 600
    Capture -outPath (Join-Path $OutDir "$($t.file).png")
}
