$ErrorActionPreference = "Stop"

$testRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $testRoot "..\..\..")).Path

function Read-Source([string]$relativePath) {
    return [System.IO.File]::ReadAllText((Join-Path $repoRoot $relativePath))
}

function Assert-Match([string]$name, [string]$text, [string]$pattern) {
    if (-not [System.Text.RegularExpressions.Regex]::IsMatch(
        $text,
        $pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "FAIL: $name"
    }

    Write-Host "PASS: $name"
}

function Assert-NotMatch([string]$name, [string]$text, [string]$pattern) {
    if ([System.Text.RegularExpressions.Regex]::IsMatch(
        $text,
        $pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "FAIL: $name"
    }

    Write-Host "PASS: $name"
}

$catalog = Read-Source "QMC.CDT-320\Equipment\Ajin\AjinIoCatalog.cs"
$factory = Read-Source "QMC.CDT-320\Equipment\Ajin\AjinFactory.cs"
$baseEquipmentNode = Read-Source "QMC.Common\BaseEquipmentNode.cs"
$baseComponent = Read-Source "QMC.Common\BaseComponent.cs"
$unitDataStore = Read-Source "QMC.Common\Data\Store\UnitDataStore.cs"
$unit = Read-Source "QMC.CDT-320\Equipment\Unit\OutputCassetteUnit.cs"
$interlock = Read-Source "QMC.CDT-320\Equipment\Interlocks\OutputCassetteInterlockRules.cs"
$registry = Read-Source "QMC.CDT-320\Equipment\Interlocks\Common\MotionGuardRuleRegistry.cs"
$guardService = Read-Source "QMC.CDT-320\Equipment\Interlocks\Common\MotionGuardService.cs"
$baseAxis = Read-Source "QMC.Common\Motion\BaseAxis.cs"
$sequence = Read-Source "QMC.CDT-320\Sequencing\OutputCassette\OutputCassetteSequenceBase.cs"
$page = Read-Source "QMC.CDT-320\Ui\Pages\Recipe\OutputCassetteRecipePage.cs"

Assert-Match "Lock DI uses an independent persistence key" $catalog 'DI\("NgBinCassetteLock",\s*2,\s*19,\s*false,\s*"DI_NgBinCassetteLock"\)'
Assert-Match "Lock DO uses an independent persistence key" $catalog 'DO\("NgBinCassetteLock",\s*4,\s*10,\s*"DO_NgBinCassetteLock"\)'
Assert-Match "Settings storage key defaults to component name" $baseEquipmentNode 'StorageKey\s*=\s*name;\s*SettingsStorageKey\s*=\s*name;'
Assert-Match "Factory passes input settings persistence key" $factory 'CreateSharedDigitalInput\(name,\s*m,\s*simMode,\s*settingsStorageKey\)'
Assert-Match "Factory passes output settings persistence key" $factory 'CreateSharedDigitalOutput\(name,\s*m,\s*simMode,\s*settingsStorageKey\)'
Assert-Match "Component loads setup by independent SettingsStorageKey" $baseComponent 'LoadSetup\(SettingsStorageKey, Setup\)'
Assert-Match "Component loads config by independent SettingsStorageKey" $baseComponent 'LoadConfig\(SettingsStorageKey, Config\)'
Assert-Match "Recipe load preserves legacy StorageKey" $baseComponent 'LoadRecipeRequired<TRecipe>\(recipeName, StorageKey\)'
Assert-Match "Recipe validation preserves legacy StorageKey" $baseComponent 'TryLoadRecipeRequired\(\s*recipeName,\s*StorageKey,'
Assert-Match "Missing setup file preserves constructor fallback" $unitDataStore 'if \(!result\.Success \|\| result\.UsedDefault \|\| result\.Data == null\).*?return fallback == null \? new T\(\) : fallback;'

Assert-Match "X082 active-low signal is converted to Unlock Check semantics" $unit 'IsNgBinUnlockCheck\(\).*?return !IsNgBinBW\(\);'
Assert-Match "Simulation reproduces active-low X082 polarity" $unit 'IsNgBinBW\(\).*?IsOutputCassetteHardwareBypassed\(\).*?return !\(NgBinCassetteUnlockOut != null && NgBinCassetteUnlockOut\.IsOn\);'
Assert-Match "Locked state requires X082 and X083 both ON" $unit 'IsNgBinCassetteLocked\(\).*?return IsNgBinBW\(\) && IsNgBinLock\(\);'
Assert-Match "Unlocked state requires X082 and X083 both OFF" $unit 'IsNgBinCassetteUnlocked\(\).*?return !IsNgBinBW\(\) && !IsNgBinLock\(\);'
Assert-Match "Feedback must be stable for 100 ms" $unit 'NgBinLockFeedbackStableMs\s*=\s*100'
Assert-Match "Every OutputLifterZ request checks NG lock in the common guard" $interlock 'IsMoving\(request, "OutputLifterZ".*?CheckNgBinCassetteLockReady\(out lockReason\)'
Assert-Match "Common lock check runs before special and normal motion routing" $interlock 'CheckNgBinCassetteLockReady\(out lockReason\).*?VerifyUnloadReleaseLift\(.*?VerifyBinLifterZ\(request, out reason\)'
Assert-Match "Missing OutputCassetteUnit fails closed" $interlock 'cassette == null \|\| !cassette\.CheckNgBinCassetteLockReady'
Assert-Match "Output cassette rule is registered in MotionGuard" $registry 'Register\(OutputCassetteInterlockRules\.Verify\);'
Assert-Match "Absolute, Home, and Continuous Jog enter BaseAxis MotionGuard" $baseAxis 'MoveAbsoluteAsync.*?VerifyMotionGuard\(targetPos, AxisMotionGuardKind\.Absolute\).*?HomeSearchAsync.*?VerifyMotionGuard\(Setup\.HomeOffset, AxisMotionGuardKind\.Home\).*?MoveJogContinuous.*?VerifyMotionGuard\(guardTarget, AxisMotionGuardKind\.JogContinuous\)'
Assert-Match "Jog requests use the normal axis move rule set" $guardService 'moveKind == MotionGuardMoveKind\.AxisContinuousJog.*?moveKind == MotionGuardMoveKind\.AxisStepJog.*?return MotionGuardMoveKind\.AxisMove;'
Assert-NotMatch "Unit motion paths do not duplicate NG lock interlock" $unit 'EnsureNgBinCassetteLockedAsync'
Assert-NotMatch "Sequence motion paths do not duplicate NG lock interlock" $sequence 'EnsureNgBinCassetteLockedAsync'
Assert-Match "Manual Lock button awaits feedback" $page 'async on =>.*?await _OutCassetteUnit\.NGBinLockCylinder\(\s*on,\s*NgBinManualFeedbackTimeoutMs\)'
Assert-NotMatch "Manual Lock button has no sensor precheck before output" $page 'VerifyNamedCylinderMove\("NgBinCassetteLock"'
Assert-Match "Manual feedback wait is limited to three seconds" $page 'NgBinManualFeedbackTimeoutMs\s*=\s*3000.*?NGBinLockCylinder\(\s*on,\s*NgBinManualFeedbackTimeoutMs\)'
Assert-Match "Lock command is issued before feedback wait" $unit 'NGBinLockCylinder\(bool nLock, int timeoutMs, CancellationToken ct\).*?SetNgBinCassetteUnlock\(false\);.*?SetNgBinCassetteLock\(true\);.*?WaitNgBinCassetteFeedbackStateAsync'
Assert-Match "Unlock command is issued before feedback wait" $unit 'NGBinLockCylinder\(bool nLock, int timeoutMs, CancellationToken ct\).*?SetNgBinCassetteLock\(false\);.*?SetNgBinCassetteUnlock\(true\);.*?WaitNgBinCassetteFeedbackStateAsync'
Assert-Match "Unlock check lamp uses active-low X082 semantics" $page 'IoCylinderItem\.Input\("NG BIN UNLOCK CHECK",\s*\(\) => _OutCassetteUnit\.IsNgBinUnlockCheck\(\)\)'

Write-Host "PASS: NG BIN LOCK static regression checks completed."
