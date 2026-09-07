using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using QMC.CDT320.Materials;
using QMC.Common;

// 실제 장비, MaterialStorage, 파일 저장기를 실행하지 않는 현재 소스 기반 복사 회귀검증.
internal static class CloneTests
{
    private static int _assertions;
    private static int _scenarios;

    private static int Main()
    {
        try
        {
            Run("current model enables typed clone", VerifyCurrentShape);
            Run("non-default graph including saved review", () => VerifyCopies(CreateSample(1)));
            Run("false boolean graph", () => VerifyCopies(CreateSample(2)));
            Run("null and empty snapshot", () =>
            {
                Assert(Invoke("CloneSnapshotTyped", null, true) == null, "null typed snapshot");
                Assert(Invoke("CloneSnapshotForSave", new object[] { null }) == null, "null reflection snapshot");
                DateTime savedAt = new DateTime(2026, 9, 7, 12, 34, 56, DateTimeKind.Utc);
                VerifyCopies(new MaterialSnapshot { SavedAt = savedAt });
                VerifyCopies(new MaterialSnapshot { SavedAt = savedAt, Cassettes = null, Wafers = null, Dies = null, PickupBinNumbers = null });
            });
            Run("null nested objects and collections", VerifyNullNested);
            Run("shared references remain detached from original", VerifySharedGraph);
            Run("inspection detail disabled", () => VerifySavePolicy(false, false, false));
            Run("inspection detail enabled", () => VerifySavePolicy(true, false, false));
            Run("unavailable settings retain detail", () => VerifySavePolicy(false, true, false));
            Run("settings access failure retains detail", () => VerifySavePolicy(false, false, true));
            Run("application exit retains detail", VerifyExitSave);
            Run("large graph matches reflection serialization", VerifyLargeGraph);
            Console.WriteLine("PASS: " + _scenarios + " scenarios, " + _assertions + " assertions.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Run(string name, Action body)
    {
        AppSettingsStore.ThrowOnRead = false;
        AppSettingsStore.Current = new AppSettings { SaveMaterialInspectionDetail = true };
        SetField("_applicationExitFullSave", false);
        SetField("_typedCloneUsable", -1);
        body();
        _scenarios++;
        Console.WriteLine("PASS: " + name);
    }

    private static void VerifyCurrentShape()
    {
        Assert((bool)Invoke("IsTypedCloneUsable"), "current model must use typed clone");
        Type[] types = { typeof(MaterialSnapshot), typeof(WaferMaterial), typeof(InputStageReviewSavedVerification),
            typeof(InputStageReviewGeometryContext), typeof(InputStageReviewGeometryTolerance), typeof(InputStageReviewMeasurement) };
        foreach (Type type in types)
        {
            int count = Properties(type).Length;
            Assert((bool)Invoke("MatchesTypedCloneShape", type, count), "actual shape accepted: " + type.Name);
            Assert(!(bool)Invoke("MatchesTypedCloneShape", type, count + 1), "changed shape rejected: " + type.Name);
        }
        Assert(!(bool)Invoke("MatchesTypedCloneShape", typeof(WaferMaterial), 82), "old 82-property guard reproduces fallback");
    }

    // 값 생성은 생산 clone의 할당 목록에 의존하지 않는다. 신규 프로퍼티가 기본값으로 누락되면 비교가 실패한다.
    private static MaterialSnapshot CreateSample(int seed)
    {
        return (MaterialSnapshot)Populate(typeof(MaterialSnapshot), "snapshot", seed);
    }

    private static object Populate(Type type, string path, int seed)
    {
        Type nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null) return Populate(nullable, path, seed);
        if (type == typeof(string)) return path + "_한글_" + seed;
        if (type == typeof(bool)) return (seed % 2) != 0;
        if (type == typeof(int)) return 100 + seed;
        if (type == typeof(long)) return 9000000000L + seed;
        if (type == typeof(double)) return 10.125 + seed;
        if (type == typeof(DateTime)) return new DateTime(2026, 9, 7, 12, 34, 56, DateTimeKind.Utc).AddSeconds(seed);
        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }
        if (typeof(IList).IsAssignableFrom(type))
        {
            IList list = (IList)Activator.CreateInstance(type);
            Type itemType = type.GetGenericArguments()[0];
            list.Add(Populate(itemType, path + "[0]", seed));
            list.Add(itemType.IsValueType ? Populate(itemType, path + "[1]", seed + 1) : null);
            return list;
        }
        object value = Activator.CreateInstance(type);
        foreach (PropertyInfo property in Properties(type))
            property.SetValue(value, Populate(property.PropertyType, path + "." + property.Name, seed), null);
        return value;
    }

    private static void VerifyCopies(MaterialSnapshot original)
    {
        MaterialSnapshot reflected = (MaterialSnapshot)Invoke("CloneSnapshotForSave", original);
        MaterialSnapshot typed = (MaterialSnapshot)Invoke("CloneSnapshotTyped", original, true);
        Compare(original, reflected, "reflection");
        Compare(reflected, typed, "typed");
        AssertIndependent(original, typed);
        Assert(Serialize(original) == Serialize(typed), "typed JSON equals original JSON");
        Assert(Serialize(reflected) == Serialize(typed), "typed JSON equals reflection JSON");
        Compare(typed, Deserialize(Serialize(typed)), "reload");
        Compare(typed, MaterialSnapshotStore.CreateStateCopy(original), "state copy");
    }

    private static void VerifyNullNested()
    {
        MaterialSnapshot sample = CreateSample(1);
        sample.Wafers[0].InputStageReviewVerification = null;
        sample.Wafers[0].InputStageRunReviewOrderedDieIds = null;
        sample.Wafers[0].DieIds = new List<string>();
        sample.Wafers[0].OutputReceiveSlots = null;
        sample.Wafers[0].CurrentLocation = null;
        sample.Wafers[0].InputResultFileSessionStartedAt = null;
        sample.Cassettes[0].Slots = null;
        sample.Dies[0].CurrentLocation = null;
        sample.Dies[0].NgCodes = null;
        sample.Dies[0].Inspections[0].Measurements = null;
        sample.Dies[0].Inspections[0].Alignments = new List<InspectionAlignmentSnapshot>();
        sample.Dies[0].BinOffset = null;
        VerifyCopies(sample);
        sample.Dies[0].Inspections = null;
        sample.Wafers[0].InputStageReviewVerification = new InputStageReviewSavedVerification();
        VerifyCopies(sample);
    }

    private static void VerifySharedGraph()
    {
        MaterialSnapshot sample = CreateSample(1);
        sample.Dies.Add(sample.Dies[0]);
        sample.Dies[0].BinOffset = sample.Dies[0].WaferOffset;
        sample.Dies[0].Inspections.Add(sample.Dies[0].Inspections[0]);
        VerifyCopies(sample);
        MaterialSnapshot copy = MaterialSnapshotStore.CreateStateCopy(sample);
        string before = Serialize(sample);
        copy.Wafers[0].InputStageReviewVerification.Context.ConditionSignature = "edited";
        copy.Wafers[0].InputStageReviewVerification.Measurements[0].RawVisionDeltaX = -998;
        copy.Dies[0].Inspections[0].Measurements[0].Name = "edited";
        copy.Wafers[0].DieIds.Clear();
        Assert(Serialize(sample) == before, "editing copy must not change original nested data");
    }

    private static void VerifySavePolicy(bool includeDetail, bool unavailable, bool throws)
    {
        AppSettingsStore.Current = unavailable ? null : new AppSettings { SaveMaterialInspectionDetail = includeDetail };
        AppSettingsStore.ThrowOnRead = throws;
        MaterialSnapshot original = CreateSample(1);
        string before = Serialize(original);
        MaterialSnapshot typed = MaterialSnapshotStore.CreateSaveCopy(original);
        SetField("_typedCloneUsable", 0);
        MaterialSnapshot fallback = MaterialSnapshotStore.CreateSaveCopy(original);
        Compare(typed, fallback, "save policy typed/fallback");
        AssertIndependent(original, typed);
        int expected = includeDetail || unavailable || throws ? 2 : 0;
        Assert(typed.Dies[0].Inspections[0].Measurements.Count == expected, "measurement detail policy");
        Assert(typed.Dies[0].Inspections[0].Alignments.Count == expected, "alignment detail policy");
        Assert(Serialize(original) == before, "saving must not trim original");
        Compare(original, MaterialSnapshotStore.CreateStateCopy(original), "state copy retains detail");
    }

    private static void VerifyExitSave()
    {
        AppSettingsStore.Current = new AppSettings { SaveMaterialInspectionDetail = false };
        MaterialSnapshotStore.BeginApplicationExitFullSave();
        MaterialSnapshot original = CreateSample(1);
        Compare(original, MaterialSnapshotStore.CreateSaveCopy(original), "exit typed retains detail");
        SetField("_typedCloneUsable", 0);
        Compare(original, MaterialSnapshotStore.CreateSaveCopy(original), "exit fallback retains detail");
    }

    private static void VerifyLargeGraph()
    {
        MaterialSnapshot sample = CreateSample(1);
        sample.Wafers.Clear();
        for (int index = 0; index < 4; index++)
            sample.Wafers.Add((WaferMaterial)Populate(typeof(WaferMaterial), "wafer" + index, index + 1));
        sample.Dies.Clear();
        for (int index = 0; index < 3128; index++)
            sample.Dies.Add((DieMaterial)Populate(typeof(DieMaterial), "die" + index, index + 1));
        VerifyCopies(sample);
        MethodInfo reflection = typeof(MaterialSnapshotStore).GetMethod("CloneSnapshotForSave", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo typed = typeof(MaterialSnapshotStore).GetMethod("CloneSnapshotTyped", BindingFlags.Static | BindingFlags.NonPublic);
        var oldClone = (Func<MaterialSnapshot, MaterialSnapshot>)Delegate.CreateDelegate(typeof(Func<MaterialSnapshot, MaterialSnapshot>), reflection);
        var fastClone = (Func<MaterialSnapshot, bool, MaterialSnapshot>)Delegate.CreateDelegate(typeof(Func<MaterialSnapshot, bool, MaterialSnapshot>), typed);
        for (int pass = 0; pass < 3; pass++) { oldClone(sample); fastClone(sample, true); }
        double[] reflectionMs = new double[7];
        double[] typedMs = new double[7];
        for (int pass = 0; pass < reflectionMs.Length; pass++)
        {
            var watch = Stopwatch.StartNew();
            MaterialSnapshot oldResult = oldClone(sample);
            watch.Stop();
            reflectionMs[pass] = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            MaterialSnapshot typedResult = fastClone(sample, true);
            watch.Stop();
            typedMs[pass] = watch.Elapsed.TotalMilliseconds;
            GC.KeepAlive(oldResult);
            GC.KeepAlive(typedResult);
        }
        Array.Sort(reflectionMs);
        Array.Sort(typedMs);
        Console.WriteLine("LOCAL TIMING ONLY (4 wafers, 3128 dies, inspection details, 7 runs after 3 warmups): reflection median=" +
            reflectionMs[3].ToString("F3") + " ms; typed median=" + typedMs[3].ToString("F3") + " ms. No equipment timing guarantee.");
    }

    private static void Compare(object expected, object actual, string path)
    {
        if (expected == null || actual == null)
        {
            Assert(expected == null && actual == null, path + " null mismatch");
            return;
        }
        Assert(expected.GetType() == actual.GetType(), path + " type mismatch");
        Type type = expected.GetType();
        if (type.IsValueType || type == typeof(string))
        {
            Assert(expected.Equals(actual), path + " value mismatch");
            return;
        }
        IList list = expected as IList;
        if (list != null)
        {
            IList actualList = (IList)actual;
            Assert(list.Count == actualList.Count, path + " count mismatch");
            for (int i = 0; i < list.Count; i++) Compare(list[i], actualList[i], path + "[" + i + "]");
            return;
        }
        foreach (PropertyInfo property in Properties(type))
            Compare(property.GetValue(expected, null), property.GetValue(actual, null), path + "." + property.Name);
    }

    private static void AssertIndependent(object original, object copy)
    {
        var originals = new HashSet<object>(ReferenceComparer.Instance);
        VisitReferences(original, item => originals.Add(item));
        VisitReferences(copy, item => Assert(!originals.Contains(item), "clone shares mutable original " + item.GetType().Name));
    }

    private static void VisitReferences(object value, Action<object> visit)
    {
        if (value == null || value.GetType().IsValueType || value is string) return;
        visit(value);
        IList list = value as IList;
        if (list != null)
        {
            foreach (object item in list) VisitReferences(item, visit);
            return;
        }
        foreach (PropertyInfo property in Properties(value.GetType())) VisitReferences(property.GetValue(value, null), visit);
    }

    private static PropertyInfo[] Properties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0).ToArray();
    }

