param([string]$SourceRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath($SourceRoot)
$taskOut = Join-Path $taskRepo ('_build_check_handler/verify-wafer-map-barcode-gate-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskOut | Out-Null
$taskSource = [IO.File]::ReadAllText((Join-Path $taskRepo 'QMC.CDT-320/Sequencing/InputFeeder/InputFeederLoadToStageSequence.cs'))
$taskStart = $taskSource.IndexOf('        internal static async Task<WaferMaterial> ValidateAndApplyInputBarcodeAsync(', [StringComparison]::Ordinal)
$taskEnd = $taskSource.IndexOf('        // 재개 판정에서도 사용하는 순수 검사다.', $taskStart, [StringComparison]::Ordinal)
if ($taskStart -lt 0 -or $taskEnd -le $taskStart) { throw 'Actual barcode validation method boundaries not found.' }
$taskMethod = Join-Path $taskOut 'ActualBarcodeMapGate.cs'
$taskCode = 'using System; using System.Threading; using System.Threading.Tasks; using QMC.CDT320; using QMC.CDT320.Materials; using QMC.CDT320.Lots; using QMC.CDT320.Recipes; namespace QMC.CDT320.Sequencing { internal static partial class InputFeederLoadToStageSequence {' + "`r`n" + $taskSource.Substring($taskStart, $taskEnd - $taskStart) + '} }'
[IO.File]::WriteAllText($taskMethod, $taskCode, (New-Object Text.UTF8Encoding($true)))
$taskExe = Join-Path $taskOut 'VerifyBarcodeMapGate.exe'
& 'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe' /nologo /target:exe "/out:$taskExe" /r:System.Core.dll $taskMethod (Join-Path $PSScriptRoot 'verify_wafer_map_barcode_gate.cs')
if ($LASTEXITCODE -ne 0) { throw 'Barcode map gate compilation failed.' }
& $taskExe | Tee-Object -FilePath (Join-Path $taskOut 'results.log')
if ($LASTEXITCODE -ne 0) { throw 'Barcode map gate verification failed.' }
Write-Output "Verification artifacts: $taskOut"
