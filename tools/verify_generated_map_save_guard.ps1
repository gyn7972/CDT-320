param([switch]$KeepArtifacts)
$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$allowedParent = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler')).TrimEnd('\') + '\'
$verifyRoot = [IO.Path]::GetFullPath((Join-Path $allowedParent ('verify-generated-map-save-guard-' + [Guid]::NewGuid().ToString('N'))))
if (-not $verifyRoot.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid verification path.' }
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
$source = Join-Path $repoRoot 'QMC.CDT-320\Form1.WaferMapGeneration.cs'
$test = Join-Path $PSScriptRoot 'verify_generated_map_save_guard.cs'
$exitCode = 1
try {
    New-Item -ItemType Directory -Path $verifyRoot | Out-Null
    $exe = Join-Path $verifyRoot 'verify_generated_map_save_guard.exe'
    & $compiler /nologo /target:exe /platform:anycpu "/out:$exe" /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll $source $test
    if ($LASTEXITCODE -ne 0) { throw 'Save coordinator verification compilation failed.' }
    & $exe | Tee-Object -FilePath (Join-Path $verifyRoot 'verification.log')
    $exitCode = $LASTEXITCODE
}
finally {
    if ($KeepArtifacts) { Write-Output "Verification artifacts: $verifyRoot" }
    elseif (Test-Path -LiteralPath $verifyRoot) {
        $resolved = (Resolve-Path -LiteralPath $verifyRoot).Path.TrimEnd('\')
        if (-not $resolved.Equals($verifyRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not $resolved.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase) -or
            -not ([IO.Path]::GetFileName($resolved)).StartsWith('verify-generated-map-save-guard-', [StringComparison]::Ordinal)) {
            throw 'Refusing cleanup outside dedicated verification directory.'
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
exit $exitCode
