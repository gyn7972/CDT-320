using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common;

namespace QMC.CDT320.Sequencing
{
    internal delegate bool AutoSequencePickerAvoidCheck(out string reason);

    internal enum AutoSequenceCameraWorkKind
    {
        InputCamera,
        OutputCamera
    }

    internal sealed class AutoSequenceCoordinatorGate
    {
        #region 실행 구성 및 공유 상태

        private const int PickerWorkZonePollIntervalMs = 20;
        // R4/B5: 선행검사 카메라 존 획득 상한. FIFO head가 물리 클리어/CanSet를 무한 대기하면
        // 무언정지가 되므로, 이 시간 초과 시 예외로 전환해 복구 알람으로 처리한다.
        // 기존 조건: 30000ms(30초). 실장비에서 상대 픽커 공정이 길어지는 정상 구간에도
        //   30초를 넘겨 오탐 타임아웃이 발생했다(2026-07-29 Front 선행검사, ticket=70).
        // 현재 기준(사용자 지시 2026-07-29): 300000ms(5분). 무언정지 방지라는 상한의 목적은
        //   유지하되, 정상 대기가 걸리지 않도록 여유를 크게 둔다.
        private const int InputCameraZoneAcquireTimeoutMs = 300000;
        private readonly MachineSequenceContext _context;
        private readonly object _pickerWorkZoneGate = new object();
        private PickerWorkZone _frontWorkZone = PickerWorkZone.Unknown;
        private PickerWorkZone _rearWorkZone = PickerWorkZone.Unknown;
        private string _frontWorkZoneOwner = "";
        private string _rearWorkZoneOwner = "";
        private PickerWorkZone _frontPendingWorkZone = PickerWorkZone.Unknown;
        private PickerWorkZone _rearPendingWorkZone = PickerWorkZone.Unknown;
        private string _frontPendingWorkZoneOwner = "";
        private string _rearPendingWorkZoneOwner = "";
        private string _inputCameraZoneOwner = "";
        private string _outputCameraZoneOwner = "";
        private int _outputLoaderConfiguredForAutoRun;
        private int _frontPickerConfiguredForRun;
        private int _rearPickerConfiguredForRun;
        private int _outputExchangePendingCheckErrorLogged;

        public AutoSequenceCoordinatorGate(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        internal void ConfigureRun(SequenceUnitKind units, SequenceRunMode mode)
        {
            bool autoMode = mode == SequenceRunMode.Auto;
            Volatile.Write(
                ref _outputLoaderConfiguredForAutoRun,
                autoMode && (units & SequenceUnitKind.OutputUnloader) == SequenceUnitKind.OutputUnloader ? 1 : 0);
            Volatile.Write(
                ref _frontPickerConfiguredForRun,
                autoMode && (units & SequenceUnitKind.PickerFront) == SequenceUnitKind.PickerFront ? 1 : 0);
            Volatile.Write(
                ref _rearPickerConfiguredForRun,
                autoMode && (units & SequenceUnitKind.PickerRear) == SequenceUnitKind.PickerRear ? 1 : 0);
            Volatile.Write(ref _outputExchangePendingCheckErrorLogged, 0);
        }

        internal bool IsOutputLoaderConfiguredForAutoRun
        {
            get { return Volatile.Read(ref _outputLoaderConfiguredForAutoRun) != 0; }
        }

        internal bool IsPickerSideConfiguredForRun(PickerSequenceSide side)
        {
            bool configured = side == PickerSequenceSide.Front
                ? Volatile.Read(ref _frontPickerConfiguredForRun) != 0
                : Volatile.Read(ref _rearPickerConfiguredForRun) != 0;
            if (!configured)
                return false;

            if (_context == null || _context.Machine == null)
                return true;

            if (side == PickerSequenceSide.Front)
            {
                return _context.Machine.PickerFrontUnit != null &&
                       _context.Machine.PickerFrontUnit.Config != null &&
                       _context.Machine.PickerFrontUnit.Config.UseUnit;
            }

            return _context.Machine.PickerRearUnit != null &&
                   _context.Machine.PickerRearUnit.Config != null &&
                   _context.Machine.PickerRearUnit.Config.UseUnit;
        }

        #endregion

        #region 작업 진입 및 작업 구역 초기화

        public Task<AutoSequenceLoaderWorkLease> BeginInputWorkAsync(
            string holder,
            CancellationToken ct,
            Func<string, CancellationToken, Task<int>> ensurePickersAvoidAsync,
            AutoSequencePickerAvoidCheck arePickersAvoidAndStopped)
        {
            return BeginLoaderWorkAsync(
                "InputLoader",
                "InputLoaderActive",
                string.IsNullOrWhiteSpace(holder) ? "InputSequence" : holder,
                ct,
                ensurePickersAvoidAsync,
                arePickersAvoidAndStopped);
        }

        public Task<AutoSequenceLoaderWorkLease> BeginOutputWorkAsync(
            string holder,
            CancellationToken ct,
            Func<string, CancellationToken, Task<int>> ensurePickersAvoidAsync,
            AutoSequencePickerAvoidCheck arePickersAvoidAndStopped)
        {
            return BeginLoaderWorkAsync(
                "OutputLoader",
                "OutputLoaderActive",
                string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder,
                ct,
                ensurePickersAvoidAsync,
                arePickersAvoidAndStopped);
        }

        public async Task<SequenceResourceLease> BeginPickerProcessAsync(
            PickerSequenceSide side,
            string holder,
            CancellationToken ct)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder)
                ? (side == PickerSequenceSide.Front ? "FrontPickerSequence:Process" : "RearPickerSequence:Process")
                : holder;
            string gateHolder = "AutoSequenceCoordinator:" + safeHolder;
            SequenceResourceKind resource = side == PickerSequenceSide.Front
                ? SequenceResourceKind.FrontPicker
                : SequenceResourceKind.RearPicker;
            bool resourceWaitLogged = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested(
                    "AutoSequenceCoordinator.PickerProcessGate:" + gateHolder,
                    ShouldDeferCycleStopForPickerDrain(side),
                    "Picker target die drain");

