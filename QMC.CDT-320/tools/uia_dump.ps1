Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process -Name 'QMC.CDT-320' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }

$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Output "UIA window missing"; exit 2 }

# 모든 자식 element 수집 (Descendants)
$all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)

Write-Output ("Total elements: " + $all.Count)
Write-Output ""
Write-Output "X >= 2300 (right sidebar) elements:"
foreach ($e in $all) {
    $r = $e.Current.BoundingRectangle
    if ($r.Left -ge 2300 -and $r.Top -ge 100 -and $r.Top -le 1300) {
        Write-Output ("  [{0,4}x{1,4} - {2,4}x{3,4}]  CT={4,-15}  Name='{5}'" -f `
            [int]$r.Left, [int]$r.Top, [int]$r.Width, [int]$r.Height, `
            $e.Current.LocalizedControlType, $e.Current.Name)
    }
}
