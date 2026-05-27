# make_pptx_v2.ps1 - Enhanced PPTX generator with shapes, diagrams, tables.
# Function: New-Pptx2 -OutPath ... -Slides @(@{kind=...; title=...; ...}, ...)
#
# Slide kinds:
#   "title"     — title page (large title + subtitle bullets, dark bg)
#   "section"   — divider page (orange bg, large white title)
#   "bullets"   — orange title bar + bullet list
#   "diagram"   — orange title bar + custom shapes (boxes, arrows)
#   "table"     — orange title bar + table
#   "tworow"    — orange title bar + 2 rows of mixed content

function New-Pptx2 {
param(
    [string]$OutPath = "report.pptx",
    [string]$DocTitle = "QMC CDT-320 Document",
    [string]$DocAuthor = "QMC",
    [string]$DocSubject = "Internal Engineering Document",
    [hashtable[]]$Slides
)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function XmlEsc($s) {
    if ($null -eq $s) { return "" }
    return $s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("`"","&quot;")
}

# ─── 공통 OOXML 템플릿 ─────────────────────────────────────────────────────

$contentTypesTemplate = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
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
  <Application>QMC CDT-320 PPTX Generator v2</Application>
  <PresentationFormat>On-screen Show (16:9)</PresentationFormat>
  <Slides>##SLIDE_COUNT##</Slides>
  <Company>QMC</Company>
</Properties>
'@

$themeXml = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="QMC Theme">
  <a:themeElements>
    <a:clrScheme name="QMC">
      <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1>
      <a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
      <a:dk2><a:srgbClr val="2D2D30"/></a:dk2>
      <a:lt2><a:srgbClr val="E7E6E6"/></a:lt2>
      <a:accent1><a:srgbClr val="E85D1A"/></a:accent1>
      <a:accent2><a:srgbClr val="D97706"/></a:accent2>
      <a:accent3><a:srgbClr val="595959"/></a:accent3>
      <a:accent4><a:srgbClr val="BFBFBF"/></a:accent4>
      <a:accent5><a:srgbClr val="00BCD4"/></a:accent5>
      <a:accent6><a:srgbClr val="70AD47"/></a:accent6>
      <a:hlink><a:srgbClr val="0563C1"/></a:hlink>
      <a:folHlink><a:srgbClr val="954F72"/></a:folHlink>
    </a:clrScheme>
    <a:fontScheme name="QMC">
      <a:majorFont><a:latin typeface="맑은 고딕"/><a:ea typeface=""/><a:cs typeface=""/></a:majorFont>
      <a:minorFont><a:latin typeface="맑은 고딕"/><a:ea typeface=""/><a:cs typeface=""/></a:minorFont>
    </a:fontScheme>
    <a:fmtScheme name="Office">
      <a:fillStyleLst>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
      </a:fillStyleLst>
      <a:lnStyleLst>
        <a:ln w="6350"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
        <a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
        <a:ln w="19050"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
      </a:lnStyleLst>
      <a:effectStyleLst>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
      </a:effectStyleLst>
      <a:bgFillStyleLst>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
      </a:bgFillStyleLst>
    </a:fmtScheme>
  </a:themeElements>
</a:theme>
'@

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

$slideRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
</Relationships>
'@

# ─── 도형 빌더 함수 ─────────────────────────────────────────────────────

# EMU: 1 inch = 914400, 16:9 slide is 12192000 x 6858000
function Make-Box {
    param($id, $name, $x, $y, $w, $h, $fillRgb, $textColor, $text, $textSize=1400, $bold=$false)
    $boldStr = if ($bold) { 'b="1"' } else { '' }
    return @"
      <p:sp>
        <p:nvSpPr><p:cNvPr id="$id" name="$name"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="$x" y="$y"/><a:ext cx="$w" cy="$h"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="$fillRgb"/></a:solidFill>
          <a:ln w="12700"><a:solidFill><a:srgbClr val="333333"/></a:solidFill></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr anchor="ctr" wrap="square"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="ko-KR" sz="$textSize" $boldStr><a:solidFill><a:srgbClr val="$textColor"/></a:solidFill></a:rPr><a:t>$(XmlEsc $text)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
"@
}

function Make-Arrow {
    param($id, $name, $x1, $y1, $x2, $y2, $colorRgb="555555")
    $w = [Math]::Abs($x2 - $x1); if ($w -lt 100) { $w = 100 }
    $h = [Math]::Abs($y2 - $y1); if ($h -lt 100) { $h = 100 }
    $ox = [Math]::Min($x1, $x2); $oy = [Math]::Min($y1, $y2)
    $flipH = if ($x2 -lt $x1) { 'flipH="1"' } else { '' }
    $flipV = if ($y2 -lt $y1) { 'flipV="1"' } else { '' }
    return @"
      <p:cxnSp>
        <p:nvCxnSpPr><p:cNvPr id="$id" name="$name"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr>
        <p:spPr>
          <a:xfrm $flipH $flipV><a:off x="$ox" y="$oy"/><a:ext cx="$w" cy="$h"/></a:xfrm>
          <a:prstGeom prst="straightConnector1"><a:avLst/></a:prstGeom>
          <a:ln w="22225"><a:solidFill><a:srgbClr val="$colorRgb"/></a:solidFill><a:tailEnd type="triangle"/></a:ln>
        </p:spPr>
        <p:style>
          <a:lnRef idx="1"><a:schemeClr val="accent1"/></a:lnRef>
          <a:fillRef idx="0"><a:schemeClr val="accent1"/></a:fillRef>
          <a:effectRef idx="0"><a:schemeClr val="accent1"/></a:effectRef>
          <a:fontRef idx="minor"><a:schemeClr val="tx1"/></a:fontRef>
        </p:style>
      </p:cxnSp>
"@
}

function Make-Text {
    param($id, $name, $x, $y, $w, $h, $text, $size=1400, $color="000000", $bold=$false, $align="l")
    $boldStr = if ($bold) { 'b="1"' } else { '' }
    return @"
      <p:sp>
        <p:nvSpPr><p:cNvPr id="$id" name="$name"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="$x" y="$y"/><a:ext cx="$w" cy="$h"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:noFill/>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="square" anchor="t"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="$align"/><a:r><a:rPr lang="ko-KR" sz="$size" $boldStr><a:solidFill><a:srgbClr val="$color"/></a:solidFill></a:rPr><a:t>$(XmlEsc $text)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
"@
}

# ─── 슬라이드 본체 빌더 ────────────────────────────────────────────────

function Build-TitleSlide($s, $idx) {
    # Dark bg + large title + bullets in white
    $title = XmlEsc $s.title
    $bodyParas = ""
    $i=0
    foreach ($l in $s.lines) {
        $i++
        $sz = if ($i -eq 1) { 2400 } else { 1800 }
        $bodyParas += @"
          <a:p><a:pPr algn="l"/><a:r><a:rPr lang="ko-KR" sz="$sz"><a:solidFill><a:srgbClr val="DDDDDD"/></a:solidFill></a:rPr><a:t>$(XmlEsc $l)</a:t></a:r></a:p>
"@
    }
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld>
    <p:bg><p:bgPr><a:solidFill><a:srgbClr val="2D2D30"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
      $(Make-Box 100 "OrangeAccent" 457200 1828800 11277600 50800 "E85D1A" "FFFFFF" "" 1000)
      <p:sp>
        <p:nvSpPr><p:cNvPr id="2" name="QMC"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="600000"/><a:ext cx="3000000" cy="900000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody>
          <a:bodyPr/><a:lstStyle/>
          <a:p><a:pPr/><a:r><a:rPr lang="en-US" sz="6000" b="1"><a:solidFill><a:srgbClr val="E85D1A"/></a:solidFill></a:rPr><a:t>QM</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="3" name="Title"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="2200000"/><a:ext cx="11277600" cy="1400000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody>
          <a:bodyPr/><a:lstStyle/>
          <a:p><a:pPr/><a:r><a:rPr lang="ko-KR" sz="4800" b="1"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></a:rPr><a:t>$title</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="4" name="Subtitle"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="3800000"/><a:ext cx="11277600" cy="2600000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody>
          <a:bodyPr/><a:lstStyle/>
$bodyParas
        </p:txBody>
      </p:sp>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

function Build-SectionSlide($s, $idx) {
    $title = XmlEsc $s.title
    $sub = if ($s.subtitle) { XmlEsc $s.subtitle } else { "" }
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld>
    <p:bg><p:bgPr><a:solidFill><a:srgbClr val="E85D1A"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="2" name="SectionTitle"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="2400000"/><a:ext cx="11277600" cy="1500000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody>
          <a:bodyPr/><a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="ko-KR" sz="6000" b="1"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></a:rPr><a:t>$title</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="3" name="SectionSub"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="4100000"/><a:ext cx="11277600" cy="600000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody>
          <a:bodyPr/><a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="ko-KR" sz="2400"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></a:rPr><a:t>$sub</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

function Build-BulletsSlide($s, $idx) {
    $title = XmlEsc $s.title
    $bodyParas = ""
    foreach ($l in $s.lines) {
        $esc = XmlEsc $l
        $sz = 1800
        if ($l.StartsWith("##")) {
            $esc = XmlEsc $l.Substring(2).TrimStart()
            $bodyParas += @"
        <a:p><a:pPr algn="l" indent="0" marL="0"/><a:r><a:rPr lang="ko-KR" sz="2200" b="1"><a:solidFill><a:srgbClr val="E85D1A"/></a:solidFill></a:rPr><a:t>$esc</a:t></a:r></a:p>
"@
        }
        elseif ($l -eq "") {
            $bodyParas += '<a:p><a:pPr/><a:endParaRPr lang="ko-KR" sz="1200"/></a:p>'
        }
        else {
            $bodyParas += @"
        <a:p><a:pPr algn="l" indent="-228600" marL="228600"><a:buFont typeface="Arial"/><a:buChar char="•"/></a:pPr><a:r><a:rPr lang="ko-KR" sz="$sz"/><a:t>$esc</a:t></a:r></a:p>
"@
        }
    }
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld>
    <p:bg><p:bgRef idx="1001"><a:schemeClr val="bg1"/></p:bgRef></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="2" name="Title"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="0" y="200000"/><a:ext cx="12192000" cy="900000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="E85D1A"/></a:solidFill></p:spPr>
        <p:txBody>
          <a:bodyPr lIns="457200" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="l"/><a:r><a:rPr lang="ko-KR" sz="3200" b="1"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></a:rPr><a:t>$title</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="3" name="Body"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="457200" y="1300000"/><a:ext cx="11277600" cy="5400000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr/><a:lstStyle/>
$bodyParas
        </p:txBody>
      </p:sp>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="999" name="Footer"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="11200000" y="6500000"/><a:ext cx="900000" cy="280000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr/><a:lstStyle/>
          <a:p><a:pPr algn="r"/><a:r><a:rPr lang="en-US" sz="900"><a:solidFill><a:srgbClr val="999999"/></a:solidFill></a:rPr><a:t>$($idx + 1)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

function Build-DiagramSlide($s, $idx) {
    $title = XmlEsc $s.title
    $shapesXml = ""
    $idCounter = 100
    if ($s.shapes) {
        foreach ($sh in $s.shapes) {
            $idCounter++
            if ($sh.kind -eq "box") {
                $textSize = if ($sh.textSize) { $sh.textSize } else { 1400 }
                $bold = if ($sh.bold) { $true } else { $false }
                $shapesXml += Make-Box $idCounter $sh.name $sh.x $sh.y $sh.w $sh.h $sh.fill $sh.color $sh.text $textSize $bold
            }
            elseif ($sh.kind -eq "arrow") {
                $clr = if ($sh.color) { $sh.color } else { "555555" }
                $shapesXml += Make-Arrow $idCounter $sh.name $sh.x1 $sh.y1 $sh.x2 $sh.y2 $clr
            }
            elseif ($sh.kind -eq "text") {
                $sz = if ($sh.size) { $sh.size } else { 1200 }
                $clr = if ($sh.color) { $sh.color } else { "000000" }
                $bold = if ($sh.bold) { $true } else { $false }
                $align = if ($sh.align) { $sh.align } else { "l" }
                $shapesXml += Make-Text $idCounter $sh.name $sh.x $sh.y $sh.w $sh.h $sh.text $sz $clr $bold $align
            }
        }
    }
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld>
    <p:bg><p:bgRef idx="1001"><a:schemeClr val="bg1"/></p:bgRef></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="2" name="Title"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="0" y="200000"/><a:ext cx="12192000" cy="900000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="E85D1A"/></a:solidFill></p:spPr>
        <p:txBody>
          <a:bodyPr lIns="457200" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="l"/><a:r><a:rPr lang="ko-KR" sz="3200" b="1"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></a:rPr><a:t>$title</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
$shapesXml
      <p:sp>
        <p:nvSpPr><p:cNvPr id="999" name="Footer"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="11200000" y="6500000"/><a:ext cx="900000" cy="280000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr/><a:lstStyle/>
          <a:p><a:pPr algn="r"/><a:r><a:rPr lang="en-US" sz="900"><a:solidFill><a:srgbClr val="999999"/></a:solidFill></a:rPr><a:t>$($idx + 1)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

function Build-TableSlide($s, $idx) {
    # Simple table via shapes
    $title = XmlEsc $s.title
    $tableXml = ""
    $idC = 100
    $startX = 457200
    $startY = 1500000
    $cellW = 0
    $cellH = 380000
    $colWidths = $s.colWidths
    if (-not $colWidths) {
        $cellW = [int](11277600 / $s.cols.Count)
        $colWidths = @()
        for ($c=0; $c -lt $s.cols.Count; $c++) { $colWidths += $cellW }
    }
    # Header row
    $cumX = $startX
    for ($c=0; $c -lt $s.cols.Count; $c++) {
        $idC++
        $tableXml += Make-Box $idC "H$c" $cumX $startY $colWidths[$c] $cellH "E85D1A" "FFFFFF" $s.cols[$c] 1300 $true
        $cumX += $colWidths[$c]
    }
    # Data rows
    for ($r=0; $r -lt $s.rows.Count; $r++) {
        $cumX = $startX
        $rowY = $startY + ($r + 1) * $cellH
        $row = $s.rows[$r]
        $bg = if ($r % 2 -eq 0) { "F0F0F0" } else { "FFFFFF" }
        for ($c=0; $c -lt $row.Count; $c++) {
            $idC++
            $tableXml += Make-Box $idC "C${r}_${c}" $cumX $rowY $colWidths[$c] $cellH $bg "000000" $row[$c] 1100
            $cumX += $colWidths[$c]
        }
    }
    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld>
    <p:bg><p:bgRef idx="1001"><a:schemeClr val="bg1"/></p:bgRef></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>
      <p:sp>
        <p:nvSpPr><p:cNvPr id="2" name="Title"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="0" y="200000"/><a:ext cx="12192000" cy="900000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="E85D1A"/></a:solidFill></p:spPr>
        <p:txBody>
          <a:bodyPr lIns="457200" anchor="ctr"/><a:lstStyle/>
          <a:p><a:pPr algn="l"/><a:r><a:rPr lang="ko-KR" sz="3200" b="1"><a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill></a:rPr><a:t>$title</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
$tableXml
      <p:sp>
        <p:nvSpPr><p:cNvPr id="999" name="Footer"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr><a:xfrm><a:off x="11200000" y="6500000"/><a:ext cx="900000" cy="280000"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>
        <p:txBody><a:bodyPr/><a:lstStyle/>
          <a:p><a:pPr algn="r"/><a:r><a:rPr lang="en-US" sz="900"><a:solidFill><a:srgbClr val="999999"/></a:solidFill></a:rPr><a:t>$($idx + 1)</a:t></a:r></a:p>
        </p:txBody>
      </p:sp>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>
"@
}

function Build-Slide($s, $idx) {
    if ($s.kind -eq "title")    { return Build-TitleSlide $s $idx }
    if ($s.kind -eq "section")  { return Build-SectionSlide $s $idx }
    if ($s.kind -eq "diagram")  { return Build-DiagramSlide $s $idx }
    if ($s.kind -eq "table")    { return Build-TableSlide $s $idx }
    return Build-BulletsSlide $s $idx
}

# ─── presentation.xml + rels 빌드 ─────────────────────────────────────────

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
  <p:sldSz cx="12192000" cy="6858000" type="screen16x9"/>
  <p:notesSz cx="6858000" cy="9144000"/>
  <p:defaultTextStyle/>
</p:presentation>
"@

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

AddE $zip "[Content_Types].xml" $contentTypesXml
AddE $zip "_rels/.rels" $rootRels
AddE $zip "docProps/core.xml" $coreXml
AddE $zip "docProps/app.xml" $appXmlFinal
AddE $zip "ppt/presentation.xml" $presXml
AddE $zip "ppt/_rels/presentation.xml.rels" $presRelsLines
AddE $zip "ppt/theme/theme1.xml" $themeXml
AddE $zip "ppt/slideMasters/slideMaster1.xml" $slideMaster
AddE $zip "ppt/slideMasters/_rels/slideMaster1.xml.rels" $slideMasterRels
AddE $zip "ppt/slideLayouts/slideLayout1.xml" $slideLayout
AddE $zip "ppt/slideLayouts/_rels/slideLayout1.xml.rels" $slideLayoutRels

for ($i = 0; $i -lt $Slides.Count; $i++) {
    $sxml = Build-Slide $Slides[$i] $i
    AddE $zip "ppt/slides/slide$($i+1).xml" $sxml
    AddE $zip "ppt/slides/_rels/slide$($i+1).xml.rels" $slideRels
}

$zip.Dispose()
$fs.Close()

$size = (Get-Item $OutPath).Length
Write-Output "Created: $OutPath  ($size bytes, $($Slides.Count) slides)"
}  # function New-Pptx2