                await WaitLoaderInactiveBeforePickerStartAsync(side, gateHolder, ct).ConfigureAwait(false);

                SequenceResourceLease lease = await TryAcquirePickerGateAsync(
                    resource,
                    gateHolder + ":PickerProcess",
                    ct).ConfigureAwait(false);

                if (lease == null)
                {
                    if (!resourceWaitLogged)
                    {
                        string current = _context.Resources.GetHolder(resource);
                        _context.LogPublic("[SEQ] AutoSequenceCoordinator picker start waiting. resource=" +
                            resource + ", holder=" + gateHolder + ", current=" +
                            (string.IsNullOrWhiteSpace(current) ? "-" : current));
                        resourceWaitLogged = true;
                    }

                    await Task.Delay(20, ct).ConfigureAwait(false);
                    continue;
                }

                if (!IsPickerProcessStartBlocked())
                {
                    Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                        "Picker process approved. side=" + side +
                        ", holder=" + safeHolder + ", resource=" + resource + " - Ok");
                    _context.LogPublic("[SEQ] PickerProcess start approved. side=" + side +
                        ", holder=" + safeHolder);
                    return lease;
                }

                lease.Dispose();
                _context.LogPublic("[SEQ] PickerProcess start recheck blocked by loader active/output exchange pending. side=" +
                    side + ", holder=" + safeHolder +
                    ", loaderActive=" + IsLoaderActive() +
                    ", outputExchangePending=" + IsOutputStageExchangePending());
                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }

        public Task<AutoSequencePickerWorkZoneLease> BeginPickerWorkZoneAsync(
            PickerSequenceSide side,
            PickerWorkZone zone,
            string holder,
            CancellationToken ct)
        {
            return WaitAndSetPickerWorkZoneAsync(null, side, zone, holder, ct);
        }

        public Task<AutoSequencePickerWorkZoneLease> TransitionPickerWorkZoneAsync(
            AutoSequencePickerWorkZoneLease lease,
            PickerWorkZone zone,
            string holder,
            CancellationToken ct)
        {
            if (lease == null || lease.IsDisposed)
                throw new InvalidOperationException("Picker work zone transition requires an active lease.");

            return WaitAndSetPickerWorkZoneAsync(lease, lease.Side, zone, holder, ct);
        }

        public Task<AutoSequenceCameraWorkZoneLease> BeginInputCameraWorkAsync(
            string holder,
            CancellationToken ct,
            PickerSequenceSide? preInspectionSide = null)
        {
            return WaitAndSetCameraWorkZoneAsync(
                AutoSequenceCameraWorkKind.InputCamera,
                PickerWorkZone.Input,
                string.IsNullOrWhiteSpace(holder) ? "InputCamera" : holder,
                ct,
                preInspectionSide);
        }

        public Task<AutoSequenceCameraWorkZoneLease> BeginOutputCameraWorkAsync(
            string holder,
            CancellationToken ct)
        {
            return WaitAndSetCameraWorkZoneAsync(
                AutoSequenceCameraWorkKind.OutputCamera,
                PickerWorkZone.Output,
                string.IsNullOrWhiteSpace(holder) ? "OutputCamera" : holder,
                ct);
        }

        public void ResetPickerWorkZones(string reason)
        {
            lock (_pickerWorkZoneGate)
            {
                _frontWorkZone = PickerWorkZone.Unknown;
                _rearWorkZone = PickerWorkZone.Unknown;
                _frontWorkZoneOwner = "";
                _rearWorkZoneOwner = "";
                _frontPendingWorkZone = PickerWorkZone.Unknown;
                _rearPendingWorkZone = PickerWorkZone.Unknown;
                _frontPendingWorkZoneOwner = "";
                _rearPendingWorkZoneOwner = "";
                _inputCameraZoneOwner = "";
                _outputCameraZoneOwner = "";
            }

            Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                "Picker work zones reset. reason=" + (reason ?? "-") + " - Reset");
            if (_context != null)
                _context.LogPublic("[SEQ] Picker work zones reset. reason=" + (reason ?? "-"));
        }

        #endregion

        #region Cycle Stop Drain 판정

        private bool ShouldDeferCycleStopForOutputCameraDrain(AutoSequenceCameraWorkKind kind)
        {
            if (kind != AutoSequenceCameraWorkKind.OutputCamera)
                return false;
            if (_context == null || !_context.IsCycleStopRequested)
                return false;

            return !IsControllerAlarm();
        }

        private bool ShouldDeferCycleStopForPickerDrain(PickerSequenceSide side)
        {
            if (!IsPickerSideConfiguredForRun(side))
                return false;

            if (_context == null || !_context.IsCycleStopRequested)
                return false;
            if (IsControllerAlarm())
                return false;

            MaterialLocationKind location = side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                if (die != null && die.IsInputTarget)
                    return true;
            }

            return false;
        }

        private bool ShouldDeferCycleStopForAnyPickerDrain()
        {
            return ShouldDeferCycleStopForPickerDrain(PickerSequenceSide.Front) ||
                   ShouldDeferCycleStopForPickerDrain(PickerSequenceSide.Rear);
        }

        private bool IsControllerAlarm()
        {
            try
            {
                return _context != null &&
                       _context.Controller != null &&
                       _context.Controller.Status == EquipmentStatus.Alarm;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Camera·Loader·Picker 작업 구역 대기

        private async Task<AutoSequenceCameraWorkZoneLease> WaitAndSetCameraWorkZoneAsync(
            AutoSequenceCameraWorkKind kind,
            PickerWorkZone zone,
            string holder,
            CancellationToken ct,
            PickerSequenceSide? preInspectionSide = null)
        {
            PickerWorkZone safeZone = PickerZoneInterlockRules.NormalizeInterlockZone(zone);
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? kind.ToString() : holder;
            if (safeZone == PickerWorkZone.Unknown || safeZone == PickerWorkZone.Avoid)
                return new AutoSequenceCameraWorkZoneLease(this, kind, PickerWorkZone.Unknown, safeHolder, false);

            bool waitLogged = false;
            bool fifoWaitLogged = false;
            DateTime acquireStart = DateTime.UtcNow;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested(
                    "AutoSequenceCoordinator.CameraWorkZone:" + kind + ":" + safeZone,
                    ShouldDeferCycleStopForOutputCameraDrain(kind),
                    "Output post-place inspection drain");

                // R4/B5: 선행검사 카메라 존 획득 루프 전체에 bounded timeout. FIFO head가 되어도
                // 물리 클리어/CanSet를 무제한 대기하면 무언정지가 되므로, 경과시간 초과 시 예외로
                // 전환해 호출자(AcquireInputCameraWorkZoneAsync)가 Fail→선행검사 Task 종료→티켓
                // 반납→복구 알람으로 처리한다. OutputCamera/수동 리뷰(preInspectionSide=null)는
                // 기존 무한 대기 동작을 유지한다.
                if (preInspectionSide.HasValue &&
                    (DateTime.UtcNow - acquireStart).TotalMilliseconds >= InputCameraZoneAcquireTimeoutMs)
                {
                    throw new TimeoutException(
                        "InputCamera 선행검사 카메라 존 획득이 제한 시간을 초과했습니다. side=" + preInspectionSide.Value +
                        ", holder=" + safeHolder +
                        ", timeoutMs=" + InputCameraZoneAcquireTimeoutMs +
                        ", queue=" + InputEntryQueue.Describe());
                }

                // 진짜 FIFO 데드락 절단(원자): InputCamera 선행검사가 카메라 존을 '승인받는 매 폴링'
                // 마다 전역 진입 큐의 head(최소 티켓)인지 재검사한다. head가 아니면 존을 잡지 않고
                // 계속 양보한다. head는 정확히 1개뿐이라 두 선행검사가 서로 양보하는 라이브락이
                // 구조적으로 불가능하다. IsHead는 InputEntryQueue 자체 lock만 잡으므로
                // _pickerWorkZoneGate lock '밖'에서 호출해 중첩 lock을 피한다.
                if (preInspectionSide.HasValue)
                {
                    string headDetail;
                    if (!InputEntryQueue.IsHead(preInspectionSide.Value, out headDetail))
                    {
                        if (!fifoWaitLogged)
                        {
                            Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                                "InputCamera 선행검사 카메라 존 획득을 FIFO 순번 양보합니다. 앞선 진입 티켓이 있어 존을 잡지 않고 대기합니다. " +
                                "holder=" + safeHolder + ", side=" + preInspectionSide.Value +
                                ", " + headDetail + " - Wait");
                            fifoWaitLogged = true;
                        }

                        await Task.Delay(PickerWorkZonePollIntervalMs, ct).ConfigureAwait(false);
                        continue;
                    }
                }

                string reason;
                if (ArePickersPhysicallyClearForCameraZone(safeZone, kind, safeHolder, out reason))
                {
                    lock (_pickerWorkZoneGate)
                    {
                        if (CanSetCameraWorkZoneNoLock(kind, safeZone, safeHolder, out reason))
                        {
                            SetCameraWorkZoneNoLock(kind, safeHolder);
                            LogCameraWorkZoneApproved(kind, safeZone, safeHolder);
                            return new AutoSequenceCameraWorkZoneLease(this, kind, safeZone, safeHolder, true);
                        }
                    }
                }

                if (!waitLogged)
                {
                    _context.LogPublic("[SEQ] AutoSequenceCoordinator camera work zone waiting. kind=" +
                        kind + ", zone=" + safeZone + ", holder=" + safeHolder + ", reason=" + reason);
                    Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                        "Camera work zone waiting. kind=" + kind +
                        ", zone=" + safeZone +
                        ", holder=" + safeHolder +
                        ", reason=" + reason + " - Wait");
                    waitLogged = true;
                }

                await Task.Delay(PickerWorkZonePollIntervalMs, ct).ConfigureAwait(false);
            }
        }

        private async Task<AutoSequenceLoaderWorkLease> BeginLoaderWorkAsync(
            string loaderName,
            string activeSignal,
            string holder,
            CancellationToken ct,
            Func<string, CancellationToken, Task<int>> ensurePickersAvoidAsync,
            AutoSequencePickerAvoidCheck arePickersAvoidAndStopped)
        {
            string gateHolder = "AutoSequenceCoordinator:" + holder;
            AutoSequencePickerGateLeases pickerLeases = null;
            bool activeSet = false;

            try
            {
                _context.LogPublic("[SEQ] " + loaderName + " start approval waiting. holder=" + holder);

                if (ensurePickersAvoidAsync != null)
                {
                    int result = await ensurePickersAvoidAsync(holder, ct).ConfigureAwait(false);
                    if (result != 0)
                        throw new InvalidOperationException(loaderName + " start failed: picker avoid check failed. result=" + result);
                }

                string reason;
                if (arePickersAvoidAndStopped != null && !arePickersAvoidAndStopped(out reason))
                    throw new InvalidOperationException(loaderName + " start denied: picker is not avoid/stopped. " + reason);

                bool deferCycleStopForPickerDrain = string.Equals(
                    loaderName,
                    "OutputLoader",
                    StringComparison.Ordinal);
                pickerLeases = await AcquireBothPickerStartGatesAsync(
                    gateHolder,
                    deferCycleStopForPickerDrain,
                    ct).ConfigureAwait(false);

                if (arePickersAvoidAndStopped != null && !arePickersAvoidAndStopped(out reason))
                    throw new InvalidOperationException(loaderName + " start denied after gate acquire: picker is not avoid/stopped. " + reason);

                _context.Bus.Set(activeSignal);
                activeSet = true;
                Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                    loaderName + " work approved. holder=" + holder + ", signal=" + activeSignal + " - Set");
                _context.LogPublic("[SEQ] " + loaderName + " start approved. holder=" + holder);

                return new AutoSequenceLoaderWorkLease(_context, loaderName, activeSignal, holder, activeSet);
            }
            catch (OperationCanceledException)
            {
                if (activeSet)
                    _context.Bus.Reset(activeSignal);
                throw;
            }
            catch
            {
                if (activeSet)
                    _context.Bus.Reset(activeSignal);
                throw;
            }
            finally
            {
                if (pickerLeases != null)
                    pickerLeases.Dispose();
            }
        }

        private async Task<AutoSequencePickerWorkZoneLease> WaitAndSetPickerWorkZoneAsync(
            AutoSequencePickerWorkZoneLease lease,
            PickerSequenceSide side,
            PickerWorkZone zone,
            string holder,
            CancellationToken ct)
        {
            PickerWorkZone safeZone = PickerZoneInterlockRules.NormalizeInterlockZone(zone);
            string safeHolder = string.IsNullOrWhiteSpace(holder)
                ? (side == PickerSequenceSide.Front ? "FrontPicker" : "RearPicker")
                : holder;

            if (safeZone == PickerWorkZone.Unknown || safeZone == PickerWorkZone.Avoid)
            {
                if (lease != null && !lease.IsDisposed)
                    ReleasePickerWorkZone(lease);
                return new AutoSequencePickerWorkZoneLease(this, side, PickerWorkZone.Unknown, safeHolder, false);
            }

            lock (_pickerWorkZoneGate)
            {
                SetPickerPendingWorkZoneNoLock(side, safeZone, safeHolder);
            }

            try
            {
                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    _context.StopIfCycleStopRequested(
                        "AutoSequenceCoordinator.PickerWorkZone:" + safeHolder + ":" + safeZone,
                        ShouldDeferCycleStopForPickerDrain(side),
                        "Picker target die drain");

                    string reason;
                    lock (_pickerWorkZoneGate)
                    {
                        if (CanSetPickerWorkZoneNoLock(side, safeZone, safeHolder, out reason))
                        {
                            SetPickerWorkZoneNoLock(side, safeZone, safeHolder);
                            if (lease != null && !lease.IsDisposed)
                            {
                                lease.Update(safeZone, safeHolder, true);
                                LogPickerWorkZoneApproved(side, safeZone, safeHolder, true);
                                return lease;
                            }

                            AutoSequencePickerWorkZoneLease newLease =
                                new AutoSequencePickerWorkZoneLease(this, side, safeZone, safeHolder, true);
                            LogPickerWorkZoneApproved(side, safeZone, safeHolder, false);
                            return newLease;
                        }
                    }

                    if (!waitLogged)
                    {
                        _context.LogPublic("[SEQ] AutoSequenceCoordinator picker work zone waiting. side=" +
                            side + ", zone=" + safeZone + ", holder=" + safeHolder + ", reason=" + reason);
                        Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                            "Picker work zone waiting. side=" + side +
                            ", zone=" + safeZone +
                            ", holder=" + safeHolder +
                            ", reason=" + reason + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(PickerWorkZonePollIntervalMs, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                lock (_pickerWorkZoneGate)
                {
                    ClearPickerPendingWorkZoneNoLock(side, safeZone, safeHolder);
                }
            }
        }

        #endregion

        #region 작업 구역 진입 조건 판정

        private bool CanSetPickerWorkZoneNoLock(
            PickerSequenceSide side,
            PickerWorkZone zone,
            string holder,
            out string reason)
        {
            reason = string.Empty;
            PickerWorkZone oppositeZone = side == PickerSequenceSide.Front ? _rearWorkZone : _frontWorkZone;
            string oppositeOwner = side == PickerSequenceSide.Front ? _rearWorkZoneOwner : _frontWorkZoneOwner;

            if (side == PickerSequenceSide.Rear &&
                PickerZoneInterlockRules.IsSameInterlockZone(zone, _frontPendingWorkZone))
            {
                reason = "front picker pending same work zone has priority. request=" +
                    zone + ", frontPending=" + _frontPendingWorkZone + ", owner=" +
                    (string.IsNullOrWhiteSpace(_frontPendingWorkZoneOwner) ? "-" : _frontPendingWorkZoneOwner);
                return false;
            }

            if (oppositeZone == PickerWorkZone.Unknown || oppositeZone == PickerWorkZone.Avoid)
            {
                string cameraOwner;
                if (IsCameraWorkZoneOccupiedNoLock(zone, out cameraOwner))
                {
                    reason = "camera is already using same work zone. request=" +
                        zone + ", cameraOwner=" + cameraOwner;
                    return false;
                }

                return true;
            }

            if (PickerZoneInterlockRules.IsSameInterlockZone(zone, oppositeZone))
            {
                reason = "opposite picker is already using same work zone. request=" +
                    zone + ", opposite=" + oppositeZone + ", owner=" +
                    (string.IsNullOrWhiteSpace(oppositeOwner) ? "-" : oppositeOwner);
                return false;
            }

            string activeCameraOwner;
            if (IsCameraWorkZoneOccupiedNoLock(zone, out activeCameraOwner))
            {
                reason = "camera is already using same work zone. request=" +
                    zone + ", cameraOwner=" + activeCameraOwner;
                return false;
            }

            return true;
        }

        private bool CanSetCameraWorkZoneNoLock(
            AutoSequenceCameraWorkKind kind,
            PickerWorkZone zone,
            string holder,
            out string reason)
        {
            reason = string.Empty;

            string ownOwner = GetCameraWorkZoneOwnerNoLock(kind);
            if (!string.IsNullOrWhiteSpace(ownOwner))
            {
                reason = kind + " work zone is already active. owner=" + ownOwner;
                return false;
            }

            PickerWorkZone normalizedZone = PickerZoneInterlockRules.NormalizeInterlockZone(zone);
            if (PickerZoneInterlockRules.IsSameInterlockZone(_frontWorkZone, normalizedZone))
            {
                reason = "front picker is using same work zone. zone=" + normalizedZone +
                    ", owner=" + (string.IsNullOrWhiteSpace(_frontWorkZoneOwner) ? "-" : _frontWorkZoneOwner);
                return false;
            }

            if (PickerZoneInterlockRules.IsSameInterlockZone(_rearWorkZone, normalizedZone))
            {
                reason = "rear picker is using same work zone. zone=" + normalizedZone +
                    ", owner=" + (string.IsNullOrWhiteSpace(_rearWorkZoneOwner) ? "-" : _rearWorkZoneOwner);
                return false;
            }

            string cameraOwner;
            if (IsCameraWorkZoneOccupiedNoLock(normalizedZone, out cameraOwner))
            {
                reason = "another camera is using same work zone. zone=" + normalizedZone +
                    ", owner=" + cameraOwner;
                return false;
            }

            return true;
        }

        private bool ArePickersPhysicallyClearForCameraZone(
            PickerWorkZone zone,
            AutoSequenceCameraWorkKind kind,
            string holder,
            out string reason)
        {
            reason = string.Empty;

            string frontDetail;
            bool frontBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                _context.Machine,
                true,
                zone,
                out frontDetail);

            string rearDetail;
            bool rearBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                _context.Machine,
                false,
                zone,
                out rearDetail);

            if (!frontBlocking && !rearBlocking)
                return true;

            // 현재 기준(사용자 확정 동시성 매트릭스 2026-07-26, 5번 "Place 검사 및 복귀 시"):
            // 후검사(BIN) 워커의 OutputCamera 존 승인은 "픽커 물리 완전 클리어"를 요구하지 않는다 —
            // 차단 중인 픽커의 Output 존 lease가 이미 반환된 상태(=Place 완료, 물리 퇴장 중)라면
            // 존을 승인해 비전이 퇴장 픽커를 return-follow로 추종 진입할 수 있게 한다.
            // 물리 안전은 무변경 유지: 이동 명령마다 -11 규칙의 제3분기(페어 간격, fail-closed)와
            // 워커의 팔로잉/공유레일 클리어 대기가 재검증한다. 진입 중(lease 보유) 픽커가 있으면
            // 기존대로 대기한다 — Place↔BIN 상호 배타는 매트릭스대로 유지.
            if (kind == AutoSequenceCameraWorkKind.OutputCamera &&
                !string.IsNullOrWhiteSpace(holder) &&
                holder.IndexOf("OutputPostPlaceInspection", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bool frontZoneReleased;
                bool rearZoneReleased;
                lock (_pickerWorkZoneGate)
                {
                    frontZoneReleased = _frontWorkZone != PickerWorkZone.Output &&
                                        _frontPendingWorkZone != PickerWorkZone.Output;
                    rearZoneReleased = _rearWorkZone != PickerWorkZone.Output &&
                                       _rearPendingWorkZone != PickerWorkZone.Output;
                }

                bool frontExitOnly = !frontBlocking || frontZoneReleased;
                bool rearExitOnly = !rearBlocking || rearZoneReleased;
                if (frontExitOnly && rearExitOnly)
                {
                    Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                        "OutputCamera 존을 픽커 퇴장 중 조기 승인합니다(복귀 동시 후검사 — return-follow 진입 허용). " +
                        "holder=" + holder +
                        ", frontBlocking=" + frontBlocking + "(zoneReleased=" + frontZoneReleased + ")" +
                        ", rearBlocking=" + rearBlocking + "(zoneReleased=" + rearZoneReleased + ") - Ok");
                    return true;
                }
            }

            reason = "picker physical zone is not clear for camera. kind=" + kind +
                ", zone=" + zone +
                ", holder=" + holder +
                ", frontBlocking=" + frontBlocking +
                ", front=" + frontDetail +
                ", rearBlocking=" + rearBlocking +
                ", rear=" + rearDetail;
            return false;
        }

        #endregion

        #region 작업 구역 상태 및 Lease 해제

        private bool IsCameraWorkZoneOccupiedNoLock(PickerWorkZone zone, out string owner)
        {
            PickerWorkZone normalizedZone = PickerZoneInterlockRules.NormalizeInterlockZone(zone);
            if (normalizedZone == PickerWorkZone.Input && !string.IsNullOrWhiteSpace(_inputCameraZoneOwner))
            {
                owner = _inputCameraZoneOwner;
                return true;
            }

            if (normalizedZone == PickerWorkZone.Output && !string.IsNullOrWhiteSpace(_outputCameraZoneOwner))
            {
                owner = _outputCameraZoneOwner;
                return true;
            }

            owner = string.Empty;
            return false;
        }

        private string GetCameraWorkZoneOwnerNoLock(AutoSequenceCameraWorkKind kind)
        {
            switch (kind)
            {
                case AutoSequenceCameraWorkKind.InputCamera:
                    return _inputCameraZoneOwner;
                case AutoSequenceCameraWorkKind.OutputCamera:
                    return _outputCameraZoneOwner;
                default:
                    return string.Empty;
            }
        }

        private void SetCameraWorkZoneNoLock(AutoSequenceCameraWorkKind kind, string holder)
        {
            if (kind == AutoSequenceCameraWorkKind.InputCamera)
                _inputCameraZoneOwner = holder ?? "";
            else if (kind == AutoSequenceCameraWorkKind.OutputCamera)
                _outputCameraZoneOwner = holder ?? "";
        }

        private void SetPickerPendingWorkZoneNoLock(PickerSequenceSide side, PickerWorkZone zone, string holder)
        {
            if (side == PickerSequenceSide.Front)
            {
                _frontPendingWorkZone = zone;
                _frontPendingWorkZoneOwner = holder ?? "";
            }
            else
            {
                _rearPendingWorkZone = zone;
                _rearPendingWorkZoneOwner = holder ?? "";
            }
        }

        private void ClearPickerPendingWorkZoneNoLock(PickerSequenceSide side, PickerWorkZone zone, string holder)
        {
            if (side == PickerSequenceSide.Front)
            {
                if (_frontPendingWorkZone == zone &&
                    string.Equals(_frontPendingWorkZoneOwner, holder ?? "", StringComparison.Ordinal))
                {
                    _frontPendingWorkZone = PickerWorkZone.Unknown;
                    _frontPendingWorkZoneOwner = "";
                }
            }
            else
            {
                if (_rearPendingWorkZone == zone &&
                    string.Equals(_rearPendingWorkZoneOwner, holder ?? "", StringComparison.Ordinal))
                {
                    _rearPendingWorkZone = PickerWorkZone.Unknown;
                    _rearPendingWorkZoneOwner = "";
                }
            }
        }

        private void SetPickerWorkZoneNoLock(PickerSequenceSide side, PickerWorkZone zone, string holder)
        {
            if (side == PickerSequenceSide.Front)
            {
                _frontWorkZone = zone;
                _frontWorkZoneOwner = holder ?? "";
            }
            else
            {
                _rearWorkZone = zone;
                _rearWorkZoneOwner = holder ?? "";
            }
        }

        internal void ReleasePickerWorkZone(AutoSequencePickerWorkZoneLease lease)
        {
            if (lease == null)
                return;

            lock (_pickerWorkZoneGate)
            {
                if (lease.Side == PickerSequenceSide.Front)
                {
                    if (_frontWorkZone == lease.Zone &&
                        string.Equals(_frontWorkZoneOwner, lease.Owner, StringComparison.Ordinal))
                    {
                        _frontWorkZone = PickerWorkZone.Unknown;
                        _frontWorkZoneOwner = "";
                    }
                }
                else
                {
                    if (_rearWorkZone == lease.Zone &&
                        string.Equals(_rearWorkZoneOwner, lease.Owner, StringComparison.Ordinal))
                    {
                        _rearWorkZone = PickerWorkZone.Unknown;
                        _rearWorkZoneOwner = "";
                    }
                }
            }

            Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                "Picker work zone released. side=" + lease.Side +
                ", zone=" + lease.Zone +
                ", holder=" + lease.Owner + " - Reset");
            _context.LogPublic("[SEQ] Picker work zone released. side=" + lease.Side +
                ", zone=" + lease.Zone + ", holder=" + lease.Owner);
        }

        internal void ReleaseCameraWorkZone(AutoSequenceCameraWorkZoneLease lease)
        {
            if (lease == null)
                return;

            lock (_pickerWorkZoneGate)
            {
                if (lease.Kind == AutoSequenceCameraWorkKind.InputCamera)
                {
                    if (string.Equals(_inputCameraZoneOwner, lease.Owner, StringComparison.Ordinal))
                        _inputCameraZoneOwner = "";
                }
                else if (lease.Kind == AutoSequenceCameraWorkKind.OutputCamera)
                {
                    if (string.Equals(_outputCameraZoneOwner, lease.Owner, StringComparison.Ordinal))
                        _outputCameraZoneOwner = "";
                }
            }

            Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                "Camera work zone released. kind=" + lease.Kind +
                ", zone=" + lease.Zone +
                ", holder=" + lease.Owner + " - Reset");
            _context.LogPublic("[SEQ] Camera work zone released. kind=" + lease.Kind +
                ", zone=" + lease.Zone + ", holder=" + lease.Owner);
        }

        private void LogPickerWorkZoneApproved(
            PickerSequenceSide side,
            PickerWorkZone zone,
            string holder,
            bool transition)
        {
            Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                "Picker work zone approved. side=" + side +
                ", zone=" + zone +
                ", holder=" + holder +
                ", transition=" + transition + " - Ok");
            _context.LogPublic("[SEQ] Picker work zone approved. side=" + side +
                ", zone=" + zone + ", holder=" + holder);
        }

        private void LogCameraWorkZoneApproved(
            AutoSequenceCameraWorkKind kind,
            PickerWorkZone zone,
            string holder)
        {
            Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                "Camera work zone approved. kind=" + kind +
                ", zone=" + zone +
                ", holder=" + holder + " - Ok");
            _context.LogPublic("[SEQ] Camera work zone approved. kind=" + kind +
                ", zone=" + zone + ", holder=" + holder);
        }

        #endregion

        #region Picker 시작 게이트 점유

        private async Task<AutoSequencePickerGateLeases> AcquireBothPickerStartGatesAsync(
            string holder,
            bool deferCycleStopForPickerDrain,
            CancellationToken ct)
        {
            bool waitLogged = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested(
                    "AutoSequenceCoordinator.LoaderGate:" + holder,
                    deferCycleStopForPickerDrain && ShouldDeferCycleStopForAnyPickerDrain(),
                    "Picker-held die output exchange drain");

                SequenceResourceLease frontLease = null;
                SequenceResourceLease rearLease = null;

                try
                {
                    frontLease = await TryAcquirePickerGateAsync(
                        SequenceResourceKind.FrontPicker,
                        holder + ":PickerStartGate",
                        ct).ConfigureAwait(false);
                    if (frontLease != null)
                    {
                        rearLease = await TryAcquirePickerGateAsync(
                            SequenceResourceKind.RearPicker,
                            holder + ":PickerStartGate",
                            ct).ConfigureAwait(false);
                        if (rearLease != null)
                            return new AutoSequencePickerGateLeases(frontLease, rearLease);
                    }
                }
                finally
                {
                    if (rearLease == null && frontLease != null)
                        frontLease.Dispose();
                }

                if (!waitLogged)
                {
                    _context.LogPublic("[SEQ] AutoSequenceCoordinator loader start waiting for both pickers. holder=" +
                        holder + ", front=" + FormatHolder(SequenceResourceKind.FrontPicker) +
                        ", rear=" + FormatHolder(SequenceResourceKind.RearPicker));
                    waitLogged = true;
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }

        private async Task<SequenceResourceLease> TryAcquirePickerGateAsync(
            SequenceResourceKind resource,
            string holder,
            CancellationToken ct)
        {
            return await _context.Resources
                .AcquireAsync(resource, holder, 1, ct, false)
                .ConfigureAwait(false);
        }

        private async Task WaitLoaderInactiveBeforePickerStartAsync(
            PickerSequenceSide side,
            string holder,
            CancellationToken ct)
        {
            bool waitLogged = false;
            // [무언정지 방지 2026-07-27] 이 루프는 timeout이 없어 조건이 영원히 참이면 알람 없이 교착된다.
            // 실제로 NG 미사용 설정에서 IsOutputStageExchangePending()이 항상 참이 되어
            // Picker가 시작조차 못 하고 전 유닛이 조용히 멈췄다. 아래 LogPublic은 Release(ProductionMinimal)
            // 에서 저장되지 않아 로그에 흔적조차 남지 않았다.
            // 따라서 일정 시간 이상 막히면 LogPolicy를 우회하는 경로로 주기 경고를 남긴다.
            const int BlockedWarnAfterMs = 5000;
            const int BlockedWarnIntervalMs = 10000;
            var blockedWatch = System.Diagnostics.Stopwatch.StartNew();
            long nextWarnMs = BlockedWarnAfterMs;

            while (IsPickerProcessStartBlocked())
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested(
                    "AutoSequenceCoordinator.PickerWaitLoaderInactive:" + holder,
                    ShouldDeferCycleStopForPickerDrain(side),
                    "Picker target die output exchange drain");

                if (!waitLogged)
                {
                    _context.LogPublic("[SEQ] AutoSequenceCoordinator picker start waiting for loader inactive. holder=" +
                        holder + ", inputActive=" + IsSignalSet("InputLoaderActive") +
                        ", outputActive=" + IsSignalSet("OutputLoaderActive") +
                        ", outputExchangePending=" + IsOutputStageExchangePending());
                    waitLogged = true;
                }

                if (blockedWatch.ElapsedMilliseconds >= nextWarnMs)
                {
                    nextWarnMs = blockedWatch.ElapsedMilliseconds + BlockedWarnIntervalMs;
                    // LogLevel 오버로드는 LogPolicy를 우회해 Release에서도 디스크에 남는다.
                    Log.Write(LogLevel.AboveNormal, "Main", "AutoSequenceCoordinator",
                        "Picker 시작이 " + (blockedWatch.ElapsedMilliseconds / 1000) + "초째 막혀 있습니다. " +
                        "holder=" + holder +
                        ", inputActive=" + IsSignalSet("InputLoaderActive") +
                        ", outputActive=" + IsSignalSet("OutputLoaderActive") +
                        ", outputExchangePending=" + IsOutputStageExchangePending() +
                        ", ngUsed=" + IsNgCassetteUsed() +
                        ", feederOccupied=" + (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null) +
                        ", goodStagePresent=" + IsOutputStageMaterialPresent(BinSide.Good) +
                        ", ngStagePresent=" + IsOutputStageMaterialPresent(BinSide.Ng) + " - Check");
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }

        #endregion

        #region 시작 차단 및 장비 상태 판정

        private bool IsPickerProcessStartBlocked()
        {
            return IsLoaderActive() || IsOutputStageExchangePending();
        }

        private bool IsOutputStageExchangePending()
        {
            if (!IsOutputLoaderConfiguredForAutoRun)
                return false;

            try
            {
                // Full뿐 아니라 Stage -> Feeder -> Cassette -> 새 Stage 공급 중간 재시작도
                // Loader가 양쪽 Picker gate를 먼저 확보해야 한다. Ready 신호만으로는 물리 자세를
                // 증명할 수 없으므로, 여기서는 영속 Material의 Feeder 점유/Stage 누락만 사용한다.
                bool feederOccupied = MaterialStateService.GetWaferAtLocation(
                    MaterialLocationKind.OutputFeeder) != null;
                if (IsStopAfterDrainPickerCapacityRequired())
                {
                    // 현재 Picker Place 정책은 모든 보유 Die를 GOOD Stage로 배출한다.
                    // Drain 중에는 GOOD 용량 확보와 이미 시작된 Feeder 이송만 우선하고,
                    // 반대 side Full 때문에 보유 Die 배출 자체를 막지 않는다.
                    return feederOccupied ||
                           IsSignalSet("OutputGoodStageReceiveComplete") ||
                           MaterialStateService.IsOutputStageReceiveComplete(BinSide.Good) ||
                           !IsOutputStageMaterialPresent(BinSide.Good);
                }

                // [NG 스킵 2026-07-27] NG 미사용(UseNgCassette=false)이면 NG Stage는 영원히 공급되지 않는다.
                // NG 조건을 그대로 두면 아래 판정이 항상 true가 되어 Picker가 절대 시작하지 못하고,
                // Input(WaitInputStageDieComplete) / Output(WaitReceiveComplete)은 그 Picker를 기다려
                // 알람 없이 교착된다(2026-07-27 현장 무언정지). NG 사용 시에만 NG를 요구한다.
                bool ngUsed = IsNgCassetteUsed();

                if (IsSignalSet("OutputGoodStageReceiveComplete") ||
                    (ngUsed && IsSignalSet("OutputNgStageReceiveComplete")) ||
                    MaterialStateService.IsOutputStageReceiveComplete(BinSide.Good) ||
                    (ngUsed && MaterialStateService.IsOutputStageReceiveComplete(BinSide.Ng)))
                {
                    return true;
                }

                return feederOccupied ||
                       !IsOutputStageMaterialPresent(BinSide.Good) ||
                       (ngUsed && !IsOutputStageMaterialPresent(BinSide.Ng));
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref _outputExchangePendingCheckErrorLogged, 1) == 0)
                {
                    Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                        "Output exchange pending check failed; picker start remains blocked. error=" +
                        ex.Message + " - Check");
                }
                return true;
            }
        }

        /// <summary>
        /// NG 카세트 사용 여부. OutputSequence.IsNgCassetteUsed()와 같은 기준(Config.UseNgCassette)을 쓴다.
        /// [NG 스킵 2026-07-27] 설정을 못 읽으면 "사용"으로 보아 기존 동작(NG 요구)을 유지한다.
        /// </summary>
        private bool IsNgCassetteUsed()
        {
            var cassette = _context != null && _context.Machine != null
                ? _context.Machine.OutputCassetteUnit
                : null;
            return cassette == null || cassette.Config == null || cassette.Config.UseNgCassette;
        }

        private bool IsStopAfterDrainPickerCapacityRequired()
        {
            WaferCompletionRunCoordinator completion = _context != null
                ? _context.WaferCompletion
                : null;
            if (completion == null || !completion.Enabled || !completion.IsDrainRequested)
                return false;

            return HasActivePickerTargetDie(PickerSequenceSide.Front) ||
                   HasActivePickerTargetDie(PickerSequenceSide.Rear);
        }

        private bool HasActivePickerTargetDie(PickerSequenceSide side)
        {
            if (!IsPickerSideConfiguredForRun(side))
                return false;

            MaterialLocationKind location = side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                if (die != null && die.IsInputTarget)
                    return true;
            }

            return false;
        }

        private static bool IsOutputStageMaterialPresent(BinSide side)
        {
            MaterialLocationKind location = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;
            return MaterialStateService.GetWaferAtLocation(location) != null;
        }

        private bool IsLoaderActive()
        {
            return IsSignalSet("InputLoaderActive") || IsSignalSet("OutputLoaderActive");
        }

        private bool IsSignalSet(string signal)
        {
            return _context != null &&
                   _context.Bus != null &&
                   _context.Bus.IsSet(signal);
        }

        private string FormatHolder(SequenceResourceKind resource)
        {
            string current = _context.Resources.GetHolder(resource);
            return string.IsNullOrWhiteSpace(current) ? "-" : current;
        }

        #endregion
    }

    #region 작업 구역 Lease 형식

    internal sealed class AutoSequencePickerWorkZoneLease : IDisposable
    {
        private readonly AutoSequenceCoordinatorGate _gate;
        private bool _disposed;

        internal AutoSequencePickerWorkZoneLease(
            AutoSequenceCoordinatorGate gate,
            PickerSequenceSide side,
            PickerWorkZone zone,
            string owner,
            bool active)
        {
            _gate = gate;
            Side = side;
            Zone = zone;
            Owner = owner ?? "";
            Active = active;
        }

        public PickerSequenceSide Side { get; private set; }
        public PickerWorkZone Zone { get; private set; }
        public string Owner { get; private set; }
        public bool Active { get; private set; }
        public bool IsDisposed { get { return _disposed; } }

        internal void Update(PickerWorkZone zone, string owner, bool active)
        {
            Zone = zone;
            Owner = owner ?? "";
            Active = active;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (Active && _gate != null)
                _gate.ReleasePickerWorkZone(this);

            Active = false;
        }
    }

    internal sealed class AutoSequencePickerGateLeases : IDisposable
    {
        private SequenceResourceLease _frontLease;
        private SequenceResourceLease _rearLease;

        public AutoSequencePickerGateLeases(
            SequenceResourceLease frontLease,
            SequenceResourceLease rearLease)
        {
            _frontLease = frontLease;
            _rearLease = rearLease;
        }

        public void Dispose()
        {
            if (_rearLease != null)
            {
                _rearLease.Dispose();
                _rearLease = null;
            }

            if (_frontLease != null)
            {
                _frontLease.Dispose();
                _frontLease = null;
            }
        }
    }

    internal sealed class AutoSequenceCameraWorkZoneLease : IDisposable
    {
        private readonly AutoSequenceCoordinatorGate _gate;
        private bool _disposed;

        internal AutoSequenceCameraWorkZoneLease(
            AutoSequenceCoordinatorGate gate,
            AutoSequenceCameraWorkKind kind,
            PickerWorkZone zone,
            string owner,
            bool active)
        {
            _gate = gate;
            Kind = kind;
            Zone = zone;
            Owner = owner ?? "";
            Active = active;
        }

        public AutoSequenceCameraWorkKind Kind { get; private set; }
        public PickerWorkZone Zone { get; private set; }
        public string Owner { get; private set; }
        public bool Active { get; private set; }
        public bool IsDisposed { get { return _disposed; } }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (Active && _gate != null)
                _gate.ReleaseCameraWorkZone(this);

            Active = false;
        }
    }

    internal sealed class AutoSequenceLoaderWorkLease : IDisposable
    {
        private readonly MachineSequenceContext _context;
        private readonly string _loaderName;
        private readonly string _activeSignal;
        private readonly string _holder;
        private int _disposed;

        internal AutoSequenceLoaderWorkLease(
            MachineSequenceContext context,
            string loaderName,
            string activeSignal,
            string holder,
            bool activeSet)
        {
            _context = context;
            _loaderName = loaderName ?? "";
            _activeSignal = activeSignal ?? "";
            _holder = holder ?? "";
            ActiveSet = activeSet;
        }

        public bool ActiveSet { get; private set; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                if (ActiveSet && _context != null && _context.Bus != null)
                {
                    _context.Bus.Reset(_activeSignal);
                    Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                        _loaderName + " work complete. holder=" + _holder +
                        ", signal=" + _activeSignal + " - Reset");
                    _context.LogPublic("[SEQ] " + _loaderName + " work complete. holder=" + _holder);
                }
            }
            catch
            {
            }
            finally
            {
                ActiveSet = false;
            }
        }
    }

    #endregion
}
