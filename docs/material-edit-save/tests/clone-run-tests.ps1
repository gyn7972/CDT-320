param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if (!(Test-Path -LiteralPath (Join-Path $repoRoot 'QMC.CDT-320.sln'))) {
    throw 'Handler 저장소에서 테스트를 실행해야 합니다.'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot '_codex_verify_material_clone'
}
$resolvedOutput = [IO.Path]::GetFullPath($OutputDirectory)
if ($resolvedOutput.Equals('D:\CDT-320', [StringComparison]::OrdinalIgnoreCase) -or
    $resolvedOutput.StartsWith('D:\CDT-320\', [StringComparison]::OrdinalIgnoreCase)) {
    throw '운영 경로는 테스트 출력으로 사용할 수 없습니다.'
}
[IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null
$materials = Join-Path $repoRoot 'QMC.CDT-320\Equipment\Materials'
$storePath = Join-Path $materials 'MaterialSnapshotStore.cs'
$store = [IO.File]::ReadAllText($storePath)
$generated = New-Object Text.StringBuilder
[void]$generated.AppendLine('using System; using System.Collections; using System.Collections.Concurrent; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Runtime.CompilerServices; using System.Threading; using QMC.Common;')
[void]$generated.AppendLine('namespace QMC.CDT320.Materials { public static class MaterialSnapshotStore {')

# 파일 저장/운영 설정/장비 연결 코드를 컴파일하지 않고 현재 생산 코드의 복사 경로만 추출한다.
$methodNames = @('CreateSaveCopy', 'CreateStateCopy', 'BeginApplicationExitFullSave', 'ShouldSaveInspectionDetail',
    'TrimInspectionDetailIfDisabled', 'IsTypedCloneUsable', 'MatchesTypedCloneShape',
    'CloneIntList', 'CloneStringList', 'CloneLocation', 'CloneVisionOffset', 'CloneMeasurement',
    'CloneAlignment', 'CloneInspectionRecord', 'CloneDie', 'CloneCassetteSlot', 'CloneCassette',
    'CloneOutputReceiveSlot', 'CloneWafer', 'CloneSnapshotTyped', 'CloneSnapshotForSave',
    'GetSnapshotProperties', 'CloneObject')
foreach ($name in $methodNames) {
    $pattern = '(?ms)^        (?:public|private|internal) static [^\r\n]*?\b' + [regex]::Escape($name) + '\s*\(.*?^        \}\r?$'
    $matches = [regex]::Matches($store, $pattern)
    if ($matches.Count -eq 0) { throw "생산 메서드를 찾지 못했습니다: $name" }
    foreach ($match in $matches) {
        $line = 1 + [regex]::Matches($store.Substring(0, $match.Index), '\n').Count
        [void]$generated.AppendLine('#line ' + $line + ' "' + $storePath.Replace('\', '\\') + '"')
        [void]$generated.AppendLine($match.Value)
        [void]$generated.AppendLine('#line default')
    }
}
foreach ($name in @('_typedCloneUsable', '_snapshotPropertyCache', '_applicationExitFullSave')) {
    $pattern = '(?ms)^        private static [^;\r\n]*\b' + [regex]::Escape($name) + '\b[^;]*;'
    $match = [regex]::Match($store, $pattern)
    if (!$match.Success) { throw "생산 필드를 찾지 못했습니다: $name" }
    [void]$generated.AppendLine($match.Value)
}
$comparer = [regex]::Match($store, '(?ms)^        private sealed class ReferenceEqualityComparer\b.*?^        \}\r?$')
if (!$comparer.Success) { throw '생산 참조 비교기를 찾지 못했습니다.' }
[void]$generated.AppendLine($comparer.Value)
[void]$generated.AppendLine('} }')

# Wafer에 저장되는 Review 검증 자료도 현재 생산 모델과 실제 Clone 메서드를 사용한다.
$reviewPath = Join-Path $materials 'MaterialStateService.InputStageReviewGeometry.cs'
$review = [IO.File]::ReadAllText($reviewPath)
$reviewClass = [regex]::Match($review, '(?ms)^    \[DataContract\]\r?\n    public sealed class InputStageReviewSavedVerification\b.*?^    \}\r?$')
if (!$reviewClass.Success) { throw '현재 Review 저장 모델을 찾지 못했습니다.' }
[void]$generated.AppendLine('namespace QMC.CDT320.Materials { using System.Runtime.Serialization;')
[void]$generated.AppendLine($reviewClass.Value)
[void]$generated.AppendLine('}')
$generatedPath = Join-Path $resolvedOutput 'CurrentMaterialSnapshotClone.cs'
$utf8Bom = New-Object Text.UTF8Encoding($true)
[IO.File]::WriteAllText($generatedPath, ($generated.ToString() -replace '\r?\n', "`r`n"), $utf8Bom)
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$exe = Join-Path $resolvedOutput 'MaterialCloneTests.exe'
$arguments = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+',
    '/reference:System.Runtime.Serialization.dll', "/out:$exe", $generatedPath,
    (Join-Path $materials 'Die.cs'), (Join-Path $materials 'MaterialModel.cs'),
    (Join-Path $materials 'InputStageReviewGeometryPolicy.cs'), (Join-Path $PSScriptRoot 'clone-tests.cs'))
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Material 복사 회귀검증 컴파일 실패: $LASTEXITCODE" }
& $exe
if ($LASTEXITCODE -ne 0) { throw "Material 복사 회귀검증 실패: $LASTEXITCODE" }
