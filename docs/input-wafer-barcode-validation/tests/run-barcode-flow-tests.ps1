$ErrorActionPreference = 'Stop'
$flowRepo = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath (Join-Path $flowRepo 'QMC.CDT-320.sln'))) {
    throw 'Handler 저장소에서 실행해야 합니다.'
}
$flowCompiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$flowFramework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
$flowScratchRoot = [IO.Path]::GetFullPath((Join-Path $flowRepo '_build_check_handler\input-wafer-barcode-flow'))
$flowRun = [IO.Path]::GetFullPath((Join-Path $flowScratchRoot ('run-' + [Guid]::NewGuid().ToString('N'))))
function Assert-FlowScratch {
    if (![IO.Path]::GetDirectoryName($flowRun).Equals($flowScratchRoot, [StringComparison]::OrdinalIgnoreCase) -or
        ![IO.Path]::GetFileName($flowRun).StartsWith('run-', [StringComparison]::Ordinal)) {
        throw '검증 출력 경로가 허용된 범위를 벗어났습니다.'
    }
    $flowAncestor = $flowRun
    while ($flowAncestor) {
        if ((Test-Path -LiteralPath $flowAncestor) -and
            ((Get-Item -LiteralPath $flowAncestor).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw '검증 출력 경로에 재분석 지점이 있습니다.'
        }
        $flowAncestor = [IO.Path]::GetDirectoryName($flowAncestor)
    }
}
Assert-FlowScratch
[IO.Directory]::CreateDirectory($flowRun) | Out-Null
try {
    $flowSourcePath = Join-Path $flowRepo 'QMC.CDT-320\Sequencing\InputFeeder\InputFeederLoadToStageSequence.cs'
    $flowPolicyPath = Join-Path $flowRepo 'QMC.CDT-320\Equipment\InputWaferBarcodePolicy.cs'
    $flowSourceHash = (Get-FileHash -LiteralPath $flowSourcePath -Algorithm SHA256).Hash
    $flowPolicyHash = (Get-FileHash -LiteralPath $flowPolicyPath -Algorithm SHA256).Hash
    $flowText = [IO.File]::ReadAllText($flowSourcePath)
    # Delimiters are production declarations. No hand-copied validation logic is executed.
    $flowSections = @(
        @('        internal static async Task<WaferMaterial> ValidateAndApplyInputBarcodeAsync(', '        private async Task<int> EnsureInputVisionAvoidAfterBarcodeAsync('),
        @('        private static string NormalizeBarcode(string value)', '        //MoveInputCassetteAvoidPositionAsync'))
    $flowExtract = ''
    foreach ($flowSection in $flowSections) {
        $flowStart = $flowText.IndexOf($flowSection[0], [StringComparison]::Ordinal)
        $flowEnd = $flowText.IndexOf($flowSection[1], [StringComparison]::Ordinal)
        if ($flowStart -lt 0 -or $flowEnd -le $flowStart) { throw '운영 메서드 추출 경계가 변경되었습니다.' }
        $flowExtract += $flowText.Substring($flowStart, $flowEnd - $flowStart)
    }
    $flowExtract = 'using System; using System.Linq; using System.Threading; using System.Threading.Tasks; ' +
        'using QMC.CDT320.Materials; using QMC.CDT320.Barcode; using QMC.CDT320.Lots; ' +
        'namespace QMC.CDT320.Sequencing { internal static class InputFeederLoadToStageSequence {' +
        [Environment]::NewLine + $flowExtract + '}}'
    $flowExtractPath = Join-Path $flowRun 'BarcodeValidation.actual-source.cs'
    [IO.File]::WriteAllText($flowExtractPath, $flowExtract, [Text.UTF8Encoding]::new($true))
    $flowExe = Join-Path $flowRun 'BarcodeFlowTests.exe'
    $flowArgs = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+', '/nostdlib+', "/out:$flowExe")
    foreach ($flowReference in @('mscorlib.dll', 'System.dll', 'System.Core.dll')) {
        $flowArgs += '/reference:' + (Join-Path $flowFramework $flowReference)
    }
    $flowArgs += @($flowExtractPath, $flowPolicyPath,
        (Join-Path $PSScriptRoot 'barcode-flow-test-stubs.cs'), (Join-Path $PSScriptRoot 'barcode-flow-tests.cs'))
    & $flowCompiler @flowArgs
    if ($LASTEXITCODE -ne 0) { throw "바코드 흐름 회귀 컴파일 실패: $LASTEXITCODE" }
    & $flowExe
    if ($LASTEXITCODE -ne 0) { throw "바코드 흐름 회귀 실패: $LASTEXITCODE" }
    if ($flowSourceHash -ne (Get-FileHash -LiteralPath $flowSourcePath -Algorithm SHA256).Hash -or
        $flowPolicyHash -ne (Get-FileHash -LiteralPath $flowPolicyPath -Algorithm SHA256).Hash) {
        throw '검증 중 운영 소스가 변경되었습니다. 최종 소스로 재검증해야 합니다.'
    }
    Write-Output "Production sequence SHA256: $flowSourceHash"
    Write-Output "Production policy SHA256: $flowPolicyHash"
}
finally {
    Assert-FlowScratch
    if (Test-Path -LiteralPath $flowRun) { Remove-Item -LiteralPath $flowRun -Recurse -Force }
}
