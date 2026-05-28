$inPath = 'D:\Work\CDT-320\AxisMotionParams.xlsx'
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

try {
    $wb = $excel.Workbooks.Open($inPath)
    $ws = $wb.Worksheets.Item('AxisMotionParams')
    $usedRows = $ws.UsedRange.Rows.Count
    Write-Output "ROWS=$usedRows"

    # Header
    $headers = @()
    for ($c = 1; $c -le 15; $c++) {
        $headers += $ws.Cells.Item(1, $c).Value2
    }
    Write-Output ("HEADER: " + ($headers -join ' | '))

    # Data rows
    for ($r = 2; $r -le $usedRows; $r++) {
        $row = @()
        for ($c = 1; $c -le 15; $c++) {
            $v = $ws.Cells.Item($r, $c).Value2
            if ($v -eq $null) { $v = '' }
            $row += $v
        }
        Write-Output ("ROW " + ($r-2).ToString('D2') + ": " + ($row -join ' | '))
    }
    $wb.Close($false)
}
finally {
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    [System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers()
}
