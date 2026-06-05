# click_cycle_only.ps1 — CYCLE RUN 만 클릭
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class M2 {
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint a, uint b, uint c, uint d, IntPtr e);
}
"@

$auto = [System.Windows.Automation.AutomationElement]::RootElement
$proc = Get-Process -Name "QMC.CDT-320" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

$c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "CYCLE RUN")
$btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
if (-not $btn) { Write-Output "CYCLE RUN button not found"; exit 2 }

$r = $btn.Current.BoundingRectangle
$cx = [int]($r.Left + $r.Width / 2)
$cy = [int]($r.Top + $r.Height / 2)
[M2]::SetCursorPos($cx, $cy)
Start-Sleep -Milliseconds 250
[M2]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
[M2]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
Write-Output ("Clicked CYCLE RUN at " + $cx + "," + $cy)
