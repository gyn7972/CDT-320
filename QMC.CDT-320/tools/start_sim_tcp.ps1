# start_sim_tcp.ps1 - CDT320Simulator 의 TCP START 버튼을 자동 클릭
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$auto = [System.Windows.Automation.AutomationElement]::RootElement
# Simulator 메인 윈도우 찾기 (이름은 CDT320Simulator 또는 유사)
$proc = Get-Process -Name "CDT320Simulator" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Simulator not running"; exit 1 }
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Output "Simulator UIA window not found"; exit 2 }
Write-Output ("Simulator window: " + $win.Current.Name)

# TCP START 버튼 찾기
$btnConds = @("TCP START", "Start TCP", "TCP Start", "START")
foreach ($name in $btnConds) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
    if ($btn) {
        Write-Output ("Button found: " + $name)
        try {
            $pat = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            $pat.Invoke()
            Write-Output "Invoked OK"
            exit 0
        } catch {
            Write-Output ("Invoke failed: " + $_.Exception.Message)
        }
    }
}

# 못 찾으면 모든 버튼 나열
Write-Output "Buttons found in window:"
$btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$btns = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
foreach ($b in $btns) {
    Write-Output ("  - " + $b.Current.Name)
}
