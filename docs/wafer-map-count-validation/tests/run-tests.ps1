param(
    [string]$RkeMapDirectory = 'D:\Source\EQP_Handler\20260907\YZAMH',
    [string]$LegacyMapDirectory = 'D:\CDT-320\Config\WaferMap',
    [switch]$SkipRealFiles,
    [switch]$KeepResults
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath (Join-Path $repoRoot 'QMC.CDT-320.sln'))) {
    throw 'Handler 저장소에서 테스트를 실행해야 합니다.'
}

$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$framework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if (!(Test-Path -LiteralPath $compiler) -or !(Test-Path -LiteralPath (Join-Path $framework 'mscorlib.dll'))) {
    throw 'Visual Studio 2022 Roslyn 및 .NET Framework 4.7.2 Developer Pack이 필요합니다.'
}

$scratchRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler\wafer-map-count-validation'))
$runDirectory = [IO.Path]::GetFullPath((Join-Path $scratchRoot ('run-' + [Guid]::NewGuid().ToString('N'))))
function Assert-SafeRunDirectory {
    if (![IO.Path]::GetDirectoryName($runDirectory).Equals($scratchRoot, [StringComparison]::OrdinalIgnoreCase) -or
        ![IO.Path]::GetFileName($runDirectory).StartsWith('run-', [StringComparison]::Ordinal)) {
        throw '테스트 출력 경로가 허용된 검증 폴더 밖입니다.'
    }
    $candidate = $runDirectory
    while ($candidate -and !$candidate.Equals($repoRoot.Replace('/', '\'), [StringComparison]::OrdinalIgnoreCase)) {
        if (Test-Path -LiteralPath $candidate) {
            if ((Get-Item -LiteralPath $candidate).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "테스트 출력 경로에 재분석 지점이 있습니다: $candidate"
            }
        }
        $candidate = [IO.Path]::GetDirectoryName($candidate)
    }
}

Assert-SafeRunDirectory
[IO.Directory]::CreateDirectory($runDirectory) | Out-Null
try {
    $generator = Join-Path $repoRoot 'QMC.CDT-320\Equipment\DieMaps\DieMapGenerator.cs'
    $model = Join-Path $repoRoot 'QMC.CDT-320\Equipment\DieMaps\DieMap.cs'
    $executable = Join-Path $runDirectory 'WaferMapParserRegressionTests.exe'
    Write-Output ('Parser SHA256: ' + (Get-FileHash -LiteralPath $generator).Hash)
    $references = @('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Runtime.Serialization.dll')
    $arguments = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+', '/nostdlib+', "/out:$executable")
    foreach ($reference in $references) {
        $arguments += '/reference:' + (Join-Path $framework $reference)
    }
    $arguments += @($generator, $model,
        (Join-Path $PSScriptRoot 'parser-test-stubs.cs'),
        (Join-Path $PSScriptRoot 'parser-regression-tests.cs'))
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw "파서 회귀검증 컴파일 실패: $LASTEXITCODE" }

    & $executable $runDirectory $RkeMapDirectory $LegacyMapDirectory $SkipRealFiles.IsPresent.ToString()
    if ($LASTEXITCODE -ne 0) { throw "파서 회귀검증 실패: $LASTEXITCODE" }
}
finally {
    if ($KeepResults) {
        Write-Output "검증 산출물 보존: $runDirectory"
    }
    elseif (Test-Path -LiteralPath $runDirectory) {
        Assert-SafeRunDirectory
        if (@(Get-ChildItem -LiteralPath $runDirectory -Directory -Force).Count -ne 0) {
            throw '예상하지 않은 하위 디렉터리가 있어 검증 산출물 자동 정리를 중단했습니다.'
        }
        Remove-Item -LiteralPath $runDirectory -Recurse -Force
    }
}
