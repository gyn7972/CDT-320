using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

internal static class DisplayTests
{
    private static int _assertions;

    private static int Main()
    {
        try
        {
            VerifyBusyReadAndRetry();
            VerifyDisplaySelection();
            VerifyAbsentStateReleasesLock();
            Console.WriteLine("PASS: 3 display scenarios, " + _assertions + " assertions.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void VerifyBusyReadAndRetry()
    {
        MaterialStateService.State = new MaterialSnapshot();
        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        {
            Task holder = Task.Run(() =>
            {
                lock (MaterialStateService.TestGate)
                {
                    entered.Set();
                    if (!release.Wait(5000)) throw new TimeoutException("Test lock holder release timeout");
                    MaterialStateService.State.Wafers.Add(Wafer(MaterialLocationKind.InputStage, "latest", ""));
                }
            });
            Assert(entered.Wait(2000), "background holder acquired lock");
            Task<bool> probe = Task.Run(() =>
            {
                string input, good, ng;
                bool read = MaterialStateService.TryGetProcessingDisplayIds(out input, out good, out ng);
                Assert(input == "-" && good == "-" && ng == "-", "busy read initializes outputs");
                return read;
            });
            try
            {
                // 판정은 절대 성능 수치가 아니라, 저장 잠금을 해제하기 전에 UI 읽기가 반환하는지다.
                Assert(probe.Wait(1000), "display read must return while background lock remains held");
                Assert(!probe.Result, "busy read requests retry instead of reading mutable state");
                Assert(!release.IsSet, "lock holder was not released to complete display read");
            }
            finally
            {
                release.Set();
                Assert(holder.Wait(2000), "background holder completed");
                Assert(probe.Wait(2000), "probe completed");
            }
        }
        string nextInput, nextGood, nextNg;
        Assert(MaterialStateService.TryGetProcessingDisplayIds(out nextInput, out nextGood, out nextNg), "next read succeeds");
        Assert(nextInput == "latest", "retry reads latest state after save finishes");
        Assert(nextGood == "-" && nextNg == "-", "missing output stages display dash");
        Console.WriteLine("PASS: busy read returns before lock release; retry sees latest state");
    }

    private static void VerifyDisplaySelection()
    {
        MaterialStateService.State = new MaterialSnapshot
        {
            Wafers = new List<WaferMaterial>
            {
                null,
                new WaferMaterial { WaferId = "no location", CurrentLocation = null, State = WaferMaterialState.Working },
                new WaferMaterial { WaferId = "empty", CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage }, State = WaferMaterialState.Empty },
                Wafer(MaterialLocationKind.InputFeeder, "feeder must not appear", ""),
                Wafer(MaterialLocationKind.InputStage, "input id", "input barcode"),
                Wafer(MaterialLocationKind.InputStage, "later duplicate", ""),
                Wafer(MaterialLocationKind.OutputStageGood, "good id", " "),
                Wafer(MaterialLocationKind.OutputStageNg, "", "")
            }
        };
        string input, good, ng;
        Assert(MaterialStateService.TryGetProcessingDisplayIds(out input, out good, out ng), "display read succeeds");
        Assert(input == "input barcode", "barcode preferred and first wafer selected");
        Assert(good == "good id", "blank barcode falls back to wafer id");
        Assert(ng == "-", "blank wafer id displays dash");
        MaterialStateService.State.Wafers[4].State = (WaferMaterialState)7;
        Assert(MaterialStateService.TryGetProcessingDisplayIds(out input, out good, out ng), "legacy finish state read succeeds");
        Assert(input == "input barcode", "legacy finish normalization is preserved");
        Console.WriteLine("PASS: null/empty/other locations excluded; barcode and legacy state selection preserved");
    }

    private static void VerifyAbsentStateReleasesLock()
    {
        MaterialStateService.State = null;
        AssertUnreadable();
        MaterialStateService.State = new MaterialSnapshot { Wafers = null };
        AssertUnreadable();
        MaterialStateService.State = new MaterialSnapshot();
        Task<bool> reader = Task.Run(() =>
        {
            string input, good, ng;
            return MaterialStateService.TryGetProcessingDisplayIds(out input, out good, out ng);
        });
        Assert(reader.Wait(1000) && reader.Result, "early returns release lock for another thread");
        Console.WriteLine("PASS: absent state early returns release lock");
    }

    private static void AssertUnreadable()
    {
        string input, good, ng;
        Assert(!MaterialStateService.TryGetProcessingDisplayIds(out input, out good, out ng), "absent state returns false");
        Assert(input == "-" && good == "-" && ng == "-", "absent state initializes outputs");
    }

    private static WaferMaterial Wafer(MaterialLocationKind location, string id, string barcode)
    {
        return new WaferMaterial
        {
            WaferId = id, BarcodeId = barcode, State = WaferMaterialState.Working,
            CurrentLocation = new MaterialLocation { Kind = location }
        };
    }

    private static void Assert(bool condition, string message)
    {
        Interlocked.Increment(ref _assertions);
        if (!condition) throw new InvalidOperationException(message);
    }
}

namespace QMC.CDT320.Materials
{
    // 상태 저장소와 잠금만 메모리 대역으로 제공한다. 표시 메서드는 별도 생산 소스를 그대로 컴파일한다.
    public static partial class MaterialStateService
    {
        private static readonly object _stateSync = new object();
        public static object TestGate { get { return _stateSync; } }
        public static MaterialSnapshot State { get; set; }
    }
}
