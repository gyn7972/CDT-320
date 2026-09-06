$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ([IO.Path]::GetFullPath($repoRoot).TrimEnd('\') -ne 'D:\Source\CDT-320_New') {
    throw '정책 검증은 현재 기준 저장소에서만 실행하십시오.'
}
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$policy = Join-Path $repoRoot 'QMC.CDT-320\Equipment\Materials\InputStageReviewGeometryPolicy.cs'
$harness = Join-Path $PSScriptRoot 'InputStageReviewGeometryPolicyTests.cs'
$outputDirectory = Join-Path $repoRoot '_codex_verify_inputstage_geometry_policy'
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$outputExe = Join-Path $outputDirectory 'InputStageReviewGeometryPolicyTests.exe'
& $compiler /nologo /target:exe /langversion:7.3 /warnaserror+ /reference:System.Runtime.Serialization.dll "/out:$outputExe" $policy $harness
if ($LASTEXITCODE -ne 0) { throw "정책 검증 코드 컴파일 실패: $LASTEXITCODE" }
& $outputExe
if ($LASTEXITCODE -ne 0) { throw "정책 오프라인 검증 실패: $LASTEXITCODE" }
