param()
$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) { throw '저장소 루트를 확인할 수 없습니다.' }
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$framework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
$scratchRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler\input-wafer-barcode-dialog'))
$runDirectory = [IO.Path]::GetFullPath((Join-Path $scratchRoot ('run-' + [Guid]::NewGuid().ToString('N'))))
if (![IO.Path]::GetDirectoryName($runDirectory).Equals($scratchRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw '격리 출력 경로가 유효하지 않습니다.'
}
$candidate = $runDirectory
while ($candidate) {
    if ((Test-Path -LiteralPath $candidate) -and ((Get-Item -LiteralPath $candidate).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw '격리 출력 경로에 재분석 지점이 있습니다.'
    }
    $candidate = [IO.Path]::GetDirectoryName($candidate)
}
[IO.Directory]::CreateDirectory($runDirectory) | Out-Null
try {
    $executable = Join-Path $runDirectory 'BarcodeDialogTests.exe'
    $arguments = @('/nologo', '/target:exe', '/langversion:7.3', '/warnaserror+', '/nostdlib+', "/out:$executable")
    foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll')) {
        $arguments += '/reference:' + (Join-Path $framework $name)
    }
    $arguments += @((Join-Path $repoRoot 'QMC.CDT-320\Ui\Dialogs\BarcodeRecoveryDialog.cs'),
        (Join-Path $repoRoot 'QMC.CDT-320\Ui\Dialogs\BarcodeRecoveryDialog.Designer.cs'),
        (Join-Path $PSScriptRoot 'dialog-test-stubs.cs'),
        (Join-Path $PSScriptRoot 'dialog-regression-tests.cs'))
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw '복구창 테스트 컴파일에 실패했습니다.' }
    & $executable
    if ($LASTEXITCODE -ne 0) { throw '복구창 테스트에 실패했습니다.' }
}
finally {
    $resolved = (Resolve-Path -LiteralPath $runDirectory).Path
    if (!$resolved.Equals($runDirectory, [StringComparison]::OrdinalIgnoreCase) -or
        ![IO.Path]::GetDirectoryName($resolved).Equals($scratchRoot, [StringComparison]::OrdinalIgnoreCase) -or
        @((Get-Item -LiteralPath $resolved),(Get-Item -LiteralPath $scratchRoot) | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -ne 0 -or
        @(Get-ChildItem -LiteralPath $resolved -Directory -Force).Count -ne 0) {
        throw '격리 출력 정리 경로 확인에 실패했습니다.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
