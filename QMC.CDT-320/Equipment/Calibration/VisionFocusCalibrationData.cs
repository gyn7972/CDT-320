using System;
using System.Runtime.Serialization;

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
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
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

            FrontSide0.EnsureDefaults();
            FrontSide90.EnsureDefaults();
            RearSide0.EnsureDefaults();
            RearSide90.EnsureDefaults();
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
