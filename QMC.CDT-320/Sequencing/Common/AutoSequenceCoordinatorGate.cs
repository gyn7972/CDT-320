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
        private const int PickerWorkZonePollIntervalMs = 20;
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

        public AutoSequenceCoordinatorGate(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

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

                await WaitLoaderInactiveBeforePickerStartAsync(gateHolder, ct).ConfigureAwait(false);

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

                if (!IsLoaderActive())
                {
                    Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                        "Picker process approved. side=" + side +
                        ", holder=" + safeHolder + ", resource=" + resource + " - Ok");
                    _context.LogPublic("[SEQ] PickerProcess start approved. side=" + side +
                        ", holder=" + safeHolder);
                    return lease;
                }

                lease.Dispose();
                _context.LogPublic("[SEQ] PickerProcess start recheck blocked by loader active. side=" +
                    side + ", holder=" + safeHolder);
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
            CancellationToken ct)
        {
            return WaitAndSetCameraWorkZoneAsync(
                AutoSequenceCameraWorkKind.InputCamera,
                PickerWorkZone.Input,
                string.IsNullOrWhiteSpace(holder) ? "InputCamera" : holder,
                ct);
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

        private async Task<AutoSequenceCameraWorkZoneLease> WaitAndSetCameraWorkZoneAsync(
            AutoSequenceCameraWorkKind kind,
            PickerWorkZone zone,
            string holder,
            CancellationToken ct)
        {
            PickerWorkZone safeZone = PickerZoneInterlockRules.NormalizeInterlockZone(zone);
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? kind.ToString() : holder;
            if (safeZone == PickerWorkZone.Unknown || safeZone == PickerWorkZone.Avoid)
                return new AutoSequenceCameraWorkZoneLease(this, kind, PickerWorkZone.Unknown, safeHolder, false);

            bool waitLogged = false;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested(
                    "AutoSequenceCoordinator.CameraWorkZone:" + kind + ":" + safeZone,
                    ShouldDeferCycleStopForOutputCameraDrain(kind),
                    "Output post-place inspection drain");

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

                pickerLeases = await AcquireBothPickerStartGatesAsync(gateHolder, ct).ConfigureAwait(false);

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

            reason = "picker physical zone is not clear for camera. kind=" + kind +
                ", zone=" + zone +
                ", holder=" + holder +
                ", frontBlocking=" + frontBlocking +
                ", front=" + frontDetail +
                ", rearBlocking=" + rearBlocking +
                ", rear=" + rearDetail;
            return false;
        }

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

        private async Task<AutoSequencePickerGateLeases> AcquireBothPickerStartGatesAsync(
            string holder,
            CancellationToken ct)
        {
            bool waitLogged = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested("AutoSequenceCoordinator.LoaderGate:" + holder);

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

        private async Task WaitLoaderInactiveBeforePickerStartAsync(string holder, CancellationToken ct)
        {
            bool waitLogged = false;

            while (IsLoaderActive())
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested("AutoSequenceCoordinator.PickerWaitLoaderInactive:" + holder);

                if (!waitLogged)
                {
                    _context.LogPublic("[SEQ] AutoSequenceCoordinator picker start waiting for loader inactive. holder=" +
                        holder + ", inputActive=" + IsSignalSet("InputLoaderActive") +
                        ", outputActive=" + IsSignalSet("OutputLoaderActive"));
                    waitLogged = true;
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
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
    }

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
}
