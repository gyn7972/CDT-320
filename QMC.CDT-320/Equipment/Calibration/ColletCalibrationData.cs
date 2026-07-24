using System;
using System.Runtime.Serialization;
using QMC.CDT320.VisionComm;

namespace QMC.CDT320.Calibration
{
    [DataContract]
    public sealed class ColletCalibrationSettings
    {
        public const string DefaultBottomFinderName = VisionToolIds.BottomInspection.ColletFinder;

        [DataMember] public string BottomFinderName { get; set; } = DefaultBottomFinderName;
        [DataMember] public int VisionTimeoutMs { get; set; } = 5000;
        [DataMember] public double ScoreThreshold { get; set; } = 0.0;
        [DataMember] public double ThetaToleranceDeg { get; set; } = 0.02;
        [DataMember] public int MaxThetaIterations { get; set; } = 5;
        [DataMember] public double ThetaMoveGain { get; set; } = 1.0;
        [DataMember] public double XyToleranceMm { get; set; } = 0.001;
        [DataMember] public int MaxXyIterations { get; set; } = 5;
        [DataMember] public double XyMoveGainX { get; set; } = 1.0;
        [DataMember] public double XyMoveGainY { get; set; } = 1.0;
        [DataMember] public bool UseDiagonalXyTolerance { get; set; } = true;
        [DataMember] public double FineAlignMaxXyMoveMm { get; set; } = 0.2;
        [DataMember] public bool RunAutoFocusAfterTheta { get; set; } = true;
        // COC 회전중심 검출 후 Side 0°/90° AutoFocus 수행 여부. COC(회전중심)는 이 값과 무관하게 수행되고,
        // 이 값이 false면 Side AutoFocus만 건너뛴다. 기본 true(기존 동작 유지).
        [DataMember] public bool RunSideAutoFocusAfterCoc { get; set; } = true;
        [DataMember] public double CocRotationVelocityDegPerSec { get; set; } = 30.0;
        [DataMember] public CalibrationMotionSettings Motion { get; set; } = new CalibrationMotionSettings();

        // 구버전 저장 데이터에 RunSideAutoFocusAfterCoc 항목이 없으면 기본값(true=기존 동작)이 되도록 역직렬화 전 초기화한다.
        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            RunSideAutoFocusAfterCoc = true;
        }

        public void EnsureDefaults()
        {
            if (Motion == null)
                Motion = new CalibrationMotionSettings();
            Motion.EnsureDefaults();
            BottomFinderName = NormalizeBottomFinderName(BottomFinderName);
            if (VisionTimeoutMs <= 0)
                VisionTimeoutMs = 5000;
            if (ScoreThreshold < 0.0)
                ScoreThreshold = 0.0;
            if (ThetaToleranceDeg <= 0.0)
                ThetaToleranceDeg = 0.02;
            if (MaxThetaIterations <= 0)
                MaxThetaIterations = 5;
            if (MaxThetaIterations > 20)
                MaxThetaIterations = 20;
            if (ThetaMoveGain <= 0.0)
                ThetaMoveGain = 1.0;
            if (XyToleranceMm <= 0.0)
                XyToleranceMm = 0.001;
            if (MaxXyIterations <= 0)
                MaxXyIterations = 5;
            if (MaxXyIterations > 20)
                MaxXyIterations = 20;
            if (Math.Abs(XyMoveGainX) <= double.Epsilon)
                XyMoveGainX = 1.0;
            if (Math.Abs(XyMoveGainY) <= double.Epsilon)
                XyMoveGainY = 1.0;
            if (FineAlignMaxXyMoveMm <= 0.0)
                FineAlignMaxXyMoveMm = 0.2;
            if (FineAlignMaxXyMoveMm > 2.0)
                FineAlignMaxXyMoveMm = 2.0;
            if (CocRotationVelocityDegPerSec <= 0.0)
                CocRotationVelocityDegPerSec = 30.0;
            if (CocRotationVelocityDegPerSec > 360.0)
                CocRotationVelocityDegPerSec = 360.0;
        }

