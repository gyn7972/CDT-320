$axes = @(
    @(0,  'WAFER LIFTER_Z',        200,  'Y'),
    @(1,  'WAFER FEEDER_Y',        300,  ''),
    @(2,  'WAFER STAGE_Y',         400,  ''),
    @(3,  'WAFER STAGE_T',         360,  ''),
    @(4,  'WAFER EXPANDING_Z',     100,  ''),
    @(5,  'ALIGN VISION_X',        300,  ''),
    @(6,  'NEEDLE_X',              200,  ''),
    @(7,  'NEEDLE_Z',              100,  'Y'),
    @(8,  'EJECT PIN_Z',            50,  ''),
    @(9,  'FRONT PICKER_X',       1500,  ''),
    @(10, 'FRONT PICKER_Y',        750,  ''),
    @(11, 'FRONT PICKER_T0',       360,  ''),
    @(12, 'FRONT PICKER_Z0',        50,  ''),
    @(13, 'FRONT PICKER_T1',       360,  ''),
    @(14, 'FRONT PICKER_Z1',        50,  ''),
    @(15, 'FRONT PICKER_T2',       360,  ''),
    @(16, 'FRONT PICKER_Z2',        50,  ''),
    @(17, 'FRONT PICKER_T3',       360,  ''),
    @(18, 'FRONT PICKER_Z3',        50,  ''),
    @(19, 'FRONT SIDE VISION_Y0',  200,  ''),
    @(20, 'REAR SIDE VISION_Y0',   200,  ''),
    @(21, 'REAR PICKER_X',        1500,  ''),
    @(22, 'REAR PICKER_Y',         750,  ''),
    @(23, 'REAR PICKER_T0',        360,  ''),
    @(24, 'REAR PICKER_Z0',         50,  ''),
    @(25, 'REAR PICKER_T1',        360,  ''),
    @(26, 'REAR PICKER_Z1',         50,  ''),
    @(27, 'REAR PICKER_T2',        360,  ''),
    @(28, 'REAR PICKER_Z2',         50,  ''),
    @(29, 'REAR PICKER_T3',        360,  ''),
    @(30, 'REAR PICKER_Z3',         50,  ''),
    @(31, 'NG BIN_Y',              500,  ''),
    @(32, 'NG BIN_Z',              100,  ''),
    @(33, 'GOOD BIN_Y',            500,  ''),
    @(34, 'INSPECTION VISION_X',   300,  ''),
    @(35, 'BIN FEEDER_Y',          300,  ''),
    @(36, 'BIN LIFTER_Z',          200,  'Y')
)

$outPath = 'D:\Work\CDT-320\AxisMotionParams.xlsx'
if (Test-Path $outPath) { Remove-Item $outPath -Force }

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

