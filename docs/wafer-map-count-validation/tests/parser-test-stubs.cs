using System;
using System.Collections.Generic;
using System.IO;

// These substitutes isolate the real parser from machine initialization and disk logging.
// Parser/model source files are compiled directly, without replacing their contents.
namespace QMC.CDT320.Materials
{
    public enum DieResult { Unknown, Good, NG }
    public enum TapeFrameRotate { None, R90, R180, R270 }

    public sealed class DieTapeFrame
    {
        public int DieMapX { get; set; }
        public int DieMapY { get; set; }
        public string ObjId { get; set; }
        public double PitchX { get; set; }
        public double PitchY { get; set; }
        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public TapeFrameRotate Rotate { get; set; }
    }
}

namespace QMC.Common.Data.Store
{
    public static class JsonPrettySerializer
    {
        public static void WriteObject(Stream stream, Type type, object value)
        {
            throw new NotSupportedException("Parser regression tests must not save production maps.");
        }
    }
}

namespace QMC.Common.Logging
{
    public enum EventKind { Event, Warning }

    public static class EventLogger
    {
        public static readonly List<string> Warnings = new List<string>();

        public static void Write(EventKind kind, params string[] parts)
        {
            if (kind == EventKind.Warning)
                Warnings.Add(string.Join(" | ", parts));
        }
    }
}
