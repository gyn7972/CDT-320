param([string]$AssemblyDirectory = '')
$ErrorActionPreference = 'Stop'
$assemblyDir = if ([string]::IsNullOrWhiteSpace($AssemblyDirectory)) { Join-Path $PSScriptRoot 'final-rad-approval' } else { $AssemblyDirectory }
[void][Reflection.Assembly]::LoadFrom((Join-Path $assemblyDir 'QMC.Common.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $assemblyDir 'QMC.CDT-320.exe'))
$root = Join-Path $PSScriptRoot ('approval-role-bypass-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $root -Force)
[QMC.Common.Data.Store.DataPaths]::Root = $root
$p = New-Object QMC.CDT320.Recipes.RecipeProject
$p.FileName = 'RoleBypassHarness'
[QMC.CDT320.Recipes.RecipeProjectConsistencyService]::EnsureStructure($p) | Out-Null
$p.Die.WidthMm = 8
$p.Die.HeightMm = 6
$p.Die.ThicknessMm = 0.25
$p.InputFrame.PitchX = 8.12
$p.InputFrame.PitchY = 6.12
$p.InputFrame.OuterDiameterMm = 300
$p.OutputFrame.PitchX = 8.12
$p.OutputFrame.PitchY = 6.12
$p.OutputFrame.OuterDiameterMm = 300
$b = [QMC.CDT320.Recipes.RecipeMapBuildService]::ImportBaseAndBuildAll($p, 'D:\CDT-320\Config\WaferMap\RAD1.txt')
if (-not $b.Success) { throw $b.Message }
$input = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($b.InputMapPath)
$good = [QMC.CDT320.DieMaps.DieMapGenerator]::LoadJson($b.GoodMapPath)
$a = [QMC.CDT320.Recipes.RecipeMapBuildService]::SaveRoleTargetMask($p, [QMC.CDT320.Recipes.RecipeMapKind]::Input, $input)
if (-not $a.Success) { throw $a.Message }
$a = [QMC.CDT320.Recipes.RecipeMapBuildService]::SaveRoleTargetMask($p, [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin, $good)
if (-not $a.Success) { throw $a.Message }
$inputPath = [QMC.CDT320.Recipes.RecipeMapPaths]::ResolveConfigured($p, [QMC.CDT320.Recipes.RecipeMapKind]::Input)
$goodPathBefore = [QMC.CDT320.Recipes.RecipeMapPaths]::ResolveConfigured($p, [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin)
$hashesEqual = $p.InputMapApprovalHash -eq $p.GoodBinMapApprovalHash
$p.GoodBinDieMapFileName = ''
$p.OutputDieMapFileName = ''
$source = ''
$reason = ''
$resolved = [QMC.CDT320.Recipes.RecipeDieMapResolver]::LoadCompatibleMap($p, [QMC.CDT320.Recipes.RecipeMapKind]::GoodBin, [ref]$source, [ref]$reason)
Write-Output ('hashesEqual=' + $hashesEqual)
Write-Output ('inputPath=' + $inputPath)
Write-Output ('goodPathBefore=' + $goodPathBefore)
Write-Output ('resolvedAfterGoodPathCleared=' + ($null -ne $resolved))
Write-Output ('resolvedSource=' + $source)
Write-Output ('reason=' + $reason)
