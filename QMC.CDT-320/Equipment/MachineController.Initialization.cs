using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.Common.Diagnostics.TactTime;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Alarms;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Initialization;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        // Common equipment button actions.

        private bool TryEnterAxisInitializeOperation(string source)
        {
            if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
            {
                LastActionFailureMessage =
                    "Alarm 상태에서는 축 초기화를 새로 시작할 수 없습니다. 알람을 조치하고 RESET 후 다시 실행하세요.";
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    source,
                    LastActionFailureMessage + " - Blocked");
                return false;
            }

            bool initializeOperationEntered;
            // Recipe 적용이 시작된 뒤 축 초기화가 끼어들지 않도록 등록 경계를 공유한다.
            lock (_recipeOperationLock)
            {
                if (_recipeApplyOperationActive)
                {
                    LastActionFailureMessage =
                        "Recipe 저장/적용 중에는 축 초기화를 시작할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", source,
                        LastActionFailureMessage + " - Blocked");
                    return false;
                }

                initializeOperationEntered = _axisInitializeOperationGate.Wait(0);
            }

            if (initializeOperationEntered)
            {
                var cts = new CancellationTokenSource();
                lock (_axisInitializeCancellationLock)
                    _axisInitializeCts = cts;

                // Gate 획득과 CTS 등록 사이에 Alarm이 발생한 경우에도 초기화 명령을 시작하지 않습니다.
                if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
                {
                    TryCancelAlarmOperationToken(cts, "AxisInitialize", "ENTER-RACE");
                    lock (_axisInitializeCancellationLock)
                    {
                        if (object.ReferenceEquals(_axisInitializeCts, cts))
                            _axisInitializeCts = null;
                    }
                    cts.Dispose();
                    _axisInitializeOperationGate.Release();
                    LastActionFailureMessage =
                        "축 초기화 진입 중 Alarm이 발생하여 초기화를 시작하지 않았습니다.";
                    QMC.Common.Log.Write(
                        "Main",
                        "SYSTEM",
                        source,
                        LastActionFailureMessage + " - Blocked");
                    return false;
                }

                return true;
            }

            const string blockedMessage =
                "다른 축 초기화 작업이 이미 실행 중입니다. 현재 작업이 끝난 뒤 다시 시도하세요.";
            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                source,
                blockedMessage + " - Blocked");
            return false;
        }

        private void ExitAxisInitializeOperation(string source)
        {
            try
            {
                CancellationTokenSource cts;
                lock (_axisInitializeCancellationLock)
                {
                    cts = _axisInitializeCts;
                    _axisInitializeCts = null;
                }

                if (cts != null)
                    cts.Dispose();

                _axisInitializeOperationGate.Release();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    source,
                    "Axis initialize operation gate release failed. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private CancellationToken GetAxisInitializeOperationToken()
        {
            lock (_axisInitializeCancellationLock)
            {
                CancellationTokenSource cts = _axisInitializeCts;
                return cts != null ? cts.Token : CancellationToken.None;
            }
        }

        private CancellationTokenSource GetAxisInitializeOperationCancellationSource()
        {
            lock (_axisInitializeCancellationLock)
                return _axisInitializeCts;
        }

        private bool IsAxisInitializeCancellationRequested(CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ||
                   _status == EquipmentStatus.Alarm ||
                   AlarmManager.HasActive;
        }

        private int FailCancelledAxisInitialize(string source)
        {
            LastActionFailureMessage =
                "Alarm/정지 요청으로 축 초기화가 취소되었습니다. 신규 Servo ON/HOME 명령을 차단했습니다.";
            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                source,
                LastActionFailureMessage + " - Cancelled");
            SetMachineInitialized(false, source + "Cancelled", false);
            return -4;
        }


        // 홈잡을때 사용함.!
        public async Task<int> InitializeAxisAsync(string axisName)
        {
            bool initializeOperationEntered = false;
            try
            {
                initializeOperationEntered =
                    TryEnterAxisInitializeOperation("InitializeAxis");
                if (!initializeOperationEntered)
                    return -1;

                LastActionFailureMessage = "";
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 축 초기화를 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                        "Axis initialize failed: sequence is running. axis=" + axisName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var axis = FindAxisByName(axisName);
                if (axis == null)
                {
                    LastActionFailureMessage = "초기화할 축을 찾을 수 없습니다. axis=" + axisName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                        "Axis initialize failed: axis not found. axis=" + axisName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-NOTFOUND", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                AxisInitializePlan plan = AxisInitializePlanStore.LoadOrCreateDefault(EnumerateAxes());
                string planStepReason;
                AxisInitializeStep planStep = ResolveSingleEnabledInitializeStepForAxis(
                    plan,
                    axis.Name,
                    out planStepReason);
                if (planStep == null)
                {
                    LastActionFailureMessage = planStepReason;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                        LastActionFailureMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-AXIS-PLAN-" + axis.Name,
                        "MachineController",
                        LastActionFailureMessage);
                    return -1;
                }

                if (_axisInitializeInterlocks == null)
                {
                    LastActionFailureMessage = "개별축 HOME 차단: 초기화 계획 인터락 서비스를 찾을 수 없습니다. axis=" +
                        axis.Name + ", step=" + planStep.StepNo + ", group=" + planStep.GroupName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                        LastActionFailureMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-AXIS-INTERLOCK-SERVICE-" + axis.Name,
                        "MachineController",
                        LastActionFailureMessage);
                    return -1;
                }

                AxisInitializeResult verifyResult =
                    _axisInitializeExecutor.VerifySingleAxisStep(planStep, axis);
                if (verifyResult == null || !verifyResult.Succeeded)
                {
                    LastActionFailureMessage = verifyResult != null
                        ? verifyResult.ErrorMessage
                        : "개별축 HOME 검증 결과가 없습니다. axis=" + axis.Name;
                    return verifyResult != null ? verifyResult.ResultCode : -1;
                }

                SetMachineInitialized(false, "InitializeAxisStart:" + axis.Name, false);
                SetStatus(EquipmentStatus.Initializing);
                CancellationToken initializeToken = GetAxisInitializeOperationToken();
                if (IsAxisInitializeCancellationRequested(initializeToken))
                    return FailCancelledAxisInitialize("InitializeAxis");

                AxisInitializeResult executeResult =
                    await _axisInitializeExecutor.ExecuteSingleAxisHomeAsync(
                        planStep,
                        axis,
                        initializeToken).ConfigureAwait(false);
                if (executeResult == null || !executeResult.Succeeded)
                {
                    if (executeResult != null &&
                        !string.IsNullOrWhiteSpace(executeResult.ErrorMessage))
                    {
                        LastActionFailureMessage = executeResult.ErrorMessage;
                    }
                    else if (string.IsNullOrWhiteSpace(LastActionFailureMessage))
                    {
                        LastActionFailureMessage =
                            "개별축 HOME 실행 결과가 없습니다. axis=" + axis.Name;
                    }

                    SetMachineInitialized(false, "InitializeAxisFailed:" + axis.Name, true);
                    SetStatus(EquipmentStatus.Alarm);
                    return executeResult != null ? executeResult.ResultCode : -1;
                }

                if (IsAxisInitializeCancellationRequested(initializeToken))
                    return FailCancelledAxisInitialize("InitializeAxisComplete");

                TryRecoverMachineInitializedFromAxisState("InitializeAxis:" + axis.Name);
                SaveMachineRuntimeState("InitializeAxis:" + axis.Name);
                SetStatus(EquipmentStatus.Idle);
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "축 초기화 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                    "Axis initialize failed. axis=" + axisName + ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-EX", "MachineController", LastActionFailureMessage);
                SetMachineInitialized(false, "InitializeAxisException", true);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
                if (initializeOperationEntered)
                    ExitAxisInitializeOperation("InitializeAxis");
            }
        }

        // 여기 사용함
        public async Task<int> InitializeAllAxesAsync(bool markMachineReady)
        {
            bool initializeOperationEntered = false;
            try
            {
                initializeOperationEntered =
                    TryEnterAxisInitializeOperation("InitializeAllAxes");
                if (!initializeOperationEntered)
                    return -1;

                return await InitializeAllAxesCoreAsync(
                    markMachineReady,
                    GetAxisInitializeOperationToken()).ConfigureAwait(false);
            }
            finally
            {
                if (initializeOperationEntered)
                    ExitAxisInitializeOperation("InitializeAllAxes");
            }
        }

        private async Task<int> InitializeAllAxesCoreAsync(
            bool markMachineReady,
            CancellationToken cancellationToken)
        {
            try
            {
                LastActionFailureMessage = "";
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 전체 축 초기화를 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        "All axes initialize failed: sequence is running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-ALL-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var allAxes = new List<BaseAxis>(EnumerateAxes());
                if (allAxes.Count == 0)
                {
                    LastActionFailureMessage = "초기화할 축 정보가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        "All axes initialize failed: axis list is empty. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-ALL-EMPTY", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var plan = AxisInitializePlanStore.LoadOrCreateDefault(allAxes);
                var steps = ResolveEnabledInitializeSteps(plan);
                if (steps.Count == 0)
                {
                    LastActionFailureMessage = "내장 초기화 Plan에 실행 가능한 Step이 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        "All axes initialize failed: built-in initialize plan has no enabled step. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-PLAN-EMPTY", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                string feederPreflightReason;
                if (!VerifyFeederInitializePreflight(out feederPreflightReason))
                {
                    LastActionFailureMessage = "전체 초기화 시작 전 Feeder 자재 확인 실패: " + feederPreflightReason;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        LastActionFailureMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-FEEDER-MATERIAL",
                        "MachineController",
                        LastActionFailureMessage);
                    return -1;
                }

                SetMachineInitialized(false, "InitializeAllAxesStart", false);
                SetStatus(EquipmentStatus.Initializing);
                Log("[INIT] Built-in axis initialize plan start.");

                if (IsAxisInitializeCancellationRequested(cancellationToken))
                    return FailCancelledAxisInitialize("InitializeAllAxesStart");

                int initResult = await ExecuteInitializeStepsAsync(
                    steps,
                    true,
                    cancellationToken).ConfigureAwait(false);
                if (initResult != 0)
                {
                    SetMachineInitialized(false, "InitializeAllAxesFailed", true);
                    SetStatus(EquipmentStatus.Alarm);
                    return initResult;
                }

                if (IsAxisInitializeCancellationRequested(cancellationToken))
                    return FailCancelledAxisInitialize("InitializeAllAxesComplete");

                string completionReason;
                if (!VerifyInitializeCompletionSafety(out completionReason))
                {
                    LastActionFailureMessage = "전체 초기화 완료 안전 확인 실패: " + completionReason;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        LastActionFailureMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-FINAL-SAFETY",
                        "MachineController",
                        LastActionFailureMessage);
                    SetMachineInitialized(false, "InitializeAllAxesFinalSafetyFailed", true);
                    SetStatus(EquipmentStatus.Alarm);
                    return -1;
                }

                if (markMachineReady)
                {
                    SetMachineInitialized(true, "InitializeAllAxesComplete", true);
                    SetStatus(EquipmentStatus.Ready);
                }
                else
                {
                    SaveMachineRuntimeState("InitializeAllAxes");
                }

                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "전체 축 초기화 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                    "All axes initialize failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-ALL-EX", "MachineController", LastActionFailureMessage);
                SetMachineInitialized(false, "InitializeAllAxesException", true);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        // 전체 초기화의 첫 모션 전에 양 Feeder의 데이터와 기존 Ring/Override 센서를 fail-closed로 확인한다.
        private bool VerifyFeederInitializePreflight(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_machine == null || _machine.InputFeederUnit == null || _machine.OutputFeederUnit == null)
                {
                    reason = "Input/Output FeederUnit 정보를 확인할 수 없습니다.";
                    return false;
                }

                string feederReason;
                if (!InputFeederInterlockRules.VerifyInputFeederMaterialClear(
                    _machine.InputFeederUnit,
                    "InitializeInputFeederPreflight",
                    out feederReason))
                {
                    reason = feederReason;
                    return false;
                }

                if (!OutputFeederInterlockRules.VerifyOutputFeederMaterialClear(
                    _machine.OutputFeederUnit,
                    "InitializeOutputFeederPreflight",
                    out feederReason))
                {
                    reason = feederReason;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Feeder 자재/센서 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }


        //홈잡을때 사용.
        public AxisInitializePlan GetAxisInitializePlan()
        {
            try
            {
                return AxisInitializePlanStore.LoadOrCreateDefault(EnumerateAxes());
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "GetAxisInitializePlan",
                    "Get initialize plan failed: " + ex.Message + " - Failed");
                return AxisInitializePlanStore.CreateDefault(EnumerateAxes());
            }
            finally
            {
            }
        }

        /// <summary>
        /// 초기화 Monitor가 현재 축·인터락 상태를 읽을 때 사용합니다.
        /// 실제 HOME 실행과 장비 상태 변경은 수행하지 않습니다.
        /// </summary>
        internal AxisInitializeRouteResult GetAxisInitializeRoutePreview()
        {
            try
            {
                AxisInitializePlan plan = GetAxisInitializePlan();
                if (plan == null || plan.Steps == null)
                    return null;

                return _axisInitializeSequence.PreviewRoute(plan.Steps);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "GetAxisInitializeRoutePreview",
                    "Get initialize route preview failed. error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public IList<AxisInitializeStepProgress> GetAxisInitializeStepStatusSnapshot()
        {
            try
            {
                EnsureAxisInitializeStepStatesLoadedFromRuntimeState();

                AxisInitializePlan plan = GetAxisInitializePlan();
                if (plan == null || plan.Steps == null)
                    return new List<AxisInitializeStepProgress>();

                return plan.Steps
                    .Where(x => x != null)
                    .Select(GetValidatedAxisInitializeStepProgress)
                    .ToList();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "GetAxisInitializeStepStatusSnapshot",
                    "Get initialize step status snapshot failed: " + ex.Message + " - Failed");
                return new List<AxisInitializeStepProgress>();
            }
            finally
            {
            }
        }

        public async Task<int> InitializePlanStepAsync(int stepNo)
        {
            bool initializeOperationEntered = false;
            try
            {
                initializeOperationEntered =
                    TryEnterAxisInitializeOperation("InitializePlanStep");
                if (!initializeOperationEntered)
                    return -1;

                LastActionFailureMessage = "";
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 초기화 Step을 수행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var plan = GetAxisInitializePlan();
                var steps = plan != null && plan.Steps != null
                    ? plan.Steps.Where(x => x != null && x.Enabled && x.StepNo == stepNo).ToList()
                    : new List<AxisInitializeStep>();

                if (steps.Count == 0)
                {
                    LastActionFailureMessage = "실행할 초기화 Step을 찾을 수 없습니다. step=" + stepNo;
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-NOTFOUND", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                SetMachineInitialized(false, "InitializePlanStepStart:" + stepNo, false);
                SetStatus(EquipmentStatus.Initializing);

                // Monitor의 개별 Step도 전체 초기화와 같은 switch/Unit 객체 바인딩을 사용합니다.
                // Executor를 직접 호출하면 AxisNames 문자열 경로로 돌아가므로 Sequence를 단일 진입점으로 둡니다.
                CancellationToken initializeToken = GetAxisInitializeOperationToken();
                if (IsAxisInitializeCancellationRequested(initializeToken))
                    return FailCancelledAxisInitialize("InitializePlanStepStart");

                int result = await ExecuteInitializeStepsAsync(
                    steps,
                    false,
                    initializeToken).ConfigureAwait(false);

                if (result == 0 && IsAxisInitializeCancellationRequested(initializeToken))
                    return FailCancelledAxisInitialize("InitializePlanStepComplete");

                SaveMachineRuntimeState("InitializePlanStep:" + stepNo);
                SetStatus(result == 0 ? EquipmentStatus.Idle : EquipmentStatus.Alarm);
                return result;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Step 실행 실패: " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-DIALOG-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
                if (initializeOperationEntered)
                    ExitAxisInitializeOperation("InitializePlanStep");
            }
        }

        public async Task<int> InitializeAllAxesForMonitorAsync()
        {
            return await InitializeAllAxesAsync(true).ConfigureAwait(false);
        }

        private static string BuildAxisMotionFailureMessage(BaseAxis axis, string action, int result)
        {
            if (axis == null)
                return (action ?? "Motion") + " 실패. result=" + result;

            if (!string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage))
                return axis.LastMotionFailureMessage;

            string reason;
            if (axis.IsAlarm)
                reason = "Axis alarm is ON. AlarmCode=0x" + axis.AlarmCode.ToString("X4");
            else if (!axis.IsServoOn)
                reason = "Servo is OFF.";
            else if (result == -11)
                reason = "Motion guard/interlock blocked.";
            else if (result == -2)
                reason = "Axis is not ready.";
            else
                reason = "Motion failed.";

            string unit = axis.Setup != null ? axis.Setup.Unit : string.Empty;
            return axis.Name + " " + (action ?? "Motion") +
                   ". result=" + result +
                   ", reason=" + reason +
                   ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                   ", pos=" + axis.ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
        }

        private async Task<int> ExecuteInitializeStepsAsync(
            IList<AxisInitializeStep> steps,
            bool requireCompleteDefaultSequence,
            CancellationToken cancellationToken)
        {
            try
            {
                AxisInitializeResult result = await _axisInitializeSequence.ExecuteAsync(
                    steps,
                    requireCompleteDefaultSequence,
                    cancellationToken).ConfigureAwait(false);
                if (result == null)
                {
                    LastActionFailureMessage = "초기화 Executor가 결과를 반환하지 않았습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        LastActionFailureMessage + " - Failed");
                    return -1;
                }

                if (!result.Succeeded)
                    LastActionFailureMessage = result.ErrorMessage;

                return result.ResultCode;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Executor 실행 위임 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-STEP-EX",
                    "MachineController",
                    LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private void OnAxisInitializeExecutorStepProgressChanged(AxisInitializeStepProgress progress)
        {
            PublishAxisInitializeStepProgress(progress);
        }

        private void PublishAxisInitializeStepProgress(AxisInitializeStepProgress progress)
        {
            if (progress == null)
                return;

            SetAxisInitializeStepProgress(progress);

            var handler = AxisInitializeStepProgressChanged;
            if (handler == null)
                return;

            try
            {
                handler(progress);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private AxisInitializeStepProgress GetValidatedAxisInitializeStepProgress(AxisInitializeStep step)
        {
            AxisInitializeStepProgress progress;
            _axisInitializeProgressStore.TryGet(step, out progress);

            if (progress == null)
            {
                if (_isMachineInitialized)
                {
                    string initializedReason;
                    if (CheckAxisInitializeStepStillValid(step, out initializedReason))
                    {
                        progress = AxisInitializeStepProgress.Create(step, AxisInitializeStepStatus.Complete, "");
                        SetAxisInitializeStepProgress(progress);
                        return progress;
                    }
                }

                progress = AxisInitializeStepProgress.Create(
                    step,
                    step != null && step.Enabled ? AxisInitializeStepStatus.Waiting : AxisInitializeStepStatus.Disabled,
                    "");
            }
            else
            {
                progress = new AxisInitializeStepProgress
                {
                    StepNo = progress.StepNo,
                    GroupName = progress.GroupName,
                    Status = progress.Status,
                    Message = progress.Message
                };
            }

            if (step == null || !step.Enabled)
                return progress;

            if (ShouldResolveInitializedStepStatus(progress.Status, _isMachineInitialized))
            {
                string reason;
                if (CheckAxisInitializeStepStillValid(step, out reason))
                {
                    if (!string.Equals(progress.Status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
                    {
                        progress.Status = AxisInitializeStepStatus.Complete;
                        progress.Message = "";
                        SetAxisInitializeStepProgress(progress);
                    }
                }
                else if (string.Equals(progress.Status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
                {
                    progress.Status = AxisInitializeStepStatus.ReinitializeRequired;
                    progress.Message = reason;
                    SetAxisInitializeStepProgress(progress);
                }
            }

            return progress;
        }

        private static bool ShouldResolveInitializedStepStatus(string status, bool machineInitialized)
        {
            if (string.Equals(status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
                return true;

            // To do: [초기화 모니터] 중단(ReinitializeRequired) 스텝을 재판정으로 되살리지 않는다.
            // 기존 조건: ReinitializeRequired도 재판정 대상이라 true를 반환했다.
            //            → 재판정은 축의 Alarm/Servo/IsHomeDone만 보는데, IsHomeDone은 이전 세션에서
            //              한 번 홈을 잡았고 서보가 유지되면 계속 참이다. 그래서 "반대 Lane 실패로 중단"된
            //              스텝이 화면을 열 때마다 Complete로 승격되고 메시지는 ""로 지워졌다.
            //              (2026-08-05: Status=Done인데 Description은 중단 메시지인 모순 표시)
            // 현재 기준: 한 번 중단으로 마킹된 스텝은 실제로 다시 실행되어 진행 보고가 올 때까지 그 상태를 유지한다.
            // if (string.Equals(status, AxisInitializeStepStatus.ReinitializeRequired, StringComparison.OrdinalIgnoreCase))
            //     return true;

            return machineInitialized &&
                (string.IsNullOrWhiteSpace(status) ||
                 string.Equals(status, AxisInitializeStepStatus.Waiting, StringComparison.OrdinalIgnoreCase));
        }

        private void SetAxisInitializeStepProgress(AxisInitializeStepProgress progress)
        {
            SetAxisInitializeStepProgress(progress, true);
        }

        private void SetAxisInitializeStepProgress(AxisInitializeStepProgress progress, bool persist)
        {
            try
            {
                if (progress == null)
                    return;

                _axisInitializeProgressStore.Set(progress);

                if (persist)
                    SaveMachineRuntimeState("InitializeStepProgress:" + progress.StepNo + ":" + (progress.GroupName ?? ""));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private List<MachineInitializeStepRuntimeState> CaptureAxisInitializeStepRuntimeStates()
        {
            try
            {
                return _axisInitializeProgressStore.Snapshot()
                    .Where(x => x != null)
                    .Select(x => new MachineInitializeStepRuntimeState
                    {
                        StepNo = x.StepNo,
                        GroupName = x.GroupName ?? "",
                        Status = x.Status ?? "",
                        Message = x.Message ?? ""
                    })
                    .ToList();
            }
            catch
            {
                return new List<MachineInitializeStepRuntimeState>();
            }
            finally
            {
            }
        }

        private void RestoreAxisInitializeStepRuntimeState(MachineRuntimeState state)
        {
            try
            {
                ClearAxisInitializeStepStates();
                if (state == null || state.InitializeSteps == null)
                    return;

                foreach (MachineInitializeStepRuntimeState saved in state.InitializeSteps)
                {
                    if (saved == null)
                        continue;

                    SetAxisInitializeStepProgress(new AxisInitializeStepProgress
                    {
                        StepNo = saved.StepNo,
                        GroupName = saved.GroupName ?? "",
                        Status = saved.Status ?? "",
                        Message = saved.Message ?? ""
                    }, false);
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state restored. count=" + state.InitializeSteps.Count + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EnsureAxisInitializeStepStatesLoadedFromRuntimeState()
        {
            try
            {
                if (_axisInitializeProgressStore.Count > 0)
                    return;

                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                if (settings == null || !settings.DeveloperMode)
                    return;

                MachineRuntimeState state = MachineRuntimeStateStore.Load();
                if (state == null || !state.DeveloperMode)
                    return;

                if (state.InitializeSteps != null && state.InitializeSteps.Count > 0)
                    RestoreAxisInitializeStepRuntimeState(state);
                else
                    RestoreAxisInitializeStepRuntimeStateFromSavedAxes(state);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step fallback restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RestoreAxisInitializeStepRuntimeStateFromSavedAxes(MachineRuntimeState state)
        {
            try
            {
                ClearAxisInitializeStepStates();
                if (state == null || state.Axes == null || state.Axes.Count == 0)
                    return;

                AxisInitializePlan plan = AxisInitializePlanStore.LoadOrCreateDefault(EnumerateAxes());
                if (plan == null || plan.Steps == null)
                    return;

                var savedAxes = state.Axes
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

                foreach (AxisInitializeStep step in plan.Steps.Where(x => x != null && x.Enabled))
                {
                    if (step.AxisNames == null || step.AxisNames.Count == 0)
                    {
                        if (state.IsMachineInitialized)
                            SetAxisInitializeStepProgress(
                                AxisInitializeStepProgress.Create(step, AxisInitializeStepStatus.Complete, ""),
                                false);
                        continue;
                    }

                    bool ready = true;
                    foreach (string axisName in step.AxisNames)
                    {
                        MachineAxisRuntimeState axisState;
                        if (string.IsNullOrWhiteSpace(axisName) ||
                            !savedAxes.TryGetValue(axisName, out axisState) ||
                            !axisState.IsServoOn ||
                            axisState.IsAlarm ||
                            !axisState.IsHomeDone)
                        {
                            ready = false;
                            break;
                        }
                    }

                    if (ready)
                    {
                        SetAxisInitializeStepProgress(
                            AxisInitializeStepProgress.Create(step, AxisInitializeStepStatus.Complete, ""),
                            false);
                    }
                }

                SaveMachineRuntimeState("InitializeStepStatusRebuiltFromSavedAxes");

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state rebuilt from saved axes. axisCount=" + savedAxes.Count + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state rebuild failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ClearAxisInitializeStepStates()
        {
            try
            {
                _axisInitializeProgressStore.Clear();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private bool CheckAxisInitializeStepStillValid(AxisInitializeStep step, out string reason)
        {
            reason = "";
            try
            {
                if (step == null)
                {
                    reason = "초기화 Step 정보가 없습니다. 다시 초기화가 필요합니다.";
                    return false;
                }

                List<BaseAxis> axes;
                List<string> missingAxisNames;
                if (!_axisInitializeRuntime.TryResolveAxesByNames(
                    step.AxisNames,
                    out axes,
                    out missingAxisNames))
                {
                    reason = "초기화 Step에 현재 장비에서 찾을 수 없는 축이 있습니다. missing=" +
                        string.Join(",", missingAxisNames.ToArray()) +
                        ". 다시 초기화 계획과 축 설정을 확인해야 합니다.";
                    return false;
                }

                foreach (BaseAxis axis in axes)
                {
                    if (axis == null)
                        continue;

                    try { axis.UpdateStatus(); } catch { }

                    if (axis.IsAlarm)
                    {
                        reason = axis.Name + " Alarm 발생. code=0x" + axis.AlarmCode.ToString("X4") +
                            ". 알람 해제 후 다시 초기화가 필요합니다.";
                        return false;
                    }

                    if (!axis.IsServoOn)
                    {
                        reason = axis.Name + " Servo OFF 상태입니다. Servo ON 후 다시 초기화가 필요합니다.";
                        return false;
                    }

                    if (!axis.IsHomeDone)
                    {
                        reason = axis.Name + " HOME 완료 상태가 해제되었습니다. 다시 초기화가 필요합니다.";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "초기화 완료 상태 확인 실패: " + ex.Message + ". 다시 초기화가 필요합니다.";
                QMC.Common.Log.Write("Main", "SYSTEM", "CheckAxisInitializeStepStillValid",
                    "Check initialize step status failed. step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "") +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private List<AxisInitializeStep> ResolveEnabledInitializeSteps(AxisInitializePlan plan)
        {
            try
            {
                if (plan == null || plan.Steps == null)
                    return new List<AxisInitializeStep>();

                return plan.Steps
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .ToList();
            }
            catch
            {
                return new List<AxisInitializeStep>();
            }
            finally
            {
            }
        }

        private AxisInitializeStep ResolveSingleEnabledInitializeStepForAxis(
            AxisInitializePlan plan,
            string axisName,
            out string reason)
        {
            reason = "";
            try
            {
                if (plan == null || plan.Steps == null)
                {
                    reason = "개별축 HOME 차단: 활성 초기화 계획 정보가 없습니다. axis=" + axisName;
                    return null;
                }

                string requestedCanonicalName = AjinAxisDefaults.ResolveName(axisName ?? "");
                List<AxisInitializeStep> matches = plan.Steps
                    .Where(step => step != null && step.Enabled && step.AxisNames != null &&
                        step.AxisNames.Any(candidate =>
                            !string.IsNullOrWhiteSpace(candidate) &&
                            string.Equals(
                                AjinAxisDefaults.ResolveName(candidate.Trim()),
                                requestedCanonicalName,
                                StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(step => step.StepNo)
                    .ToList();

                if (matches.Count == 1)
                    return matches[0];

                if (matches.Count == 0)
                {
                    reason = "개별축 HOME 차단: 활성 초기화 계획에서 선택 축이 포함된 Step을 찾을 수 없습니다. axis=" +
                        axisName + ", planVersion=" + plan.Version;
                    return null;
                }

                reason = "개별축 HOME 차단: 선택 축이 둘 이상의 활성 초기화 Step에 중복 등록되어 있습니다. axis=" +
                    axisName + ", steps=" + string.Join(",", matches
                        .Select(step => step.StepNo + ":" + (step.GroupName ?? "-"))
                        .ToArray());
                return null;
            }
            catch (Exception ex)
            {
                reason = "개별축 HOME 차단: 활성 초기화 Step 확인 중 예외가 발생했습니다. axis=" +
                    axisName + ", error=" + ex.Message;
                return null;
            }
            finally
            {
            }
        }

    }
}
