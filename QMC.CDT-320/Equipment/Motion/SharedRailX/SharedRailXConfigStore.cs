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
        [DataMember(Order = 2)] public bool RequireSameVelocityForGroupMove { get; set; }
        [DataMember(Order = 3)] public List<SharedRailXAxisTestRow> Axes { get; set; }
        [DataMember(Order = 4)] public List<SharedRailXCollisionPairRow> CollisionPairs { get; set; }

        public SharedRailXConfigDocument()
        {
            Axes = new List<SharedRailXAxisTestRow>();
            CollisionPairs = new List<SharedRailXCollisionPairRow>();
        }
    }

    [DataContract]
    public sealed class SharedRailXAxisTestRow
    {
        [DataMember(Order = 0)] public string Axis { get; set; }
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
            config.RequireSameVelocityForGroupMove = document.RequireSameVelocityForGroupMove;

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
            document.RequireSameVelocityForGroupMove = config.RequireSameVelocityForGroupMove;
            document.CollisionPairs.Clear();

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
                RequireSameVelocityForGroupMove = true
            };

            document.Axes.Add(CreateRow(SharedRailXAxis.InputVisionX, 0.0, 5.0));
            document.Axes.Add(CreateRow(SharedRailXAxis.FrontPickerX, 0.0, 5.0));
            document.Axes.Add(CreateRow(SharedRailXAxis.RearPickerX, 0.0, 5.0));
            document.Axes.Add(CreateRow(SharedRailXAxis.OutputVisionX, 0.0, 5.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.InputVisionX, SharedRailXAxis.FrontPickerX, 19.0, 1, -1, 10.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.InputVisionX, SharedRailXAxis.RearPickerX, 19.0, 1, -1, 10.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.OutputVisionX, SharedRailXAxis.FrontPickerX, 500.0, -1, 1, 10.0));
            document.CollisionPairs.Add(CreatePairRow(SharedRailXAxis.OutputVisionX, SharedRailXAxis.RearPickerX, 500.0, -1, 1, 10.0));
            return document;
        }

        private static SharedRailXAxisTestRow CreateRow(
            SharedRailXAxis axis,
            double target,
            double velocity)
        {
            return new SharedRailXAxisTestRow
            {
                Axis = axis.ToString(),
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
                document.Axes = new List<SharedRailXAxisTestRow>();
            if (document.CollisionPairs == null)
                document.CollisionPairs = new List<SharedRailXCollisionPairRow>();

            EnsureRow(document, SharedRailXAxis.InputVisionX);
            EnsureRow(document, SharedRailXAxis.FrontPickerX);
            EnsureRow(document, SharedRailXAxis.RearPickerX);
            EnsureRow(document, SharedRailXAxis.OutputVisionX);
            EnsureDefaultCollisionPairs(document);
            RemoveInputOutputVisionCollisionPairs(document);

            foreach (SharedRailXAxisTestRow row in document.Axes)
            {
                if (row == null)
                    continue;
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

        private static void EnsureRow(SharedRailXConfigDocument document, SharedRailXAxis axis)
        {
            foreach (SharedRailXAxisTestRow row in document.Axes)
            {
                if (row != null && string.Equals(row.Axis, axis.ToString(), StringComparison.OrdinalIgnoreCase))
                    return;
            }

            SharedRailXConfigDocument defaults = CreateDefaultDocument();
            foreach (SharedRailXAxisTestRow row in defaults.Axes)
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
