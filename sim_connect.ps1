Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class M3 {
    [DllImport("user32.dll")] public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
"@

$proc = Get-Process -Name 'QMC.CDT-320' -EA SilentlyContinue | Select-Object -First 1
$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

function Click-ByName([string]$nm) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $nm)
    $all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $c)
    Write-Output "='$nm' → $($all.Count) 개"
    $best = $null; $bestX = -1
    foreach ($el in $all) {
        $r = $el.Current.BoundingRectangle
        if ($r.Left -gt $bestX) { $bestX = $r.Left; $best = $el }
    }
    if ($best -eq $null) { return $false }
    $nh = $best.Current.NativeWindowHandle
    Write-Output "  Hwnd=$nh, Type=$($best.Current.ControlType.ProgrammaticName), Enabled=$($best.Current.IsEnabled)"
    if ($nh -eq 0) { return $false }
    $WM_DOWN = 0x0201; $WM_UP = 0x0202; $lp = (5 -bor (5 -shl 16))
    [M3]::PostMessage([IntPtr]$nh, $WM_DOWN, [IntPtr]1, [IntPtr]$lp) | Out-Null
    Start-Sleep -Milliseconds 80
    [M3]::PostMessage([IntPtr]$nh, $WM_UP, [IntPtr]0, [IntPtr]$lp) | Out-Null
    return $true
}

# 1) 하단 탭바의 "설정"
Click-ByName "설정"
Start-Sleep -Milliseconds 700

# 2) Settings 사이드바의 "시뮬레이터 연결"
Click-ByName "시뮬레이터 연결"
Start-Sleep -Seconds 1

# 3) 페이지의 "연결" 버튼
Click-ByName "연결"
Start-Sleep -Seconds 2

Write-Output "=== Handler outgoing TCP (after Connect attempt) ==="
$h = (Get-Process -Name 'QMC.CDT-320' -EA SilentlyContinue).Id
Get-NetTCPConnection -State Established -EA SilentlyContinue | Where-Object { $_.OwningProcess -eq $h } | Select-Object LocalPort, RemoteAddress, RemotePort | Sort-Object RemotePort | Format-Table | Out-String
