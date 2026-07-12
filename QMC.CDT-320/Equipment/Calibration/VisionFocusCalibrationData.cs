using System;
using System.Runtime.Serialization;
using System.Threading;

namespace QMC.CDT320.Calibration
{
    public enum VisionFocusPickerSide
    {
        Front,
        Rear
    }

    public enum VisionFocusScanKind
    {
        BottomCollet = 0,
        FrontSide0 = 1,
        FrontSide90 = 2,
        RearSide0 = 3,
        RearSide90 = 4,
        BottomDie = 5
    }

    public enum VisionFocusValueReceiveMode
    {
        AckOnly = 0,
        WaitResultForTest = 1
    }

    public enum RuntimeAutoFocusScanMode
    {
        None = 0,
        FineOnly = 1,
        RoughAndFine = 2
    }

    [DataContract]
    public sealed class VisionFocusScanSettings
    {
        [DataMember] public double MinusRange { get; set; } = 0.2;
        [DataMember] public double PlusRange { get; set; } = 0.2;
        [DataMember] public double Step { get; set; } = 0.02;
        [DataMember] public double FineMinusRange { get; set; } = 0.05;
        [DataMember] public double FinePlusRange { get; set; } = 0.05;
        [DataMember] public double FineStep { get; set; } = 0.01;
        [DataMember] public int RepeatCount { get; set; } = 1;
        [DataMember] public double MoveVelocity { get; set; } = 30.0;
        [DataMember] public double MoveAcceleration { get; set; } = 300.0;
        [DataMember] public double MoveDeceleration { get; set; } = 300.0;
        [DataMember] public int SettleDelayMs { get; set; } = 50;
        [DataMember] public int MotionTimeoutMs { get; set; } = 5000;
        [DataMember] public int VisionTimeoutMs { get; set; } = 5000;
        [DataMember] public int VisionBestTimeoutMs { get; set; } = 120000;
        [DataMember] public VisionFocusValueReceiveMode FocusValueReceiveMode { get; set; } = VisionFocusValueReceiveMode.AckOnly;
        [DataMember] public bool ReturnToDefaultAfterScan { get; set; } = true;
        [DataMember] public bool AutoFocusBeforeBottomEnabled { get; set; }
        [DataMember] public bool AutoFocusOnStartEnabled { get; set; }
        [DataMember] public bool AutoFocusOnWaferChange { get; set; } = true;
        [DataMember] public bool AutoFocusOnPickCountEnabled { get; set; }
        [DataMember] public int AutoFocusPickInterval { get; set; }
        [DataMember] public bool AutoFocusRuntimePolicyInitialized { get; set; }

        public void EnsureDefaults()
        {
            if (MinusRange <= 0) MinusRange = 0.2;
            if (PlusRange <= 0) PlusRange = 0.2;
            if (Step <= 0) Step = 0.02;
            if (FineMinusRange <= 0) FineMinusRange = 0.05;
            if (FinePlusRange <= 0) FinePlusRange = 0.05;
            if (FineStep <= 0) FineStep = 0.01;
            if (RepeatCount <= 0) RepeatCount = 1;
            if (RepeatCount > 100) RepeatCount = 100;
            if (MoveVelocity <= 0) MoveVelocity = 30.0;
            if (MoveAcceleration <= 0) MoveAcceleration = 300.0;
            if (MoveDeceleration <= 0) MoveDeceleration = 300.0;
            if (SettleDelayMs < 0) SettleDelayMs = 50;
            if (MotionTimeoutMs <= 0) MotionTimeoutMs = 5000;
            if (VisionTimeoutMs <= 0) VisionTimeoutMs = 5000;
            if (VisionBestTimeoutMs <= 0) VisionBestTimeoutMs = 120000;
            if (!AutoFocusRuntimePolicyInitialized)
            {
                AutoFocusOnWaferChange = true;
                AutoFocusRuntimePolicyInitialized = true;
            }
            if (AutoFocusPickInterval < 0) AutoFocusPickInterval = 0;
            if (AutoFocusPickInterval > 1000000) AutoFocusPickInterval = 1000000;
            if (!Enum.IsDefined(typeof(VisionFocusValueReceiveMode), FocusValueReceiveMode))
                FocusValueReceiveMode = VisionFocusValueReceiveMode.AckOnly;
        }
    }

