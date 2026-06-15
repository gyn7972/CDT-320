using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Motion.SharedRailX
{
    [DataContract]
    public sealed class SharedRailXConfigDocument
    {
        [DataMember(Order = 0)] public double DefaultSafetyDistance { get; set; }
        [DataMember(Order = 1)] public bool EnablePathCheck { get; set; }
        [DataMember(Order = 2)] public bool RequireSameVelocityForGroupMove { get; set; }
        [DataMember(Order = 3)] public List<SharedRailXAxisGeometryRow> Axes { get; set; }
        [DataMember(Order = 4)] public List<SharedRailXCollisionPairRow> CollisionPairs { get; set; }

        public SharedRailXConfigDocument()
        {
            Axes = new List<SharedRailXAxisGeometryRow>();
            CollisionPairs = new List<SharedRailXCollisionPairRow>();
        }
    }

    [DataContract]
    public sealed class SharedRailXAxisGeometryRow
    {
        [DataMember(Order = 0)] public string Axis { get; set; }
        [DataMember(Order = 1)] public double BodyOffsetMin { get; set; }
        [DataMember(Order = 2)] public double BodyOffsetMax { get; set; }
        [DataMember(Order = 3)] public double RailOriginOffset { get; set; }
        [DataMember(Order = 4)] public double PositionScale { get; set; }
        [DataMember(Order = 5)] public double? SafetyDistance { get; set; }
        [DataMember(Order = 6)] public double TestTargetPosition { get; set; }
        [DataMember(Order = 7)] public double TestVelocity { get; set; }
    }

    [DataContract]
    public sealed class SharedRailXCollisionPairRow
    {
        [DataMember(Order = 0)] public string AxisA { get; set; }
        [DataMember(Order = 1)] public string AxisB { get; set; }
        [DataMember(Order = 2)] public double HomeClearance { get; set; }
        [DataMember(Order = 3)] public int AxisATowardSign { get; set; }
        [DataMember(Order = 4)] public int AxisBTowardSign { get; set; }
        [DataMember(Order = 5)] public double? SafetyDistance { get; set; }
    }

    public static class SharedRailXConfigStore
    {
        public static string Path_
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "shared_rail_x.json");
            }
        }

        public static SharedRailXConfig LoadOrCreateDefault()
        {
            SharedRailXConfigDocument document = LoadDocumentOrCreateDefault();
            return ToConfig(document);
        }

        public static SharedRailXConfigDocument LoadDocumentOrCreateDefault()
        {
            try
            {
                if (!File.Exists(Path_))
                {
                    SharedRailXConfigDocument created = CreateDefaultDocument();
                    SaveDocument(created);
                    return created;
                }

                using (FileStream fs = File.OpenRead(Path_))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SharedRailXConfigDocument));
                    SharedRailXConfigDocument document = serializer.ReadObject(fs) as SharedRailXConfigDocument;
                    Normalize(document);
                    return document ?? CreateDefaultDocument();
                }
            }
            catch
            {
                return CreateDefaultDocument();
            }
            finally
            {
            }
        }

        public static void SaveDocument(SharedRailXConfigDocument document)
        {
            if (document == null)
                document = CreateDefaultDocument();

            Normalize(document);
            string dir = Path.GetDirectoryName(Path_);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using (FileStream fs = File.Create(Path_))
            {
                JsonPrettySerializer.WriteObject(fs, typeof(SharedRailXConfigDocument), document);
            }
        }

        public static SharedRailXConfig ToConfig(SharedRailXConfigDocument document)
        {
            Normalize(document);
            SharedRailXConfig config = SharedRailXConfig.CreateDefault();
            config.DefaultSafetyDistance = document.DefaultSafetyDistance;
            config.EnablePathCheck = document.EnablePathCheck;
            config.RequireSameVelocityForGroupMove = document.RequireSameVelocityForGroupMove;

            foreach (SharedRailXAxisGeometryRow row in document.Axes)
            {
                SharedRailXAxis axis;
                if (!Enum.TryParse(row.Axis, true, out axis))
                    continue;

                config.SetGeometry(
                    axis,
                    row.BodyOffsetMin,
                    row.BodyOffsetMax,
                    row.SafetyDistance,
                    row.RailOriginOffset,
                    row.PositionScale);
            }

            var pairs = new List<SharedRailXAxisPair>();
            foreach (SharedRailXCollisionPairRow row in document.CollisionPairs)
            {
                if (row == null)
                    continue;

                SharedRailXAxis axisA;
                SharedRailXAxis axisB;
                if (!Enum.TryParse(row.AxisA, true, out axisA) ||
                    !Enum.TryParse(row.AxisB, true, out axisB))
                    continue;

                pairs.Add(new SharedRailXAxisPair(
                    axisA,
                    axisB,
                    row.HomeClearance,
                    NormalizeSign(row.AxisATowardSign),
                    NormalizeSign(row.AxisBTowardSign),
                    row.SafetyDistance));
            }

            config.SetCollisionPairs(pairs);

            return config;
        }

        public static SharedRailXConfigDocument FromConfig(SharedRailXConfig config)
        {
            SharedRailXConfigDocument document = CreateDefaultDocument();
            if (config == null)
                return document;

            document.DefaultSafetyDistance = config.DefaultSafetyDistance;
            document.EnablePathCheck = config.EnablePathCheck;
            document.RequireSameVelocityForGroupMove = config.RequireSameVelocityForGroupMove;
            document.CollisionPairs.Clear();

            foreach (SharedRailXAxisGeometryRow row in document.Axes)
            {
                SharedRailXAxis axis;
                if (!Enum.TryParse(row.Axis, true, out axis))
                    continue;

                SharedRailXAxisGeometry geometry;
                if (!config.Geometry.TryGetValue(axis, out geometry) || geometry == null)
                    continue;

                row.BodyOffsetMin = geometry.BodyOffsetMin;
                row.BodyOffsetMax = geometry.BodyOffsetMax;
                row.RailOriginOffset = geometry.RailOriginOffset;
                row.PositionScale = geometry.PositionScale;
                row.SafetyDistance = geometry.SafetyDistance;
            }

            if (config.CollisionPairs != null)
            {
                foreach (SharedRailXAxisPair pair in config.CollisionPairs)
                    document.CollisionPairs.Add(CreatePairRow(
                        pair.AxisA,
                        pair.AxisB,
                        pair.HomeClearance,
                        pair.AxisATowardSign,
                        pair.AxisBTowardSign,
                        pair.SafetyDistance));
            }

            return document;
        }

        public static SharedRailXConfigDocument CreateDefaultDocument()
        {
            var document = new SharedRailXConfigDocument
            {
                DefaultSafetyDistance = 10.0,
                EnablePathCheck = true,
                RequireSameVelocityForGroupMove = true
            };

            document.Axes.Add(CreateRow(SharedRailXAxis.InputVisionX, -100.0, 100.0, 0.0, 1.0, null, 0.0, 5.0));
            document.Axes.Add(CreateRow(SharedRailXAxis.FrontPickerX, -100.0, 100.0, 300.0, 1.0, null, 0.0, 5.0));
            document.Axes.Add(CreateRow(SharedRailXAxis.RearPickerX, -100.0, 100.0, 600.0, 1.0, null, 0.0, 5.0));
            document.Axes.Add(CreateRow(SharedRailXAxis.OutputVisionX, -100.0, 100.0, 900.0, 1.0, null, 0.0, 5.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.InputVisionX, SharedRailXAxis.FrontPickerX, 19.0, 1, -1, 10.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.InputVisionX, SharedRailXAxis.RearPickerX, 19.0, 1, -1, 10.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.OutputVisionX, SharedRailXAxis.FrontPickerX, 500.0, -1, 1, 10.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.OutputVisionX, SharedRailXAxis.RearPickerX, 500.0, -1, 1, 10.0));
            return document;
        }

        private static SharedRailXAxisGeometryRow CreateRow(
            SharedRailXAxis axis,
            double bodyMin,
            double bodyMax,
            double origin,
            double scale,
            double? safety,
            double target,
            double velocity)
        {
            return new SharedRailXAxisGeometryRow
            {
                Axis = axis.ToString(),
                BodyOffsetMin = bodyMin,
                BodyOffsetMax = bodyMax,
                RailOriginOffset = origin,
                PositionScale = scale,
                SafetyDistance = safety,
                TestTargetPosition = target,
                TestVelocity = velocity
            };
        }

        private static SharedRailXCollisionPairRow CreatePairRow(SharedRailXAxis axisA, SharedRailXAxis axisB)
        {
            SharedRailXCollisionPairRow defaults = CreateDefaultPairRow(axisA, axisB);
            return defaults ?? new SharedRailXCollisionPairRow
            {
                AxisA = axisA.ToString(),
                AxisB = axisB.ToString()
            };
        }

        private static SharedRailXCollisionPairRow CreatePairRow(
            SharedRailXAxis axisA,
            SharedRailXAxis axisB,
            double homeClearance,
            int axisATowardSign,
            int axisBTowardSign,
            double? safetyDistance)
        {
            return new SharedRailXCollisionPairRow
            {
                AxisA = axisA.ToString(),
                AxisB = axisB.ToString(),
                HomeClearance = homeClearance,
                AxisATowardSign = NormalizeSign(axisATowardSign),
                AxisBTowardSign = NormalizeSign(axisBTowardSign),
                SafetyDistance = safetyDistance
            };
        }

        private static SharedRailXCollisionPairRow CreateDefaultPairRow(SharedRailXAxis axisA, SharedRailXAxis axisB)
        {
            if (MatchesPair(axisA, axisB, SharedRailXAxis.InputVisionX, SharedRailXAxis.FrontPickerX))
                return CreatePairRow(axisA, axisB, 19.0, SignForAxis(axisA, SharedRailXAxis.InputVisionX, 1, -1), SignForAxis(axisB, SharedRailXAxis.InputVisionX, 1, -1), 10.0);
            if (MatchesPair(axisA, axisB, SharedRailXAxis.InputVisionX, SharedRailXAxis.RearPickerX))
                return CreatePairRow(axisA, axisB, 19.0, SignForAxis(axisA, SharedRailXAxis.InputVisionX, 1, -1), SignForAxis(axisB, SharedRailXAxis.InputVisionX, 1, -1), 10.0);
            if (MatchesPair(axisA, axisB, SharedRailXAxis.OutputVisionX, SharedRailXAxis.FrontPickerX))
                return CreatePairRow(axisA, axisB, 500.0, SignForAxis(axisA, SharedRailXAxis.OutputVisionX, -1, 1), SignForAxis(axisB, SharedRailXAxis.OutputVisionX, -1, 1), 10.0);
            if (MatchesPair(axisA, axisB, SharedRailXAxis.OutputVisionX, SharedRailXAxis.RearPickerX))
                return CreatePairRow(axisA, axisB, 500.0, SignForAxis(axisA, SharedRailXAxis.OutputVisionX, -1, 1), SignForAxis(axisB, SharedRailXAxis.OutputVisionX, -1, 1), 10.0);

            return null;
        }

        private static bool MatchesPair(SharedRailXAxis axisA, SharedRailXAxis axisB, SharedRailXAxis expectedA, SharedRailXAxis expectedB)
        {
            return (axisA == expectedA && axisB == expectedB) ||
                   (axisA == expectedB && axisB == expectedA);
        }

        private static int SignForAxis(SharedRailXAxis axis, SharedRailXAxis primaryAxis, int primarySign, int otherSign)
        {
            return axis == primaryAxis ? primarySign : otherSign;
        }

        private static int NormalizeSign(int sign)
        {
            if (sign > 0)
                return 1;
            if (sign < 0)
                return -1;
            return 0;
        }

        private static void Normalize(SharedRailXConfigDocument document)
        {
            if (document == null)
                return;

            if (document.DefaultSafetyDistance <= 0.0)
                document.DefaultSafetyDistance = 10.0;
            if (document.Axes == null)
                document.Axes = new List<SharedRailXAxisGeometryRow>();
            if (document.CollisionPairs == null)
                document.CollisionPairs = new List<SharedRailXCollisionPairRow>();

            EnsureRow(document, SharedRailXAxis.InputVisionX);
            EnsureRow(document, SharedRailXAxis.FrontPickerX);
            EnsureRow(document, SharedRailXAxis.RearPickerX);
            EnsureRow(document, SharedRailXAxis.OutputVisionX);
            EnsureDefaultCollisionPairs(document);
            RemoveInputOutputVisionCollisionPairs(document);
            NormalizeLegacyZeroGeometry(document);

            foreach (SharedRailXAxisGeometryRow row in document.Axes)
            {
                if (row == null)
                    continue;
                if (row.PositionScale == 0.0)
                    row.PositionScale = 1.0;
                if (row.TestVelocity <= 0.0)
                    row.TestVelocity = 5.0;
            }

            foreach (SharedRailXCollisionPairRow row in document.CollisionPairs)
                NormalizeCollisionPairRule(row);
        }

        private static void NormalizeCollisionPairRule(SharedRailXCollisionPairRow row)
        {
            if (row == null)
                return;

            SharedRailXAxis axisA;
            SharedRailXAxis axisB;
            if (!Enum.TryParse(row.AxisA, true, out axisA) ||
                !Enum.TryParse(row.AxisB, true, out axisB))
            {
                return;
            }

            row.AxisATowardSign = NormalizeSign(row.AxisATowardSign);
            row.AxisBTowardSign = NormalizeSign(row.AxisBTowardSign);
            if (row.HomeClearance > 0.0 &&
                row.AxisATowardSign != 0 &&
                row.AxisBTowardSign != 0)
            {
                return;
            }

            SharedRailXCollisionPairRow defaults = CreateDefaultPairRow(axisA, axisB);
            if (defaults == null)
                return;

            row.HomeClearance = defaults.HomeClearance;
            row.AxisATowardSign = defaults.AxisATowardSign;
            row.AxisBTowardSign = defaults.AxisBTowardSign;
            if (!row.SafetyDistance.HasValue)
                row.SafetyDistance = defaults.SafetyDistance;
        }

        private static void EnsureDefaultCollisionPairs(SharedRailXConfigDocument document)
        {
            if (document == null)
                return;
            if (document.CollisionPairs == null)
                document.CollisionPairs = new List<SharedRailXCollisionPairRow>();
            if (document.CollisionPairs.Count > 0)
                return;

            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.InputVisionX, SharedRailXAxis.FrontPickerX));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.InputVisionX, SharedRailXAxis.RearPickerX));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.OutputVisionX, SharedRailXAxis.FrontPickerX));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.OutputVisionX, SharedRailXAxis.RearPickerX));
        }

        private static void RemoveInputOutputVisionCollisionPairs(SharedRailXConfigDocument document)
        {
            if (document == null || document.CollisionPairs == null)
                return;

            document.CollisionPairs.RemoveAll(IsInputOutputVisionPair);
        }

        private static bool IsInputOutputVisionPair(SharedRailXCollisionPairRow row)
        {
            if (row == null)
                return false;

            SharedRailXAxis axisA;
            SharedRailXAxis axisB;
            if (!Enum.TryParse(row.AxisA, true, out axisA) ||
                !Enum.TryParse(row.AxisB, true, out axisB))
                return false;

            return (axisA == SharedRailXAxis.InputVisionX && axisB == SharedRailXAxis.OutputVisionX) ||
                   (axisA == SharedRailXAxis.OutputVisionX && axisB == SharedRailXAxis.InputVisionX);
        }

        private static void NormalizeLegacyZeroGeometry(SharedRailXConfigDocument document)
        {
            if (document == null || document.Axes == null)
                return;

            SharedRailXConfigDocument defaults = CreateDefaultDocument();
            double? firstOrigin = null;
            bool allKnownOriginsEqual = true;
            bool anyKnownAxis = false;
            foreach (SharedRailXAxisGeometryRow row in document.Axes)
            {
                if (row == null)
                    continue;

                SharedRailXAxis parsedAxis;
                if (!Enum.TryParse(row.Axis, true, out parsedAxis))
                    continue;

                anyKnownAxis = true;
                if (!firstOrigin.HasValue)
                    firstOrigin = row.RailOriginOffset;
                else if (Math.Abs(firstOrigin.Value - row.RailOriginOffset) > 0.000001)
                {
                    allKnownOriginsEqual = false;
                    break;
                }
            }

            if (!anyKnownAxis ||
                !allKnownOriginsEqual ||
                !firstOrigin.HasValue ||
                Math.Abs(firstOrigin.Value) > 0.000001)
            {
                return;
            }

            foreach (SharedRailXAxisGeometryRow row in document.Axes)
            {
                if (row == null)
                    continue;

                SharedRailXAxisGeometryRow defaultRow = defaults.Axes.Find(x =>
                    x != null && string.Equals(x.Axis, row.Axis, StringComparison.OrdinalIgnoreCase));
                if (defaultRow == null)
                    continue;

                row.BodyOffsetMin = defaultRow.BodyOffsetMin;
                row.BodyOffsetMax = defaultRow.BodyOffsetMax;
                row.RailOriginOffset = defaultRow.RailOriginOffset;
                row.PositionScale = defaultRow.PositionScale;
                if (!row.SafetyDistance.HasValue)
                    row.SafetyDistance = defaultRow.SafetyDistance;
            }
        }

        private static void EnsureRow(SharedRailXConfigDocument document, SharedRailXAxis axis)
        {
            foreach (SharedRailXAxisGeometryRow row in document.Axes)
            {
                if (row != null && string.Equals(row.Axis, axis.ToString(), StringComparison.OrdinalIgnoreCase))
                    return;
            }

            SharedRailXConfigDocument defaults = CreateDefaultDocument();
            foreach (SharedRailXAxisGeometryRow row in defaults.Axes)
            {
                if (string.Equals(row.Axis, axis.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    document.Axes.Add(row);
                    return;
                }
            }
        }
    }
}
