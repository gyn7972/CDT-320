using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Data.Store;

[DataContract]
public sealed class UiSaveSample
{
    [DataMember] public int Value { get; set; }
    [DataMember] public List<int> Nested { get; set; } = new List<int>();
    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) { if (Value == 0) Value = 888; }
}

[DataContract]
public sealed class BrokenUiSaveSample
{
    [DataMember] public int Value { get { throw new InvalidOperationException("capture failed"); } set { } }
}

internal static class UiParameterSaveTests
{
    private static int _checks;
    private static string _root;
    private static int _gateNumber;
    private static object _writerGate;
    private static void Check(bool value, string message)
    {
        _checks++;
        if (!value) throw new Exception(message);
    }
    private static string PathOf(string name) { return Path.Combine(_root, name); }
    private static UiSaveSample Sample(int value) { return new UiSaveSample { Value = value, Nested = new List<int> { value } }; }
    private static Task<DataStoreResult> Queue(int value, string name)
    {
        return JsonDataSaveCoordinator.Enqueue(JsonDataSaveCoordinator.Capture(Sample(value), PathOf(name)));
    }
    private static void StartGate()
    {
        string path = PathOf("gate" + (++_gateNumber) + ".json");
        object state = typeof(JsonDataSaveCoordinator).GetMethod("GetState", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { path });
        _writerGate = state.GetType().GetField("Writer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(state);
        Monitor.Enter(_writerGate);
        var queued = JsonDataSaveCoordinator.Enqueue(JsonDataSaveCoordinator.Capture(Sample(-1), path));
        Check(!queued.Wait(30), "worker waits at exclusive file writer gate");
    }
    private static void ReleaseGate()
    {
        Monitor.Exit(_writerGate); _writerGate = null;
        Check(JsonDataSaveCoordinator.FlushAsync().GetAwaiter().GetResult().Success, "queue drained");
    }
    private static int Read(string name)
    {
        var loaded = JsonDataStore.Load<UiSaveSample>(PathOf(name));
        Check(loaded.Success && !loaded.UsedDefault, "load " + name);
        return loaded.Data.Value;
    }

    public static int Main(string[] args)
    {
        try
        {
            _root = Path.GetFullPath(args[0]); Directory.CreateDirectory(_root);

            var live = Sample(7);
            var frozen = JsonDataSaveCoordinator.Capture(live, PathOf("snapshot.json"));
            live.Value = 8; live.Nested[0] = 8;
            Check(JsonDataSaveCoordinator.Enqueue(frozen).Result.Success, "independent capture saved");
            var snapshot = JsonDataStore.Load<UiSaveSample>(PathOf("snapshot.json"));
            Check(snapshot.Data.Value == 7 && snapshot.Data.Nested[0] == 7, "nested live data isolated");
            Check(live.Value == 8 && live.Nested[0] == 8, "live data untouched");
            Check(JsonDataStore.Save(Sample(7), PathOf("sync.json")).Success, "sync save");
            Check(File.ReadAllText(PathOf("sync.json")) == File.ReadAllText(PathOf("snapshot.json")), "sync and queued JSON contract identical");
            Check(Queue(0, "zero.json").Result.Success, "zero value queued");
            Check(JsonDataStore.Save(Sample(0), PathOf("zero-sync.json")).Success, "zero value sync");
            Check(File.ReadAllText(PathOf("zero.json")) == File.ReadAllText(PathOf("zero-sync.json")), "capture does not invoke OnDeserialized and replace current value");

            StartGate();
            Stopwatch ui = Stopwatch.StartNew();
            Queue(1, "latest.json"); Queue(2, "latest.json");
            Check(ui.ElapsedMilliseconds < 500, "enqueue does not wait for slow disk worker");
            Check(JsonDataSaveCoordinator.HasUnfinishedSaves, "pending distinguished from durable");
            var timeout = JsonDataSaveCoordinator.FlushAsync(40).Result;
            Check(!timeout.Success, "timeout is not successful save");
            Check(JsonDataStore.Save(Sample(99), PathOf("latest.json")).Success, "new synchronous save succeeds");
            ReleaseGate();
            Check(Read("latest.json") == 99, "old queued snapshots do not overwrite newer synchronous save");

            string orderedPath = PathOf("sync-order.json");
            var orderedState = (JsonDataSaveCoordinator.FileState)typeof(JsonDataSaveCoordinator)
                .GetMethod("GetState", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { orderedPath });
            _writerGate = orderedState.Writer; Monitor.Enter(_writerGate);
            Queue(1, "sync-order.json");
            var waitingSync = Task.Run(() => JsonDataStore.Save(Sample(2), orderedPath));
            Check(SpinWait.SpinUntil(() => { lock (orderedState.Sync) return orderedState.Revision >= 2; }, 5000), "sync request registered before waiting writer");
            Queue(3, "sync-order.json");
            ReleaseGate();
            Check(waitingSync.Result.Success && Read("sync-order.json") == 3, "waiting synchronous request cannot invalidate later UI request");

            string activePath = PathOf("active-sync.json");
            var activeState = (JsonDataSaveCoordinator.FileState)typeof(JsonDataSaveCoordinator)
                .GetMethod("GetState", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { activePath });
            _writerGate = activeState.Writer; Monitor.Enter(_writerGate);
            var activeSync = Task.Run(() => JsonDataStore.Save(Sample(13), activePath));
            Check(SpinWait.SpinUntil(() => { lock (activeState.Sync) return activeState.Latest != null; }, 5000), "inflight sync exposed to readers");
            var activeRead = Task.Run(() => JsonDataStore.Load<UiSaveSample>(activePath));
            Check(!activeRead.Wait(40), "read cannot bypass inflight sync");
            ReleaseGate();
            Check(activeSync.Result.Success && activeRead.Result.Data.Value == 13, "read waits for durable sync value");

            StartGate(); Queue(23, "load-latest.json");
            var read = Task.Run(() => JsonDataStore.Load<UiSaveSample>(PathOf("load-latest.json")));
            Check(!read.Wait(40), "explicit load waits pending target save");
            ReleaseGate();
            Check(read.Result.Success && read.Result.Data.Value == 23, "load sees latest edit");

            string failing = PathOf("failure.json"); Directory.CreateDirectory(failing);
            var failure = Queue(42, "failure.json").Result;
            Check(!failure.Success, "disk failure surfaced");
            Check(JsonDataSaveCoordinator.GetCurrentFailure(failing) != null, "current failure query reports dirty path");
            Check(!JsonDataSaveCoordinator.FlushAsync().Result.Success, "failed snapshot stays dirty");
            Check(!JsonDataStore.Save(Sample(43), failing).Success, "newer synchronous value also fails");
            Check(JsonDataSaveCoordinator.HasUnfinishedSaves, "sync failure preserves queued retry data");
            Directory.Delete(failing); // This test created this empty directory beneath its explicit output root.
            Check(JsonDataSaveCoordinator.RetryFailedAsync().Result.Success, "failed immutable snapshot retries");
            Check(Read("failure.json") == 43, "retry preserves newest failed synchronous value, not older UI value");
            Check(JsonDataSaveCoordinator.GetCurrentFailure(failing) == null, "old failure is suppressed after newer success");
            Check(!JsonDataStore.Save(new BrokenUiSaveSample(), PathOf("broken.json")).Success, "synchronous snapshot failure surfaced");
            Check(JsonDataSaveCoordinator.HasUnfinishedSaves, "first synchronous failure remains dirty");
            Check(!JsonDataSaveCoordinator.RetryFailedAsync().Result.Success, "uncapturable value is never replaced with an older snapshot");
            Check(JsonDataStore.Save(Sample(73), PathOf("broken.json")).Success, "explicit successful recapture resolves failure");

            StartGate(); Queue(31, "rename-source/node.json");
            var rename = Task.Run(() => JsonDataStore.RenameDirectory(PathOf("rename-source"), PathOf("renamed")));
            Check(!rename.Wait(40), "rename drains target request");
            ReleaseGate();
            Check(rename.Result.Success && Read("renamed/node.json") == 31, "rename preserves pending latest value");
            Check(!Directory.Exists(PathOf("rename-source")), "old pending write cannot resurrect source");

            StartGate(); Queue(51, "copy-source/node.json");
            var copy = Task.Run(() => JsonDataStore.CopyDirectory(PathOf("copy-source"), PathOf("copy-target")));
            Check(!copy.Wait(40), "copy drains pending source");
            ReleaseGate();
            Check(copy.Result.Success && Read("copy-target/node.json") == 51, "copy reads latest edit");

            StartGate(); Queue(61, "delete-latest.json");
            var delete = Task.Run(() => JsonDataStore.DeleteFile(PathOf("delete-latest.json")));
            Check(!delete.Wait(40), "delete drains pending new file before existence check");
            ReleaseGate();
            Check(delete.Result.Success && !File.Exists(PathOf("delete-latest.json")), "queued write cannot resurrect deleted file");
            Check(!JsonDataSaveCoordinator.HasUnfinishedSaves, "no pending changes left");

            Console.WriteLine("PASS: UI parameter save coordinator, assertions=" + _checks);
            return 0;
        }
        catch (Exception ex) { if (_writerGate != null) Monitor.Exit(_writerGate); Console.Error.WriteLine(ex); return 1; }
    }
}
