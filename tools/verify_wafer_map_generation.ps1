param(
    [string]$Reference684Path = '',
    [switch]$KeepArtifacts
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$modelSource = Join-Path $repoRoot 'QMC.CDT-320\Equipment\DieMaps\WaferMapGeneration.cs'
$testSource = Join-Path $PSScriptRoot 'verify_wafer_map_generation.cs'
$compiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
if (-not (Test-Path -LiteralPath $compiler) -or -not (Test-Path -LiteralPath $modelSource)) {
    throw 'The standalone compiler and WaferMapGeneration.cs must exist before verification.'
}
if ($Reference684Path) {
    $Reference684Path = (Resolve-Path -LiteralPath $Reference684Path).Path
}

$allowedParent = [IO.Path]::GetFullPath((Join-Path $repoRoot '_build_check_handler')).TrimEnd('\') + '\'
$verifyRoot = [IO.Path]::GetFullPath((Join-Path $allowedParent ('verify-wafer-map-generation-' + [Guid]::NewGuid().ToString('N'))))
if (-not $verifyRoot.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Verification output escaped this repository _build_check_handler directory.'
}
$exitCode = 1
try {
    New-Item -ItemType Directory -Path $verifyRoot | Out-Null
    $verificationExe = Join-Path $verifyRoot 'verify_wafer_map_generation.exe'
    # Compile only the pure model and this oracle harness. No Handler assembly, machine,
    # settings, GUI, SDK, production recipe, or operational executable is loaded.
    & $compiler /nologo /target:exe /platform:anycpu "/out:$verificationExe" /r:System.Core.dll $modelSource $testSource
    if ($LASTEXITCODE -ne 0) { throw "Standalone verification compilation failed: $LASTEXITCODE" }
    & $verificationExe $verifyRoot $Reference684Path | Tee-Object -FilePath (Join-Path $verifyRoot 'verification.log')
    $exitCode = $LASTEXITCODE
}
finally {
    if ($KeepArtifacts) {
        Write-Output "Verification artifacts: $verifyRoot"
    }
    elseif (Test-Path -LiteralPath $verifyRoot) {
        $resolvedVerify = (Resolve-Path -LiteralPath $verifyRoot).Path.TrimEnd('\')
        if (-not $resolvedVerify.Equals($verifyRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not $resolvedVerify.StartsWith($allowedParent, [StringComparison]::OrdinalIgnoreCase) -or
            -not ([IO.Path]::GetFileName($resolvedVerify)).StartsWith('verify-wafer-map-generation-', [StringComparison]::Ordinal)) {
            throw 'Refusing cleanup because the resolved verification path differs from its dedicated build directory.'
        }
        Remove-Item -LiteralPath $resolvedVerify -Recurse -Force
    }
}
exit $exitCode
