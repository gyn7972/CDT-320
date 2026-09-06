[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$controllerPath = Join-Path $repoRoot 'QMC.CDT-320\Equipment\MachineController.RecipeReset.cs'
$templatePath = Join-Path $PSScriptRoot 'InputCassetteClearControllerTests.cs'
$outputDirectory = Join-Path $repoRoot '_codex_verify_input_clear_controller'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) {
    $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compilerPath)) {
    throw '.NET Framework 4 C# compiler was not found.'
}

$controller = [IO.File]::ReadAllText($controllerPath)
$methodStart = $controller.IndexOf('        public bool TryBeginInputCassetteClearOperation(')
$methodEnd = $controller.IndexOf('        private bool TryRegisterRecipeApplyOperation(', $methodStart)
if ($methodStart -lt 0 -or $methodEnd -le $methodStart) {
    throw 'Controller test extraction markers changed. Review the harness before running.'
}
$methods = $controller.Substring($methodStart, $methodEnd - $methodStart)
$template = [IO.File]::ReadAllText($templatePath)
$marker = '// __INPUT_CASSETTE_CLEAR_METHODS__'
if (($template.Split([string[]]@($marker), [StringSplitOptions]::None)).Count -ne 2) {
    throw 'The controller test template must contain exactly one injection marker.'
}

# Only an isolated executable built from stubs and the current methods is launched.
# The equipment application, hardware board, motion, and persistent Material files are never used.
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$generatedSource = Join-Path $outputDirectory 'controller-harness.cs'
$testExecutable = Join-Path $outputDirectory 'controller-harness.exe'
[IO.File]::WriteAllText($generatedSource, $template.Replace($marker, $methods), (New-Object Text.UTF8Encoding($true)))
& $compilerPath /nologo /warn:4 "/out:$testExecutable" $generatedSource
if ($LASTEXITCODE -ne 0) {
    throw "Controller test compilation failed with exit code $LASTEXITCODE."
}
& $testExecutable
if ($LASTEXITCODE -ne 0) {
    throw "Controller tests failed with exit code $LASTEXITCODE."
}
