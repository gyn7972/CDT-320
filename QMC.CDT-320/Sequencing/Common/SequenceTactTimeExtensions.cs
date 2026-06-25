using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Diagnostics.TactTime;

namespace QMC.CDT320.Sequencing
{
    internal static class SequenceTactTimeExtensions
    {
        public static TactTimeScope Unit(this TactTimeRecorder recorder, UnitSequenceBase sequence)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, null, null);
            return Begin(recorder, TactTimeCategory.Unit, info);
        }

        public static TactTimeScope Process(this TactTimeRecorder recorder, object sequence, string processName)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, processName, null);
            return Begin(recorder, TactTimeCategory.Process, info);
        }

        public static TactTimeScope Step(this TactTimeRecorder recorder, object sequence, object step)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, null, step);
            return Begin(recorder, TactTimeCategory.Step, info);
        }

        public static Task<int> UnitAsync(
            this TactTimeRecorder recorder,
            UnitSequenceBase sequence,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, null, null);
            return MeasureIntAsync(recorder, TactTimeCategory.Unit, info, ct, action);
        }

        public static Task ProcessAsync(
            this TactTimeRecorder recorder,
            object sequence,
            string processName,
            CancellationToken ct,
            Func<Task> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, processName, null);
            return MeasureAsync(recorder, TactTimeCategory.Process, info, ct, action);
        }

        public static Task<int> ProcessAsync(
            this TactTimeRecorder recorder,
            object sequence,
            string processName,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, processName, null);
            return MeasureIntAsync(recorder, TactTimeCategory.Process, info, ct, action);
        }

        public static Task<int> StepAsync(
            this TactTimeRecorder recorder,
            object sequence,
            object step,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, null, step);
            return MeasureIntAsync(recorder, TactTimeCategory.Step, info, ct, action);
        }

        public static Task<int> MotionAsync(
            this TactTimeRecorder recorder,
            object sequence,
            string processName,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, processName, null);
            return MeasureIntAsync(recorder, TactTimeCategory.Motion, info, ct, action);
        }

        public static Task<int> VisionAsync(
            this TactTimeRecorder recorder,
            object sequence,
            string processName,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, processName, null);
            return MeasureIntAsync(recorder, TactTimeCategory.Vision, info, ct, action);
        }

        public static Task<int> WaitAsync(
            this TactTimeRecorder recorder,
            object sequence,
            string processName,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, processName, null);
            return MeasureIntAsync(recorder, TactTimeCategory.Wait, info, ct, action);
        }

        public static Task<int> ResourceAsync(
            this TactTimeRecorder recorder,
            object sequence,
            string processName,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            SequenceTactInfo info = SequenceTactInfo.From(sequence, processName, null);
            return MeasureIntAsync(recorder, TactTimeCategory.Resource, info, ct, action);
        }

        private static TactTimeScope Begin(TactTimeRecorder recorder, TactTimeCategory category, SequenceTactInfo info)
        {
            recorder = recorder ?? NullTactTimeRecorder.Instance;
            return recorder.BeginScope(
                category,
                info.UnitName,
                info.SequenceName,
                info.ProcessName,
                info.StepName,
                null,
                info.Detail);
        }

        private static Task MeasureAsync(
            TactTimeRecorder recorder,
            TactTimeCategory category,
            SequenceTactInfo info,
            CancellationToken ct,
            Func<Task> action)
        {
            recorder = recorder ?? NullTactTimeRecorder.Instance;
            return recorder.MeasureAsync(
                category,
                info.UnitName,
                info.SequenceName,
                info.ProcessName,
                info.StepName,
                ct,
                action);
        }

        private static Task<int> MeasureIntAsync(
            TactTimeRecorder recorder,
            TactTimeCategory category,
            SequenceTactInfo info,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            recorder = recorder ?? NullTactTimeRecorder.Instance;
            return recorder.MeasureAsync(
                category,
                info.UnitName,
                info.SequenceName,
                info.ProcessName,
                info.StepName,
                ct,
                action);
        }

        private sealed class SequenceTactInfo
        {
            public string UnitName;
            public string SequenceName;
            public string ProcessName;
            public string StepName;
            public string Detail;

            public static SequenceTactInfo From(object sequence, string processName, object step)
            {
                var info = new SequenceTactInfo();
                try
                {
                    if (sequence == null)
                    {
                        info.UnitName = "";
                        info.SequenceName = "";
                    }
                    else
                    {
                        UnitSequenceBase unit = sequence as UnitSequenceBase;
                        if (unit != null)
                        {
                            info.UnitName = unit.Name ?? unit.Kind.ToString();
                            info.SequenceName = sequence.GetType().Name;
                            info.Detail = "kind=" + unit.Kind + ", mode=" + unit.Mode;
                        }
                        else
                        {
                            string name = ReadMemberString(sequence, "Name");
                            string side = ReadMemberString(sequence, "Side");
                            string kind = ReadMemberString(sequence, "Kind");
                            string mode = ReadRunMode(sequence);
                            info.UnitName = ResolvePickerUnitName(name, side);
                            info.SequenceName = sequence.GetType().Name;
                            info.Detail = "name=" + name + ", side=" + side + ", kind=" + kind + ", mode=" + mode;
                        }
                    }

                    info.ProcessName = processName ?? "";
                    info.StepName = step != null ? step.ToString() : ReadMemberString(sequence, "CurrentStep");
                }
                catch (Exception ex)
                {
                    info.UnitName = sequence != null ? sequence.GetType().Name : "";
                    info.SequenceName = sequence != null ? sequence.GetType().Name : "";
                    info.ProcessName = processName ?? "";
                    info.StepName = step != null ? step.ToString() : "";
                    info.Detail = "택타임 정보 해석 실패: " + ex.Message;
                }
                finally
                {
                }

                return info;
            }

            private static string ResolvePickerUnitName(string name, string side)
            {
                if (!string.IsNullOrWhiteSpace(side))
                {
                    if (string.Equals(side, "Front", StringComparison.OrdinalIgnoreCase))
                        return "FrontPicker";
                    if (string.Equals(side, "Rear", StringComparison.OrdinalIgnoreCase))
                        return "RearPicker";
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (name.IndexOf("Front", StringComparison.OrdinalIgnoreCase) >= 0)
                        return "FrontPicker";
                    if (name.IndexOf("Rear", StringComparison.OrdinalIgnoreCase) >= 0)
                        return "RearPicker";
                    return name;
                }

                return "";
            }

            private static string ReadRunMode(object sequence)
            {
                try
                {
                    object options = ReadMemberValue(sequence, "Options");
                    if (options == null)
                        return "";

                    object runMode = ReadMemberValue(options, "RunMode");
                    return runMode != null ? runMode.ToString() : "";
                }
                catch
                {
                    return "";
                }
                finally
                {
                }
            }

            private static string ReadMemberString(object target, string name)
            {
                object value = ReadMemberValue(target, name);
                return value != null ? value.ToString() : "";
            }

            private static object ReadMemberValue(object target, string name)
            {
                if (target == null || string.IsNullOrWhiteSpace(name))
                    return null;

                Type type = target.GetType();
                while (type != null)
                {
                    PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null)
                        return property.GetValue(target, null);

                    FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null)
                        return field.GetValue(target);

                    type = type.BaseType;
                }

                return null;
            }
        }
    }
}
