using System;
using System.Runtime.Serialization;

namespace QMC.CDT320.Calibration
{
    [DataContract]
    public sealed class AutoCalibrationSettings
    {
        [DataMember] public bool UseColletCalibration { get; set; } = true;
        [DataMember] public bool UsePickUpZCalibration { get; set; } = true;
        [DataMember] public bool UsePlaceZCalibration { get; set; } = true;

        public AutoCalibrationSettings Clone()
        {
            try
            {
                return new AutoCalibrationSettings
                {
                    UseColletCalibration = UseColletCalibration,
                    UsePickUpZCalibration = UsePickUpZCalibration,
                    UsePlaceZCalibration = UsePlaceZCalibration
                };
            }
            catch
            {
                return new AutoCalibrationSettings();
            }
            finally
            {
            }
        }
    }

    [DataContract]
    public sealed class CalibrationMotionSettings
    {
        public const double DefaultMoveVelocity = 10.0;
        public const double DefaultMoveAcceleration = 200.0;
        public const double DefaultMoveDeceleration = 200.0;
        public const int DefaultMoveTimeoutMs = 10000;

        [DataMember] public double MoveVelocity { get; set; } = DefaultMoveVelocity;
        [DataMember] public double MoveAcceleration { get; set; } = DefaultMoveAcceleration;
        [DataMember] public double MoveDeceleration { get; set; } = DefaultMoveDeceleration;
        [DataMember] public int MoveTimeoutMs { get; set; } = DefaultMoveTimeoutMs;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (MoveVelocity <= 0.0)
                MoveVelocity = DefaultMoveVelocity;
            if (MoveAcceleration <= 0.0)
                MoveAcceleration = DefaultMoveAcceleration;
            if (MoveDeceleration <= 0.0)
                MoveDeceleration = DefaultMoveDeceleration;
            if (MoveTimeoutMs <= 0)
                MoveTimeoutMs = DefaultMoveTimeoutMs;
        }

