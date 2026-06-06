# make_pptx_qmc.ps1 — QMC standard PPT generator (CDT-310 template compatible)
#
# Slide size: 9144000 x 5143500 (4:3 wide, matches CDT310 template)
# Colors: navy #0F2D4F / sub-navy #1F4E79 / orange #ED7D31 / bg #F2F2F2
# Body: #FFFFFF white + navy title bar + orange accent + page number
# Logo: ppt/media/image-1.png (QMC logo)
#
# Usage:
#   . make_pptx_qmc.ps1
#   New-PptxQmc -OutPath ... -DocTitle ... -DocSubtitle ... -SubmittedTo "ASE" -DocDate "2026-04-28" -Slides @(...)
#
# Slide kinds:
#   "title"     — cover (large CDT-320 title + subtitle + submitted-to + date + logo)
#   "section"   — section divider (orange bg + white title)
#   "bullets"   — content slide with QMC header + bullet list
#   "diagram"   — content slide with shape boxes/arrows
#   "table"     — content slide with table

function New-PptxQmc {
param(
    [string]$OutPath = "report.pptx",
    [string]$DocTitle = "CDT-320",
    [string]$DocSubtitle = "Engineering Document",
    [string]$DocSubject = "Internal",
    [string]$DocAuthor = "QMC",
    [string]$SubmittedTo = "ASE",
    [string]$DocDate = "",
    [hashtable[]]$Slides
)

if ($DocDate -eq "") { $DocDate = (Get-Date -Format "yyyy. MM. dd") }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function XmlEsc($s) {
    if ($null -eq $s) { return "" }
    return $s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("`"","&quot;")
}

# ─── 공통 색상 ─────────────────────────────────────────────────────────
$NAVY    = "0F2D4F"
$SUBNAVY = "1F4E79"
$ORANGE  = "ED7D31"
$LIGHTBG = "F2F2F2"
$BODYGRAY= "595959"
$REDDISC = "C00000"
$BORDER  = "D9D9D9"

# ─── 로고 PNG (template 에서 추출) ────────────────────────────────────
$logoPath = Join-Path (Split-Path $PSCommandPath -Parent) "template_ref\image-1-2.png"
if (-not (Test-Path $logoPath)) {
    $logoPath = "D:\Work\CDT-320\QMC.CDT-320\tools\template_ref\image-1-2.png"
}
$logoBytes = [System.IO.File]::ReadAllBytes($logoPath)

# ─── OOXML 골격 ─────────────────────────────────────────────────────────

$contentTypesTemplate = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Default Extension="png" ContentType="image/png"/>
  <Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/>
  <Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
##SLIDE_TYPES##
</Types>
'@

$rootRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="ppt/presentation.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
'@

$coreXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>$(XmlEsc $DocTitle)</dc:title>
  <dc:subject>$(XmlEsc $DocSubject)</dc:subject>
  <dc:creator>$(XmlEsc $DocAuthor)</dc:creator>
  <cp:lastModifiedBy>$(XmlEsc $DocAuthor)</cp:lastModifiedBy>
  <cp:revision>1</cp:revision>
  <dcterms:created xsi:type="dcterms:W3CDTF">$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")</dcterms:modified>
</cp:coreProperties>
"@

$appXml = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>QMC PPTX Generator (CDT-310 template)</Application>
  <PresentationFormat>On-screen Show (Custom)</PresentationFormat>
  <Slides>##SLIDE_COUNT##</Slides>
  <Company>QMC</Company>
</Properties>
'@

$themeXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="QMC Theme"><a:themeElements><a:clrScheme name="QMC"><a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1><a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1><a:dk2><a:srgbClr val="$NAVY"/></a:dk2><a:lt2><a:srgbClr val="$LIGHTBG"/></a:lt2><a:accent1><a:srgbClr val="$SUBNAVY"/></a:accent1><a:accent2><a:srgbClr val="$ORANGE"/></a:accent2><a:accent3><a:srgbClr val="A5A5A5"/></a:accent3><a:accent4><a:srgbClr val="FFC000"/></a:accent4><a:accent5><a:srgbClr val="5B9BD5"/></a:accent5><a:accent6><a:srgbClr val="70AD47"/></a:accent6><a:hlink><a:srgbClr val="0563C1"/></a:hlink><a:folHlink><a:srgbClr val="954F72"/></a:folHlink></a:clrScheme><a:fontScheme name="QMC"><a:majorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/><a:font script="Hang" typeface="맑은 고딕"/></a:majorFont><a:minorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/><a:font script="Hang" typeface="맑은 고딕"/></a:minorFont></a:fontScheme><a:fmtScheme name="Office"><a:fillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:fillStyleLst><a:lnStyleLst><a:ln w="6350"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln><a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln><a:ln w="19050"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln></a:lnStyleLst><a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst><a:bgFillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:bgFillStyleLst></a:fmtScheme></a:themeElements></a:theme>
"@

$slideMaster = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld><p:bg><p:bgRef idx="1001"><a:schemeClr val="bg1"/></p:bgRef></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
    </p:spTree>
  </p:cSld>
  <p:clrMap bg1="lt1" tx1="dk1" bg2="lt2" tx2="dk2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/>
  <p:sldLayoutIdLst><p:sldLayoutId id="2147483649" r:id="rId1"/></p:sldLayoutIdLst>
  <p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles>
</p:sldMaster>
'@

$slideMasterRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="../theme/theme1.xml"/>
</Relationships>
'@

$slideLayout = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="title" preserve="1">
  <p:cSld name="Title Slide">
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sldLayout>
'@

$slideLayoutRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="../slideMasters/slideMaster1.xml"/>
</Relationships>
'@

# ─── 도형 빌더 ─────────────────────────────────────────────────────────

# 16:9 narrow: 9144000 × 5143500 EMU

function Make-Box {
    param($id, $name, $x, $y, $w, $h, $fillRgb, $textColor, $text, $textSize=1200, $bold=$false)
    $boldStr = if ($bold) { 'b="1"' } else { '' }
    return @"
      <p:sp>
        <p:nvSpPr><p:cNvPr id="$id" name="$name"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="$x" y="$y"/><a:ext cx="$w" cy="$h"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="$fillRgb"/></a:solidFill><a:ln w="9525"><a:solidFill><a:srgbClr val="$BORDER"/></a:solidFill></a:ln></p:spPr>
        <p:txBody><a:bodyPr anchor="ctr" wrap="square"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="ko-KR" sz="$textSize" $boldStr><a:solidFill><a:srgbClr val="$textColor"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$(XmlEsc $text)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
"@
}

function Make-Arrow {
    param($id, $name, $x1, $y1, $x2, $y2, $colorRgb="595959")
    $w = [Math]::Abs($x2 - $x1); if ($w -lt 100) { $w = 100 }
    $h = [Math]::Abs($y2 - $y1); if ($h -lt 100) { $h = 100 }
    $ox = [Math]::Min($x1, $x2); $oy = [Math]::Min($y1, $y2)
    $flipH = if ($x2 -lt $x1) { 'flipH="1"' } else { '' }
    $flipV = if ($y2 -lt $y1) { 'flipV="1"' } else { '' }
    return @"
      <p:cxnSp>
        <p:nvCxnSpPr><p:cNvPr id="$id" name="$name"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr>
        <p:spPr><a:xfrm $flipH $flipV><a:off x="$ox" y="$oy"/><a:ext cx="$w" cy="$h"/></a:xfrm><a:prstGeom prst="straightConnector1"><a:avLst/></a:prstGeom><a:ln w="19050"><a:solidFill><a:srgbClr val="$colorRgb"/></a:solidFill><a:tailEnd type="triangle"/></a:ln></p:spPr>
      </p:cxnSp>
"@
}

function Make-Text {
    param($id, $name, $x, $y, $w, $h, $text, $size=1200, $color="000000", $bold=$false, $align="l", $italic=$false)
    $boldStr = if ($bold) { 'b="1"' } else { '' }
    $italicStr = if ($italic) { 'i="1"' } else { '' }
    return @"
      <p:sp>
        <p:nvSpPr><p:cNvPr id="$id" name="$name"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="$x" y="$y"/><a:ext cx="$w" cy="$h"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" anchor="t" lIns="0" tIns="0" rIns="0" bIns="0"/><a:lstStyle/>
          <a:p><a:pPr algn="$align" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="$size" $boldStr $italicStr dirty="0"><a:solidFill><a:srgbClr val="$color"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$(XmlEsc $text)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
"@
}

# QMC content slide header (logo + title bar + page number + divider + subtitle)
function Make-Header {
    param($title, $subtitle, $pageNum, $totalPages, $hasLogo)
    $logoXml = ""
    if ($hasLogo) {
        $logoXml = @"
      <p:pic>
        <p:nvPicPr><p:cNvPr id="50" name="QmcLogo"/><p:cNvPicPr><a:picLocks noChangeAspect="1"/></p:cNvPicPr><p:nvPr/></p:nvPicPr>
        <p:blipFill><a:blip r:embed="rId2"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>
        <p:spPr><a:xfrm><a:off x="320040" y="201168"/><a:ext cx="777240" cy="292608"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr>
      </p:pic>
"@
    }
    return @"
$logoXml
      <p:sp>
        <p:nvSpPr><p:cNvPr id="51" name="HeaderTitle"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="1280160" y="182880"/><a:ext cx="6400800" cy="365760"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="l" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="1900" b="1" dirty="0"><a:solidFill><a:srgbClr val="$SUBNAVY"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$(XmlEsc $title)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="52" name="PageNum"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="7955280" y="201168"/><a:ext cx="914400" cy="274320"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="r" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="en-US" sz="1000" dirty="0"><a:solidFill><a:srgbClr val="$BODYGRAY"/></a:solidFill><a:latin typeface="Calibri"/></a:rPr><a:t>$pageNum / $totalPages</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="53" name="DividerLine"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="320040" y="603504"/><a:ext cx="8503920" cy="22860"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="$SUBNAVY"/></a:solidFill></p:spPr>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="54" name="OrangeAccent"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="320040" y="603504"/><a:ext cx="548640" cy="22860"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="$ORANGE"/></a:solidFill></p:spPr>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="55" name="HeaderSubtitle"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="320040" y="749808"/><a:ext cx="8503920" cy="320040"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="l" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="1400" dirty="0"><a:solidFill><a:srgbClr val="$BODYGRAY"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$(XmlEsc $subtitle)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
"@
}

function Make-Disclaimer {
    return @"
      <p:sp>
        <p:nvSpPr><p:cNvPr id="999" name="Disclaimer"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="274320" y="4983480"/><a:ext cx="8595360" cy="155448"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="en-US" sz="750" i="1" dirty="0"><a:solidFill><a:srgbClr val="$REDDISC"/></a:solidFill><a:latin typeface="Calibri"/></a:rPr><a:t>"Taking this material out of the company without the authorization can be subject to restriction by the sales secret protection law."</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
"@
}

# ─── Title Slide (cover) ────────────────────────────────────────────────

function Build-TitleSlide($s, $idx, $totalSlides) {
    $title = XmlEsc $DocTitle
    $subtitle = XmlEsc $DocSubtitle
    $sub2 = if ($s.subtitle2) { XmlEsc $s.subtitle2 } else { "Engineering Document Series" }
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val="$LIGHTBG"/></a:solidFill></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="2" name="MainTitle"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="640080"/><a:ext cx="8229600" cy="822960"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="5400" b="1" dirty="0"><a:solidFill><a:srgbClr val="$NAVY"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$title</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="3" name="MainSubtitle"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="1417320"/><a:ext cx="8229600" cy="594360"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="3000" b="1" dirty="0"><a:solidFill><a:srgbClr val="$NAVY"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$subtitle</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="4" name="OrangeStripe"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="3840480" y="2103120"/><a:ext cx="1463040" cy="36576"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="$ORANGE"/></a:solidFill></p:spPr>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="5" name="WhiteBox"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="914400" y="2286000"/><a:ext cx="7315200" cy="457200"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="FFFFFF"><a:alpha val="80000"/></a:srgbClr></a:solidFill><a:ln w="6350"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></a:ln></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="1500" i="1" dirty="0"><a:solidFill><a:srgbClr val="$SUBNAVY"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$sub2</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="6" name="SubmitBox"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="2286000" y="3749040"/><a:ext cx="4572000" cy="777240"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="FFFFFF"><a:alpha val="85000"/></a:srgbClr></a:solidFill><a:ln w="12700"><a:solidFill><a:srgbClr val="$SUBNAVY"/></a:solidFill></a:ln></p:spPr>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="7" name="SubmittedTo"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="2286000" y="3794760"/><a:ext cx="4572000" cy="365760"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="en-US" sz="1700" b="1" dirty="0"><a:solidFill><a:srgbClr val="$SUBNAVY"/></a:solidFill><a:latin typeface="Calibri"/></a:rPr><a:t>Submitted to : $(XmlEsc $SubmittedTo)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="8" name="DateLabel"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="2286000" y="4160520"/><a:ext cx="4572000" cy="320040"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="en-US" sz="1300" dirty="0"><a:solidFill><a:srgbClr val="$BODYGRAY"/></a:solidFill><a:latin typeface="Calibri"/></a:rPr><a:t>$(XmlEsc $DocDate)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:pic>
        <p:nvPicPr><p:cNvPr id="9" name="QmcLogo"/><p:cNvPicPr><a:picLocks noChangeAspect="1"/></p:cNvPicPr><p:nvPr/></p:nvPicPr>
        <p:blipFill><a:blip r:embed="rId2"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>
        <p:spPr><a:xfrm><a:off x="3931920" y="4645152"/><a:ext cx="1280160" cy="292608"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr>
      </p:pic>
$(Make-Disclaimer)
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

# ─── Section Slide ──────────────────────────────────────────────────────

function Build-SectionSlide($s, $idx, $totalSlides) {
    $title = XmlEsc $s.title
    $subtitle = if ($s.subtitle) { XmlEsc $s.subtitle } else { "" }
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val="$NAVY"/></a:solidFill></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="10" name="OrangeStripe"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="3340480" y="1900000"/><a:ext cx="2463040" cy="50000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="$ORANGE"/></a:solidFill></p:spPr>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="11" name="SectionTitle"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="2100000"/><a:ext cx="8229600" cy="900000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="4400" b="1" dirty="0"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$title</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="12" name="SectionSub"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="3050000"/><a:ext cx="8229600" cy="500000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="ctr" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="1800" i="1" dirty="0"><a:solidFill><a:srgbClr val="$LIGHTBG"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$subtitle</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

# ─── Bullets Slide ─────────────────────────────────────────────────────

function Build-BulletsSlide($s, $idx, $totalSlides) {
    $title = $s.title
    $subtitle = if ($s.subtitle) { $s.subtitle } else { "" }
    $bodyParas = ""
    foreach ($l in $s.lines) {
        if ($null -eq $l) { continue }
        $esc = XmlEsc $l
        if ($l.StartsWith("##")) {
            $h = XmlEsc $l.Substring(2).TrimStart()
            $bodyParas += @"
        <a:p><a:pPr algn="l" indent="0" marL="0"><a:buNone/></a:pPr><a:r><a:rPr lang="ko-KR" sz="1500" b="1" dirty="0"><a:solidFill><a:srgbClr val="$SUBNAVY"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$h</a:t></a:r></a:p>
"@
        }
        elseif ($l -eq "") {
            $bodyParas += '<a:p><a:pPr/><a:endParaRPr lang="ko-KR" sz="500"/></a:p>'
        }
        else {
            $bodyParas += @"
        <a:p><a:pPr algn="l" indent="-228600" marL="228600"><a:buFont typeface="Arial"/><a:buChar char="•"/></a:pPr><a:r><a:rPr lang="ko-KR" sz="1300" dirty="0"><a:solidFill><a:srgbClr val="333333"/></a:solidFill><a:latin typeface="Calibri"/><a:ea typeface="맑은 고딕"/></a:rPr><a:t>$esc</a:t></a:r></a:p>
"@
        }
    }
    $header = Make-Header $title $subtitle ($idx+1) $totalSlides $true
    $disc = Make-Disclaimer
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
$header
      <p:sp>
        <p:nvSpPr><p:cNvPr id="60" name="Body"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="320040" y="1188720"/><a:ext cx="8503920" cy="3500000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0"/><a:lstStyle/>
$bodyParas
        </p:txBody>
      </p:sp>
$disc
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

# ─── Diagram Slide ─────────────────────────────────────────────────────

function Build-DiagramSlide($s, $idx, $totalSlides) {
    $title = $s.title
    $subtitle = if ($s.subtitle) { $s.subtitle } else { "" }
    $shapesXml = ""
    $idC = 100
    if ($s.shapes) {
        foreach ($sh in $s.shapes) {
            $idC++
            if ($sh.kind -eq "box") {
                $textSize = if ($sh.textSize) { $sh.textSize } else { 1200 }
                $bold = if ($sh.bold) { $true } else { $false }
                $shapesXml += Make-Box $idC $sh.name $sh.x $sh.y $sh.w $sh.h $sh.fill $sh.color $sh.text $textSize $bold
            }
            elseif ($sh.kind -eq "arrow") {
                $clr = if ($sh.color) { $sh.color } else { "595959" }
                $shapesXml += Make-Arrow $idC $sh.name $sh.x1 $sh.y1 $sh.x2 $sh.y2 $clr
            }
            elseif ($sh.kind -eq "text") {
                $sz = if ($sh.size) { $sh.size } else { 1100 }
                $clr = if ($sh.color) { $sh.color } else { "000000" }
                $bold = if ($sh.bold) { $true } else { $false }
                $italic = if ($sh.italic) { $true } else { $false }
                $align = if ($sh.align) { $sh.align } else { "l" }
                $shapesXml += Make-Text $idC $sh.name $sh.x $sh.y $sh.w $sh.h $sh.text $sz $clr $bold $align $italic
            }
        }
    }
    $header = Make-Header $title $subtitle ($idx+1) $totalSlides $true
    $disc = Make-Disclaimer
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
$header
$shapesXml
$disc
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

# ─── Table Slide (simple shape-based) ─────────────────────────────────

function Build-TableSlide($s, $idx, $totalSlides) {
    $title = $s.title
    $subtitle = if ($s.subtitle) { $s.subtitle } else { "" }
    $tableXml = ""
    $idC = 100
    $startX = 320040
    $startY = 1188720
    $cellH = 320040
    $colWidths = $s.colWidths
    if (-not $colWidths) {
        $cellW = [int](8503920 / $s.cols.Count)
        $colWidths = @()
        for ($c=0; $c -lt $s.cols.Count; $c++) { $colWidths += $cellW }
    }
    # Header row
    $cumX = $startX
    for ($c=0; $c -lt $s.cols.Count; $c++) {
        $idC++
        $tableXml += Make-Box $idC "H$c" $cumX $startY $colWidths[$c] $cellH $SUBNAVY "FFFFFF" $s.cols[$c] 1200 $true
        $cumX += $colWidths[$c]
    }
    # Data rows
    for ($r=0; $r -lt $s.rows.Count; $r++) {
        $cumX = $startX
        $rowY = $startY + ($r + 1) * $cellH
        $row = $s.rows[$r]
        $bg = if ($r % 2 -eq 0) { $LIGHTBG } else { "FFFFFF" }
        for ($c=0; $c -lt $row.Count; $c++) {
            $idC++
            $tableXml += Make-Box $idC "C${r}_${c}" $cumX $rowY $colWidths[$c] $cellH $bg "333333" $row[$c] 1000 $false
            $cumX += $colWidths[$c]
        }
    }
    $header = Make-Header $title $subtitle ($idx+1) $totalSlides $true
    $disc = Make-Disclaimer
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
$header
$tableXml
$disc
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

function Build-Slide($s, $idx, $totalSlides) {
    if ($s.kind -eq "title")   { return Build-TitleSlide $s $idx $totalSlides }
    if ($s.kind -eq "section") { return Build-SectionSlide $s $idx $totalSlides }
    if ($s.kind -eq "diagram") { return Build-DiagramSlide $s $idx $totalSlides }
    if ($s.kind -eq "table")   { return Build-TableSlide $s $idx $totalSlides }
    return Build-BulletsSlide $s $idx $totalSlides
}

# ─── presentation.xml + rels ────────────────────────────────────────────

$presRelsLines = @"
<?xml version=`"1.0`" encoding=`"UTF-8`" standalone=`"yes`"?>
<Relationships xmlns=`"http://schemas.openxmlformats.org/package/2006/relationships`">
  <Relationship Id=`"rId1`" Type=`"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster`" Target=`"slideMasters/slideMaster1.xml`"/>
"@
$rid = 2
$slideRelIds = @()
for ($i = 0; $i -lt $Slides.Count; $i++) {
    $slideRelIds += "rId$rid"
    $presRelsLines += "  <Relationship Id=`"rId$rid`" Type=`"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide`" Target=`"slides/slide$($i+1).xml`"/>`r`n"
    $rid++
}
$presRelsLines += "  <Relationship Id=`"rId$rid`" Type=`"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme`" Target=`"theme/theme1.xml`"/>`r`n"
$presRelsLines += "</Relationships>"

$slideIdList = ""
$sid = 256
for ($i = 0; $i -lt $Slides.Count; $i++) {
    $slideIdList += "    <p:sldId id=`"$($sid + $i)`" r:id=`"$($slideRelIds[$i])`"/>`r`n"
}
$presXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" saveSubsetFonts="1">
  <p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rId1"/></p:sldMasterIdLst>
  <p:sldIdLst>
$slideIdList  </p:sldIdLst>
  <p:sldSz cx="9144000" cy="5143500"/>
  <p:notesSz cx="5143500" cy="9144000"/>
  <p:defaultTextStyle/>
</p:presentation>
"@

# Each slide has rels → 1 slideLayout + 1 image (logo)
$slideRelsContent = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="../media/image1.png"/>
</Relationships>
'@

# ─── ZIP 패키지 빌드 ──────────────────────────────────────────────────────

$slideTypes = ""
for ($i = 0; $i -lt $Slides.Count; $i++) {
    $slideTypes += "  <Override PartName=`"/ppt/slides/slide$($i+1).xml`" ContentType=`"application/vnd.openxmlformats-officedocument.presentationml.slide+xml`"/>`r`n"
}
$contentTypesXml = $contentTypesTemplate -replace '##SLIDE_TYPES##', $slideTypes
$appXmlFinal    = $appXml -replace '##SLIDE_COUNT##', $Slides.Count

if (Test-Path $OutPath) { Remove-Item $OutPath -Force }
$dir = Split-Path -Parent $OutPath
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

$fs = [System.IO.File]::Create($OutPath)
$zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)

function AddE($zip, $name, $content) {
    $e = $zip.CreateEntry($name)
    $s = $e.Open()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $s.Write($bytes, 0, $bytes.Length)
    $s.Close()
}
function AddBin($zip, $name, $bytes) {
    $e = $zip.CreateEntry($name)
    $s = $e.Open()
    $s.Write($bytes, 0, $bytes.Length)
    $s.Close()
}

AddE  $zip "[Content_Types].xml" $contentTypesXml
AddE  $zip "_rels/.rels" $rootRels
AddE  $zip "docProps/core.xml" $coreXml
AddE  $zip "docProps/app.xml" $appXmlFinal
AddE  $zip "ppt/presentation.xml" $presXml
AddE  $zip "ppt/_rels/presentation.xml.rels" $presRelsLines
AddE  $zip "ppt/theme/theme1.xml" $themeXml
AddE  $zip "ppt/slideMasters/slideMaster1.xml" $slideMaster
AddE  $zip "ppt/slideMasters/_rels/slideMaster1.xml.rels" $slideMasterRels
AddE  $zip "ppt/slideLayouts/slideLayout1.xml" $slideLayout
AddE  $zip "ppt/slideLayouts/_rels/slideLayout1.xml.rels" $slideLayoutRels
AddBin $zip "ppt/media/image1.png" $logoBytes

for ($i = 0; $i -lt $Slides.Count; $i++) {
    $sxml = Build-Slide $Slides[$i] $i $Slides.Count
    AddE $zip "ppt/slides/slide$($i+1).xml" $sxml
    AddE $zip "ppt/slides/_rels/slide$($i+1).xml.rels" $slideRelsContent
}

$zip.Dispose()
$fs.Close()

$size = (Get-Item $OutPath).Length
Write-Output "Created: $OutPath  ($size bytes, $($Slides.Count) slides)"
}  # function New-PptxQmc
