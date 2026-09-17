param([string]$SourceRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath($SourceRoot)
$taskOut = Join-Path $taskRepo ('_build_check_handler/verify-wafer-map-process-save-guard-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskOut | Out-Null
$taskCompiler = 'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe'
$taskExe = Join-Path $taskOut 'VerifyProcessSaveGuard.exe'
& $taskCompiler /nologo /target:exe /platform:anycpu "/out:$taskExe" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $taskRepo 'QMC.CDT-320/Form1.WaferMapProcessing.cs') (Join-Path $PSScriptRoot 'verify_wafer_map_process_save_guard.cs')
if ($LASTEXITCODE -ne 0) { throw 'Save guard verification compilation failed.' }
& $taskExe | Tee-Object -FilePath (Join-Path $taskOut 'results.log')
if ($LASTEXITCODE -ne 0) { throw 'Save guard verification failed.' }
Write-Output "Verification artifacts: $taskOut"
