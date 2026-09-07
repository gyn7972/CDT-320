$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if (!(Test-Path -LiteralPath (Join-Path $repoRoot 'QMC.CDT-320.sln'))) {
    throw 'Handler 저장소 루트에서 테스트를 실행해야 합니다.'
}
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$outputDirectory = Join-Path $repoRoot '_codex_verify_input_cassette_clear'
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$materials = Join-Path $repoRoot 'QMC.CDT-320\Equipment\Materials'
$source = New-Object Text.StringBuilder
[void]$source.AppendLine('using System; using System.Collections.Generic; using System.Linq; using QMC.Common;')
[void]$source.AppendLine('namespace QMC.CDT320.Materials { public static partial class MaterialStateService {')

# 테스트 대상 메서드는 현재 소스에서 본문 전체를 그대로 가져온다.
# MaterialStorage/Save, Machine, Unit 및 운영 EXE/DLL은 로드하거나 실행하지 않는다.
function Add-ProductionMethods([string]$fileName, [string[]]$methodNames) {
    $path = Join-Path $materials $fileName
    $contents = [IO.File]::ReadAllText($path)
    foreach ($methodName in $methodNames) {
        $pattern = '(?ms)^        (?:public|private|internal) static [^\r\n]*?\b' +
            [regex]::Escape($methodName) + '\s*\(.*?^        \}\r?$'
        $matches = [regex]::Matches($contents, $pattern)
        if ($matches.Count -eq 0) { throw "생산 코드 메서드를 찾지 못했습니다: $fileName / $methodName" }
        foreach ($match in $matches) {
            $line = 1 + [regex]::Matches($contents.Substring(0, $match.Index), '\n').Count
            [void]$source.AppendLine('#line ' + $line + ' "' + $path.Replace('\', '\\') + '"')
            [void]$source.AppendLine($match.Value)
            [void]$source.AppendLine('#line default')
        }
    }
}

$clearMethods = @(
    'LogMaterialClearBlocked', 'RemoveDieMaterialsNoLock',
    'TryCollectInputParentDiesNoLock', 'TryCollectInputLocationClearDiesNoLock',
    'TryCollectInputHistoryForClearNoLock', 'DemotePreservedInputHistoryLocationsNoLock',
    'ClearInputCassetteSlotData', 'ResetCassetteMappingIfEmptyNoLock',
    'ClearInputCassetteAllSlotData', 'GetCassetteClearTargetWafersNoLock',
    'TryValidateCassetteRoleForClearNoLock', 'TryCollectOutputCassetteClearDiesNoLock',
    'TryCollectOutputParentDiesNoLock', 'TryClassifyOutputDiesForDetachNoLock',
    'CanCompleteInputCassetteExchange', 'ClearInputCassetteForExchange',
    'TryValidatePickerProductDataEmpty', 'TryValidatePickerProductDataEmptyNoLock',
    'TryValidateInputCassetteExchangeNoLock', 'ClearInputCassetteAllSlotDataCore',
    'IsInputClearLocationInScope', 'TryValidateInputCassetteWaferPointersNoLock'
)
Add-ProductionMethods 'MaterialStateService.Clear.cs' $clearMethods
Add-ProductionMethods 'MaterialStateService.CassetteMapping.cs' @(
    'ResolveCassetteSlotWaferNoLock', 'ResolveCassetteSlotWaferForValidationNoLock',
    'ResolveCassetteSlotWaferCoreNoLock', 'IsWaferAtCassetteSlot', 'IsOutputCassetteRole')
Add-ProductionMethods 'MaterialStateService.cs' @('EnsureWaferInstanceIdNoLock', 'CreateWaferInstanceId')
[void]$source.AppendLine('} }')
$generated = Join-Path $outputDirectory 'CurrentInputCassetteClear.cs'
$utf8Bom = New-Object Text.UTF8Encoding($true)
[IO.File]::WriteAllText($generated, $source.ToString(), $utf8Bom)
$exe = Join-Path $outputDirectory 'InputCassetteClearTests.exe'
$arguments = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+',
    '/reference:System.Runtime.Serialization.dll', "/out:$exe")
$arguments += @(
    (Join-Path $materials 'Die.cs'),
    (Join-Path $materials 'MaterialModel.cs'),
    (Join-Path $materials 'MaterialStateCompactor.cs'),
    (Join-Path $materials 'MaterialSnapshotRevisionPolicy.cs'),
    $generated,
    (Join-Path $PSScriptRoot 'InputCassetteClearTests.cs'))
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "카세트 Clear 오프라인 테스트 컴파일 실패: $LASTEXITCODE" }
& $exe
if ($LASTEXITCODE -ne 0) { throw "카세트 Clear 회귀 테스트 실패: $LASTEXITCODE" }
