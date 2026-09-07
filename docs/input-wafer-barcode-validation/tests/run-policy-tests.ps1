param([string]$ConfigPath = 'D:\Source\DATA_LOG\Log\EquipmentData\Config\InputStageUnit.json')

$ErrorActionPreference = 'Stop'
$barcodeRepo = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath (Join-Path $barcodeRepo 'QMC.CDT-320.sln'))) {
    throw 'Handler 저장소에서 실행해야 합니다.'
}
$barcodeCompiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$barcodeFramework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
$barcodeScratchRoot = [IO.Path]::GetFullPath((Join-Path $barcodeRepo '_build_check_handler\input-wafer-barcode-policy'))
$barcodeRun = [IO.Path]::GetFullPath((Join-Path $barcodeScratchRoot ('run-' + [Guid]::NewGuid().ToString('N'))))
function Assert-BarcodeScratch {
    if (![IO.Path]::GetDirectoryName($barcodeRun).Equals($barcodeScratchRoot, [StringComparison]::OrdinalIgnoreCase) -or
        ![IO.Path]::GetFileName($barcodeRun).StartsWith('run-', [StringComparison]::Ordinal)) {
        throw '검증 출력 경로가 허용된 범위를 벗어났습니다.'
    }
    $barcodeAncestor = $barcodeRun
    while ($barcodeAncestor) {
        if ((Test-Path -LiteralPath $barcodeAncestor) -and
            ((Get-Item -LiteralPath $barcodeAncestor).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw '검증 출력 경로에 재분석 지점이 있습니다.'
        }
        $barcodeAncestor = [IO.Path]::GetDirectoryName($barcodeAncestor)
    }
}
Assert-BarcodeScratch
if (!(Test-Path -LiteralPath $ConfigPath)) { throw '읽기 전용 InputStage 공통 Config를 찾을 수 없습니다.' }
[IO.Directory]::CreateDirectory($barcodeRun) | Out-Null
try {
    # 장비 클래스는 컴파일하지 않고 실제 소스의 데이터 선언부만 그대로 추출한다.
    $barcodeUnitPath = Join-Path $barcodeRepo 'QMC.CDT-320\Equipment\Unit\InputStageUnit.cs'
    $barcodeUnitText = [IO.File]::ReadAllText($barcodeUnitPath)
    $barcodeDataStart = $barcodeUnitText.IndexOf('    public enum InputDieVisionFailureAction', [StringComparison]::Ordinal)
    if ($barcodeDataStart -lt 0) { throw 'InputStage 데이터 선언을 찾을 수 없습니다.' }
    $barcodeDataStart = $barcodeUnitText.LastIndexOf('    [DataContract]', $barcodeDataStart, [StringComparison]::Ordinal)
    $barcodeDataEnd = $barcodeUnitText.IndexOf('    public partial class InputStageUnit', $barcodeDataStart, [StringComparison]::Ordinal)
    if ($barcodeDataStart -lt 0 -or $barcodeDataEnd -le $barcodeDataStart) { throw 'InputStage 데이터 선언 경계가 변경되었습니다.' }
    $barcodeDataText = 'using System; using System.Runtime.Serialization; using QMC.Common; using QMC.CDT320.VisionComm; namespace QMC.CDT320 {' +
        [Environment]::NewLine + $barcodeUnitText.Substring($barcodeDataStart, $barcodeDataEnd - $barcodeDataStart) + '}'
    $barcodeDataPath = Join-Path $barcodeRun 'InputStageData.actual-source.cs'
    [IO.File]::WriteAllText($barcodeDataPath, $barcodeDataText, [Text.UTF8Encoding]::new($true))
    $barcodeExe = Join-Path $barcodeRun 'InputWaferBarcodePolicyTests.exe'
    $barcodeArgs = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+', '/nostdlib+', "/out:$barcodeExe")
    foreach ($barcodeReference in @('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Runtime.Serialization.dll')) {
        $barcodeArgs += '/reference:' + (Join-Path $barcodeFramework $barcodeReference)
    }
    $barcodeArgs += @(
        (Join-Path $barcodeRepo 'QMC.CDT-320\Equipment\InputWaferBarcodePolicy.cs'),
        (Join-Path $barcodeRepo 'QMC.CDT-320\Equipment\Vision\VisionAlignTargetIds.cs'),
        (Join-Path $barcodeRepo 'QMC.CDT-320\Equipment\Vision\VisionToolIds.cs'),
        (Join-Path $barcodeRepo 'QMC.Common\Interfaces\IEquipmentData.cs'),
        $barcodeDataPath,
        (Join-Path $PSScriptRoot 'policy-regression-tests.cs'))
    & $barcodeCompiler @barcodeArgs
    if ($LASTEXITCODE -ne 0) { throw "정책 회귀 컴파일 실패: $LASTEXITCODE" }
    & $barcodeExe $ConfigPath
    if ($LASTEXITCODE -ne 0) { throw "정책 회귀 실패: $LASTEXITCODE" }
}
finally {
    Assert-BarcodeScratch
    if (Test-Path -LiteralPath $barcodeRun) { Remove-Item -LiteralPath $barcodeRun -Recurse -Force }
}