try {
    $wb = $excel.Workbooks.Add()
    while ($wb.Worksheets.Count -gt 1) { $wb.Worksheets.Item($wb.Worksheets.Count).Delete() }

    $ws = $wb.Worksheets.Item(1)
    $ws.Name = 'AxisMotionParams'

    $headers = @(
        'No','Name','Stroke[mm]','Brake',
        'DefaultVel[mm/s]','MaxVel[mm/s]',
        'Accel[mm/s2]','Decel[mm/s2]','HomeVel[mm/s]',
        'JogCoarse[mm/s]','JogFine[mm/s]',
        'SoftLimit+[mm]','SoftLimit-[mm]','HomeOffset[mm]','Note'
    )
    $widths = @(6,26,12,8,17,14,15,15,15,16,14,15,15,16,30)

    for ($i = 0; $i -lt $headers.Count; $i++) {
        $c = $ws.Cells.Item(1, $i + 1)
        $c.Value = $headers[$i]
        $c.Font.Bold = $true
        $c.Font.Color = 0xFFFFFF
        $c.Font.Name = 'Arial'
        $c.Font.Size = 11
        $c.Interior.Color = 0x6A4A2A
        $c.HorizontalAlignment = -4108
        $c.VerticalAlignment   = -4108
        $c.WrapText = $true
        $ws.Columns.Item($i + 1).ColumnWidth = $widths[$i]
    }
    $ws.Rows.Item(1).RowHeight = 32

    for ($r = 0; $r -lt $axes.Count; $r++) {
        $row = $r + 2
        $a = $axes[$r]
        ($ws.Cells.Item($row, 1)).Value  = [int]$a[0]
        ($ws.Cells.Item($row, 2)).Value  = [string]$a[1]
        ($ws.Cells.Item($row, 3)).Value  = [int]$a[2]
        ($ws.Cells.Item($row, 4)).Value  = [string]$a[3]
        ($ws.Cells.Item($row, 5)).Value  = [double]100
        ($ws.Cells.Item($row, 7)).Value  = [double]500
        ($ws.Cells.Item($row, 8)).Value  = [double]500
        ($ws.Cells.Item($row, 9)).Value  = [double]200
        ($ws.Cells.Item($row, 10)).Value = [double]50
        ($ws.Cells.Item($row, 11)).Value = [double]5
        ($ws.Cells.Item($row, 12)).Value = [double]200
        ($ws.Cells.Item($row, 13)).Value = [double](-5)
        ($ws.Cells.Item($row, 14)).Value = [double]0

        for ($c = 1; $c -le 15; $c++) {
            $cell = $ws.Cells.Item($row, $c)
            $cell.Font.Name = 'Arial'
            $cell.Font.Size = 11
            $cell.HorizontalAlignment = -4108
            $cell.VerticalAlignment   = -4108
            if ($c -eq 3) { $cell.Interior.Color = 0xE8E8E8 }
            elseif ($c -eq 6) { $cell.Interior.Color = 0xE0FFFF }
        }
    }

    $range = $ws.Range($ws.Cells.Item(1,1), $ws.Cells.Item($axes.Count + 1, $headers.Count))
    foreach ($idx in 7,8,9,10,11,12) {
        $b = $range.Borders.Item($idx)
        $b.LineStyle = 1
        $b.Weight = 2
        $b.Color = 0x999999
    }

    $ws.Application.ActiveWindow.SplitRow = 1
    $ws.Application.ActiveWindow.FreezePanes = $true

    $ws2 = $wb.Worksheets.Add([System.Reflection.Missing]::Value, $ws)
    $ws2.Name = 'Guide'
    $ws2.Columns.Item(1).ColumnWidth = 22
    $ws2.Columns.Item(2).ColumnWidth = 70

    $guide = @(
        @('Field', 'Description'),
        @('', ''),
        @('No', 'Axis number (0..36, 37 axes total). Same order as MotionMap.cs Register.'),
        @('Name', 'Axis name (informational, do not change).'),
        @('Stroke[mm]', 'Mechanical max travel [mm]. Gray cell = reference only, do NOT edit.'),
        @('Brake', 'Y means brake engaged when stopped (prevents Z-axis free-fall).'),
        @('', ''),
        @('DefaultVel', 'Default move velocity [mm/s]. Default 100.'),
        @('MaxVel', 'Maximum velocity limit [mm/s]. PLEASE FILL IN. (yellow cell)'),
        @('Accel', 'Acceleration [mm/s2]. Default 500.'),
        @('Decel', 'Deceleration [mm/s2]. Default 500.'),
        @('HomeVel', 'Home return velocity [mm/s]. Currently 200 (raised for SIM speed).'),
        @('JogCoarse', 'Manual jog coarse velocity [mm/s]. Default 50.'),
        @('JogFine', 'Manual jog fine velocity [mm/s]. Default 5.'),
        @('', ''),
        @('SoftLimit+', 'Positive soft limit [mm].'),
        @('SoftLimit-', 'Negative soft limit [mm].'),
        @('HomeOffset', 'Home offset applied after home sensor detected [mm].'),
        @('', ''),
        @('Units', 'velocity=mm/s,  accel/decel=mm/s2,  position=mm,  rotation axes (T0..T3)=deg'),
        @('', ''),
        @('Note', 'Edit values, save same file and return. The values will be applied to AxisData.cs.')
    )

    for ($i = 0; $i -lt $guide.Count; $i++) {
        $row = $i + 1
        $a = $ws2.Cells.Item($row, 1); $a.Value = $guide[$i][0]
        $b = $ws2.Cells.Item($row, 2); $b.Value = $guide[$i][1]
        $a.Font.Name = 'Arial'; $a.Font.Size = 11
        $b.Font.Name = 'Arial'; $b.Font.Size = 11
        $a.VerticalAlignment = -4108
        $b.VerticalAlignment = -4108
        $b.WrapText = $true
        if ($i -eq 0) {
            $a.Font.Bold = $true; $a.Font.Color = 0xFFFFFF; $a.Interior.Color = 0x6A4A2A
            $b.Font.Bold = $true; $b.Font.Color = 0xFFFFFF; $b.Interior.Color = 0x6A4A2A
        }
    }

    $ws.Activate()
    $ws.Cells.Item(2,1).Select()

    $wb.SaveAs($outPath, 51)
    $wb.Close($false)
    Write-Output ("Saved: " + $outPath)
}
finally {
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
}
