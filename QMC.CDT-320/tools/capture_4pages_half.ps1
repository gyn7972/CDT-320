# Capture 4 new Stage 59 settings pages at half resolution
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$src = @'
using System;
using System.Runtime.InteropServices;
public class W {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint a, uint b, uint c, uint d, IntPtr e);
}
'@
Add-Type -TypeDefinition $src

$proc = Get-Process -Name 'QMC.CDT-320' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Handler not running"; exit 1 }
$hwnd = $proc.MainWindowHandle
if ([W]::IsIconic($hwnd)) { [W]::ShowWindowAsync($hwnd, 9) | Out-Null }
[W]::ShowWindowAsync($hwnd, 5) | Out-Null
[W]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 500

$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Output "UIA window missing"; exit 2 }

function Click-ByName($name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
    if (-not $btn) { Write-Output "  NOT FOUND: $name"; return $false }
    $r = $btn.Current.BoundingRectangle
    [W]::SetCursorPos([int]($r.Left + $r.Width/2), [int]($r.Top + $r.Height/2))
    Start-Sleep -Milliseconds 150
    [W]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [W]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 600
    return $true
}

function Capture-Half($outFile) {
    $rect = New-Object W+RECT
    [W]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [W]::PrintWindow($hwnd, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
    $g.Dispose()
    # PrintWindow 결과가 검정이면 CopyFromScreen 폴백
    $sample = $bmp.GetPixel(10, 10)
    if ($sample.R -lt 5 -and $sample.G -lt 5 -and $sample.B -lt 5) {
        $bmp.Dispose()
        $bmp = New-Object System.Drawing.Bitmap $w, $h
        $g2 = [System.Drawing.Graphics]::FromImage($bmp)
        $g2.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
        $g2.Dispose()
    }
    $newW = [int][Math]::Round($w / 2)
    $newH = [int][Math]::Round($h / 2)
    $half = New-Object System.Drawing.Bitmap $newW, $newH
    $gh = [System.Drawing.Graphics]::FromImage($half)
    $gh.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gh.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $gh.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $gh.DrawImage($bmp, 0, 0, $newW, $newH)
    $gh.Dispose()
    $bmp.Dispose()
    $path = "D:\Work\CDT-320\$outFile"
    $half.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $half.Dispose()
    Write-Output ("  Saved -> $path  ($newW x $newH, was $w x $h)")
}

# Settings 탭 진입
Write-Output "Step 1: Click Settings tab"
$null = Click-ByName ([char]0xC124 + [char]0xC815)  # 설정
Start-Sleep -Milliseconds 600

$pages = @(
    @{ name = '위치 티칭';   file = 'demo_p1_teach.png' },
    @{ name = '축 셋업';     file = 'demo_p2_axis.png' },
    @{ name = '카메라 셋업'; file = 'demo_p3_cam.png' },
    @{ name = '조명 셋업';   file = 'demo_p4_light.png' }
)
foreach ($p in $pages) {
    Write-Output ("Step: Click '$($p.name)'")
    $ok = Click-ByName $p.name
    if (-not $ok) { Write-Output "  SKIP capture (button not found)"; continue }
    Capture-Half $p.file
}
Write-Output "DONE"
