param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if (!(Test-Path -LiteralPath (Join-Path $repoRoot 'QMC.CDT-320.sln'))) {
    throw 'Handler 저장소에서 테스트를 실행해야 합니다.'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot '_codex_verify_material_display'
}
$resolvedOutput = [IO.Path]::GetFullPath($OutputDirectory)
if ($resolvedOutput.Equals('D:\CDT-320', [StringComparison]::OrdinalIgnoreCase) -or
    $resolvedOutput.StartsWith('D:\CDT-320\', [StringComparison]::OrdinalIgnoreCase)) {
    throw '운영 경로는 테스트 출력으로 사용할 수 없습니다.'
}
[IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null
$materials = Join-Path $repoRoot 'QMC.CDT-320\Equipment\Materials'
$review = [IO.File]::ReadAllText((Join-Path $materials 'MaterialStateService.InputStageReviewGeometry.cs'))
$reviewClass = [regex]::Match($review, '(?ms)^    \[DataContract\]\r?\n    public sealed class InputStageReviewSavedVerification\b.*?^    \}\r?$')
if (!$reviewClass.Success) { throw '현재 Review 저장 모델을 찾지 못했습니다.' }
$model = 'using System.Collections.Generic; using System.Runtime.Serialization; namespace QMC.CDT320.Materials {' +
    "`r`n" + $reviewClass.Value + "`r`n}"
$modelPath = Join-Path $resolvedOutput 'CurrentReviewSavedVerification.cs'
[IO.File]::WriteAllText($modelPath, ($model -replace '\r?\n', "`r`n"), (New-Object Text.UTF8Encoding($true)))
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$exe = Join-Path $resolvedOutput 'MaterialDisplayTests.exe'
$arguments = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+',
    '/reference:System.Runtime.Serialization.dll', "/out:$exe", $modelPath,
    (Join-Path $materials 'Die.cs'), (Join-Path $materials 'MaterialModel.cs'),
    (Join-Path $materials 'InputStageReviewGeometryPolicy.cs'),
    (Join-Path $materials 'MaterialStateService.Display.cs'), (Join-Path $PSScriptRoot 'display-tests.cs'))
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Material 표시 회귀검증 컴파일 실패: $LASTEXITCODE" }
& $exe
if ($LASTEXITCODE -ne 0) { throw "Material 표시 회귀검증 실패: $LASTEXITCODE" }
