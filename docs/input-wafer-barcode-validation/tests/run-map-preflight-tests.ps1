param([switch]$KeepResults)
$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath (Join-Path $repoRoot 'QMC.CDT-320.sln'))) {
    throw 'Handler 저장소에서 테스트를 실행해야 합니다.'
}
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$framework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
$scratchRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler\input-wafer-map-preflight'))
$runDirectory = [IO.Path]::GetFullPath((Join-Path $scratchRoot ('run-' + [Guid]::NewGuid().ToString('N'))))
function Assert-SafeRunDirectory {
    if (![IO.Path]::GetDirectoryName($runDirectory).Equals($scratchRoot, [StringComparison]::OrdinalIgnoreCase) -or
        ![IO.Path]::GetFileName($runDirectory).StartsWith('run-', [StringComparison]::Ordinal)) {
        throw '검증 출력 경로가 허용된 테스트 폴더 밖입니다.'
    }
    $candidate = $runDirectory
    while ($candidate) {
        if ((Test-Path -LiteralPath $candidate) -and
            ((Get-Item -LiteralPath $candidate).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "검증 출력 경로에 재분석 지점이 있습니다: $candidate"
        }
        $candidate = [IO.Path]::GetDirectoryName($candidate)
    }
}
Assert-SafeRunDirectory
[IO.Directory]::CreateDirectory($runDirectory) | Out-Null
try {
    $service = Join-Path $repoRoot 'QMC.CDT-320\Equipment\Lots\InputWaferMapPreflightService.cs'
    $executable = Join-Path $runDirectory 'MapPreflightTests.exe'
    Write-Output ('Preflight SHA256: ' + (Get-FileHash -LiteralPath $service).Hash)
    $arguments = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+', '/nostdlib+', "/out:$executable")
    foreach ($reference in @('mscorlib.dll', 'System.dll', 'System.Core.dll')) {
        $arguments += '/reference:' + (Join-Path $framework $reference)
    }
    $arguments += @($service,
        (Join-Path $PSScriptRoot 'map-preflight-test-stubs.cs'),
        (Join-Path $PSScriptRoot 'map-preflight-tests.cs'))
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw "맵 사전 확인 회귀검증 컴파일 실패: $LASTEXITCODE" }
    & $executable
    if ($LASTEXITCODE -ne 0) { throw "맵 사전 확인 회귀검증 실패: $LASTEXITCODE" }
}
finally {
    if ($KeepResults) { Write-Output "검증 산출물 보존: $runDirectory" }
    elseif (Test-Path -LiteralPath $runDirectory) {
        Assert-SafeRunDirectory
        if (@(Get-ChildItem -LiteralPath $runDirectory -Directory -Force).Count -ne 0) {
            throw '예상하지 않은 하위 디렉터리가 있어 검증 산출물을 자동 삭제하지 않습니다.'
        }
        Remove-Item -LiteralPath $runDirectory -Recurse -Force
    }
}
