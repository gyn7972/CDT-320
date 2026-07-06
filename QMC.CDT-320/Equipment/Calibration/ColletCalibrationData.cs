using System;
using System.Runtime.Serialization;

namespace QMC.CDT320.Calibration
{
    [DataContract]
    public sealed class ColletCalibrationSettings
    {
        public const string DefaultBottomFinderName = "ColletFinder";

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

        public void EnsureDefaults()
        {
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
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }

        public void EnsureDefaults(VisionFocusPickerSide side, int colletNo)
        {
            Side = side;
            ColletNo = colletNo < 1 ? 1 : colletNo > 4 ? 4 : colletNo;
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
