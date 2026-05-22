# Stage 60 — 여러 페이지를 cross-tab --start-page 로 차례차례 캡처
# 각 페이지마다: handler 종료 → 재시작 (--start-page <key>) → 8초 대기 → half-res 캡처
param(
    [string[]]$Keys = @(
        'work.page.main',
        'work.inputMapTransfer',
        'work.visionAlign',
        'wi.frontHead',
        'wi.inputCassette',
        'hist.alarm',
        'hist.event',
        'recipe.project',
        'recipe.frontHead',
        'recipe.inputStage',
        'recipe.inputVision',
        'recipe.inputMapCreate',
        'set.motion',
        'set.teach',
        'set.lightSetup'
    )
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr d, uint f);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h, int n);
}
"@

function CapturePage([string]$Key, [string]$Out) {
    Stop-Process -Name 'QMC.CDT-320' -Force -EA SilentlyContinue
    Start-Sleep -Seconds 1
    Start-Process -FilePath "D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\bin\Debug\QMC.CDT-320.exe" -WorkingDirectory "D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\bin\Debug" -ArgumentList "--start-page", $Key
    Start-Sleep -Seconds 7

    $proc = Get-Process -Name 'QMC.CDT-320' -EA SilentlyContinue | Select-Object -First 1
    if (-not $proc) { Write-Output "  [$Key] FAILED to start"; return }
    $hwnd = $proc.MainWindowHandle
    [W]::ShowWindowAsync($hwnd, 5) | Out-Null
    [W]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 500

    $rect = New-Object W+RECT
    [W]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { Write-Output "  [$Key] invalid rect"; return }

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
    $half.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    $half.Dispose()
    Write-Output "  [$Key] -> $Out ($newW x $newH)"
}

foreach ($k in $Keys) {
    $safe = $k -replace '\.','_'
    $out  = "D:\Work\CDT-320\demo_audit_${safe}.png"
    CapturePage $k $out
}
Stop-Process -Name 'QMC.CDT-320' -Force -EA SilentlyContinue
Write-Output "DONE"
