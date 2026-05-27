# Stage 60 — 사이드바(우측) 클릭 검증
# 1) handler 종료 후 --start-page set.teach 로 시작 (위치 티칭 페이지에 진입)
# 2) UIA FindAll 로 "축 셋업" 이름의 element 모두 찾고 화면 우측(X > 1500) 만 필터
# 3) 그 중 첫번째 클릭 → 5초 대기 → 캡처
# 4) 캡처 보고 페이지가 변경됐는지 확인
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$src = @'
using System;
using System.Runtime.InteropServices;
public class W {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr d, uint f);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint a, uint b, uint c, uint d, IntPtr e);
}
'@
Add-Type -TypeDefinition $src

# 1) handler 재시작
Stop-Process -Name 'QMC.CDT-320' -Force -EA SilentlyContinue
Start-Sleep -Seconds 1
Start-Process -FilePath "D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\bin\Debug\QMC.CDT-320.exe" -WorkingDirectory "D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\bin\Debug" -ArgumentList "--start-page", "set.teach"
Start-Sleep -Seconds 8

$proc = Get-Process -Name 'QMC.CDT-320' -EA SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "Failed to start"; exit 1 }
$hwnd = $proc.MainWindowHandle
[W]::ShowWindowAsync($hwnd, 5) | Out-Null
[W]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 500

# 2) UIA: "축 셋업" 검색 (전부) → X 좌표 큰 것 (사이드바)
$auto = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $auto.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Output "UIA window missing"; exit 2 }

$targetName = [char]0xCD95 + ' ' + [char]0xC14B + [char]0xC5C5  # "축 셋업"
$cName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $targetName)
$all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cName)
Write-Output ("Found {0} elements named '{1}':" -f $all.Count, $targetName)

# 가장 X 좌표 큰 element (사이드바일 가능성 가장 높음) 선택
$best = $null
$bestX = -1
foreach ($el in $all) {
    $r = $el.Current.BoundingRectangle
    Write-Output ("  ControlType={0,-12} X={1,4} Y={2,4} W={3,4} H={4,4} Class={5}" -f $el.Current.ControlType.ProgrammaticName, [int]$r.Left, [int]$r.Top, [int]$r.Width, [int]$r.Height, $el.Current.ClassName)
    if ($r.Left -gt $bestX) { $bestX = $r.Left; $best = $el }
}
if (-not $best) { Write-Output "No element matched"; exit 3 }
$br = $best.Current.BoundingRectangle
$cx = [int]($br.Left + $br.Width/2); $cy = [int]($br.Top + $br.Height/2)
Write-Output ("Click target: X={0} Y={1}" -f $cx, $cy)

# 3) 클릭
[W]::SetCursorPos($cx, $cy)
Start-Sleep -Milliseconds 200
[W]::mouse_event(0x02, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 50
[W]::mouse_event(0x04, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 800

# 4) 캡처 (half-res)
$rect = New-Object W+RECT
[W]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[W]::PrintWindow($hwnd, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc); $g.Dispose()
$newW = [int]($w/2); $newH = [int]($h/2)
$half = New-Object System.Drawing.Bitmap $newW, $newH
$gh = [System.Drawing.Graphics]::FromImage($half)
$gh.InterpolationMode = 'HighQualityBicubic'
$gh.DrawImage($bmp, 0, 0, $newW, $newH)
$gh.Dispose(); $bmp.Dispose()
$out = "D:\Work\CDT-320\demo_sidebar_test.png"
$half.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$half.Dispose()
Write-Output ("Saved {0} ({1}x{2})" -f $out, $newW, $newH)
