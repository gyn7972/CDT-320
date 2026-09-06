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
    /// <summary>
    /// [알람 강등 2026-08-11] 수동 동작 시작이 "거부"되었음을 나타내는 예외.
    /// Alarm 상태이거나 자동/시퀀스 동작 중이라 시작할 수 없다는 안내이며, 장비 이상이 아니다.
    ///
    /// UI 는 이 예외를 잡아 알람 대신 안내창 + Warning 로그로 처리해야 한다.
    /// AlarmManager 에 알람을 올리면 중앙 안전 계약(AlarmResponseService)이 severity 와 무관하게
    /// 전체 축 EStop 과 모든 시퀀스 정지를 수행한다 — 실측 2026-08-11 13:08/13:16:
    /// Auto 운전 중 LIFT WAFER MAPPING 클릭 → 정상 거부가 Error 알람으로 등록 →
    /// 가동 중이던 FrontPicker 시퀀스가 전축 EStop 으로 정지했다.
    ///
    /// InvalidOperationException 을 상속하므로 이 타입을 모르는 기존
    /// catch(Exception)/catch(InvalidOperationException) 호출자의 동작은 바뀌지 않는다.
    /// </summary>
    public sealed class ManualActionBlockedException : InvalidOperationException
    {
        public ManualActionBlockedException(string message)
            : base(message)
        {
        }
    }

    public partial class MachineController
    {
        private void StopInputStageRunReviewAxes()
        {
            InputStageUnit stage = _machine != null ? _machine.InputStageUnit : null;
            if (stage == null)
                return;

            StopInputStageRunReviewAxis(stage.CameraX, "InputCameraX");
            StopInputStageRunReviewAxis(stage.StageY, "InputStageY");
            StopInputStageRunReviewAxis(stage.StageT, "InputStageT");
            StopInputStageRunReviewAxis(stage.EjectPinZ, "InputEjectPinZ");
        }

        private static void StopInputStageRunReviewAxis(BaseAxis axis, string axisName)
        {
            if (axis == null)
                return;

            try
            {
                axis.StopJog();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual",
                    (axisName ?? "InputStageAxis") + " StopJog 호출 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }

            try
            {
                axis.Stop();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual",
                    (axisName ?? "InputStageAxis") + " Stop 호출 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
        }

        private void OnStopRequested()
        {
            CancelRecipeStartAttempt("StopRequested");

            var handler = StopRequested;
            if (handler == null)
                return;

            try
            {
                handler();
            }
            catch
            {
            }
        }

        public bool TryEnterInputStageRunReviewManual(
            QMC.CDT320.Sequencing.MachineSequenceContext context,
            string waferId,
            out string reason)
        {
            reason = string.Empty;
            string safeWaferId = string.IsNullOrWhiteSpace(waferId) ? string.Empty : waferId.Trim();
            CancellationTokenSource previousActionCts = null;

            try
            {
                if (context == null)
                {
                    reason = "InputStage Review Manual 진입 실패: Sequence context가 없습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(safeWaferId))
                {
                    reason = "InputStage Review Manual 진입 실패: Review 대상 Wafer ID가 없습니다.";
                    return false;
                }

                lock (_inputStageRunReviewManualLock)
                {
                    if (_inputStageRunReviewManualActive)
                    {
                        if (object.ReferenceEquals(_inputStageRunReviewContext, context) &&
                            string.Equals(_inputStageRunReviewWaferId, safeWaferId, StringComparison.OrdinalIgnoreCase) &&
                            _status == EquipmentStatus.ManualRunning)
                        {
                            reason = "InputStage Review Manual 세션이 이미 활성 상태입니다. wafer=" + safeWaferId;
                            return true;
                        }

                        reason = "InputStage Review Manual 진입 실패: 다른 Review 세션이 이미 활성 상태입니다. wafer=" +
                                 (_inputStageRunReviewWaferId ?? string.Empty);
                        return false;
                    }

                    if (!object.ReferenceEquals(_seqContext, context))
                    {
                        reason = "InputStage Review Manual 진입 실패: 현재 Auto Sequence context와 일치하지 않습니다.";
                        return false;
                    }

                    if (!IsSequenceRunning || _coordinator == null)
                    {
                        reason = "InputStage Review Manual 진입 실패: 실행 중인 Auto Coordinator가 없습니다.";
                        return false;
                    }

                    if (ActiveSequenceRunMode != QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        reason = "InputStage Review Manual 진입 실패: 현재 Sequence가 Auto mode가 아닙니다. mode=" +
                                 (ActiveSequenceRunMode.HasValue ? ActiveSequenceRunMode.Value.ToString() : "-");
                        return false;
                    }

                    if (_status != EquipmentStatus.AutoRunning)
                    {
                        reason = "InputStage Review Manual 진입 실패: 장비 상태가 AutoRunning이 아닙니다. status=" + _status;
                        return false;
                    }

                    if (AlarmManager.HasActive || _status == EquipmentStatus.Alarm)
                    {
                        reason = "InputStage Review Manual 진입 실패: 활성 Alarm이 있습니다.";
                        return false;
                    }

                    if (context.IsCycleStopRequested)
                    {
                        reason = "InputStage Review Manual 진입 실패: Cycle Stop 요청이 진행 중입니다.";
                        return false;
                    }

                    previousActionCts = _inputStageRunReviewActionCts;
                    _inputStageRunReviewActionCts = new CancellationTokenSource();
                    _inputStageRunReviewActionBusyCount = 0;
                    _inputStageRunReviewContext = context;
                    _inputStageRunReviewWaferId = safeWaferId;
                    _inputStageRunReviewManualActive = true;
                    SetStatus(EquipmentStatus.ManualRunning);
                }

                if (previousActionCts != null)
                {
                    try { previousActionCts.Cancel(); } catch { }
                    try { previousActionCts.Dispose(); } catch { }
                }

                RaiseInputStageRunReviewManualStateChanged(true, safeWaferId);
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual",
                    "InputStage Review Manual 세션에 진입했습니다. wafer=" + safeWaferId +
                    ", coordinatorMaintained=True, activeMode=" + ActiveSequenceRunMode + " - Set");
                reason = "InputStage Review Manual 세션에 진입했습니다. wafer=" + safeWaferId;
                return true;
            }
            catch (Exception ex)
            {
                reason = "InputStage Review Manual 진입 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual", reason + " - Failed");
                return false;
            }
        }

        public bool TryExitInputStageRunReviewManual(
            QMC.CDT320.Sequencing.MachineSequenceContext context,
            bool resumeAuto,
            out string reason)
        {
            reason = string.Empty;
            string waferId = string.Empty;
            CancellationTokenSource actionCts = null;

            try
            {
                if (context == null)
                {
                    reason = "InputStage Review Manual 종료 실패: Sequence context가 없습니다.";
                    return false;
                }

                lock (_inputStageRunReviewManualLock)
                {
                    if (!_inputStageRunReviewManualActive)
                    {
                        reason = "InputStage Review Manual 종료 실패: 활성 Review 세션이 없습니다.";
                        return false;
                    }

                    if (!object.ReferenceEquals(_inputStageRunReviewContext, context) ||
                        !object.ReferenceEquals(_seqContext, context))
                    {
                        reason = "InputStage Review Manual 종료 실패: 활성 Review/Auto Sequence context와 일치하지 않습니다.";
                        return false;
                    }

                    if (_inputStageRunReviewActionBusyCount > 0)
                    {
                        reason = "InputStage Review Manual 종료 실패: Review 수동 동작이 아직 실행 중입니다. busyCount=" +
                                 _inputStageRunReviewActionBusyCount;
                        return false;
                    }

                    if (resumeAuto)
                    {
                        if (!IsSequenceRunning || _coordinator == null ||
                            ActiveSequenceRunMode != QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                        {
                            reason = "InputStage Review Manual Auto 복귀 실패: 동일 Auto Coordinator가 실행 중이 아닙니다.";
                            return false;
                        }

                        if (_status != EquipmentStatus.ManualRunning)
                        {
                            reason = "InputStage Review Manual Auto 복귀 실패: 장비 상태가 ManualRunning이 아닙니다. status=" + _status;
                            return false;
                        }

                        if (AlarmManager.HasActive || _status == EquipmentStatus.Alarm)
                        {
                            reason = "InputStage Review Manual Auto 복귀 실패: 활성 Alarm이 있습니다.";
                            return false;
                        }

                        if (context.IsCycleStopRequested ||
                            _status == EquipmentStatus.CycleStopped || _status == EquipmentStatus.Stopped)
                        {
                            reason = "InputStage Review Manual Auto 복귀 실패: Stop/Cycle Stop 요청이 진행 중입니다.";
                            return false;
                        }
                    }

                    waferId = _inputStageRunReviewWaferId ?? string.Empty;
                    actionCts = _inputStageRunReviewActionCts;
                    _inputStageRunReviewActionCts = null;
                    _inputStageRunReviewActionBusyCount = 0;
                    _inputStageRunReviewContext = null;
                    _inputStageRunReviewWaferId = string.Empty;
                    _inputStageRunReviewManualActive = false;

                    if (resumeAuto)
                        SetStatus(EquipmentStatus.AutoRunning);
                }

                if (actionCts != null)
                {
                    try { actionCts.Cancel(); } catch { }
                    try { actionCts.Dispose(); } catch { }
                }

                RaiseInputStageRunReviewManualStateChanged(false, waferId);
                reason = "InputStage Review Manual 세션을 종료했습니다. wafer=" + waferId +
                         ", resumeAuto=" + resumeAuto;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual",
                    reason + ", coordinatorMaintained=" + IsSequenceRunning +
                    ", activeMode=" + (ActiveSequenceRunMode.HasValue ? ActiveSequenceRunMode.Value.ToString() : "-") +
                    " - Reset");
                return true;
            }
            catch (Exception ex)
            {
                reason = "InputStage Review Manual 종료 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual", reason + " - Failed");
                return false;
            }
        }

        public IDisposable BeginInputStageRunReviewActionScope(ManualMotionScopeKind kind, string reason)
        {
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "InputStageRunReviewAction" : reason.Trim();

            try
            {
                lock (_inputStageRunReviewManualLock)
                {
                    if (!_inputStageRunReviewManualActive || _inputStageRunReviewContext == null)
                        throw new InvalidOperationException("활성 InputStage Review Manual 세션이 없습니다. reason=" + safeReason);

                    if (!object.ReferenceEquals(_inputStageRunReviewContext, _seqContext) ||
                        !IsSequenceRunning || _coordinator == null ||
                        ActiveSequenceRunMode != QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        throw new InvalidOperationException(
                            "동일 Auto Coordinator가 유지되지 않아 Review 수동 동작을 시작할 수 없습니다. reason=" + safeReason);
                    }

                    if (_status != EquipmentStatus.ManualRunning)
                        throw new InvalidOperationException(
                            "장비 상태가 ManualRunning이 아니므로 Review 수동 동작을 시작할 수 없습니다. status=" +
                            _status + ", reason=" + safeReason);

                    if (AlarmManager.HasActive || _inputStageRunReviewContext.IsCycleStopRequested)
                        throw new InvalidOperationException(
                            "Alarm/Stop 요청 중에는 Review 수동 동작을 시작할 수 없습니다. reason=" + safeReason);

                    if (_inputStageRunReviewActionBusyCount > 0)
                        throw new InvalidOperationException(
                            "다른 Review 수동 동작이 진행 중입니다. 완료 또는 STOP 후 다시 실행하세요. reason=" +
                            safeReason);

                    if (_inputStageRunReviewActionBusyCount == 0 &&
                        (_inputStageRunReviewActionCts == null || _inputStageRunReviewActionCts.IsCancellationRequested))
                    {
                        if (_inputStageRunReviewActionCts != null)
                        {
                            try { _inputStageRunReviewActionCts.Dispose(); } catch { }
                        }
                        _inputStageRunReviewActionCts = new CancellationTokenSource();
                    }

                    _inputStageRunReviewActionBusyCount++;
                    try
                    {
                        if (kind != ManualMotionScopeKind.ProcessSequence)
                            throw new InvalidOperationException(
                                "InputStage Review는 ProcessSequence 동작만 허용합니다. kind=" + kind);

                        // MotionSpeedScale은 전역 상태이므로 Auto Coordinator와 병행하는 Review에서 잡지 않는다.
                        // 실제 InputStage 명령은 명시 Jog/Fine 속도를 사용하며, 호출 지점에서 AsyncLocal 기반
                        // ManualSequenceProcess MotionGuard를 감싸 Output 계통 속도에는 영향을 주지 않는다.
                        QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewAction",
                            "Review 수동 동작 스코프를 시작했습니다. reason=" + safeReason +
                            ", kind=" + kind +
                            ", globalSpeedScale=False" +
                            ", busyCount=" + _inputStageRunReviewActionBusyCount + " - Start");
                        return new InputStageRunReviewActionScope(this, null, safeReason);
                    }
                    catch
                    {
                        LeaveInputStageRunReviewAction(safeReason);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewAction",
                    "Review 수동 동작 스코프 시작 실패. reason=" + safeReason +
                    ", kind=" + kind + ", error=" + ex.Message + " - Failed");
                throw;
            }
        }

        public async Task<IDisposable> BeginInputStageRunReviewWorkAsync(
            ManualMotionScopeKind kind,
            string reason,
            CancellationToken ct)
        {
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "InputStageRunReviewWork" : reason.Trim();
            IDisposable actionScope = null;
            CancellationTokenSource linkedCts = null;
            QMC.CDT320.Sequencing.SequenceResourceLease stageLease = null;
            QMC.CDT320.Sequencing.AutoSequenceCameraWorkZoneLease cameraLease = null;

            try
            {
                actionScope = BeginInputStageRunReviewActionScope(kind, safeReason);

                QMC.CDT320.Sequencing.MachineSequenceContext context;
                CancellationToken reviewToken;
                lock (_inputStageRunReviewManualLock)
                {
                    context = _inputStageRunReviewContext;
                    reviewToken = _inputStageRunReviewActionCts != null
                        ? _inputStageRunReviewActionCts.Token
                        : CancellationToken.None;
                }

                if (context == null || !object.ReferenceEquals(context, _seqContext))
                    throw new InvalidOperationException("Review Auto Sequence context가 변경되었습니다.");

                string safetyReason;
                if (!AreInputStageRunReviewPickersSafe(out safetyReason))
                    throw new InvalidOperationException("Review 수동 동작 전 Picker 안전 조건이 유효하지 않습니다. " + safetyReason);

                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, reviewToken);
                stageLease = await context.Resources.AcquireAsync(
                    QMC.CDT320.Sequencing.SequenceResourceKind.InputStageArea,
                    "InputStageRunReview:" + safeReason,
                    30000,
                    linkedCts.Token).ConfigureAwait(false);
                if (stageLease == null)
                    throw new InvalidOperationException("InputStageArea 리소스 점유에 실패했습니다.");

                cameraLease = await context.AutoLoaderGate.BeginInputCameraWorkAsync(
                    "InputStageRunReview:" + safeReason,
                    linkedCts.Token).ConfigureAwait(false);
                if (cameraLease == null)
                    throw new InvalidOperationException("InputCamera 작업 영역 점유에 실패했습니다.");

                if (!AreInputStageRunReviewPickersSafe(out safetyReason))
                    throw new InvalidOperationException("Review 수동 동작 직전 Picker 안전 재확인에 실패했습니다. " + safetyReason);

                return new InputStageRunReviewWorkScope(
                    cameraLease,
                    stageLease,
                    linkedCts,
                    actionScope);
            }
            catch
            {
                if (cameraLease != null)
                    cameraLease.Dispose();
                if (stageLease != null)
                    stageLease.Dispose();
                if (linkedCts != null)
                    linkedCts.Dispose();
                if (actionScope != null)
                    actionScope.Dispose();
                throw;
            }
        }

        public bool AreInputStageRunReviewPickersSafe(out string reason)
        {
            reason = string.Empty;

            QMC.CDT320.Sequencing.MachineSequenceContext context;
            lock (_inputStageRunReviewManualLock)
            {
                context = _inputStageRunReviewManualActive
                    ? _inputStageRunReviewContext
                    : _seqContext;
            }

            if (context == null || !object.ReferenceEquals(context, _seqContext))
            {
                reason = "현재 Auto Sequence와 일치하는 InputStage Review context가 없습니다.";
                return false;
            }

            if (!IsSequenceRunning || _coordinator == null ||
                ActiveSequenceRunMode != QMC.CDT320.Sequencing.SequenceRunMode.Auto)
            {
                reason = "InputStage Review 안전 확인에 필요한 Auto Coordinator가 실행 중이 아닙니다.";
                return false;
            }

            if (context.Bus == null || !context.Bus.IsSet("InputLoaderActive"))
            {
                reason = "InputLoaderActive Review Hold 신호가 없습니다.";
                return false;
            }

            if (context.PickerPhases == null)
            {
                reason = "Picker phase coordinator가 없습니다.";
                return false;
            }

            QMC.CDT320.Sequencing.PickerPhaseSnapshot phaseSnapshot = context.PickerPhases.GetSnapshot();
            if (phaseSnapshot.Front.Phase != QMC.CDT320.Sequencing.PickerProcessPhase.Idle ||
                phaseSnapshot.Rear.Phase != QMC.CDT320.Sequencing.PickerProcessPhase.Idle)
            {
                reason = "Front/Rear Picker phase가 모두 Idle이 아닙니다. " + phaseSnapshot;
                return false;
            }

            PickerFrontUnit front = context.Machine != null ? context.Machine.PickerFrontUnit : null;
            PickerRearUnit rear = context.Machine != null ? context.Machine.PickerRearUnit : null;
            if (front == null || rear == null)
            {
                reason = "Front/Rear Picker Unit을 모두 확인할 수 없습니다. front=" +
                         (front != null) + ", rear=" + (rear != null);
                return false;
            }

            if (!front.IsFrontPickerInAvoidPosition())
            {
                reason = "FrontPicker X/Y/Z/T 축이 모두 Avoid 위치가 아닙니다.";
                return false;
            }
            foreach (BaseAxis axis in front.Axes.Values)
            {
                if (axis != null && axis.IsMoving)
                {
                    reason = "FrontPicker 축이 이동 중입니다. axis=" + axis.Name;
                    return false;
                }
            }

            if (!rear.IsRearPickerInAvoidPosition())
            {
                reason = "RearPicker X/Y/Z/T 축이 모두 Avoid 위치가 아닙니다.";
                return false;
            }
            foreach (BaseAxis axis in rear.Axes.Values)
            {
                if (axis != null && axis.IsMoving)
                {
                    reason = "RearPicker 축이 이동 중입니다. axis=" + axis.Name;
                    return false;
                }
            }

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial frontDie = MaterialStateService.GetDieAtPicker(
                    MaterialLocationKind.PickerFront,
                    pickerNo);
                if (frontDie != null)
                {
                    reason = "FrontPicker가 제품을 보유 중입니다. picker=" + pickerNo +
                             ", die=" + (frontDie.DieId ?? string.Empty);
                    return false;
                }

                DieMaterial rearDie = MaterialStateService.GetDieAtPicker(
                    MaterialLocationKind.PickerRear,
                    pickerNo);
                if (rearDie != null)
                {
                    reason = "RearPicker가 제품을 보유 중입니다. picker=" + pickerNo +
                             ", die=" + (rearDie.DieId ?? string.Empty);
                    return false;
                }
            }

            return true;
        }

        private sealed class InputStageRunReviewWorkScope : IDisposable
        {
            private IDisposable _cameraLease;
            private IDisposable _stageLease;
            private CancellationTokenSource _linkedCts;
            private IDisposable _actionScope;

            public InputStageRunReviewWorkScope(
                IDisposable cameraLease,
                IDisposable stageLease,
                CancellationTokenSource linkedCts,
                IDisposable actionScope)
            {
                _cameraLease = cameraLease;
                _stageLease = stageLease;
                _linkedCts = linkedCts;
                _actionScope = actionScope;
            }

            public void Dispose()
            {
                IDisposable cameraLease = Interlocked.Exchange(ref _cameraLease, null);
                IDisposable stageLease = Interlocked.Exchange(ref _stageLease, null);
                CancellationTokenSource linkedCts = Interlocked.Exchange(ref _linkedCts, null);
                IDisposable actionScope = Interlocked.Exchange(ref _actionScope, null);

                try { if (cameraLease != null) cameraLease.Dispose(); } catch { }
                try { if (stageLease != null) stageLease.Dispose(); } catch { }
                try { if (linkedCts != null) linkedCts.Dispose(); } catch { }
                try { if (actionScope != null) actionScope.Dispose(); } catch { }
            }
        }

        public void CancelInputStageRunReviewAction()
        {
            CancellationTokenSource cts;
            bool actionBusy;
            lock (_inputStageRunReviewManualLock)
            {
                cts = _inputStageRunReviewActionCts;
                actionBusy = _inputStageRunReviewActionBusyCount > 0;
            }

            if (cts == null)
                return;

            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
                if (actionBusy)
                    StopInputStageRunReviewAxes();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewAction",
                    "Review 수동 동작 취소 요청 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
        }

        private void LeaveInputStageRunReviewAction(string reason)
        {
            CancellationTokenSource canceledCts = null;
            int busyCount;

            lock (_inputStageRunReviewManualLock)
            {
                if (_inputStageRunReviewActionBusyCount > 0)
                    _inputStageRunReviewActionBusyCount--;
                else
                    _inputStageRunReviewActionBusyCount = 0;

                busyCount = _inputStageRunReviewActionBusyCount;
                if (busyCount == 0 &&
                    _inputStageRunReviewActionCts != null &&
                    _inputStageRunReviewActionCts.IsCancellationRequested)
                {
                    canceledCts = _inputStageRunReviewActionCts;
                    _inputStageRunReviewActionCts = null;
                }
            }

            if (canceledCts != null)
            {
                try { canceledCts.Dispose(); } catch { }
            }

            QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewAction",
                "Review 수동 동작 스코프를 종료했습니다. reason=" + (reason ?? string.Empty) +
                ", busyCount=" + busyCount + " - End");
        }

        private sealed class InputStageRunReviewActionScope : IDisposable
        {
            private MachineController _owner;
            private IDisposable _motionScope;
            private readonly string _reason;

            public InputStageRunReviewActionScope(
                MachineController owner,
                IDisposable motionScope,
                string reason)
            {
                _owner = owner;
                _motionScope = motionScope;
                _reason = reason ?? string.Empty;
            }

            public void Dispose()
            {
                MachineController owner = Interlocked.Exchange(ref _owner, null);
                if (owner == null)
                    return;

                try
                {
                    IDisposable motionScope = Interlocked.Exchange(ref _motionScope, null);
                    if (motionScope != null)
                        motionScope.Dispose();
                }
                finally
                {
                    owner.LeaveInputStageRunReviewAction(_reason);
                }
            }
        }

        private void ClearInputStageRunReviewManualState(string clearReason)
        {
            bool notify;
            string waferId;
            CancellationTokenSource actionCts;

            lock (_inputStageRunReviewManualLock)
            {
                notify = _inputStageRunReviewManualActive;
                waferId = _inputStageRunReviewWaferId ?? string.Empty;
                actionCts = _inputStageRunReviewActionCts;

                _inputStageRunReviewManualActive = false;
                _inputStageRunReviewWaferId = string.Empty;
                _inputStageRunReviewContext = null;
                _inputStageRunReviewActionBusyCount = 0;
                _inputStageRunReviewActionCts = null;
            }

            if (actionCts != null)
            {
                try { actionCts.Cancel(); } catch { }
                try { actionCts.Dispose(); } catch { }
            }

            if (!notify)
                return;

            RaiseInputStageRunReviewManualStateChanged(false, waferId);
            QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual",
                "InputStage Review Manual 상태를 강제 정리했습니다. wafer=" + waferId +
                ", reason=" + (clearReason ?? string.Empty) + " - Reset");
        }

        private void RaiseInputStageRunReviewManualStateChanged(bool active, string waferId)
        {
            Action<bool, string> handler = InputStageRunReviewManualStateChanged;
            if (handler == null)
                return;

            try
            {
                handler(active, waferId ?? string.Empty);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual",
                    "InputStage Review Manual 상태 변경 이벤트 처리 중 예외가 발생했습니다. active=" + active +
                    ", wafer=" + (waferId ?? string.Empty) +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        private IDisposable EnterManualOperation()
        {
            // Recipe 적용 등록과 수동 busy 등록을 같은 잠금으로 묶어 확인 직후 진입 경합을 막는다.
            lock (_recipeOperationLock)
            {
                if (_recipeApplyOperationActive)
                    throw new ManualActionBlockedException(
                        "Recipe 저장/적용 중에는 수동 동작을 시작할 수 없습니다.");

                if (Interlocked.Increment(ref _manualBusyCount) == 1)
                {
                    var old = Interlocked.Exchange(ref _manualCts, new CancellationTokenSource());
                    if (old != null)
                        old.Dispose();
                }
            }

            if (_status != EquipmentStatus.Alarm && _status != EquipmentStatus.AutoRunning)
                SetStatus(EquipmentStatus.ManualRunning);
            return new ManualOperationScope(this);
        }

        private IDisposable BeginManualMotionSpeedOnlyScope(string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 동작 속도 스코프 적용. percent=" +
                    MotionSpeedScale.ManualSequencePercent.ToString("0.###") +
                    ", reason=" + reason + " - Start");
                return MotionSpeedScale.BeginManualSequenceScale();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 동작 속도 스코프 적용 실패. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginReadySequenceSpeedScope(string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ReadySequenceSpeedScale",
                    "READY 시퀀스 속도 스코프 적용. percent=" +
                    MotionSpeedScale.ReadySequencePercent.ToString("0.###") +
                    ", reason=" + reason + " - Start");
                return MotionSpeedScale.BeginReadySequenceScale();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ReadySequenceSpeedScale",
                    "READY 시퀀스 속도 스코프 적용 실패. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginManualProcessSequenceScope(string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 시컨스 속도/공정 인터락 스코프 적용. percent=" +
                    MotionSpeedScale.ManualSequencePercent.ToString("0.###") +
                    ", reason=" + reason + " - Start");
                IDisposable speedScope = null;
                IDisposable guardScope = null;
                try
                {
                    speedScope = MotionSpeedScale.BeginManualSequenceScale();
                    guardScope = MotionGuardRuntime.BeginManualSequenceProcessMove(reason);
                    return new CompositeManualSequenceScope(speedScope, guardScope);
                }
                catch
                {
                    if (guardScope != null)
                        guardScope.Dispose();
                    if (speedScope != null)
                        speedScope.Dispose();
                    throw;
                }
                finally
                {
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 시컨스 속도/공정 인터락 스코프 적용 실패. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginManualMotionScope(ManualMotionScopeKind kind, string reason)
        {
            switch (kind)
            {
                case ManualMotionScopeKind.SpeedOnly:
                    return BeginManualMotionSpeedOnlyScope(reason);
                case ManualMotionScopeKind.ReadySequence:
                    return BeginReadySequenceSpeedScope(reason);
                case ManualMotionScopeKind.ProcessSequence:
                    return BeginManualProcessSequenceScope(reason);
                default:
                    throw new ArgumentOutOfRangeException(
                        "kind",
                        kind,
                        "지원하지 않는 수동 모션 스코프 종류입니다.");
            }
        }

        public IDisposable BeginManualActionScope(ManualMotionScopeKind kind, string reason)
        {
            try
            {
                // [알람 강등 2026-08-11] 시작 거부는 ManualActionBlockedException 으로 던져
                // UI 가 알람 대신 안내로 처리할 수 있게 구분한다(타입 정의부 주석 참고).
                if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
                    throw new ManualActionBlockedException(
                        "Alarm 상태에서는 수동 동작을 시작할 수 없습니다. reason=" + reason);

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                    throw new ManualActionBlockedException(
                        "자동/시컨스 동작 중에는 수동 동작을 시작할 수 없습니다. reason=" + reason +
                        ", status=" + _status +
                        ", activeMode=" + (ActiveSequenceRunMode.HasValue ? ActiveSequenceRunMode.Value.ToString() : "-"));

                IDisposable manualScope = null;
                IDisposable motionScope = null;
                try
                {
                    manualScope = EnterManualOperation();
                    if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
                    {
                        CancelManualOperation();
                        throw new ManualActionBlockedException(
                            "수동 동작 진입 중 Alarm이 발생하여 실행을 취소했습니다. reason=" + reason);
                    }

                    motionScope = BeginManualMotionScope(kind, reason);
                    return new ManualActionScope(manualScope, motionScope);
                }
                catch
                {
                    if (motionScope != null)
                        motionScope.Dispose();
                    if (manualScope != null)
                        manualScope.Dispose();
                    throw;
                }
                finally
                {
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualActionScope",
                    "수동 동작 스코프 시작 실패. reason=" + reason +
                    ", kind=" + kind +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginManualProcessSequenceScopeIfNeeded(QMC.CDT320.Sequencing.SequenceRunMode mode)
        {
            try
            {
                if (mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    return null;

                return BeginManualProcessSequenceScope("Coordinator:" + mode);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public void CancelManualOperation()
        {
            try
            {
                var cts = _manualCts;
                if (cts != null && !cts.IsCancellationRequested)
                    cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void LeaveManualOperation()
        {
            if (Interlocked.Decrement(ref _manualBusyCount) < 0)
                Interlocked.Exchange(ref _manualBusyCount, 0);

            if (!IsManualBusy)
            {
                var cts = Interlocked.Exchange(ref _manualCts, null);
                if (cts != null)
                    cts.Dispose();
            }

            if (!IsManualBusy && !IsSequenceRunning && _status == EquipmentStatus.ManualRunning)
                SetStatus(EquipmentStatus.Stopped);
        }

        private sealed class ManualOperationScope : IDisposable
        {
            private MachineController _owner;

            public ManualOperationScope(MachineController owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref _owner, null);
                if (owner != null)
                    owner.LeaveManualOperation();
            }
        }

        private sealed class ManualActionScope : IDisposable
        {
            private IDisposable _manualScope;
            private IDisposable _motionScope;
            private bool _disposed;

            public ManualActionScope(IDisposable manualScope, IDisposable motionScope)
            {
                _manualScope = manualScope;
                _motionScope = motionScope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                Exception first = null;
                try
                {
                    IDisposable motion = _motionScope;
                    _motionScope = null;
                    if (motion != null)
                        motion.Dispose();
                }
                catch (Exception ex)
                {
                    first = ex;
                }

                try
                {
                    IDisposable manual = _manualScope;
                    _manualScope = null;
                    if (manual != null)
                        manual.Dispose();
                }
                catch (Exception ex)
                {
                    if (first == null)
                        first = ex;
                }
                finally
                {
                    _disposed = true;
                }

                if (first != null)
                    throw first;
            }
        }

        private sealed class CompositeManualSequenceScope : IDisposable
        {
            private IDisposable _speedScope;
            private IDisposable _guardScope;
            private bool _disposed;

            public CompositeManualSequenceScope(IDisposable speedScope, IDisposable guardScope)
            {
                _speedScope = speedScope;
                _guardScope = guardScope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                Exception first = null;
                try
                {
                    IDisposable guard = _guardScope;
                    _guardScope = null;
                    if (guard != null)
                        guard.Dispose();
                }
                catch (Exception ex)
                {
                    first = ex;
                }

                try
                {
                    IDisposable speed = _speedScope;
                    _speedScope = null;
                    if (speed != null)
                        speed.Dispose();
                }
                catch (Exception ex)
                {
                    if (first == null)
                        first = ex;
                }
                finally
                {
                    _disposed = true;
                }

                if (first != null)
                    throw first;
            }
        }

        /// <summary>지정한 유닛들을 Manual 모드로 시작합니다.</summary>
        public Task<int> StartManualAsync(QMC.CDT320.Sequencing.SequenceUnitKind units)
        {
            return StartSequenceAsync(new QMC.CDT320.Sequencing.SequenceRunOptions
            {
                Units = units,
                Mode = QMC.CDT320.Sequencing.SequenceRunMode.Manual
            });
        }

        /// <summary>지정한 단일 유닛을 지정 실행 모드로 시작합니다.</summary>
        public Task<int> StartSingleUnitAsync(
            QMC.CDT320.Sequencing.SequenceUnitKind unit,
            QMC.CDT320.Sequencing.SequenceRunMode mode)
        {
            return StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions.Single(unit, mode));
        }

        /// <summary>Manual 또는 Step 모드에서 지정 유닛을 1단계 진행시킵니다.</summary>
        public void ManualStep(QMC.CDT320.Sequencing.SequenceUnitKind unit)
        {
            if (_coordinator == null)
            {
                Log("[SEQ] ManualStep ignored: coordinator 없음");
                return;
            }

            _coordinator.StepUnit(unit);
        }

        /// <summary>Manual 또는 Step 모드에서 활성 유닛 전체를 1단계 진행합니다.</summary>
        public void ManualStepAll()
        {
            if (_coordinator == null)
            {
                Log("[SEQ] ManualStepAll ignored: coordinator 없음");
                return;
            }

            _coordinator.StepAll();
        }

        /// <summary>Manual Sequence Dialog에서 지정 유닛을 Auto와 같은 데이터/시퀀스 컨텍스트로 1단계 진행합니다.</summary>
        public async Task<int> RunManualSequenceUnitStepAsync(QMC.CDT320.Sequencing.SequenceUnitKind unit)
        {
            try
            {
                LastActionFailureMessage = "";

                if (unit == QMC.CDT320.Sequencing.SequenceUnitKind.None)
                {
                    LastActionFailureMessage = "실행할 Manual 시퀀스 유닛이 선택되지 않았습니다.";
                    return -1;
                }

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 Manual 시퀀스를 실행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        LastActionFailureMessage + " unit=" + unit + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualSequenceUnitStepAsync"))
                    return -1;

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 Manual 시퀀스를 실행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        LastActionFailureMessage + " unit=" + unit + " - Failed");
                    return -1;
                }

                if (IsSequenceRunning)
                {
                    if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        LastActionFailureMessage = "Auto Sequence 실행 중에는 Manual 시퀀스를 실행할 수 없습니다.";
                        QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                            LastActionFailureMessage + " unit=" + unit + " - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-AUTO-RUNNING", "MachineController", LastActionFailureMessage);
                        return -1;
                    }

                    ManualStep(unit);
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        "Manual sequence unit step gate released. unit=" + unit + " - Ok");
                    return 0;
                }

                if (_status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto Running 상태에서는 Manual 시퀀스를 시작할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        LastActionFailureMessage + " unit=" + unit + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                int startResult = await StartSequenceAsync(
                    QMC.CDT320.Sequencing.SequenceRunOptions.ProcessStep()).ConfigureAwait(false);
                if (startResult != 0)
                    return startResult;

                ManualStep(unit);

                QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                    "Manual sequence process step mode started. unit=" + unit + " - Ok");
                Log("[SEQ] Manual sequence step start. unit=" + unit);
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Manual 시퀀스 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                    LastActionFailureMessage + " unit=" + unit + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Manual Sequence Dialog에서 Picker 단위 공정을 Auto와 동일한 Material/DieMap 상태로 실행합니다.</summary>
        public async Task<int> RunManualPickerProcessAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            string processName)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 Picker Manual 공정을 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 Picker Manual 공정을 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 Picker Manual 공정을 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualPickerProcessAsync"))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualPickerProcess:" + side + ":" + processName))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.ApplyInputStageVisionPolicy(Machine);

                    string name = (processName ?? "").Trim();
                    int result;
                    if (string.Equals(name, "PickUp", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else if (string.Equals(name, "Bottom", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerBottomInspectionSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else if (string.Equals(name, "Side", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerSideInspectionSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else if (string.Equals(name, "Place", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerPlaceSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else
                    {
                        LastActionFailureMessage = "지원하지 않는 Picker Manual 공정입니다. process=" + processName;
                        return -1;
                    }

                    if (result != 0)
                    {
                        LastActionFailureMessage = "Picker Manual 공정 실패. side=" + side + ", process=" + processName + ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState("ManualPickerProcess:" + side + ":" + processName);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "Picker Manual 공정이 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Picker Manual 공정 실행 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Manual Sequence Dialog에서 PickUp Z 세부 모션만 단독 테스트합니다. Material/DieMap 상태는 변경하지 않습니다.</summary>
        public async Task<int> RunManualPickerPickUpZMotionTestAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 PickUp Z 단독 테스트를 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-Z-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 PickUp Z 단독 테스트를 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 PickUp Z 단독 테스트를 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-Z-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualPickerPickUpZMotionTestAsync"))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualPickerPickUpZMotionTest:" + side + ":" + pickerNo))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.PickerNo = pickerNo;
                    options.ApplyInputStageVisionPolicy(Machine);

                    int result = await new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side)
                        .RunManualZMotionOnlyAsync(pickerNo, ManualOperationToken, options).ConfigureAwait(false);
                    if (result != 0)
                    {
                        LastActionFailureMessage = "PickUp Z 단독 테스트 실패. side=" + side + ", pickerNo=" + pickerNo + ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState("ManualPickerPickUpZMotionTest:" + side + ":" + pickerNo);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "PickUp Z 단독 테스트가 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "PickUp Z 단독 테스트 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-Z-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        private bool ShouldSimulatePickerVisionResult(QMC.CDT320.Sequencing.PickerSequenceSide side)
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings != null &&
                    (settings.SimulationMode || settings.BypassHardware || !settings.UseAjin))
                    return true;

                if (_machine == null)
                    return false;

                if (side == QMC.CDT320.Sequencing.PickerSequenceSide.Front)
                {
                    return _machine.PickerFrontUnit != null &&
                           ((_machine.PickerFrontUnit.Setup != null && _machine.PickerFrontUnit.Setup.IsSimulationMode) ||
                            (_machine.PickerFrontUnit.Config != null && _machine.PickerFrontUnit.Config.IsSimulationMode));
                }

                return _machine.PickerRearUnit != null &&
                       ((_machine.PickerRearUnit.Setup != null && _machine.PickerRearUnit.Setup.IsSimulationMode) ||
                        (_machine.PickerRearUnit.Config != null && _machine.PickerRearUnit.Config.IsSimulationMode));
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        /// <summary>선택한 InputStage Die를 대상으로 Manual PickUp 테스트를 실행합니다. 실제 Material 상태를 Picker 보유 상태로 갱신합니다.</summary>
        public async Task<int> RunManualPickerSelectedDiePickUpAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 선택 Die PickUp 테스트를 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-DIE-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 선택 Die PickUp 테스트를 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 선택 Die PickUp 테스트를 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-DIE-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (string.IsNullOrWhiteSpace(dieId))
                {
                    LastActionFailureMessage = "PickUp 테스트 대상 Die가 선택되지 않았습니다.";
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualPickerSelectedDiePickUpAsync"))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualPickerSelectedDiePickUp:" + side + ":" + pickerNo))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.PickerNo = pickerNo;
                    options.SimulateVisionResult = ShouldSimulatePickerVisionResult(side);
                    options.ApplyInputStageVisionPolicy(Machine);

                    int result = await new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side)
                        .RunManualSelectedDiePickUpAsync(dieId, pickerNo, ManualOperationToken, options)
                        .ConfigureAwait(false);
                    if (result != 0)
                    {
                        LastActionFailureMessage = "선택 Die PickUp 테스트 실패. side=" + side +
                                                   ", pickerNo=" + pickerNo +
                                                   ", die=" + dieId +
                                                   ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState("ManualPickerSelectedDiePickUp:" + side + ":" + pickerNo + ":" + dieId);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "선택 Die PickUp 테스트가 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "선택 Die PickUp 테스트 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-DIE-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> RunManualPickerSelectedOutputSlotPlaceAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            QMC.CDT320.BinSide outputSide,
            QMC.CDT320.Materials.OutputStageReceiveTarget receiveTarget)
        {
            return await RunManualPickerSelectedOutputSlotPlaceCoreAsync(
                side,
                pickerNo,
                outputSide,
                receiveTarget,
                null,
                "선택 Output Slot Place 테스트",
                "ManualPickerSelectedOutputSlotPlace",
                "SEQ-MANUAL-PICKER-PLACE").ConfigureAwait(false);
        }

        public async Task<int> RunManualPickerSelectedOutputSlotPlaceStepAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            QMC.CDT320.BinSide outputSide,
            QMC.CDT320.Materials.OutputStageReceiveTarget receiveTarget,
            QMC.CDT320.Sequencing.PickerPlaceManualStep step)
        {
            return await RunManualPickerSelectedOutputSlotPlaceCoreAsync(
                side,
                pickerNo,
                outputSide,
                receiveTarget,
                step,
                "선택 Output Slot Place Step 테스트",
                "ManualPickerSelectedOutputSlotPlaceStep:" + step,
                "SEQ-MANUAL-PICKER-PLACE-STEP").ConfigureAwait(false);
        }

        private async Task<int> RunManualPickerSelectedOutputSlotPlaceCoreAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            QMC.CDT320.BinSide outputSide,
            QMC.CDT320.Materials.OutputStageReceiveTarget receiveTarget,
            QMC.CDT320.Sequencing.PickerPlaceManualStep? step,
            string actionTitle,
            string scopeName,
            string alarmCode)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 " + actionTitle + "를 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCode + "-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 " + actionTitle + "를 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 " + actionTitle + "를 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCode + "-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (receiveTarget == null)
                {
                    LastActionFailureMessage = "Place 테스트 대상 Output slot이 선택되지 않았습니다.";
                    return -1;
                }

                if (!EnsureMachineInitializedForRun(scopeName))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, scopeName + ":" + side + ":" + pickerNo))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.PickerNo = pickerNo;
                    options.RestrictToPickerNo = pickerNo;
                    options.ApplyInputStageVisionPolicy(Machine);

                    var sequence = new QMC.CDT320.Sequencing.PickerPlaceSequence(context, side);
                    int result = step.HasValue
                        ? await sequence.RunManualSelectedOutputSlotPlaceStepAsync(
                            outputSide,
                            receiveTarget,
                            pickerNo,
                            step.Value,
                            ManualOperationToken,
                            options).ConfigureAwait(false)
                        : await sequence.RunManualSelectedOutputSlotPlaceAsync(
                            outputSide,
                            receiveTarget,
                            pickerNo,
                            ManualOperationToken,
                            options).ConfigureAwait(false);

                    if (result != 0)
                    {
                        LastActionFailureMessage = actionTitle + " 실패. side=" + side +
                                                   ", pickerNo=" + pickerNo +
                                                   ", outputSide=" + outputSide +
                                                   ", order=" + receiveTarget.OrderIndex +
                                                   ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState(scopeName + ":" + side + ":" + pickerNo + ":" + outputSide + ":" + receiveTarget.OrderIndex);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = actionTitle + "가 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = actionTitle + " 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode + "-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> RunManualPickerSelectedDiePrepareAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            return await RunManualPickerSelectedDieStepAsync(
                side,
                pickerNo,
                dieId,
                false,
                "선택 Die PickUp 준비",
                "ManualPickerSelectedDiePrepare",
                "SEQ-MANUAL-PICKER-DIE-PREPARE").ConfigureAwait(false);
        }

        public async Task<int> RunManualPickerPreparedDiePickZAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            return await RunManualPickerSelectedDieStepAsync(
                side,
                pickerNo,
                dieId,
                true,
                "준비 Die Pick Z 테스트",
                "ManualPickerPreparedDiePickZ",
                "SEQ-MANUAL-PICKER-DIE-Z").ConfigureAwait(false);
        }

        public async Task<int> RunManualPickerPreparedDiePickZStepAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId,
            QMC.CDT320.Sequencing.PickerPickUpZManualStep step)
        {
            return await RunManualPickerSelectedDieStepAsync(
                side,
                pickerNo,
                dieId,
                false,
                "준비 Die Pick Z Step 테스트",
                "ManualPickerPreparedDiePickZStep:" + step,
                "SEQ-MANUAL-PICKER-DIE-Z-STEP",
                step).ConfigureAwait(false);
        }

        private async Task<int> RunManualPickerSelectedDieStepAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId,
            bool pickZ,
            string label,
            string saveReason,
            string alarmCodePrefix,
            QMC.CDT320.Sequencing.PickerPickUpZManualStep? pickZStep = null)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 " + label + "를 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 " + label + "를 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 " + label + "를 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (string.IsNullOrWhiteSpace(dieId))
                {
                    LastActionFailureMessage = label + " 대상 Die가 선택되지 않았습니다.";
                    return -1;
                }

                if (!EnsureMachineInitializedForRun(saveReason))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, saveReason + ":" + side + ":" + pickerNo))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.PickerNo = pickerNo;
                    options.SimulateVisionResult = ShouldSimulatePickerVisionResult(side);
                    options.ApplyInputStageVisionPolicy(Machine);

                    var sequence = new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side);
                    int result;
                    if (pickZStep.HasValue)
                    {
                        result = await sequence.RunManualPreparedDiePickZStepAsync(
                            dieId,
                            pickerNo,
                            pickZStep.Value,
                            ManualOperationToken,
                            options).ConfigureAwait(false);
                    }
                    else
                    {
                        result = pickZ
                            ? await sequence.RunManualPreparedDiePickZAsync(dieId, pickerNo, ManualOperationToken, options).ConfigureAwait(false)
                            : await sequence.RunManualSelectedDiePrepareAsync(dieId, pickerNo, ManualOperationToken, options).ConfigureAwait(false);
                    }
                    if (result != 0)
                    {
                        LastActionFailureMessage = label + " 실패. side=" + side +
                                                   ", pickerNo=" + pickerNo +
                                                   ", die=" + dieId +
                                                   ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState(saveReason + ":" + side + ":" + pickerNo + ":" + dieId);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = label + "가 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = label + " 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        public Task<int> RunManualInputLoadAsync()
        {
            // slot이 음수이면 자동 순번 로딩이므로 role은 사용되지 않는다.
            return RunManualInputLoadAsync(CassetteMaterialRole.Input1, -1);
        }

        /// <summary>
        /// CYCLE RUN INPUT LOAD: Auto 운전이 실제로 사용하는 재개 판정 + 스텝 상태머신을 그대로 실행해
        /// Input 로딩 물류를 수동으로 테스트한다. requestedSlotIndex가 0 이상이면 지정 Wafer를 로딩한다.
        /// </summary>
        public Task<int> RunManualInputLoadAsync(CassetteMaterialRole requestedRole, int requestedSlotIndex)
        {
            string label = requestedSlotIndex >= 0
                ? "INPUT LOAD(" + requestedRole + "/" + (requestedSlotIndex + 1).ToString("00") + ")"
                : "INPUT LOAD";

            return RunManualUnitProcessAsync(
                label,
                "SEQ-MANUAL-IN-LOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    if (!TrySynchronizeInputCassetteSlotProjection(
                        "ManualInputLoad",
                        "SEQ-MANUAL-IN-LOAD-SLOT-SYNC"))
                    {
                        SetStatus(EquipmentStatus.Alarm);
                        return -1;
                    }

                    var sequence = new QMC.CDT320.Sequencing.InputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteAutoStepLoadingForTestAsync(token, requestedRole, requestedSlotIndex)
                        .ConfigureAwait(false);
                });
        }

        public Task<int> RunManualInputUnloadAsync()
        {
            // CYCLE RUN INPUT UNLOAD: Auto 사이클과 동일한 판정(잔류 wafer 카세트 복귀 재개 포함)으로
            // Input 언로딩 물류를 수동으로 테스트한다.
            return RunManualUnitProcessAsync(
                "INPUT UNLOAD",
                "SEQ-MANUAL-IN-UNLOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    var sequence = new QMC.CDT320.Sequencing.InputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteAutoStepUnloadingForTestAsync(token)
                        .ConfigureAwait(false);
                });
        }

        // CYCLE RUN OUTPUT LOAD: 선택한 GOOD/NG side만 별개로 로딩한다.
        public Task<int> RunManualOutputLoadAsync(BinSide side)
        {
            // slot이 음수이면 자동 순번 공급이므로 role은 사용되지 않는다.
            return RunManualOutputLoadAsync(side, CassetteMaterialRole.Good1, -1);
        }

        /// <summary>
        /// CYCLE RUN OUTPUT LOAD. requestedSlotIndex가 0 이상이면 작업자가 지정한 Bin을 공급한다.
        /// </summary>
        public Task<int> RunManualOutputLoadAsync(BinSide side, CassetteMaterialRole requestedRole, int requestedSlotIndex)
        {
            string label = requestedSlotIndex >= 0
                ? "OUTPUT LOAD(" + side + " " + requestedRole + "/" + (requestedSlotIndex + 1).ToString("00") + ")"
                : "OUTPUT LOAD(" + side + ")";
            return RunManualUnitProcessAsync(
                label,
                "SEQ-MANUAL-OUT-LOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    var sequence = new QMC.CDT320.Sequencing.OutputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteManualOutputLoadAsync(token, side, requestedRole, requestedSlotIndex)
                        .ConfigureAwait(false);
                });
        }

        // CYCLE RUN OUTPUT UNLOAD: 선택한 GOOD/NG side의 Stage/Feeder Bin만 별개로 배출한다.
        public Task<int> RunManualOutputUnloadAsync(BinSide side)
        {
            return RunManualUnitProcessAsync(
                "OUTPUT UNLOAD(" + side + ")",
                "SEQ-MANUAL-OUT-UNLOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    var sequence = new QMC.CDT320.Sequencing.OutputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteManualOutputUnloadAsync(token, side)
                        .ConfigureAwait(false);
                });
        }

        private async Task<int> RunManualUnitProcessAsync(
            string processLabel,
            string alarmCodePrefix,
            Func<QMC.CDT320.Sequencing.MachineSequenceContext, CancellationToken, Task<int>> action,
            Func<string> failureDetail = null)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 " + processLabel + " Manual 공정을 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 " + processLabel + " Manual 공정을 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 " + processLabel + " Manual 공정을 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (_status != EquipmentStatus.Ready)
                {
                    LastActionFailureMessage = processLabel +
                        " Manual 공정은 READY 완료 후에만 시작할 수 있습니다. currentStatus=" + _status;
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        alarmCodePrefix + "-READY-REQUIRED",
                        "MachineController",
                        LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualUnitProcessAsync:" + processLabel))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualUnitProcess:" + processLabel))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);

                    int result = await action(context, ManualOperationToken).ConfigureAwait(false);
                    if (result != 0)
                    {
                        LastActionFailureMessage = ResolveManualProcessFailure(
                            processLabel + " Manual 공정 실패. result=" + result, failureDetail);
                        if (_status != EquipmentStatus.Alarm)
                            SetStatus(EquipmentStatus.Stopped);
                        return result;
                    }

                    SaveMachineRuntimeState("ManualUnitProcess:" + processLabel);

                    // [사용자 확정 2026-08-17] 수동 LOAD/UNLOAD 완료 시 자재 상태를 확정 저장한다.
                    // 기존 조건: 물류 시퀀스는 NotifyAndSave(디바운스 quiet 1s / 최소 간격 5s)만 걸었다.
                    //   수동 언로드 직후 5초 안에 앱이 종료되면 슬롯 포인터와 Wafer 레코드 중 한쪽만
                    //   파일에 남아, 다음 기동 후 Cassette DATA CLEAR 사전검사가 차단되는 원인이 됐다.
                    // 현재 기준: 작업자가 "이 시점 상태가 저장되었다"고 기대하는 지점이므로 즉시 flush한다.
                    //   저장 실패는 공정 결과(0)를 뒤집지 않고 경고로만 올린다 — 물류는 이미 끝났다.
                    if (!MaterialStateService.TryFlushPendingSave("ManualUnitProcess:" + processLabel))
                    {
                        string flushFailure =
                            processLabel + " Manual 공정은 완료했지만 자재 상태를 저장 파일에 확정하지 못했습니다. " +
                            "reason=" + MaterialSnapshotStore.LastSaveFailureReason;
                        AlarmManager.Raise(
                            AlarmSeverity.Warning,
                            alarmCodePrefix + "-MATERIAL-SAVE",
                            "MachineController",
                            flushFailure);
                    }

                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = ResolveManualProcessFailure(
                    processLabel + " Manual 공정이 취소되었습니다.", failureDetail);
                if (_status != EquipmentStatus.Alarm)
                    SetStatus(EquipmentStatus.Stopped);
                return -1;
            }
            catch (QMC.CDT320.Sequencing.SequenceStopException ex)
            {
                LastActionFailureMessage = ResolveManualProcessFailure(
                    processLabel + " Manual 공정이 정지되었습니다. " + ex.Message, failureDetail);
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualUnitProcessStop", LastActionFailureMessage + " - Stopped");
                if (_status != EquipmentStatus.Alarm)
                    SetStatus(QMC.CDT320.Sequencing.SequenceStopException.IsCycleStopMessage(ex.Message)
                        ? EquipmentStatus.CycleStopped : EquipmentStatus.Stopped);
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = ResolveManualProcessFailure(
                    processLabel + " Manual 공정 실행 중 예외가 발생했습니다. " + ex.Message, failureDetail);
                AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Work CYCLE RUN 버튼에서 공정 전체 시퀀스를 1단계 진행합니다.</summary>
        public async Task<int> RunProcessSequenceStepAsync()
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 CYCLE RUN을 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                        "Process sequence step run failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-ALARM", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Process step failed: alarm status is active");
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunProcessSequenceStepAsync"))
                    return -1;

                if (IsSequenceRunning)
                {
                    if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        LastActionFailureMessage = "Auto Sequence 실행 중에는 CYCLE RUN Step을 수행할 수 없습니다.";
                        QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                            "Process sequence step run failed: auto sequence is running. - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-AUTO-RUNNING", "MachineController", LastActionFailureMessage);
                        Log("[SEQ] Process step failed: auto sequence is running");
                        return -1;
                    }

                    ManualStepAll();
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                        "Process sequence step gate released. - Ok");
                    return 0;
                }

                if (_status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto Running 상태에서는 CYCLE RUN Step을 시작할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                        "Process sequence step run failed: equipment auto running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Process step failed: equipment auto running");
                    return -1;
                }

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                int startResult = await StartSequenceAsync(
                    QMC.CDT320.Sequencing.SequenceRunOptions.ProcessStep()).ConfigureAwait(false);
                if (startResult != 0)
                    return startResult;

                ManualStepAll();
                QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                    "Process sequence step mode started and first gate released. - Ok");
                Log("[SEQ] Process step start");
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Process sequence step run failed: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-EX", "MachineController", LastActionFailureMessage);
                Log("[SEQ] Process step failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Work CYCLE RUN 버튼에서 Input 시퀀스를 1단계만 진행합니다.</summary>
        public async Task<int> RunInputSequenceStepAsync()
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 CYCLE RUN을 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                        "Input sequence step run failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-ALARM", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Input step failed: alarm status is active");
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunInputSequenceStepAsync"))
                    return -1;

                if (IsSequenceRunning)
                {
                    if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        LastActionFailureMessage = "Auto Sequence 실행 중에는 CYCLE RUN Step을 수행할 수 없습니다.";
                        QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                            "Input sequence step run failed: auto sequence is running. - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-AUTO-RUNNING", "MachineController", LastActionFailureMessage);
                        Log("[SEQ] Input step failed: auto sequence is running");
                        return -1;
                    }

                    ManualStep(QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader);
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                        "Input sequence step gate released. - Ok");
                    return 0;
                }

                if (_status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto Running 상태에서는 CYCLE RUN Step을 시작할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                        "Input sequence step run failed: equipment auto running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Input step failed: equipment auto running");
                    return -1;
                }

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                int startResult = await StartSingleUnitAsync(
                    QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader,
                    QMC.CDT320.Sequencing.SequenceRunMode.Step).ConfigureAwait(false);
                if (startResult != 0)
                    return startResult;

                ManualStep(QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader);
                QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                    "Input sequence step mode started and first gate released. - Ok");
                Log("[SEQ] Input step start");
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Input sequence step run failed: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-EX", "MachineController", LastActionFailureMessage);
                Log("[SEQ] Input step failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

    }
}
