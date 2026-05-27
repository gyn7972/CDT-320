# fill_doc2_doc3_tables.ps1 (ASCII-only; Korean data lives in fill_doc2_doc3_data.json)
# Fills empty tables in 02_CDT320_dev-plan.pptx and 03_CDT320_checklist.pptx
# (Korean filenames; see JSON for absolute paths).
#
# The slide XMLs already contain:
#   - Header shapes  H0..H{N-1}        (one per column, with x / cx coordinates)
#   - First-column placeholders C0_0, C1_0, ... (only column 0 filled, rest missing)
# This script:
#   1) overwrites the text of existing C{r}_0 cells with col-0 data
#   2) injects new <p:sp> shapes for every (row, col>=1) cell, copying header
#      x/cx and the row's y / cy with alternating row fill.
# It re-saves the same .pptx in place.  A backup .bak.pptx is expected to exist.

[CmdletBinding()]
param(
    [string]$DataPath = "D:\Work\CDT-320\QMC.CDT-320\tools\fill_doc2_doc3_data.json"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# ----- Read JSON in UTF-8 (avoids PS5.1 default-encoding issues with Korean) -----
$jsonText = [System.IO.File]::ReadAllText($DataPath, [System.Text.Encoding]::UTF8)
$data = $jsonText | ConvertFrom-Json

function Encode-Xml([string]$s) {
    if ($null -eq $s) { return "" }
    $t = $s -replace '&', '&amp;'
    $t = $t -replace '<', '&lt;'
    $t = $t -replace '>', '&gt;'
    $t = $t -replace '"', '&quot;'
    $t = $t -replace "'", '&apos;'
    return $t
}

function Read-EntryText([System.IO.Compression.ZipArchive]$zip, [string]$entryName) {
    $entry = $zip.GetEntry($entryName)
    if ($null -eq $entry) { throw "Entry not found: $entryName" }
    $stream = $entry.Open()
    try {
        $sr = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
        try { return $sr.ReadToEnd() } finally { $sr.Dispose() }
    } finally {
        # StreamReader disposes the underlying stream
    }
}

function Write-EntryText([System.IO.Compression.ZipArchive]$zip, [string]$entryName, [string]$text) {
    $entry = $zip.GetEntry($entryName)
    if ($null -ne $entry) { $entry.Delete() }
    $newEntry = $zip.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $stream = $newEntry.Open()
    try {
        # No BOM - PowerPoint expects plain UTF-8 here
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        $sw = New-Object System.IO.StreamWriter($stream, $utf8NoBom)
        try { $sw.Write($text) } finally { $sw.Dispose() }
    } finally {
        # StreamWriter disposes the underlying stream
    }
}

function Build-CellSp {
    param(
        [int]$Id,
        [string]$Name,
        [string]$X, [string]$Y, [string]$Cx, [string]$Cy,
        [string]$Fill,
        [string]$Text,
        [string]$Align = "ctr"
    )
    $encText = Encode-Xml $Text
    # Korean "Malgun Gothic" assembled via codepoints (script is ASCII-only on disk)
    $eaFont = ([char]0xB9D1).ToString() + ([char]0xC740).ToString() + " " + ([char]0xACE0).ToString() + ([char]0xB515).ToString()
    $parts = New-Object 'System.Collections.Generic.List[string]'
    $parts.Add('      <p:sp>')
    $parts.Add('<p:nvSpPr><p:cNvPr id="' + $Id + '" name="' + $Name + '"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>')
    $parts.Add('<p:spPr><a:xfrm><a:off x="' + $X + '" y="' + $Y + '"/><a:ext cx="' + $Cx + '" cy="' + $Cy + '"/></a:xfrm>')
    $parts.Add('<a:prstGeom prst="rect"><a:avLst/></a:prstGeom>')
    $parts.Add('<a:solidFill><a:srgbClr val="' + $Fill + '"/></a:solidFill>')
    $parts.Add('<a:ln w="9525"><a:solidFill><a:srgbClr val="D9D9D9"/></a:solidFill></a:ln></p:spPr>')
    $parts.Add('<p:txBody><a:bodyPr anchor="ctr" wrap="square"/><a:lstStyle/>')
    $parts.Add('<a:p><a:pPr algn="' + $Align + '"/>')
    $parts.Add('<a:r><a:rPr lang="ko-KR" sz="900"><a:solidFill><a:srgbClr val="333333"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="' + $eaFont + '"/></a:rPr>')
    $parts.Add('<a:t>' + $encText + '</a:t></a:r></a:p>')
    $parts.Add('</p:txBody></p:sp>')
    return [string]::Concat($parts)
}

function Fill-Slide {
    param(
        [string]$Xml,
        [int]$Cols,
        [array]$Rows
    )

    # ---- 1. Header coordinates --------------------------------------------------
    $hX  = New-Object int[] $Cols
    $hCx = New-Object int[] $Cols
    for ($c = 0; $c -lt $Cols; $c++) {
        $patternH = '<p:cNvPr[^/]*name="H' + $c + '"[^/]*/>\s*<p:cNvSpPr/><p:nvPr/></p:nvSpPr>\s*<p:spPr><a:xfrm><a:off x="(\d+)" y="(\d+)"/><a:ext cx="(\d+)" cy="(\d+)"/>'
        $m = [regex]::Match($Xml, $patternH)
        if (-not $m.Success) { throw "Header H$c not found" }
        $hX[$c]  = [int]$m.Groups[1].Value
        $hCx[$c] = [int]$m.Groups[3].Value
    }

    # ---- 2. Existing C{r}_0 row coordinates ------------------------------------
    $cellRegex = [regex]'<p:cNvPr[^/]*name="C(\d+)_0"[^/]*/>\s*<p:cNvSpPr/><p:nvPr/></p:nvSpPr>\s*<p:spPr><a:xfrm><a:off x="(\d+)" y="(\d+)"/><a:ext cx="(\d+)" cy="(\d+)"/>'
    $existingRows = @{}
    foreach ($mm in $cellRegex.Matches($Xml)) {
        $idx = [int]$mm.Groups[1].Value
        $existingRows[$idx] = @{
            Y  = [int]$mm.Groups[3].Value
            Cy = [int]$mm.Groups[5].Value
        }
    }

    if ($existingRows.Count -lt 2) { throw "Need at least 2 existing C*_0 cells" }
    $sortedKeys = @($existingRows.Keys | Sort-Object)
    $stride = $existingRows[$sortedKeys[1]].Y - $existingRows[$sortedKeys[0]].Y
    $cyDefault = $existingRows[$sortedKeys[0]].Cy
    $lastIdx = $sortedKeys[$sortedKeys.Count - 1]

    # ---- 3. Compute y for each data row ---------------------------------------
    $rowY = @()
    for ($r = 0; $r -lt $Rows.Count; $r++) {
        if ($existingRows.ContainsKey($r)) {
            $rowY += $existingRows[$r].Y
        } else {
            $rowY += $existingRows[$lastIdx].Y + ($r - $lastIdx) * $stride
        }
    }

    # ---- 4. Replace col-0 text inline -----------------------------------------
    $newXml = $Xml
    for ($r = 0; $r -lt $Rows.Count; $r++) {
        $rowData = $Rows[$r]
        $col0enc = Encode-Xml $rowData[0]
        if ($existingRows.ContainsKey($r)) {
            # Replace content of <a:t></a:t> within the C{r}_0 shape
            $patReplace = '(<p:cNvPr[^/]*name="C' + $r + '_0"[^/]*/>(?:.|\n|\r)*?<a:t>)([^<]*)(</a:t>)'
            $cb = {
                param($m)
                return $m.Groups[1].Value + $col0enc + $m.Groups[3].Value
            }.GetNewClosure()
            $newXml = [regex]::Replace($newXml, $patReplace, $cb, 1)
        }
    }

    # ---- 4b. Remove unused C{r}_0 placeholders (rows beyond data) -------------
    # Each placeholder shape is "      <p:sp>...<p:cNvPr ... name=\"C{r}_0\".../>...</p:sp>"
    # We strip them so the slide does not show a long tail of blank cells.
    foreach ($k in $sortedKeys) {
        if ($k -ge $Rows.Count) {
            $patStrip = '\s*<p:sp>\s*<p:nvSpPr><p:cNvPr[^/]*name="C' + $k + '_0"[^/]*/>(?:.|\n|\r)*?</p:sp>'
            $newXml = [regex]::Replace($newXml, $patStrip, "", 1)
        }
    }

    # ---- 5. Build new <p:sp> shapes for col>=1 (and rows past existing) -------
    $idCounter = 5000
    $sb = New-Object System.Text.StringBuilder
    for ($r = 0; $r -lt $Rows.Count; $r++) {
        $rowData = $Rows[$r]
        $y = $rowY[$r]
        $startCol = if ($existingRows.ContainsKey($r)) { 1 } else { 0 }
        for ($c = $startCol; $c -lt $Cols; $c++) {
            $idCounter++
            $fill = if ($r % 2 -eq 0) { "F2F2F2" } else { "FFFFFF" }
            $sp = Build-CellSp `
                -Id $idCounter `
                -Name ("FillCell_" + $r + "_" + $c) `
                -X ([string]$hX[$c]) `
                -Y ([string]$y) `
                -Cx ([string]$hCx[$c]) `
                -Cy ([string]$cyDefault) `
                -Fill $fill `
                -Text $rowData[$c]
            [void]$sb.Append($sp)
        }
    }

    if ($sb.Length -gt 0) {
        $newXml = $newXml -replace '</p:spTree>', ($sb.ToString() + "</p:spTree>")
    }
    return $newXml
}

function Process-Pptx {
    param(
        [string]$Path,
        [array]$Tables
    )

    Write-Host ("[*] Processing " + $Path) -ForegroundColor Cyan
    $zip = [System.IO.Compression.ZipFile]::Open($Path, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        foreach ($t in $Tables) {
            $entryName = $t.file
            $rowsArr = @($t.rows)
            Write-Host ("    - " + $entryName + " (" + $rowsArr.Count + " rows x " + $t.cols + " cols)") -ForegroundColor Gray
            $xml = Read-EntryText $zip $entryName
            $newXml = Fill-Slide -Xml $xml -Cols ([int]$t.cols) -Rows $rowsArr
            Write-EntryText $zip $entryName $newXml
        }
    } finally {
        $zip.Dispose()
    }
    Write-Host ("[+] Done " + $Path) -ForegroundColor Green
}

# ============================================================================
# Run
# ============================================================================
Process-Pptx -Path $data.doc2.path -Tables @($data.doc2.tables)
Process-Pptx -Path $data.doc3.path -Tables @($data.doc3.tables)

Write-Host ""
Write-Host "All tables filled." -ForegroundColor Green
