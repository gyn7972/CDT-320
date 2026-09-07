using System;
using System.Linq;
using System.Threading;
using QMC.Common.IO;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        private int _recipeSensitiveUiOperationCount;
        private IDisposable _recipeInspectionResetScope;
        private IDisposable _recipeReviewMaterialResetScope;
        private string _confirmedRecipeSwitchPrevious = string.Empty;
        private string _confirmedRecipeSwitchTarget = string.Empty;
        private bool _confirmedRecipeSwitchEmptyMaterial;
        private bool _confirmedRecipeSwitchRequiresHardwareInput;
        private bool _startupMaterialResetOperationActive;
        private string _recipeApplyFailureReason = string.Empty;
        private string _failedRecipeSwitchTarget = string.Empty;
        private string _inputCassetteClearFailureReason = string.Empty;

        public string RecipeApplyFailureReason
        {
            get { lock (_recipeOperationLock) return _recipeApplyFailureReason; }
        }

        public bool RequiresRecipeResetRecovery(string targetRecipeName)
        {
            lock (_recipeOperationLock)
                return !string.IsNullOrWhiteSpace(_recipeApplyFailureReason) &&
                    !string.IsNullOrWhiteSpace(_failedRecipeSwitchTarget) &&
                    string.Equals(_failedRecipeSwitchTarget, (targetRecipeName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public bool TryBeginRecipeSensitiveUiOperation(string operationName, out IDisposable scope, out string reason)
        {
            scope = null;
            reason = string.Empty;
            lock (_recipeOperationLock)
            {
                if (_recipeApplyOperationActive)
                {
                    reason = "Recipe 적용 및 Material 초기화 중에는 해당 동작을 시작할 수 없습니다. action=" +
                        (operationName ?? string.Empty);
                    return false;
                }
                _recipeSensitiveUiOperationCount++;
                scope = new RecipeSensitiveUiOperationScope(this);
                return true;
            }
        }

        /// <summary>
        /// 입력 카세트 교체 데이터 초기화와 저장이 끝날 때까지 운전 진입을 보호합니다.
        /// </summary>
        public bool TryBeginInputCassetteClearOperation(out IDisposable scope, out string reason)
        {
            scope = null;
            reason = string.Empty;
            if (_status == EquipmentStatus.Alarm || QMC.Common.Alarms.AlarmManager.HasActive)
            {
                reason = "Alarm 원인을 조치하고 RESET을 완료한 뒤 INPUT CST CLEAR를 실행하십시오.";
                return false;
            }

            // 카세트 데이터만 지우더라도 START/수동/직접 I/O/Review 저장과 경합하면 안 됩니다.
            // 기존 데이터 초기화 Gate를 공유하며 축 정지까지 확인하고, 모션이나 출력은 실행하지 않습니다.
            if (!TryRegisterRecipeApplyOperation(out reason, true))
            {
                reason = "INPUT CST CLEAR를 시작할 수 없습니다. " + reason;
                return false;
            }

            bool admitted = false;
            try
            {
                if (_status == EquipmentStatus.Alarm || QMC.Common.Alarms.AlarmManager.HasActive)
                {
                    reason = "INPUT CST CLEAR 준비 중 Alarm이 발생했습니다. 원인을 조치한 뒤 다시 실행하십시오.";
                    return false;
                }
                if (!MaterialStateService.CanCompleteInputCassetteExchange(out reason))
                    return false;
                if (!TryValidateInputCassetteClearPresence(out reason))
                    return false;

                scope = new RecipeApplyOperationLease(this);
                admitted = true;
                return true;
            }
            catch (Exception ex)
            {
                reason = "INPUT CST CLEAR의 장비 상태 확인에 실패했습니다. 초기화하지 않았습니다. error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputCassetteClear",
                    reason + ", exception=" + ex + " - Failed");
                return false;
            }
            finally
            {
                if (!admitted)
                    EndRecipeApplyOperation();
            }
        }

        public void ReportInputCassetteClearCompletion(bool completed, string reason)
        {
            lock (_recipeOperationLock)
            {
                if (!_recipeApplyOperationActive)
                    throw new InvalidOperationException("INPUT CST CLEAR 작업 보호 범위 안에서만 완료 상태를 변경할 수 있습니다.");

                if (completed)
                {
                    // 재시도 성공으로 해제할 수 있는 것은 이 클리어 작업이 설정한 START 차단뿐입니다.
                    // 다른 Recipe 오류가 이후에 등록되었다면 그대로 유지합니다.
                    if (!string.IsNullOrWhiteSpace(_inputCassetteClearFailureReason) &&
                        string.Equals(_recipeApplyFailureReason, _inputCassetteClearFailureReason, StringComparison.Ordinal))
                        _recipeApplyFailureReason = string.Empty;
                    _inputCassetteClearFailureReason = string.Empty;
                    return;
                }

                if (string.IsNullOrWhiteSpace(_recipeApplyFailureReason) ||
                    string.Equals(_recipeApplyFailureReason, _inputCassetteClearFailureReason, StringComparison.Ordinal))
                {
                    _inputCassetteClearFailureReason =
                        "INPUT CST CLEAR의 초기화/저장/상태 반영이 완료되지 않아 START를 차단했습니다. " +
                        "원인을 조치하고 INPUT CST CLEAR를 다시 실행하십시오. " + (reason ?? string.Empty);
                    _recipeApplyFailureReason = _inputCassetteClearFailureReason;
                }
            }
        }

        private bool TryValidateInputCassetteClearPresence(out string reason)
        {
            reason = string.Empty;
            if (_machine == null)
            {
                reason = "장비 객체를 확인할 수 없어 INPUT CST CLEAR를 차단했습니다.";
                return false;
            }
            bool requireHardwareOutput = IsRecipeSwitchHardwareInputRequired();
            if (requireHardwareOutput && !QMC.CDT320.Ajin.AjinFactory.IsRealBoardReady)
            {
                reason = "실장비 I/O 모드이지만 AJIN 보드가 준비되지 않아 INPUT CST CLEAR의 자재 감지를 확인할 수 없습니다.";
                return false;
            }

            // 실장비는 Material이 비어 있어도 원본 Ring 입력을 재확인합니다.
            // 센서와 저장 위치가 다르면 복구가 필요하므로 센서값으로 Material을 자동 삭제하지 않습니다.
            if (requireHardwareOutput)
            {
                if (!TryValidateInputCassetteClearSensorEmpty(
                        _machine.InputFeederUnit != null ? _machine.InputFeederUnit.WaferFeederRingCheckSensor : null,
                        "InputFeeder Ring", out reason) ||
                    !TryValidateInputCassetteClearSensorEmpty(
                        _machine.InputStageUnit != null ? _machine.InputStageUnit.WaferStage8RingCheckSensor : null,
                        "InputStage 8 Ring", out reason) ||
                    !TryValidateInputCassetteClearSensorEmpty(
                        _machine.InputStageUnit != null ? _machine.InputStageUnit.WaferStage12RingCheckSensor : null,
                        "InputStage 12 Ring", out reason))
                    return false;
            }

            return TryValidatePickerVacuumOutputsAndMaterial(
                "INPUT CST CLEAR", requireHardwareOutput, out reason);
        }

        private static bool TryValidateInputCassetteClearSensorEmpty(
            BaseDigitalInput input, string sensorName, out string reason)
        {
            bool detected;
            if (!TryReadRecipePresenceSensor(input, sensorName, out detected, out reason))
            {
                reason = "자재 실입력을 확인할 수 없어 INPUT CST CLEAR를 차단했습니다. " + reason;
                return false;
            }
            if (detected)
            {
                reason = sensorName + " 실입력이 ON이므로 INPUT CST CLEAR를 차단했습니다. " +
                    "실제 자재와 Material 위치 데이터를 확인하고 반납 또는 위치 복구를 완료한 뒤 다시 실행하십시오.";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private bool TryValidatePickerVacuumOutputsAndMaterial(
            string operationName, bool requireHardwareOutput, out string reason)
        {
            reason = string.Empty;
            string materialReason;
            if (!MaterialStateService.TryValidatePickerProductDataEmpty(out materialReason))
            {
                reason = operationName + "을(를) 차단했습니다. Picker 제품 데이터 없음이 확인되지 않았습니다. " +
                    materialReason;
                return false;
            }
            if (!requireHardwareOutput)
                return true;
            if (_machine == null)
            {
                reason = operationName + " 전에 Picker Vacuum Output을 확인할 장비 객체가 없습니다.";
                return false;
            }
            if (!QMC.CDT320.Ajin.AjinFactory.IsRealBoardReady)
            {
                reason = "실장비 I/O 모드이지만 AJIN 보드가 준비되지 않아 " + operationName +
                    "의 Picker Vacuum Output을 확인할 수 없습니다.";
                return false;
            }

            // Flow 입력은 Vacuum 비인가 상태에도 ON일 수 있으므로 제품 유무 근거로 사용하지 않습니다.
            // 실제 AJIN 출력과 Picker Material이 모두 비어 있을 때만 초기화/Recipe 변경을 허용합니다.
            for (int side = 0; side < 2; side++)
            {
                BaseDigitalOutput[] vacuums = side == 0
                    ? (_machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.Vacuums : null)
                    : (_machine.PickerRearUnit != null ? _machine.PickerRearUnit.Vacuums : null);
                for (int picker = 0; picker < 4; picker++)
                {
                    string outputName = (side == 0 ? "Front" : "Rear") + " P" + (picker + 1) + " VACUUM";
                    BaseDigitalOutput output = vacuums != null && picker < vacuums.Length ? vacuums[picker] : null;
                    int errorCode;
                    if (!AjinIoScanService.TryReadHardwareOutput(output, out errorCode))
                    {
                        reason = outputName + " 실제 출력을 읽을 수 없어 " + operationName +
                            "을(를) 차단했습니다. error=" + errorCode;
                        return false;
                    }
                    if (output.IsOn)
                    {
                        reason = outputName + " 실제 출력이 ON이므로 " + operationName +
                            "을(를) 차단했습니다. Vacuum을 OFF하고 Picker 제품 데이터와 실물을 확인하십시오.";
                        return false;
                    }
                }
            }
            return true;
        }

        private bool TryRegisterRecipeApplyOperation(out string reason, bool requireStoppedAxes = false)
        {
            reason = string.Empty;
            lock (_recipeOperationLock)
            {
                if (_recipeApplyOperationActive)
                {
                    reason = "다른 Recipe 저장/적용 작업이 이미 진행 중입니다.";
                    return false;
                }
                if (_recipeStartAttemptCts != null)
                {
                    reason = "START 준비 작업이 진행 중이므로 Recipe 저장/적용을 차단했습니다. START 완료 또는 STOP 후 다시 실행하십시오.";
                    return false;
                }
                if (_recipeSensitiveUiOperationCount > 0)
                {
                    reason = "직접 I/O 또는 모션 명령의 처리가 아직 끝나지 않아 Recipe 적용을 차단했습니다.";
                    return false;
                }
                // 진행 중 작업을 먼저 확인합니다. 거절할 요청이 잠깐이라도 기존 모션을 막으면 안 됩니다.
                if (IsSequenceRunning || Volatile.Read(ref _manualBusyCount) > 0 ||
                    Volatile.Read(ref _inputStageRunReviewManualActive) ||
                    Volatile.Read(ref _inputStageRunReviewActionBusyCount) > 0 ||
                    IsReadySequenceRunning ||
                    (_axisInitializeOperationGate != null && _axisInitializeOperationGate.CurrentCount == 0) ||
                    _status == EquipmentStatus.AutoRunning || _status == EquipmentStatus.ManualRunning ||
                    _status == EquipmentStatus.Initializing)
                {
                    reason = "Auto/Manual/READY/초기화/Review 작업이 진행 중이므로 Recipe 적용을 차단했습니다.";
                    return false;
                }
                if (!TryValidateRecipeBackgroundWorkStopped(out reason))
                    return false;
                if (requireStoppedAxes && !TryValidateRecipeSwitchAxesStopped(out reason))
                    return false;
                // Review 저장의 실제 종료는 Coordinator 종료와 별개로 확인합니다.
                // 두 admission 잠금 안에서는 Controller/Material 잠금이나 IO를 기다리지 않습니다.
                IDisposable reviewMaterialScope;
                if (!MaterialStateService.TryBeginInputStageReviewRecipeReset(out reviewMaterialScope, out reason))
                    return false;
                bool registered = false;
                try
                {
                    IDisposable inspectionScope;
                    if (!InputCameraPreInspectionCoordinator.TryBeginRecipeReset(out inspectionScope, out reason))
                        return false;
                    _recipeReviewMaterialResetScope = reviewMaterialScope;
                    _recipeInspectionResetScope = inspectionScope;
                    _recipeApplyOperationActive = true;
                    registered = true;
                    return true;
                }
                finally
                {
                    if (!registered)
                        reviewMaterialScope.Dispose();
                }
            }
        }

        public bool TryBeginConfirmedRecipeSwitchOperation(
            string targetRecipeName, bool confirmedEmptyMaterial, out IDisposable lease, out string reason)
        {
            lease = null;
            reason = string.Empty;
            if (!confirmedEmptyMaterial)
            {
                reason = "실제 제품 제거 확인이 완료되지 않았습니다.";
                return false;
            }
            if (!TryBeginRecipeSwitchPreparationOperation(targetRecipeName, out lease, out reason))
                return false;
            if (TryConfirmRecipeSwitchEmptyMaterial(targetRecipeName, out reason))
                return true;
            lease.Dispose();
            lease = null;
            return false;
        }

        public bool TryBeginRecipeSwitchPreparationOperation(
            string targetRecipeName, out IDisposable lease, out string reason)
        {
            lease = null;
            if (!TryRegisterRecipeApplyOperation(out reason, true))
                return false;

            bool admitted = false;
            try
            {
                string previous = (ActiveRecipeName ?? string.Empty).Trim();
                string target = (targetRecipeName ?? string.Empty).Trim();
                lock (_recipeOperationLock)
                {
                    bool sameRecipeRecovery = !string.IsNullOrWhiteSpace(_recipeApplyFailureReason) &&
                        string.Equals(_failedRecipeSwitchTarget, target, StringComparison.OrdinalIgnoreCase);
                    if (string.IsNullOrWhiteSpace(previous) || string.IsNullOrWhiteSpace(target) ||
                        (string.Equals(previous, target, StringComparison.OrdinalIgnoreCase) && !sameRecipeRecovery))
                    {
                        reason = "전체 Material 초기화는 다른 Recipe 전환 또는 실패한 해당 전환의 복구에만 사용할 수 있습니다.";
                        return false;
                    }
                    _confirmedRecipeSwitchPrevious = previous;
                    _confirmedRecipeSwitchTarget = target;
                    _confirmedRecipeSwitchEmptyMaterial = false;
                    // 전환 후 대상 Recipe의 DryRun 값이 바뀌어도 최초 실장비 확인 의무는 유지합니다.
                    _confirmedRecipeSwitchRequiresHardwareInput = IsRecipeSwitchHardwareInputRequired();
                }

                if (!TryRevalidateRecipeSwitchCore(target, false, true, out reason))
                    return false;

                lease = new RecipeApplyOperationLease(this);
                admitted = true;
                return true;
            }
            catch (Exception ex)
            {
                reason = "Recipe 전환 시작 조건 확인 중 오류가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
                if (!admitted)
                    EndRecipeApplyOperation();
            }
        }

        public bool TryRevalidateConfirmedRecipeSwitch(string targetRecipeName, bool targetAlreadyApplied, out string reason)
        {
            lock (_recipeOperationLock)
            {
                if (!_confirmedRecipeSwitchEmptyMaterial)
                {
                    reason = "실제 제품 제거 확인이 확정되지 않아 Recipe 변경과 Material 초기화를 수행할 수 없습니다.";
                    return false;
                }
            }
            return TryRevalidateRecipeSwitchCore(targetRecipeName, targetAlreadyApplied, false, out reason);
        }

        public bool TryConfirmRecipeSwitchEmptyMaterial(string targetRecipeName, out string reason)
        {
            if (!TryRevalidateRecipeSwitchCore(targetRecipeName, false, true, out reason))
                return false;
            lock (_recipeOperationLock)
            {
                if (!_recipeApplyOperationActive || _recipeInspectionResetScope == null || _recipeReviewMaterialResetScope == null ||
                    !string.Equals(_confirmedRecipeSwitchTarget, (targetRecipeName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(_confirmedRecipeSwitchPrevious, (ActiveRecipeName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    reason = "실제 제품 제거 확인 중 Recipe 전환 소유권이 변경되었습니다.";
                    return false;
                }
                _confirmedRecipeSwitchEmptyMaterial = true;
                return true;
            }
        }

        private bool TryRevalidateRecipeSwitchCore(
            string targetRecipeName, bool targetAlreadyApplied, bool allowUnconfirmedMaterial, out string reason)
        {
            reason = string.Empty;
            bool allowResidualMaterial;
            bool requireHardwareInput;
            lock (_recipeOperationLock)
            {
                string expectedActive = targetAlreadyApplied ? _confirmedRecipeSwitchTarget : _confirmedRecipeSwitchPrevious;
                if (!_recipeApplyOperationActive || _recipeInspectionResetScope == null || _recipeReviewMaterialResetScope == null ||
                    string.IsNullOrWhiteSpace(_confirmedRecipeSwitchTarget) ||
                    !string.Equals(_confirmedRecipeSwitchTarget, (targetRecipeName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(expectedActive, (ActiveRecipeName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Recipe 전환 소유권 또는 적용 전후 Recipe 이름이 변경되었습니다.";
                    return false;
                }
                allowResidualMaterial = _confirmedRecipeSwitchEmptyMaterial || allowUnconfirmedMaterial;
                requireHardwareInput = _confirmedRecipeSwitchRequiresHardwareInput || IsRecipeSwitchHardwareInputRequired();
            }

            try
            {
                if (!TryValidateRecipeSwitchStopped(out reason))
                    return false;

                bool physicalPresent;
                string physicalDetail;
                if (!TryCollectRecipePhysicalProductEvidence(out physicalPresent, out physicalDetail, out reason, requireHardwareInput))
                    return false;
                if (physicalPresent)
                {
                    reason = "실제 제품 감지 신호가 있어 Recipe 전환을 차단했습니다. physical=" + physicalDetail;
                    return false;
                }
                if (!TryValidatePickerVacuumOutputsAndMaterial(
                        "Recipe 전환", requireHardwareInput, out reason))
                    return false;

                string materialRecipe;
                string materialDetail;
                if (!allowResidualMaterial && HasInMachineMaterial(out materialRecipe, out materialDetail))
                {
                    reason = "잔여 Material의 실제 제거 확인이 없어 Recipe 전환을 차단했습니다. material=" + materialDetail;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                reason = "Recipe 전환 조건 재확인 중 오류가 발생했습니다. error=" + ex.Message;
                return false;
            }
        }

        private bool TryValidateRecipeSwitchStopped(out string reason)
        {
            reason = string.Empty;
            if (_status == EquipmentStatus.Alarm || QMC.Common.Alarms.AlarmManager.HasActive)
            {
                reason = "Alarm 상태에서는 Recipe 전환과 Material 초기화를 수행할 수 없습니다.";
                return false;
            }
            if (HasActiveEquipmentOperation || _status == EquipmentStatus.AutoRunning ||
                _status == EquipmentStatus.ManualRunning || _status == EquipmentStatus.Initializing)
            {
                reason = "Auto/Manual/READY/초기화/Review 작업을 완전히 종료한 뒤 Recipe를 변경하십시오.";
                return false;
            }
            lock (_recipeOperationLock)
            {
                if (_recipeSensitiveUiOperationCount > 0)
                {
                    reason = "직접 I/O 또는 모션 명령 처리가 종료되지 않았습니다.";
                    return false;
                }
            }
            if (!TryValidateRecipeBackgroundWorkStopped(out reason))
                return false;
            return TryValidateRecipeSwitchAxesStopped(out reason);
        }

        private bool TryValidateRecipeBackgroundWorkStopped(out string reason)
        {
            reason = string.Empty;
            if (PendingSequenceTaskRegistry.TryGetPending(out reason))
                return false;
            if (_seqContext != null && _seqContext.OutputPostPlaceInspections != null &&
                !_seqContext.OutputPostPlaceInspections.IsSafelyIdleForDrain(out reason))
                return false;
            if (_inputStageRunReviewContext != null && _inputStageRunReviewContext.OutputPostPlaceInspections != null &&
                !_inputStageRunReviewContext.OutputPostPlaceInspections.IsSafelyIdleForDrain(out reason))
                return false;
            return !VisionIndependentRetreatCoordinator.TryGetRecipeChangePendingWork(out reason);
        }

        private bool TryValidateRecipeSwitchAxesStopped(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_machine == null)
                {
                    reason = "Recipe 전환 전에 확인할 장비 객체가 없습니다.";
                    return false;
                }
                var axes = EnumerateAxes().Where(axis => axis != null).Distinct().ToList();
                if (axes.Count == 0)
                {
                    reason = "Recipe 전환 전에 확인할 축 상태가 없습니다.";
                    return false;
                }
                foreach (var axis in axes)
                {
                    axis.UpdateStatus();
                    if (axis.IsMoving || axis.IsAlarm)
                    {
                        reason = "축의 완전 정지가 확인되지 않았습니다. axis=" + axis.Name +
                            ", moving=" + axis.IsMoving + ", alarm=" + axis.IsAlarm;
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                reason = "Recipe 전환 전 축 상태를 확인하지 못했습니다. error=" + ex.Message;
                return false;
            }
        }

        private bool IsRecipeSwitchHardwareInputRequired()
        {
            AppSettings settings = AppSettingsStore.Current;
            return !(DryRun || GlobalDryRun || (settings != null &&
                (!settings.UseAjin || settings.SimulationMode || settings.DryRunMode || settings.BypassHardware)));
        }

        public bool TryResetCompletedRecipeRuntime(string targetRecipeName, out string reason)
        {
            if (!TryRevalidateConfirmedRecipeSwitch(targetRecipeName, true, out reason))
                return false;
            return TryClearCompletedRecipeRuntime("RecipeChange:" + targetRecipeName, targetRecipeName, out reason);
        }

        internal bool TryBeginStartupMaterialResetOperation(out IDisposable lease, out string reason)
        {
            lease = null;
            if (!TryRegisterRecipeApplyOperation(out reason))
                return false;
            bool admitted = false;
            try
            {
                // 기동 'Material 사용 안 함'의 기존 센서/INIT 정책은 바꾸지 않고 미종료 작업만 거절합니다.
                if (!TryValidateStartupMaterialResetStopped(out reason))
                    return false;
                lock (_recipeOperationLock)
                {
                    _startupMaterialResetOperationActive = true;
                }
                lease = new RecipeApplyOperationLease(this);
                admitted = true;
                return true;
            }
            catch (Exception ex)
            {
                reason = "기동 Material 초기화 시작 조건 확인 중 오류가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
                if (!admitted)
                    EndRecipeApplyOperation();
            }
        }

        internal bool TryResetCompletedRecipeRuntimeForStartup(string reason, out string error)
        {
            lock (_recipeOperationLock)
            {
                if (!_recipeApplyOperationActive || !_startupMaterialResetOperationActive ||
                    _recipeInspectionResetScope == null || _recipeReviewMaterialResetScope == null)
                {
                    error = "기동 Material 초기화 작업의 소유권이 확인되지 않았습니다.";
                    return false;
                }
            }
            if (!TryValidateStartupMaterialResetStopped(out error))
                return false;
            return TryClearCompletedRecipeRuntime(reason ?? "StartupDiscardMaterial", null, out error);
        }

        private bool TryValidateStartupMaterialResetStopped(out string reason)
        {
            reason = string.Empty;
            if (HasActiveEquipmentOperation || _status == EquipmentStatus.AutoRunning ||
                _status == EquipmentStatus.ManualRunning || _status == EquipmentStatus.Initializing)
            {
                reason = "진행 중인 작업이 있어 기동 Material 초기화를 수행할 수 없습니다.";
                return false;
            }
            return TryValidateRecipeBackgroundWorkStopped(out reason);
        }

        private bool TryClearCompletedRecipeRuntime(string resetReason, string prepareRecipeName, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (!InputCameraPreInspectionCoordinator.TryClearCompletedForRecipeReset(out reason))
                    return false;
                SequenceResumeStore.ClearAll();
                SequenceFailureStore.Clear();
                InputDieVisionBatchCoordinateStore.Clear(PickerSequenceSide.Front);
                InputDieVisionBatchCoordinateStore.Clear(PickerSequenceSide.Rear);
                InputDieVisionWaitRetryStore.ClearAll(resetReason);
                QMC.CDT320.VisionComm.VisionDieAddressStore.ClearAll();
                InputStageHybridResultSession.Clear();
                MaterialStateService.ClearAllPickerFlowRecoveries(resetReason);
                if (_machine.InputStageUnit != null)
                {
                    _machine.InputStageUnit.ClearCurrentWaferMaterial();
                    _machine.InputStageUnit.ClearCurrentWaferMap();
                }
                if (_machine.InputCassetteUnit != null)
                    _machine.InputCassetteUnit.ResetMaterialProjection();
                if (_machine.OutputCassetteUnit != null)
                    _machine.OutputCassetteUnit.ResetMaterialProjection();
                if (_machine.InputFeederUnit != null)
                    _machine.InputFeederUnit.ClearCurrentWaferMaterial();
                if (_machine.OutputFeederUnit != null)
                    _machine.OutputFeederUnit.ClearFeederMaterialState();
                // 표시용 적재 수만 비우며 레시피의 Plate 크기와 식별자는 유지합니다.
                PlateRegistry.Reset();
                ClearInputDieMap(resetReason);
                if (_inputDieMap != null || _inputPickupSequence.Count != 0 || LotStorage.ActiveInputDieMap != null)
                {
                    reason = "이전 Recipe의 입력 맵 또는 픽업 순서가 남아 있습니다.";
                    return false;
                }
                InvalidateRecipeRunReadiness();
                if (!string.IsNullOrWhiteSpace(prepareRecipeName))
                    RequestOutputFullPreparation("RecipeChangeReset", prepareRecipeName);
                return true;
            }
            catch (Exception ex)
            {
                reason = "종료된 Recipe 공정 상태 초기화 중 오류가 발생했습니다. error=" + ex.Message;
                return false;
            }
        }

        public void BlockRecipeStartAfterFailedApply(string reason)
        {
            lock (_recipeOperationLock)
            {
                _recipeApplyFailureReason = "Recipe 적용 및 초기화가 완료되지 않아 START를 차단했습니다. " +
                    (string.IsNullOrWhiteSpace(reason) ? "Recipe를 다시 검증하고 적용하십시오." : reason);
                if (!string.IsNullOrWhiteSpace(_confirmedRecipeSwitchTarget))
                    _failedRecipeSwitchTarget = _confirmedRecipeSwitchTarget;
            }
            InvalidateRecipeRunReadiness();
        }

        internal void BlockRecipeStartAfterFailedMaterialReset(string targetRecipeName, string reason)
        {
            BlockRecipeStartAfterFailedApply(reason);
            string target = (targetRecipeName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target))
                return;
            lock (_recipeOperationLock)
                _failedRecipeSwitchTarget = target;
        }

        public void ClearRecipeStartBlockAfterSuccessfulApply()
        {
            lock (_recipeOperationLock)
            {
                if (!_recipeApplyOperationActive)
                    throw new InvalidOperationException("Recipe 적용 작업 안에서만 저장 완료 차단을 해제할 수 있습니다.");
                _recipeApplyFailureReason = string.Empty;
                _failedRecipeSwitchTarget = string.Empty;
            }
        }

        private sealed class RecipeSensitiveUiOperationScope : IDisposable
        {
            private MachineController _owner;

            public RecipeSensitiveUiOperationScope(MachineController owner) { _owner = owner; }

            public void Dispose()
            {
                MachineController owner = Interlocked.Exchange(ref _owner, null);
                if (owner == null) return;
                lock (owner._recipeOperationLock)
                    owner._recipeSensitiveUiOperationCount--;
            }
        }
    }
}
