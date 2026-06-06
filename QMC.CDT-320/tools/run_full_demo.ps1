# run_full_demo.ps1 — Handler 의 Init → CycleRun(8) 을 UIA 로 클릭
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-Window([string]$procName) {
    $auto = [System.Windows.Automation.AutomationElement]::RootElement
    $proc = Get-Process -Name $procName -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $proc) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    return $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class M {
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint a, uint b, uint c, uint d, IntPtr e);
}
"@

function Click-Button($win, [string[]]$names) {
    foreach ($name in $names) {
        $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
        $btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
        if ($btn) {
            # InvokePattern 시도
            try {
                $pat = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                $pat.Invoke()
                Write-Output ("  Invoked: " + $name)
                return $true
            } catch { }
            # Mouse click fallback
            try {
                $r = $btn.Current.BoundingRectangle
                $cx = [int]($r.Left + $r.Width / 2)
                $cy = [int]($r.Top + $r.Height / 2)
                [M]::SetCursorPos($cx, $cy)
                Start-Sleep -Milliseconds 250
                [M]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
                [M]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
                Write-Output ("  Clicked (mouse): " + $name + " at " + $cx + "," + $cy)
                return $true
            } catch {
                Write-Output ("  Click fail: " + $_.Exception.Message)
            }
        }
    }
    return $false
}

$h = Find-Window "QMC.CDT-320"
if (-not $h) { Write-Output "Handler not running"; exit 1 }
Write-Output ("Handler: " + $h.Current.Name)

Write-Output "[1] Click 초기화 (Init)"
Click-Button $h @("초기화", "Init", "INIT")
Start-Sleep -Seconds 5

Write-Output "[2] Click CYCLE RUN"
Click-Button $h @("CYCLE RUN", "CycleRun", "Cycle Run")
Write-Output "Cycle started. Watch the windows."
