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

        public SharedRailXConfigDocument()
        {
            Axes = new List<SharedRailXAxisGeometryRow>();
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

        private static void Normalize(SharedRailXConfigDocument document)
        {
            if (document == null)
                return;

            if (document.DefaultSafetyDistance <= 0.0)
                document.DefaultSafetyDistance = 10.0;
            if (document.Axes == null)
                document.Axes = new List<SharedRailXAxisGeometryRow>();

            EnsureRow(document, SharedRailXAxis.InputVisionX);
            EnsureRow(document, SharedRailXAxis.FrontPickerX);
            EnsureRow(document, SharedRailXAxis.RearPickerX);
            EnsureRow(document, SharedRailXAxis.OutputVisionX);

            foreach (SharedRailXAxisGeometryRow row in document.Axes)
            {
                if (row == null)
                    continue;
                if (row.PositionScale == 0.0)
                    row.PositionScale = 1.0;
                if (row.TestVelocity <= 0.0)
                    row.TestVelocity = 5.0;
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