    [DataContract]
    public sealed class VisionFocusPositionRecord
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public double DefaultPosition { get; set; }
        [DataMember] public double BestPosition { get; set; }
        [DataMember] public double BestScore { get; set; }
        [DataMember] public int SampleCount { get; set; }
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }
        [DataMember] public int AutoFocusPickCountSinceLast { get; set; }
        [DataMember] public string LastAutoFocusWaferId { get; set; }
        [DataMember] public DateTime LastAutoFocusAt { get; set; }
        [DataMember] public bool ForceNextAutoFocus { get; set; }

        public void ApplyBest(double defaultPosition, double bestPosition, double bestScore, int sampleCount, string updatedBy)
        {
            DefaultPosition = defaultPosition;
            BestPosition = bestPosition;
            BestScore = bestScore;
            SampleCount = sampleCount;
            Valid = true;
            UpdatedAt = DateTime.Now;
            UpdatedBy = updatedBy ?? string.Empty;
        }

        public void EnsureDefaults()
        {
            if (UpdatedBy == null)
                UpdatedBy = string.Empty;
            if (LastAutoFocusWaferId == null)
                LastAutoFocusWaferId = string.Empty;
            if (AutoFocusPickCountSinceLast < 0)
                AutoFocusPickCountSinceLast = 0;
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            LastAutoFocusAt = EnsureSerializableDateTime(LastAutoFocusAt);
        }

        public void RecordAutoFocusPick()
        {
            if (AutoFocusPickCountSinceLast < 0)
                AutoFocusPickCountSinceLast = 0;
            if (AutoFocusPickCountSinceLast < int.MaxValue)
                AutoFocusPickCountSinceLast++;
        }

        public void MarkAutoFocusComplete(string waferId)
        {
            AutoFocusPickCountSinceLast = 0;
            LastAutoFocusWaferId = waferId ?? string.Empty;
            LastAutoFocusAt = DateTime.Now;
            ForceNextAutoFocus = false;
        }

        public void RequestAutoFocusNext()
        {
            ForceNextAutoFocus = true;
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }

    [DataContract]
    public sealed class VisionFocusCalibrationData
    {
        private static readonly DateTime SafeRuntimeUnsetDateTime = new DateTime(2000, 1, 1);
        private object _runtimeAutoFocusSync;
        private bool _runtimeAutoFocusReserved;
        private VisionFocusPickerSide _reservedRuntimeAutoFocusSide;
        private int _reservedRuntimeAutoFocusPickerNo;
        private RuntimeAutoFocusScanMode _reservedRuntimeAutoFocusMode;
        private RuntimeAutoFocusScanMode _startupAutoFocusMode;

        [DataMember] public VisionFocusScanSettings BottomColletScan { get; set; } = new VisionFocusScanSettings();
        [DataMember] public VisionFocusScanSettings BottomDieScan { get; set; } = new VisionFocusScanSettings();
        [DataMember] public VisionFocusScanSettings SideVisionScan { get; set; } = new VisionFocusScanSettings();
        [DataMember] public VisionFocusPositionRecord[] FrontCollets { get; set; } = CreatePickerRecords();
        [DataMember] public VisionFocusPositionRecord[] RearCollets { get; set; } = CreatePickerRecords();
        [DataMember] public VisionFocusPositionRecord[] FrontDies { get; set; } = CreatePickerRecords();
        [DataMember] public VisionFocusPositionRecord[] RearDies { get; set; } = CreatePickerRecords();
        [DataMember] public VisionFocusPositionRecord FrontSide0 { get; set; } = new VisionFocusPositionRecord();
        [DataMember] public VisionFocusPositionRecord FrontSide90 { get; set; } = new VisionFocusPositionRecord();
        [DataMember] public VisionFocusPositionRecord RearSide0 { get; set; } = new VisionFocusPositionRecord();
        [DataMember] public VisionFocusPositionRecord RearSide90 { get; set; } = new VisionFocusPositionRecord();
        [DataMember] public int RuntimeAutoFocusTotalPickCount { get; set; }
        [DataMember] public string RuntimeAutoFocusLastWaferId { get; set; }
        [DataMember] public DateTime RuntimeAutoFocusLastCompletedAt { get; set; }
        [DataMember] public RuntimeAutoFocusScanMode[] FrontRuntimeAutoFocusPendingModes { get; set; } = CreateRuntimeAutoFocusModes();
        [DataMember] public RuntimeAutoFocusScanMode[] RearRuntimeAutoFocusPendingModes { get; set; } = CreateRuntimeAutoFocusModes();
        [DataMember] public string RuntimeAutoFocusPendingReason { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            if (BottomColletScan == null) BottomColletScan = new VisionFocusScanSettings();
            if (BottomDieScan == null) BottomDieScan = new VisionFocusScanSettings();
            if (SideVisionScan == null) SideVisionScan = new VisionFocusScanSettings();
            BottomColletScan.EnsureDefaults();
            BottomDieScan.EnsureDefaults();
            SideVisionScan.EnsureDefaults();

            FrontCollets = EnsurePickerRecords(FrontCollets);
            RearCollets = EnsurePickerRecords(RearCollets);
            FrontDies = EnsurePickerRecords(FrontDies);
            RearDies = EnsurePickerRecords(RearDies);
            if (FrontSide0 == null) FrontSide0 = new VisionFocusPositionRecord();
            if (FrontSide90 == null) FrontSide90 = new VisionFocusPositionRecord();
            if (RearSide0 == null) RearSide0 = new VisionFocusPositionRecord();
            if (RearSide90 == null) RearSide90 = new VisionFocusPositionRecord();
            if (RuntimeAutoFocusLastWaferId == null) RuntimeAutoFocusLastWaferId = string.Empty;
            if (RuntimeAutoFocusPendingReason == null) RuntimeAutoFocusPendingReason = string.Empty;
            if (RuntimeAutoFocusTotalPickCount < 0) RuntimeAutoFocusTotalPickCount = 0;
            FrontRuntimeAutoFocusPendingModes = EnsureRuntimeAutoFocusModes(FrontRuntimeAutoFocusPendingModes);
            RearRuntimeAutoFocusPendingModes = EnsureRuntimeAutoFocusModes(RearRuntimeAutoFocusPendingModes);
            if (RuntimeAutoFocusLastCompletedAt <= DateTime.MinValue.AddDays(1) ||
                RuntimeAutoFocusLastCompletedAt >= DateTime.MaxValue.AddDays(-1))
                RuntimeAutoFocusLastCompletedAt = SafeRuntimeUnsetDateTime;

            FrontSide0.EnsureDefaults();
            FrontSide90.EnsureDefaults();
            RearSide0.EnsureDefaults();
            RearSide90.EnsureDefaults();
        }

        public int RecordRuntimeAutoFocusPick()
        {
            lock (RuntimeAutoFocusSync)
            {
                if (RuntimeAutoFocusTotalPickCount < int.MaxValue)
                    RuntimeAutoFocusTotalPickCount++;
                return RuntimeAutoFocusTotalPickCount;
            }
        }

        public void SetStartupAutoFocusMode(RuntimeAutoFocusScanMode mode)
        {
            lock (RuntimeAutoFocusSync)
                _startupAutoFocusMode = mode;
        }

        public bool TryReserveRuntimeAutoFocus(
            VisionFocusPickerSide side,
            int pickerNo,
            string waferId,
            out RuntimeAutoFocusScanMode scanMode,
            out string reason)
        {
            lock (RuntimeAutoFocusSync)
            {
                scanMode = RuntimeAutoFocusScanMode.None;
                reason = string.Empty;
                if (_runtimeAutoFocusReserved)
                    return false;

                VisionFocusScanSettings settings = BottomDieScan;
                settings.EnsureDefaults();
                if (_startupAutoFocusMode != RuntimeAutoFocusScanMode.None)
                {
                    ArmAllRuntimeAutoFocus(_startupAutoFocusMode, "Start", waferId);
                    _startupAutoFocusMode = RuntimeAutoFocusScanMode.None;
                }
                else if (settings.AutoFocusOnWaferChange &&
                         !string.IsNullOrWhiteSpace(waferId) &&
                         !string.Equals(RuntimeAutoFocusLastWaferId ?? string.Empty, waferId, StringComparison.OrdinalIgnoreCase))
                {
                    ArmAllRuntimeAutoFocus(RuntimeAutoFocusScanMode.RoughAndFine, "WaferChanged", waferId);
                }
                else if (settings.AutoFocusOnPickCountEnabled &&
                         settings.AutoFocusPickInterval > 0 &&
                         RuntimeAutoFocusTotalPickCount >= settings.AutoFocusPickInterval)
                {
                    ArmAllRuntimeAutoFocus(RuntimeAutoFocusScanMode.RoughAndFine, "TotalPickCount", waferId);
                }

                RuntimeAutoFocusScanMode[] pendingModes = side == VisionFocusPickerSide.Rear
                    ? RearRuntimeAutoFocusPendingModes
                    : FrontRuntimeAutoFocusPendingModes;
                int pickerIndex = NormalizePickerIndex(pickerNo);
                scanMode = pendingModes[pickerIndex];
                if (scanMode == RuntimeAutoFocusScanMode.None)
                    return false;

                reason = RuntimeAutoFocusPendingReason ?? string.Empty;
                pendingModes[pickerIndex] = RuntimeAutoFocusScanMode.None;
                _runtimeAutoFocusReserved = true;
                _reservedRuntimeAutoFocusSide = side;
                _reservedRuntimeAutoFocusPickerNo = pickerNo;
                _reservedRuntimeAutoFocusMode = scanMode;
                return true;
            }
        }

        public void CompleteRuntimeAutoFocus(VisionFocusPickerSide side, int pickerNo, string waferId)
        {
            lock (RuntimeAutoFocusSync)
            {
                if (!_runtimeAutoFocusReserved ||
                    _reservedRuntimeAutoFocusSide != side ||
                    _reservedRuntimeAutoFocusPickerNo != pickerNo)
                    return;

                RuntimeAutoFocusLastCompletedAt = DateTime.Now;
                _runtimeAutoFocusReserved = false;
                _reservedRuntimeAutoFocusPickerNo = 0;
                _reservedRuntimeAutoFocusMode = RuntimeAutoFocusScanMode.None;
            }
        }

        public void ReleaseRuntimeAutoFocusReservation()
        {
            lock (RuntimeAutoFocusSync)
            {
                if (_runtimeAutoFocusReserved &&
                    _reservedRuntimeAutoFocusPickerNo >= 1 &&
                    _reservedRuntimeAutoFocusPickerNo <= 4 &&
                    _reservedRuntimeAutoFocusMode != RuntimeAutoFocusScanMode.None)
                {
                    RuntimeAutoFocusScanMode[] pendingModes =
                        _reservedRuntimeAutoFocusSide == VisionFocusPickerSide.Rear
                            ? RearRuntimeAutoFocusPendingModes
                            : FrontRuntimeAutoFocusPendingModes;
                    pendingModes[NormalizePickerIndex(_reservedRuntimeAutoFocusPickerNo)] =
                        _reservedRuntimeAutoFocusMode;
                }

                _runtimeAutoFocusReserved = false;
                _reservedRuntimeAutoFocusPickerNo = 0;
                _reservedRuntimeAutoFocusMode = RuntimeAutoFocusScanMode.None;
            }
        }

        public void ResetRuntimeAutoFocusTracking()
        {
            lock (RuntimeAutoFocusSync)
            {
                RuntimeAutoFocusTotalPickCount = 0;
                RuntimeAutoFocusLastWaferId = string.Empty;
                RuntimeAutoFocusLastCompletedAt = SafeRuntimeUnsetDateTime;
                _runtimeAutoFocusReserved = false;
                _startupAutoFocusMode = RuntimeAutoFocusScanMode.None;
                FrontRuntimeAutoFocusPendingModes = CreateRuntimeAutoFocusModes();
                RearRuntimeAutoFocusPendingModes = CreateRuntimeAutoFocusModes();
                RuntimeAutoFocusPendingReason = string.Empty;
            }
        }

        public string BuildRuntimeAutoFocusPendingText()
        {
            lock (RuntimeAutoFocusSync)
            {
                return "Front=" + BuildPendingModeText(FrontRuntimeAutoFocusPendingModes) +
                       ", Rear=" + BuildPendingModeText(RearRuntimeAutoFocusPendingModes);
            }
        }

        private void ArmAllRuntimeAutoFocus(RuntimeAutoFocusScanMode mode, string reason, string waferId)
        {
            FrontRuntimeAutoFocusPendingModes = EnsureRuntimeAutoFocusModes(FrontRuntimeAutoFocusPendingModes);
            RearRuntimeAutoFocusPendingModes = EnsureRuntimeAutoFocusModes(RearRuntimeAutoFocusPendingModes);
            for (int i = 0; i < 4; i++)
            {
                FrontRuntimeAutoFocusPendingModes[i] = mode;
                RearRuntimeAutoFocusPendingModes[i] = mode;
            }

            RuntimeAutoFocusPendingReason = reason ?? string.Empty;
            RuntimeAutoFocusTotalPickCount = 0;
            if (!string.IsNullOrWhiteSpace(waferId))
                RuntimeAutoFocusLastWaferId = waferId;
        }

        private static RuntimeAutoFocusScanMode[] EnsureRuntimeAutoFocusModes(RuntimeAutoFocusScanMode[] modes)
        {
            RuntimeAutoFocusScanMode[] result = CreateRuntimeAutoFocusModes();
            if (modes == null)
                return result;

            int count = Math.Min(4, modes.Length);
            for (int i = 0; i < count; i++)
            {
                result[i] = Enum.IsDefined(typeof(RuntimeAutoFocusScanMode), modes[i])
                    ? modes[i]
                    : RuntimeAutoFocusScanMode.None;
            }
            return result;
        }

        private static RuntimeAutoFocusScanMode[] CreateRuntimeAutoFocusModes()
        {
            return new RuntimeAutoFocusScanMode[4];
        }

        private static string BuildPendingModeText(RuntimeAutoFocusScanMode[] modes)
        {
            modes = EnsureRuntimeAutoFocusModes(modes);
            return "P1=" + modes[0] + ",P2=" + modes[1] +
                   ",P3=" + modes[2] + ",P4=" + modes[3];
        }

        private object RuntimeAutoFocusSync
        {
            get
            {
                if (_runtimeAutoFocusSync == null)
                    Interlocked.CompareExchange(ref _runtimeAutoFocusSync, new object(), null);
                return _runtimeAutoFocusSync;
            }
        }

        public VisionFocusPositionRecord GetColletRecord(VisionFocusPickerSide side, int pickerNo)
        {
            EnsureObjects();
            int index = NormalizePickerIndex(pickerNo);
            return side == VisionFocusPickerSide.Front ? FrontCollets[index] : RearCollets[index];
        }

        public VisionFocusPositionRecord GetDieRecord(VisionFocusPickerSide side, int pickerNo)
        {
            EnsureObjects();
            int index = NormalizePickerIndex(pickerNo);
            return side == VisionFocusPickerSide.Front ? FrontDies[index] : RearDies[index];
        }

        public VisionFocusPositionRecord GetBottomRecord(VisionFocusScanKind kind, VisionFocusPickerSide side, int pickerNo)
        {
            return kind == VisionFocusScanKind.BottomDie
                ? GetDieRecord(side, pickerNo)
                : GetColletRecord(side, pickerNo);
        }

        public VisionFocusPositionRecord GetSideRecord(VisionFocusScanKind kind)
        {
            EnsureObjects();
            switch (kind)
            {
                case VisionFocusScanKind.FrontSide0: return FrontSide0;
                case VisionFocusScanKind.FrontSide90: return FrontSide90;
                case VisionFocusScanKind.RearSide0: return RearSide0;
                case VisionFocusScanKind.RearSide90: return RearSide90;
                default: return FrontSide0;
            }
        }

        private static int NormalizePickerIndex(int pickerNo)
        {
            if (pickerNo < 1) return 0;
            if (pickerNo > 4) return 3;
            return pickerNo - 1;
        }

        private static VisionFocusPositionRecord[] EnsurePickerRecords(VisionFocusPositionRecord[] records)
        {
            if (records == null || records.Length != 4)
            {
                VisionFocusPositionRecord[] next = CreatePickerRecords();
                if (records != null)
                {
                    int count = Math.Min(records.Length, next.Length);
                    for (int i = 0; i < count; i++)
                    {
                        if (records[i] != null)
                            next[i] = records[i];
                    }
                }

                records = next;
            }

            for (int i = 0; i < records.Length; i++)
            {
                if (records[i] == null)
                    records[i] = new VisionFocusPositionRecord();
                records[i].EnsureDefaults();
            }

            return records;
        }

        private static VisionFocusPositionRecord[] CreatePickerRecords()
        {
            return new[]
            {
                new VisionFocusPositionRecord(),
                new VisionFocusPositionRecord(),
                new VisionFocusPositionRecord(),
                new VisionFocusPositionRecord()
            };
        }
    }
}