        public static string NormalizeBottomFinderName(string finderName)
        {
            if (string.IsNullOrWhiteSpace(finderName))
                return DefaultBottomFinderName;

            string value = finderName.Trim();
            if (string.Equals(value, "COLLET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Collet", StringComparison.OrdinalIgnoreCase))
                return DefaultBottomFinderName;

            return value;
        }
    }

    [DataContract]
    public sealed class ColletCalibrationRecord
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public VisionFocusPickerSide Side { get; set; }
        [DataMember] public int ColletNo { get; set; }
        [DataMember] public double CenterPixelX { get; set; }
        [DataMember] public double CenterPixelY { get; set; }
        [DataMember] public double CenterMmX { get; set; }
        [DataMember] public double CenterMmY { get; set; }
        [DataMember] public double OffsetX { get; set; }
        [DataMember] public double OffsetY { get; set; }
        [DataMember] public double ThetaOffset { get; set; }
        [DataMember] public double TZeroHomeOffset { get; set; }
        [DataMember] public double MeasuredTPosition { get; set; }
        [DataMember] public double FinalPickerX { get; set; }
        [DataMember] public double FinalPickerY { get; set; }
        [DataMember] public double FinalPickerZ { get; set; }
        [DataMember] public double FinalPickerT { get; set; }
        // Bottom AF Z Offset(mm): AF Best Z 기반 새 검사 Z와 기존 Bottom 티칭 Z의 차이. +면 덜 내려오고 -면 더 내려온다.
        [DataMember] public double AfZOffset { get; set; }
        [DataMember] public double RotationCenterPixelX { get; set; }
        [DataMember] public double RotationCenterPixelY { get; set; }
        [DataMember] public double RotationCenterRadiusPixel { get; set; }
        [DataMember] public int RotationCenterSampleCount { get; set; }
        [DataMember] public bool RotationCenterValid { get; set; }
        [DataMember] public DateTime RotationCenterUpdatedAt { get; set; }
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }

        public void EnsureDefaults(VisionFocusPickerSide side, int colletNo)
        {
            Side = side;
            ColletNo = colletNo < 1 ? 1 : colletNo > 4 ? 4 : colletNo;
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            RotationCenterUpdatedAt = EnsureSerializableDateTime(RotationCenterUpdatedAt);
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }

    [DataContract]
    public sealed class ColletCalibrationData
    {
        [DataMember] public ColletCalibrationSettings Settings { get; set; } = new ColletCalibrationSettings();
        [DataMember] public ColletCalibrationRecord[] FrontCollets { get; set; } = CreateRecords(VisionFocusPickerSide.Front);
        [DataMember] public ColletCalibrationRecord[] RearCollets { get; set; } = CreateRecords(VisionFocusPickerSide.Rear);

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            if (Settings == null)
                Settings = new ColletCalibrationSettings();
            Settings.EnsureDefaults();
            FrontCollets = EnsureRecords(FrontCollets, VisionFocusPickerSide.Front);
            RearCollets = EnsureRecords(RearCollets, VisionFocusPickerSide.Rear);
        }

        public ColletCalibrationRecord GetRecord(VisionFocusPickerSide side, int colletNo)
        {
            EnsureObjects();
            int index = NormalizeIndex(colletNo);
            return side == VisionFocusPickerSide.Front ? FrontCollets[index] : RearCollets[index];
        }

        private static ColletCalibrationRecord[] EnsureRecords(ColletCalibrationRecord[] records, VisionFocusPickerSide side)
        {
            if (records == null || records.Length != 4)
            {
                ColletCalibrationRecord[] next = CreateRecords(side);
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
                    records[i] = new ColletCalibrationRecord();
                records[i].EnsureDefaults(side, i + 1);
            }

            return records;
        }

        private static ColletCalibrationRecord[] CreateRecords(VisionFocusPickerSide side)
        {
            return new[]
            {
                CreateRecord(side, 1),
                CreateRecord(side, 2),
                CreateRecord(side, 3),
                CreateRecord(side, 4)
            };
        }

        private static ColletCalibrationRecord CreateRecord(VisionFocusPickerSide side, int colletNo)
        {
            return new ColletCalibrationRecord { Side = side, ColletNo = colletNo };
        }

        private static int NormalizeIndex(int colletNo)
        {
            if (colletNo < 1) return 0;
            if (colletNo > 4) return 3;
            return colletNo - 1;
        }
    }
}
