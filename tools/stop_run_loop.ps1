Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WLoop {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint procId);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT p);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, IntPtr dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x; public int y; }
}
"@

function Activate-Handler {
    $p = Get-Process -Name 'QMC.CDT-320' -EA SilentlyContinue | Where-Object { $_.MainWindowTitle -eq 'CDT-320' } | Select-Object -First 1
    if (-not $p) { return $false }
    $hWnd = $p.MainWindowHandle
    $fore = [WLoop]::GetForegroundWindow()
    [uint32]$fp = 0
    $ft = [WLoop]::GetWindowThreadProcessId($fore, [ref]$fp)
    $ct = [WLoop]::GetCurrentThreadId()
    [WLoop]::AttachThreadInput($ct, $ft, $true) | Out-Null
    [WLoop]::ShowWindow($hWnd, 3) | Out-Null
    [WLoop]::SetForegroundWindow($hWnd) | Out-Null
    [WLoop]::AttachThreadInput($ct, $ft, $false) | Out-Null
    Start-Sleep -Milliseconds 300
    return $true
}

function Send-MouseClick {
    param([int]$x, [int]$y)
    [WLoop]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 100
    [WLoop]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)   # LEFTDOWN
    Start-Sleep -Milliseconds 50
    [WLoop]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)   # LEFTUP
}

$end = (Get-Date).Date.AddHours(11).AddMinutes(30)
$logPath = 'D:\Work\CDT-320\tools\stop_run_loop.log'
"Loop start at $(Get-Date)  end target = $end" | Out-File $logPath

while ((Get-Date) -lt $end) {
    # 5분 동안 사이클 진행 (이미 RUN 중)
    Start-Sleep -Seconds 300

    if ((Get-Date) -ge $end) { break }

    # CYCLE STOP 클릭 (1387, 250)
    if (Activate-Handler) {
        Send-MouseClick -x 1387 -y 250
        Add-Content $logPath "$(Get-Date)  STOP clicked"
    }
    Start-Sleep -Seconds 5

    # CYCLE RUN 클릭 (1387, 223)
    if (Activate-Handler) {
        Send-MouseClick -x 1387 -y 223
        Add-Content $logPath "$(Get-Date)  RUN clicked"
    }
}
Add-Content $logPath "$(Get-Date)  Loop end"
