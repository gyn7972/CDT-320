$ErrorActionPreference = 'Stop'
$batchRepoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ([IO.Path]::GetFullPath($batchRepoRoot).TrimEnd('\') -ne 'D:\Source\CDT-320_New') {
    throw 'ALL 어댑터 검증은 현재 기준 저장소에서만 실행하십시오.'
}
$batchCompiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$batchRunner = Join-Path $batchRepoRoot 'QMC.CDT-320\Sequencing\ManualOutputBatchRunner.cs'
$batchAdapter = Join-Path $batchRepoRoot 'QMC.CDT-320\Sequencing\OutputSequence.ManualBatch.cs'
$batchStopException = Join-Path $batchRepoRoot 'QMC.CDT-320\Sequencing\Common\SequenceStopException.cs'
$batchStubs = Join-Path $PSScriptRoot 'ManualOutputAdapterTestStubs.cs'
$batchHarness = Join-Path $PSScriptRoot 'ManualOutputAdapterTests.cs'
$batchOutputDirectory = Join-Path $batchRepoRoot '_codex_verify_manual_output_all'
[IO.Directory]::CreateDirectory($batchOutputDirectory) | Out-Null
$batchOutputExe = Join-Path $batchOutputDirectory 'ManualOutputAdapterTests.exe'
& $batchCompiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$batchOutputExe" $batchRunner $batchAdapter $batchStopException $batchStubs $batchHarness
if ($LASTEXITCODE -ne 0) { throw "ALL 어댑터 검증 코드 컴파일 실패: $LASTEXITCODE" }
& $batchOutputExe
if ($LASTEXITCODE -ne 0) { throw "ALL 어댑터 오프라인 검증 실패: $LASTEXITCODE" }
