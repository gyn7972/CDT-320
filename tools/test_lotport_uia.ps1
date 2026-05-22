# test_lotport_uia.ps1 - Click LIFT WAFER buttons via UIA InvokePattern (reliable).
param([string]$OutDir = ".")

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W5 {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
}
"@

$proc = Get-Process -Name "QMC.CDT-320" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$hwnd = $proc.MainWindowHandle
if ([W5]::IsIconic($hwnd)) { [W5]::ShowWindow($hwnd, 9) | Out-Null; Start-Sleep -Milliseconds 400 }
[W5]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 800

# Find main window via UIA
$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win  = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Output "UIA window not found"; exit 2 }
Write-Output "UIA window: $($win.Current.Name)"

function Find-ByName($parent, $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Invoke-Or-Click($el) {
    if (-not $el) { return $false }
    try {
        $p = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $p.Invoke()
        return $true
    } catch {}
    try {
        $p = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $p.Select()
        return $true
    } catch {}
    return $false
}

function Capture($name) {
    $wr = New-Object W5+RECT
    [W5]::GetWindowRect($hwnd, [ref]$wr) | Out-Null
    $ww = $wr.R - $wr.L
    $wh = $wr.B - $wr.T
    $bmp = New-Object System.Drawing.Bitmap $ww, $wh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($wr.L, $wr.T, 0, 0, (New-Object System.Drawing.Size $ww, $wh))
    $out = Join-Path $OutDir $name
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Saved $out"
}

# 1) WorkInfo tab
Write-Output "[1] Click bottom-nav tab.workInfo"
$workInfoBtn = Find-ByName $win "i"
if (-not $workInfoBtn) { $workInfoBtn = Find-ByName $win "i 작업 정보" }  # sometimes label
$ok = Invoke-Or-Click $workInfoBtn
Write-Output "  WorkInfo invoke: $ok"
Start-Sleep -Milliseconds 800

# 2) MAPPING button
Write-Output "[2] Click LIFT WAFER MAPPING"
$mapBtn = Find-ByName $win "LIFT WAFER MAPPING"
$ok = Invoke-Or-Click $mapBtn
Write-Output "  Mapping invoke: $ok"
Start-Sleep -Milliseconds 9000

Capture "ui_after_map.png"

# 3) LOADING button
Write-Output "[3] Click LIFT WAFER LOADING"
$loadBtn = Find-ByName $win "LIFT WAFER LOADING"
$ok = Invoke-Or-Click $loadBtn
Write-Output "  Loading invoke: $ok"
Start-Sleep -Milliseconds 5000

Capture "ui_after_load.png"

Write-Output "Done"
