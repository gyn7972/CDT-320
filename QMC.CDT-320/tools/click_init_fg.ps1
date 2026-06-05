Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$src = @'
using System;
using System.Runtime.InteropServices;
public class FG {
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint a, uint b, uint c, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
}
'@
Add-Type -TypeDefinition $src

$proc = Get-Process -Name 'QMC.CDT-320' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output 'Handler not running'; exit 1 }
$hwnd = $proc.MainWindowHandle

if ([FG]::IsIconic($hwnd)) { [FG]::ShowWindowAsync($hwnd, 9) | Out-Null }
[FG]::ShowWindowAsync($hwnd, 5) | Out-Null

$fg = [FG]::GetForegroundWindow()
$fgPid = 0
[FG]::GetWindowThreadProcessId($fg, [ref]$fgPid) | Out-Null
$fgTid = 0
try { $fgTid = (Get-Process -Id $fgPid -ErrorAction SilentlyContinue).Threads[0].Id } catch {}
$myTid = [FG]::GetCurrentThreadId()
if ($fgTid -gt 0) { [FG]::AttachThreadInput($myTid, $fgTid, $true) | Out-Null }
[FG]::BringWindowToTop($hwnd) | Out-Null
[FG]::SetForegroundWindow($hwnd) | Out-Null
if ($fgTid -gt 0) { [FG]::AttachThreadInput($myTid, $fgTid, $false) | Out-Null }
Start-Sleep -Milliseconds 500

$fgNow = [FG]::GetForegroundWindow()
Write-Output ('Foreground hwnd=' + $fgNow + ' handler=' + $hwnd + ' match=' + ($fgNow -eq $hwnd))

$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Output 'UIA window missing'; exit 2 }

$btnName = [char]0xCD08 + [char]0xAE30 + [char]0xD654
$c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $btnName)
$btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
if (-not $btn) { Write-Output 'Init btn missing'; exit 3 }

$r  = $btn.Current.BoundingRectangle
$cx = [int]($r.Left + $r.Width / 2)
$cy = [int]($r.Top + $r.Height / 2)
[FG]::SetCursorPos($cx, $cy)
Start-Sleep -Milliseconds 300
[FG]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 50
[FG]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
Write-Output ('Clicked Init at ' + $cx + ',' + $cy)
