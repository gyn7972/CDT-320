# gui_cycle_automation.ps1 - Handler GUI Cycle automation (Stage 5, ASCII only)
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File gui_cycle_automation.ps1

Param(
    [int]$WaitSeconds = 30,
    [string]$ExeRoot = "D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\bin\Debug",
    [string]$LotLogDir = "D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\bin\Debug\Log\Lots"
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

function LogMsg($m) {
    $ts = Get-Date -Format "HH:mm:ss"
    Write-Host "[$ts] $m"
}

function Find-CDT320Window {
    $auto = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition `
        ([System.Windows.Automation.AutomationElement]::NameProperty), "CDT-320"
    $win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
    return $win
}

function Find-ButtonByText($parent, $textPatterns) {
    foreach ($t in $textPatterns) {
        $cond = New-Object System.Windows.Automation.PropertyCondition `
            ([System.Windows.Automation.AutomationElement]::NameProperty), $t
        $btn = $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($btn) { return $btn }
    }
    return $null
}

function Click-Element($el) {
    if (-not $el) { return $false }
    try {
        $pat = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        if ($pat) { $pat.Invoke(); return $true }
    } catch {}
    try {
        $pat = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        if ($pat) { $pat.Select(); return $true }
    } catch {}
    return $false
}

LogMsg "GUI Cycle Automation Start"

$handler = Get-Process -Name "QMC.CDT-320" -ErrorAction SilentlyContinue
if (-not $handler) {
    $exe = Join-Path $ExeRoot "QMC.CDT-320.exe"
    if (-not (Test-Path $exe)) { LogMsg "Handler exe not found: $exe"; exit 1 }
    LogMsg "Handler launching..."
    Start-Process -FilePath $exe -WorkingDirectory $ExeRoot
    Start-Sleep -Seconds 6
    $handler = Get-Process -Name "QMC.CDT-320" -ErrorAction SilentlyContinue
    if (-not $handler) { LogMsg "Handler launch failed"; exit 2 }
}
LogMsg "Handler PID=$($handler.Id)"

$win = $null
for ($i = 0; $i -lt 10; $i++) {
    $win = Find-CDT320Window
    if ($win) { break }
    Start-Sleep -Seconds 1
}
if (-not $win) { LogMsg "Main window not found"; exit 3 }
LogMsg "Main window OK"

$beforeLots = @()
if (Test-Path $LotLogDir) {
    $beforeLots = @(Get-ChildItem $LotLogDir -Filter "*.json" -ErrorAction SilentlyContinue)
}
LogMsg "Lot file baseline: $($beforeLots.Count)"

$initBtn = Find-ButtonByText $win @("INITIALIZE", "Initialize", "Init")
if ($initBtn) {
    $r = Click-Element $initBtn
    LogMsg "Initialize click: $r"
    Start-Sleep -Seconds 4
}
else { LogMsg "Initialize button not found" }

$cycleBtn = Find-ButtonByText $win @("CYCLE RUN", "CycleRun", "Cycle Run", "Run")
if (-not $cycleBtn) { LogMsg "Cycle Run button not found"; exit 4 }
$r = Click-Element $cycleBtn
LogMsg "CycleRun click: $r"

LogMsg "Waiting $WaitSeconds seconds..."
Start-Sleep -Seconds $WaitSeconds

$stopBtn = Find-ButtonByText $win @("CYCLE STOP", "CycleStop", "Cycle Stop", "Stop")
if ($stopBtn) {
    $r = Click-Element $stopBtn
    LogMsg "CycleStop click: $r"
}
else { LogMsg "Cycle Stop button not found - natural end" }
Start-Sleep -Seconds 3

$afterLots = @()
if (Test-Path $LotLogDir) {
    $afterLots = @(Get-ChildItem $LotLogDir -Filter "*.json" -ErrorAction SilentlyContinue)
}
$newLots = $afterLots.Count - $beforeLots.Count
LogMsg "Lot files new: $newLots"
if ($newLots -gt 0) {
    $latest = $afterLots | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    LogMsg "Latest Lot: $($latest.FullName)"
    $content = Get-Content $latest.FullName -Raw
    LogMsg "Lot JSON size: $($content.Length) bytes"
}

LogMsg "Done"
exit 0