        public CalibrationMotionSettings Clone()
        {
            EnsureDefaults();
            return new CalibrationMotionSettings
            {
                MoveVelocity = MoveVelocity,
                MoveAcceleration = MoveAcceleration,
                MoveDeceleration = MoveDeceleration,
                MoveTimeoutMs = MoveTimeoutMs
            };
        }
    }

    [DataContract]
    public sealed class NeedleCalibrationData
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public CalibrationMotionSettings Motion { get; set; } = new CalibrationMotionSettings();
        [DataMember] public NeedleZCalibrationSettings ZCalibration { get; set; } = new NeedleZCalibrationSettings();
        [DataMember] public double VisionXPosition { get; set; }
        [DataMember] public double StageYPosition { get; set; }
        [DataMember] public double NeedleXPosition { get; set; }
        [DataMember] public double NeedleZPosition { get; set; }
        [DataMember] public double EjectPinZPosition { get; set; }
        [DataMember] public double VisionOffsetX { get; set; }
        [DataMember] public double VisionOffsetY { get; set; }
        [DataMember] public double NeedleXToVisionXOffset { get; set; }
        [DataMember] public double NeedleYToVisionYOffset { get; set; }
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }
        [DataMember] public double NeedleCapTouchPosition { get; set; }
        [DataMember] public double NeedlePinFlushPosition { get; set; }
        [DataMember] public double NeedlePinReadyPosition { get; set; }
        [DataMember] public bool NeedleZCalibrationValid { get; set; }
        [DataMember] public DateTime NeedleZCalibrationUpdatedAt { get; set; }
        [DataMember] public string NeedleZCalibrationUpdatedBy { get; set; }
        [DataMember] public string NeedleZCalibrationMessage { get; set; }

        public void EnsureObjects()
        {
            if (Motion == null)
                Motion = new CalibrationMotionSettings();
            Motion.EnsureDefaults();
            if (ZCalibration == null)
                ZCalibration = new NeedleZCalibrationSettings();
            ZCalibration.EnsureDefaults();
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            NeedleZCalibrationUpdatedAt = EnsureSerializableDateTime(NeedleZCalibrationUpdatedAt);
            if (UpdatedBy == null)
                UpdatedBy = string.Empty;
            if (NeedleZCalibrationUpdatedBy == null)
                NeedleZCalibrationUpdatedBy = string.Empty;
            if (NeedleZCalibrationMessage == null)
                NeedleZCalibrationMessage = string.Empty;
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }

    [DataContract]
    public sealed class PickUpZCalibrationSettings
    {
        [DataMember] public CalibrationMotionSettings Motion { get; set; } = new CalibrationMotionSettings();
        [DataMember] public double StartZMm { get; set; } = 0.0;
        [DataMember] public double SearchStartOffsetMm { get; set; } = 1.0;
        [DataMember] public double SearchMaxDistanceMm { get; set; } = 2.0;
        [DataMember] public double CoarseSearchVelocityMmPerSec { get; set; } = 5.0;
        [DataMember] public double CoarseSearchAccelerationMmPerSec2 { get; set; } = 50.0;
        [DataMember] public double CoarseSearchDecelerationMmPerSec2 { get; set; } = 50.0;
        [DataMember] public double FineSearchVelocityMmPerSec { get; set; } = 1.0;
        [DataMember] public double FineSearchAccelerationMmPerSec2 { get; set; } = 10.0;
        [DataMember] public double FineSearchDecelerationMmPerSec2 { get; set; } = 10.0;
        [DataMember] public double BackOffDistanceMm { get; set; } = 0.2;
        [DataMember] public double ContactOffsetMm { get; set; } = 0.0;
        [DataMember] public double FilmThicknessMm { get; set; } = 0.0;
        [DataMember] public double DieThicknessMm { get; set; } = 0.0;
        [DataMember] public double PositionOffsetXmm { get; set; } = 0.0;
        [DataMember] public double PositionOffsetYmm { get; set; } = 0.0;
        [DataMember] public int VacuumOnDelayMs { get; set; } = 100;
        [DataMember] public int VacuumReOnDelayMs { get; set; } = 100;
        [DataMember] public int BlowPulseTimeMs { get; set; } = 50;
        [DataMember] public int BlowSettleTimeMs { get; set; } = 50;
        [DataMember] public int FlowOffConfirmTimeoutMs { get; set; } = 1000;
        [DataMember] public int FlowStableMs { get; set; } = 30;
        [DataMember] public int FlowPollIntervalMs { get; set; } = 5;
        [DataMember] public int RepeatCount { get; set; } = 2;
        [DataMember] public double RepeatToleranceMm { get; set; } = 0.01;
        [DataMember] public bool MoveAvoidAfterScan { get; set; } = true;
        [DataMember] public bool FailIfFlowAlreadyOn { get; set; } = true;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (Motion == null)
                Motion = new CalibrationMotionSettings();
            Motion.EnsureDefaults();
            if (double.IsNaN(StartZMm) || double.IsInfinity(StartZMm))
                StartZMm = 0.0;
            if (SearchStartOffsetMm < 0.0)
                SearchStartOffsetMm = 1.0;
            if (SearchMaxDistanceMm <= 0.0)
                SearchMaxDistanceMm = 2.0;
            if (double.IsNaN(CoarseSearchVelocityMmPerSec) || double.IsInfinity(CoarseSearchVelocityMmPerSec) || CoarseSearchVelocityMmPerSec <= 0.0)
                CoarseSearchVelocityMmPerSec = 5.0;
            if (double.IsNaN(CoarseSearchAccelerationMmPerSec2) || double.IsInfinity(CoarseSearchAccelerationMmPerSec2) || CoarseSearchAccelerationMmPerSec2 <= 0.0)
                CoarseSearchAccelerationMmPerSec2 = 50.0;
            if (double.IsNaN(CoarseSearchDecelerationMmPerSec2) || double.IsInfinity(CoarseSearchDecelerationMmPerSec2) || CoarseSearchDecelerationMmPerSec2 <= 0.0)
                CoarseSearchDecelerationMmPerSec2 = 50.0;
            if (double.IsNaN(FineSearchVelocityMmPerSec) || double.IsInfinity(FineSearchVelocityMmPerSec) || FineSearchVelocityMmPerSec <= 0.0)
                FineSearchVelocityMmPerSec = Math.Max(0.001, Math.Min(Motion.MoveVelocity, 1.0));
            if (double.IsNaN(FineSearchAccelerationMmPerSec2) || double.IsInfinity(FineSearchAccelerationMmPerSec2) || FineSearchAccelerationMmPerSec2 <= 0.0)
                FineSearchAccelerationMmPerSec2 = 10.0;
            if (double.IsNaN(FineSearchDecelerationMmPerSec2) || double.IsInfinity(FineSearchDecelerationMmPerSec2) || FineSearchDecelerationMmPerSec2 <= 0.0)
                FineSearchDecelerationMmPerSec2 = 10.0;
            if (double.IsNaN(BackOffDistanceMm) || double.IsInfinity(BackOffDistanceMm) || BackOffDistanceMm <= 0.0)
                BackOffDistanceMm = 0.2;
            if (double.IsNaN(FilmThicknessMm) || double.IsInfinity(FilmThicknessMm) || FilmThicknessMm < 0.0)
                FilmThicknessMm = 0.0;
            if (double.IsNaN(DieThicknessMm) || double.IsInfinity(DieThicknessMm) || DieThicknessMm < 0.0)
                DieThicknessMm = Math.Max(0.0, ContactOffsetMm);
            if (Math.Abs(ContactOffsetMm) > 1e-9 && DieThicknessMm <= 0.0)
                DieThicknessMm = Math.Max(0.0, ContactOffsetMm);
            ContactOffsetMm = DieThicknessMm;
            if (double.IsNaN(PositionOffsetXmm) || double.IsInfinity(PositionOffsetXmm))
                PositionOffsetXmm = 0.0;
            if (double.IsNaN(PositionOffsetYmm) || double.IsInfinity(PositionOffsetYmm))
                PositionOffsetYmm = 0.0;
            if (VacuumOnDelayMs < 0)
                VacuumOnDelayMs = 100;
            if (VacuumReOnDelayMs < 0)
                VacuumReOnDelayMs = 100;
            if (BlowPulseTimeMs < 0)
                BlowPulseTimeMs = 50;
            if (BlowSettleTimeMs < 0)
                BlowSettleTimeMs = 50;
            if (FlowOffConfirmTimeoutMs <= 0)
                FlowOffConfirmTimeoutMs = 1000;
            if (FlowStableMs < 0)
                FlowStableMs = 30;
            if (FlowPollIntervalMs <= 0)
                FlowPollIntervalMs = 5;
            if (FlowPollIntervalMs > 100)
                FlowPollIntervalMs = 100;
            if (RepeatCount <= 0)
                RepeatCount = 1;
            if (RepeatCount > 5)
                RepeatCount = 5;
            if (double.IsNaN(RepeatToleranceMm) || double.IsInfinity(RepeatToleranceMm) || RepeatToleranceMm <= 0.0)
                RepeatToleranceMm = 0.01;
        }
    }

    [DataContract]
    public sealed class PickUpZCalibrationRecord
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public VisionFocusPickerSide Side { get; set; }
        [DataMember] public int PickerNo { get; set; }
        [DataMember] public double OldPickPosition { get; set; }
        [DataMember] public double DetectedFlowPosition { get; set; }
        [DataMember] public double SavedPickPosition { get; set; }
        [DataMember] public double ContactOffsetMm { get; set; }
        [DataMember] public double StartZMm { get; set; }
        [DataMember] public double FilmThicknessMm { get; set; }
        [DataMember] public double DieThicknessMm { get; set; }
        [DataMember] public double PositionOffsetXmm { get; set; }
        [DataMember] public double PositionOffsetYmm { get; set; }
        [DataMember] public int DetectElapsedMs { get; set; }
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }
        [DataMember] public string Message { get; set; }

        public void EnsureDefaults(VisionFocusPickerSide side, int pickerNo)
        {
            Side = side;
            PickerNo = pickerNo;
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            if (UpdatedBy == null)
                UpdatedBy = string.Empty;
            if (Message == null)
                Message = string.Empty;
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }

    [DataContract]
    public sealed class NeedleZCalibrationSettings
    {
        [DataMember] public CalibrationMotionSettings Motion { get; set; } = new CalibrationMotionSettings();
        [DataMember] public double TouchStageYPosition { get; set; }
        [DataMember] public double TouchNeedleXPosition { get; set; }
        [DataMember] public double NeedleCapTeachingPosition { get; set; }
        [DataMember] public double NeedlePinTeachingPosition { get; set; }
        [DataMember] public double NeedleCapNearTouchOffsetMm { get; set; } = 0.01;
        [DataMember] public double NeedlePinReadyBelowFlushMm { get; set; } = 0.05;
        [DataMember] public double CapSearch100umMaxDistanceMm { get; set; } = 5.0;
        [DataMember] public double CapSearch10umMaxDistanceMm { get; set; } = 0.5;
        [DataMember] public double CapSearch1umMaxDistanceMm { get; set; } = 0.05;
        [DataMember] public double PinSearch10umMaxDistanceMm { get; set; } = 0.5;
        [DataMember] public double PinSearch1umMaxDistanceMm { get; set; } = 0.05;
        [DataMember] public int TouchStableMs { get; set; } = 10;
        [DataMember] public int TouchPollIntervalMs { get; set; } = 5;
        [DataMember] public bool MoveAvoidAfterCalibration { get; set; } = true;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (Motion == null)
                Motion = new CalibrationMotionSettings();
            Motion.EnsureDefaults();
            if (NeedleCapNearTouchOffsetMm < 0.0)
                NeedleCapNearTouchOffsetMm = 0.01;
            if (NeedlePinReadyBelowFlushMm <= 0.0)
                NeedlePinReadyBelowFlushMm = 0.05;
            if (CapSearch100umMaxDistanceMm <= 0.0)
                CapSearch100umMaxDistanceMm = 5.0;
            if (CapSearch10umMaxDistanceMm <= 0.0)
                CapSearch10umMaxDistanceMm = 0.5;
            if (CapSearch1umMaxDistanceMm <= 0.0)
                CapSearch1umMaxDistanceMm = 0.05;
            if (PinSearch10umMaxDistanceMm <= 0.0)
                PinSearch10umMaxDistanceMm = 0.5;
            if (PinSearch1umMaxDistanceMm <= 0.0)
                PinSearch1umMaxDistanceMm = 0.05;
            if (TouchStableMs < 0)
                TouchStableMs = 10;
            if (TouchPollIntervalMs <= 0)
                TouchPollIntervalMs = 5;
            if (TouchPollIntervalMs > 100)
                TouchPollIntervalMs = 100;
        }
    }

    [DataContract]
    public sealed class PickUpZCalibrationData
    {
        [DataMember] public PickUpZCalibrationSettings Settings { get; set; } = new PickUpZCalibrationSettings();
        [DataMember] public PickUpZCalibrationRecord[] Front { get; set; } = CreateRecords(VisionFocusPickerSide.Front);
        [DataMember] public PickUpZCalibrationRecord[] Rear { get; set; } = CreateRecords(VisionFocusPickerSide.Rear);

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            if (Settings == null)
                Settings = new PickUpZCalibrationSettings();
            Settings.EnsureDefaults();
            Front = EnsureRecords(Front, VisionFocusPickerSide.Front);
            Rear = EnsureRecords(Rear, VisionFocusPickerSide.Rear);
        }

        public PickUpZCalibrationRecord GetRecord(VisionFocusPickerSide side, int pickerNo)
        {
            EnsureObjects();
            int index = NormalizePickerIndex(pickerNo);
            return side == VisionFocusPickerSide.Front ? Front[index] : Rear[index];
        }

        private static PickUpZCalibrationRecord[] EnsureRecords(PickUpZCalibrationRecord[] records, VisionFocusPickerSide side)
        {
            if (records == null || records.Length < 4)
            {
                PickUpZCalibrationRecord[] expanded = CreateRecords(side);
                if (records != null)
                {
                    int copyCount = Math.Min(records.Length, expanded.Length);
                    for (int i = 0; i < copyCount; i++)
                    {
                        if (records[i] != null)
                            expanded[i] = records[i];
                    }
                }

                records = expanded;
            }

            for (int i = 0; i < records.Length; i++)
            {
                if (records[i] == null)
                    records[i] = new PickUpZCalibrationRecord();
                records[i].EnsureDefaults(side, i + 1);
            }

            return records;
        }

        private static PickUpZCalibrationRecord[] CreateRecords(VisionFocusPickerSide side)
        {
            PickUpZCalibrationRecord[] records = new PickUpZCalibrationRecord[4];
            for (int i = 0; i < records.Length; i++)
            {
                records[i] = new PickUpZCalibrationRecord();
                records[i].EnsureDefaults(side, i + 1);
            }

            return records;
        }

        private static int NormalizePickerIndex(int pickerNo)
        {
            if (pickerNo <= 1)
                return 0;
            if (pickerNo >= 4)
                return 3;
            return pickerNo - 1;
        }
    }

    [DataContract]
    public sealed class PlaceZCalibrationSettings
    {
        [DataMember] public CalibrationMotionSettings Motion { get; set; } = new CalibrationMotionSettings();
        [DataMember] public double StartZMm { get; set; } = 0.0;
        [DataMember] public double SearchStartOffsetMm { get; set; } = 1.0;
        [DataMember] public double SearchMaxDistanceMm { get; set; } = 2.0;
        [DataMember] public double CoarseSearchVelocityMmPerSec { get; set; } = 5.0;
        [DataMember] public double CoarseSearchAccelerationMmPerSec2 { get; set; } = 50.0;
        [DataMember] public double CoarseSearchDecelerationMmPerSec2 { get; set; } = 50.0;
        [DataMember] public double FineSearchVelocityMmPerSec { get; set; } = 1.0;
        [DataMember] public double FineSearchAccelerationMmPerSec2 { get; set; } = 10.0;
        [DataMember] public double FineSearchDecelerationMmPerSec2 { get; set; } = 10.0;
        [DataMember] public double BackOffDistanceMm { get; set; } = 0.2;
        [DataMember] public double ContactOffsetMm { get; set; } = 0.0;
        [DataMember] public double FilmThicknessMm { get; set; } = 0.0;
        [DataMember] public double DieThicknessMm { get; set; } = 0.0;
        [DataMember] public double PositionOffsetXmm { get; set; } = 0.0;
        [DataMember] public double PositionOffsetYmm { get; set; } = 0.0;
        [DataMember] public int VacuumOnDelayMs { get; set; } = 100;
        [DataMember] public int VacuumReOnDelayMs { get; set; } = 100;
        [DataMember] public int BlowPulseTimeMs { get; set; } = 50;
        [DataMember] public int BlowSettleTimeMs { get; set; } = 50;
        [DataMember] public int FlowOffConfirmTimeoutMs { get; set; } = 1000;
        [DataMember] public int FlowStableMs { get; set; } = 30;
        [DataMember] public int FlowPollIntervalMs { get; set; } = 5;
        [DataMember] public int RepeatCount { get; set; } = 2;
        [DataMember] public double RepeatToleranceMm { get; set; } = 0.01;
        [DataMember] public bool MoveAvoidAfterScan { get; set; } = true;
        [DataMember] public bool FailIfFlowAlreadyOn { get; set; } = true;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (Motion == null)
                Motion = new CalibrationMotionSettings();
            Motion.EnsureDefaults();
            if (double.IsNaN(StartZMm) || double.IsInfinity(StartZMm))
                StartZMm = 0.0;
            if (SearchStartOffsetMm < 0.0)
                SearchStartOffsetMm = 1.0;
            if (SearchMaxDistanceMm <= 0.0)
                SearchMaxDistanceMm = 2.0;
            if (double.IsNaN(CoarseSearchVelocityMmPerSec) || double.IsInfinity(CoarseSearchVelocityMmPerSec) || CoarseSearchVelocityMmPerSec <= 0.0)
                CoarseSearchVelocityMmPerSec = 5.0;
            if (double.IsNaN(CoarseSearchAccelerationMmPerSec2) || double.IsInfinity(CoarseSearchAccelerationMmPerSec2) || CoarseSearchAccelerationMmPerSec2 <= 0.0)
                CoarseSearchAccelerationMmPerSec2 = 50.0;
            if (double.IsNaN(CoarseSearchDecelerationMmPerSec2) || double.IsInfinity(CoarseSearchDecelerationMmPerSec2) || CoarseSearchDecelerationMmPerSec2 <= 0.0)
                CoarseSearchDecelerationMmPerSec2 = 50.0;
            if (double.IsNaN(FineSearchVelocityMmPerSec) || double.IsInfinity(FineSearchVelocityMmPerSec) || FineSearchVelocityMmPerSec <= 0.0)
                FineSearchVelocityMmPerSec = Math.Max(0.001, Math.Min(Motion.MoveVelocity, 1.0));
            if (double.IsNaN(FineSearchAccelerationMmPerSec2) || double.IsInfinity(FineSearchAccelerationMmPerSec2) || FineSearchAccelerationMmPerSec2 <= 0.0)
                FineSearchAccelerationMmPerSec2 = 10.0;
            if (double.IsNaN(FineSearchDecelerationMmPerSec2) || double.IsInfinity(FineSearchDecelerationMmPerSec2) || FineSearchDecelerationMmPerSec2 <= 0.0)
                FineSearchDecelerationMmPerSec2 = 10.0;
            if (double.IsNaN(BackOffDistanceMm) || double.IsInfinity(BackOffDistanceMm) || BackOffDistanceMm <= 0.0)
                BackOffDistanceMm = 0.2;
            if (double.IsNaN(FilmThicknessMm) || double.IsInfinity(FilmThicknessMm) || FilmThicknessMm < 0.0)
                FilmThicknessMm = 0.0;
            if (double.IsNaN(DieThicknessMm) || double.IsInfinity(DieThicknessMm) || DieThicknessMm < 0.0)
                DieThicknessMm = Math.Max(0.0, ContactOffsetMm);
            if (Math.Abs(ContactOffsetMm) > 1e-9 && DieThicknessMm <= 0.0)
                DieThicknessMm = Math.Max(0.0, ContactOffsetMm);
            ContactOffsetMm = DieThicknessMm;
            if (double.IsNaN(PositionOffsetXmm) || double.IsInfinity(PositionOffsetXmm))
                PositionOffsetXmm = 0.0;
            if (double.IsNaN(PositionOffsetYmm) || double.IsInfinity(PositionOffsetYmm))
                PositionOffsetYmm = 0.0;
            if (VacuumOnDelayMs < 0)
                VacuumOnDelayMs = 100;
            if (VacuumReOnDelayMs < 0)
                VacuumReOnDelayMs = 100;
            if (BlowPulseTimeMs < 0)
                BlowPulseTimeMs = 50;
            if (BlowSettleTimeMs < 0)
                BlowSettleTimeMs = 50;
            if (FlowOffConfirmTimeoutMs <= 0)
                FlowOffConfirmTimeoutMs = 1000;
            if (FlowStableMs < 0)
                FlowStableMs = 30;
            if (FlowPollIntervalMs <= 0)
                FlowPollIntervalMs = 5;
            if (FlowPollIntervalMs > 100)
                FlowPollIntervalMs = 100;
            if (RepeatCount <= 0)
                RepeatCount = 1;
            if (RepeatCount > 5)
                RepeatCount = 5;
            if (double.IsNaN(RepeatToleranceMm) || double.IsInfinity(RepeatToleranceMm) || RepeatToleranceMm <= 0.0)
                RepeatToleranceMm = 0.01;
        }
    }

    [DataContract]
    public sealed class PlaceZCalibrationRecord
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public VisionFocusPickerSide Side { get; set; }
        [DataMember] public BinSide OutputSide { get; set; }
        [DataMember] public int PickerNo { get; set; }
        [DataMember] public double OldPlacePosition { get; set; }
        [DataMember] public double DetectedFlowPosition { get; set; }
        [DataMember] public double SavedPlacePosition { get; set; }
        [DataMember] public double ContactOffsetMm { get; set; }
        [DataMember] public double StartZMm { get; set; }
        [DataMember] public double FilmThicknessMm { get; set; }
        [DataMember] public double DieThicknessMm { get; set; }
        [DataMember] public double PositionOffsetXmm { get; set; }
        [DataMember] public double PositionOffsetYmm { get; set; }
        [DataMember] public int DetectElapsedMs { get; set; }
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }
        [DataMember] public string Message { get; set; }

        public void EnsureDefaults(VisionFocusPickerSide side, int pickerNo)
        {
            Side = side;
            PickerNo = pickerNo;
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            if (UpdatedBy == null)
                UpdatedBy = string.Empty;
            if (Message == null)
                Message = string.Empty;
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }

    [DataContract]
    public sealed class PlaceZCalibrationData
    {
        [DataMember] public PlaceZCalibrationSettings Settings { get; set; } = new PlaceZCalibrationSettings();
        [DataMember] public PlaceZCalibrationRecord[] Front { get; set; } = CreateRecords(VisionFocusPickerSide.Front);
        [DataMember] public PlaceZCalibrationRecord[] Rear { get; set; } = CreateRecords(VisionFocusPickerSide.Rear);

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            if (Settings == null)
                Settings = new PlaceZCalibrationSettings();
            Settings.EnsureDefaults();
            Front = EnsureRecords(Front, VisionFocusPickerSide.Front);
            Rear = EnsureRecords(Rear, VisionFocusPickerSide.Rear);
        }

        public PlaceZCalibrationRecord GetRecord(VisionFocusPickerSide side, int pickerNo)
        {
            EnsureObjects();
            int index = NormalizePickerIndex(pickerNo);
            return side == VisionFocusPickerSide.Front ? Front[index] : Rear[index];
        }

        private static PlaceZCalibrationRecord[] EnsureRecords(PlaceZCalibrationRecord[] records, VisionFocusPickerSide side)
        {
            if (records == null || records.Length < 4)
            {
                PlaceZCalibrationRecord[] expanded = CreateRecords(side);
                if (records != null)
                {
                    int copyCount = Math.Min(records.Length, expanded.Length);
                    for (int i = 0; i < copyCount; i++)
                    {
                        if (records[i] != null)
                            expanded[i] = records[i];
                    }
                }

                records = expanded;
            }

            for (int i = 0; i < records.Length; i++)
            {
                if (records[i] == null)
                    records[i] = new PlaceZCalibrationRecord();
                records[i].EnsureDefaults(side, i + 1);
            }

            return records;
        }

        private static PlaceZCalibrationRecord[] CreateRecords(VisionFocusPickerSide side)
        {
            PlaceZCalibrationRecord[] records = new PlaceZCalibrationRecord[4];
            for (int i = 0; i < records.Length; i++)
            {
                records[i] = new PlaceZCalibrationRecord();
                records[i].EnsureDefaults(side, i + 1);
            }

            return records;
        }

        private static int NormalizePickerIndex(int pickerNo)
        {
            if (pickerNo <= 1)
                return 0;
            if (pickerNo >= 4)
                return 3;
            return pickerNo - 1;
        }
    }

    [DataContract]
    public sealed class CalibrationData
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public string Version { get; set; } = "1.0";
        [DataMember] public VisionCameraCalibrationData Camera { get; set; } = new VisionCameraCalibrationData();
        [DataMember] public ColletCalibrationData Collet { get; set; } = new ColletCalibrationData();
        [DataMember] public NeedleCalibrationData Needle { get; set; } = new NeedleCalibrationData();
        [DataMember] public PickUpZCalibrationData PickUpZ { get; set; } = new PickUpZCalibrationData();
        [DataMember] public PlaceZCalibrationData PlaceZ { get; set; } = new PlaceZCalibrationData();
        [DataMember] public AutoCalibrationSettings AutoCalibration { get; set; } = new AutoCalibrationSettings();
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            if (string.IsNullOrWhiteSpace(Version))
                Version = "1.0";
            if (Camera == null)
                Camera = new VisionCameraCalibrationData();
            if (Collet == null)
                Collet = new ColletCalibrationData();
            if (Needle == null)
                Needle = new NeedleCalibrationData();
            if (PickUpZ == null)
                PickUpZ = new PickUpZCalibrationData();
            if (PlaceZ == null)
                PlaceZ = new PlaceZCalibrationData();
            if (AutoCalibration == null)
                AutoCalibration = new AutoCalibrationSettings();

            Camera.EnsureObjects();
            Collet.EnsureObjects();
            Needle.EnsureObjects();
            PickUpZ.EnsureObjects();
            PlaceZ.EnsureObjects();
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            if (UpdatedBy == null)
                UpdatedBy = string.Empty;
        }

        public void Touch(string updatedBy)
        {
            UpdatedAt = DateTime.Now;
            UpdatedBy = updatedBy ?? string.Empty;
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }
}