    private static string Serialize(MaterialSnapshot value)
    {
        using (var stream = new MemoryStream())
        {
            new DataContractJsonSerializer(typeof(MaterialSnapshot)).WriteObject(stream, value);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    private static MaterialSnapshot Deserialize(string json)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            return (MaterialSnapshot)new DataContractJsonSerializer(typeof(MaterialSnapshot)).ReadObject(stream);
    }

    private static object Invoke(string name, params object[] arguments)
    {
        MethodInfo method = typeof(MaterialSnapshotStore).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(m => m.Name == name && m.GetParameters().Length == arguments.Length);
        return method.Invoke(null, arguments);
    }

    private static void SetField(string name, object value)
    {
        typeof(MaterialSnapshotStore).GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
    }

    private static void Assert(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new ReferenceComparer();
        public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
        public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
    }
}

namespace QMC.Common
{
    public sealed class AppSettings { public bool SaveMaterialInspectionDetail { get; set; } }
    public static class AppSettingsStore
    {
        private static AppSettings _current;
        public static bool ThrowOnRead { get; set; }
        public static AppSettings Current
        {
            get { if (ThrowOnRead) throw new IOException("Injected settings read failure"); return _current; }
            set { _current = value; }
        }
    }
    public static class Log
    {
        public static void Write(string category, string user, string action, string message) { }
    }
}
