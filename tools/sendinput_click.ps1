param([string]$ButtonName = "위치 티칭", [string]$OutFile = "demo_page.png")

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

$src = @'
using System;
using System.Runtime.InteropServices;
public class SI {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int  GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT {
        public uint type;
        public MOUSEINPUT mi;
        public int pad1; public int pad2; public int pad3;
    }
    public const uint MOUSEEVENTF_MOVE         = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN     = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP       = 0x0004;
    public const uint MOUSEEVENTF_ABSOLUTE     = 0x8000;
}
'@
Add-Type -TypeDefinition $src

$proc = Get-Process -Name 'QMC.CDT-320' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$hwnd = $proc.MainWindowHandle
[SI]::ShowWindowAsync($hwnd, 5) | Out-Null
[SI]::BringWindowToTop($hwnd) | Out-Null
[SI]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 400

# UIA로 버튼 좌표 찾기
$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
$nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $ButtonName)
$el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nameCond)
if (-not $el) { Write-Output "Not found: $ButtonName"; exit 2 }
$r = $el.Current.BoundingRectangle
$cx = [int]($r.Left + $r.Width/2)
$cy = [int]($r.Top + $r.Height/2)

# Convert to ABSOLUTE coords (0..65535 across virtual desktop)
$sx = [SI]::GetSystemMetrics(0)   # SM_CXSCREEN
$sy = [SI]::GetSystemMetrics(1)
$ax = [int](($cx * 65535) / $sx)
$ay = [int](($cy * 65535) / $sy)

Write-Output ("Target screen=($cx,$cy) abs=($ax,$ay) screen-size=($sx x $sy)")

$inputs = New-Object SI+INPUT[] 3
$inputs[0].type = 0   # INPUT_MOUSE
$inputs[0].mi.dx = $ax
$inputs[0].mi.dy = $ay
$inputs[0].mi.dwFlags = [SI]::MOUSEEVENTF_MOVE -bor [SI]::MOUSEEVENTF_ABSOLUTE
$inputs[1].type = 0
$inputs[1].mi.dwFlags = [SI]::MOUSEEVENTF_LEFTDOWN
$inputs[2].type = 0
$inputs[2].mi.dwFlags = [SI]::MOUSEEVENTF_LEFTUP
$result = [SI]::SendInput(3, $inputs, [System.Runtime.InteropServices.Marshal]::SizeOf([type][SI+INPUT]))
Write-Output ("SendInput sent: $result events")
Start-Sleep -Milliseconds 1200

# Capture
$rect = New-Object SI+RECT
[SI]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[SI]::PrintWindow($hwnd, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$path = "D:\Work\CDT-320\$OutFile"
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output ("Captured -> $path")
