using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common.Motion;

// 이 파일은 실제 ALL 어댑터를 독립 컴파일하기 위한 메모리 전용 경계입니다.
// 하위 모션 구현, 장비 SDK, 실제 Material 저장소를 검증하거나 로드하지 않습니다.
namespace QMC.Common { internal sealed class NamespaceMarker { } }
namespace QMC.Common.Alarms
{
    public static class AlarmManager { public static bool HasActive { get; set; } }
}
namespace QMC.Common.Motion
{
    public sealed class AxisConfig { public double InPositionTolerance { get; set; } = 0.01; }
    public sealed class BaseAxis
    {
        public AxisConfig Config { get; set; } = new AxisConfig();
        public double ActualPosition { get; set; }
        public bool IsAlarm { get; set; }
        public bool IsServoOn { get; set; } = true;
        public bool IsHomeDone { get; set; } = true;
        public bool IsMoving { get; set; }
        public void UpdateStatus() { }
    }
}
namespace QMC.CDT320.Materials
{
    public enum MaterialLocationKind { OutputFeeder, OutputStageGood, OutputStageNg, Cassette }
    public enum CassetteMaterialRole { Input, Ng1, Good1, Good2 }
    public enum WaferMaterialState { Empty, Occupied }
    public sealed class MaterialLocation { public MaterialLocationKind Kind { get; set; } }
    public sealed class WaferMaterial
    {
        public string WaferInstanceId { get; set; }
        public string WaferId { get; set; }
        public int SourceSlotNumber { get; set; }
        public CassetteMaterialRole SourceCassetteRole { get; set; }
        public MaterialLocation CurrentLocation { get; set; }
        public string State { get; set; } = "Occupied";
        public BinSide OutputGrade { get; set; }
        public bool Complete { get; set; }
        public bool BarcodeConfirmed { get; set; } = true;
        public string BarcodeId { get; set; } = "TEST-BIN";
    }
    public static class WaferMaterialStateText
    {
        public static WaferMaterialState Normalize(string state) { return state == "Empty" ? WaferMaterialState.Empty : WaferMaterialState.Occupied; }
    }
    public sealed class MaterialSnapshot { public List<WaferMaterial> Wafers { get; set; } = new List<WaferMaterial>(); }
    public static class MaterialStateService
    {
        public static MaterialSnapshot State { get; set; } = new MaterialSnapshot();
        public static T ReadState<T>(Func<MaterialSnapshot, T> read) { return read(State); }
        public static WaferMaterial GetWaferAtLocation(MaterialLocationKind location)
        {
            return State.Wafers.FirstOrDefault(w => w.CurrentLocation.Kind == location && w.State != "Empty");
        }
    }
}
namespace QMC.CDT320
{
    public enum BinSide { Ng, Good }
    public enum BinStageAxis { NgBinY, NgBinZ, GoodBinY, GoodBinZ, VisionX }
    public enum EquipmentStatus { Ready, ManualRunning, Stopped }
    public sealed class AppSettings { public bool UseOutputBinBarcode { get; set; } }
    public static class AppSettingsStore { public static AppSettings Current { get; set; } = new AppSettings(); }
    public sealed class MachineController
    {
        public EquipmentStatus Status { get; set; } = EquipmentStatus.ManualRunning;
        public bool GlobalDryRun { get; set; }
    }
    public sealed class TestSignalBus
    {
        private readonly HashSet<string> _set = new HashSet<string>();
        public void Set(string name) { _set.Add(name); }
        public void Reset(string name) { _set.Remove(name); }
        public bool IsSet(string name) { return _set.Contains(name); }
    }
    public sealed class TestRingSensor { public bool Present { get; set; } }
    public sealed class TestStageAxes
    {
        public BaseAxis StageY { get; set; } = new BaseAxis();
        public BaseAxis StageZ { get; set; } = new BaseAxis();
    }
    public sealed class OutputStageUnit
    {
        public TestStageAxes GoodStage { get; set; } = new TestStageAxes();
        public TestStageAxes NgStage { get; set; } = new TestStageAxes();
        public BaseAxis OutputCameraX { get; set; } = new BaseAxis();
        public TestRingSensor GoodBinRingSensor { get; set; } = new TestRingSensor();
        public TestRingSensor NgBinRingSensor { get; set; } = new TestRingSensor();
        public bool GoodGuideSecure { get; set; } = true;
        public bool NgGuideSecure { get; set; } = true;
        public bool IsBinGuideDown(BinSide side) { return IsSecure(side); }
        public bool IsBinGuideClampLiftUp(BinSide side) { return IsSecure(side); }
        public bool IsBinGuideClamped(BinSide side) { return IsSecure(side); }
        private bool IsSecure(BinSide side) { return side == BinSide.Ng ? NgGuideSecure : GoodGuideSecure; }
        public double GetStageTeachingPosition(BinStageAxis kind, string position) { return position == "Process" ? 10 : 0; }
        public bool IsStageAxisInPosition(BinStageAxis kind, double target, double tolerance)
        {
            BaseAxis axis = kind == BinStageAxis.GoodBinY ? GoodStage.StageY :
                kind == BinStageAxis.GoodBinZ ? GoodStage.StageZ :
                kind == BinStageAxis.NgBinY ? NgStage.StageY :
                kind == BinStageAxis.NgBinZ ? NgStage.StageZ : OutputCameraX;
            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }
    }
    public sealed class OutputFeederUnit
    {
        public BaseAxis FeederY { get; set; } = new BaseAxis();
        public bool RingPresent { get; set; }
        public bool SafePosture { get; set; } = true;
        public string LastTransportRingConfirmationFailure { get; set; } = "테스트 센서 불일치";
        public Action BeforeConfirmation { get; set; }
        public Task<bool> WaitTransportRingStatesConfirmedAsync(bool feederExpected, TestRingSensor stageSensor,
            bool stageExpected, bool dryRun, bool materialFeeder, bool materialStage, int timeout, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            BeforeConfirmation?.Invoke();
            return Task.FromResult(RingPresent == feederExpected && stageSensor.Present == stageExpected);
        }
        public bool IsBinFeederYInAvoidPosition() { return SafePosture; }
        public bool IsBinFeederAvoidPositionCheck() { return SafePosture; }
        public bool IsFeederDown() { return SafePosture; }
        public bool IsFeederUnclamped() { return SafePosture; }
    }
    public sealed class OutputCassetteUnit
    {
        public BaseAxis OutputLifterZ { get; set; } = new BaseAxis();
        public bool IsBinLifterZInAvoidPosition() { return true; }
    }
    public sealed class TestMachine
    {
        public OutputStageUnit OutputStageUnit { get; set; } = new OutputStageUnit();
        public OutputFeederUnit OutputFeederUnit { get; set; } = new OutputFeederUnit();
        public OutputCassetteUnit OutputCassetteUnit { get; set; } = new OutputCassetteUnit();
    }
}
namespace QMC.CDT320.Sequencing
{
    public enum SequenceRunMode { Manual, Auto }
    public enum SequenceStartMode { Resume }
    public sealed class MachineSequenceContext
    {
        public TestMachine Machine { get; set; } = new TestMachine();
        public MachineController Controller { get; set; } = new MachineController();
        public TestSignalBus Bus { get; set; } = new TestSignalBus();
        public bool StopRequested { get; set; }
        public void StopIfCycleStopRequested(string operation, bool force, string message)
        {
            if (StopRequested) throw new SequenceStopException("CYCLE STOP 테스트");
        }
    }
    public sealed class SequenceFailureInfo { public string Message { get; set; } }
    public static class SequenceFailureStore
    {
        public static SequenceFailureInfo Last { get; set; }
        public static void Clear() { Last = null; }
        public static SequenceFailureInfo GetLast() { return Last; }
    }
    public static class OutputFeederLoadToStageSequence
    {
        public static bool IsUsableBarcode(string barcode) { return !string.IsNullOrWhiteSpace(barcode); }
    }
    public partial class OutputSequence
    {
        public SequenceRunMode Mode { get; set; } = SequenceRunMode.Manual;
        public MachineSequenceContext Context { get; set; } = new MachineSequenceContext();
        public bool NgEnabled { get; set; } = true;
        public bool PickersSafe { get; set; } = true;
        public List<string> TestCommands { get; } = new List<string>();
        public List<bool> ActiveDuringTransfer { get; } = new List<bool>();
        public Action<OutputSequence> AfterTransfer { get; set; }
        public Action BeforeReady { get; set; }
        public static WaferMaterial CreateBin(BinSide side, MaterialLocationKind location)
        {
            return new WaferMaterial
            {
                WaferId = side + "-TEST", WaferInstanceId = Guid.NewGuid().ToString("N"),
                SourceSlotNumber = 1, SourceCassetteRole = side == BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1,
                OutputGrade = side, CurrentLocation = new MaterialLocation { Kind = location }
            };
        }
        public void SyncTestSensors()
        {
            Context.Machine.OutputFeederUnit.RingPresent = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null;
            Context.Machine.OutputStageUnit.GoodBinRingSensor.Present = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) != null;
            Context.Machine.OutputStageUnit.NgBinRingSensor.Present = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) != null;
        }
        private bool IsNgCassetteUsed() { return NgEnabled; }
        private static bool IsOutputBinReceiveComplete(WaferMaterial wafer) { return wafer.Complete; }
        private static bool TryResolveBinSide(WaferMaterial wafer, out BinSide side)
        {
            side = wafer != null ? wafer.OutputGrade : BinSide.Good;
            return wafer != null;
        }
        private static void WriteLog(string operation, string message) { }
        private int Fail(string code, string sequence, string message)
        {
            SequenceFailureStore.Last = new SequenceFailureInfo { Message = message };
            QMC.Common.Alarms.AlarmManager.HasActive = true;
            return -1;
        }
        private bool AreOutputPickersAvoidAndStopped(out string reason) { reason = "테스트 Picker"; return PickersSafe; }
        private void SetOutputStageReadySignals()
        {
            BeforeReady?.Invoke();
            Context.Bus.Set("OutputGoodStageReady");
            Context.Bus.Set("OutputNgStageReady");
            Context.Bus.Set("OutputStageReady");
        }
        public Task<int> ExecuteManualOutputLoadAsync(CancellationToken ct, BinSide side)
        {
            ct.ThrowIfCancellationRequested();
            TestCommands.Add("ManualLoad:" + side);
            ActiveDuringTransfer.Add(_manualOutputBatchActive);
            MaterialStateService.State.Wafers.Add(CreateBin(side, side == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood));
            FinishTestTransfer(true);
            return Task.FromResult(0);
        }
        public Task<int> ExecuteManualOutputUnloadAsync(CancellationToken ct, BinSide side)
        {
            ct.ThrowIfCancellationRequested();
            TestCommands.Add("ManualUnload:" + side);
            ActiveDuringTransfer.Add(_manualOutputBatchActive);
            WaferMaterial bin = MaterialStateService.GetWaferAtLocation(side == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood);
            bin.CurrentLocation.Kind = MaterialLocationKind.Cassette;
            FinishTestTransfer(false);
            return Task.FromResult(0);
        }
        private Task<int> ExecuteOccupiedFeederActionAsync(CancellationToken ct, bool autoMode, int slot, SequenceStartMode mode)
        {
            ct.ThrowIfCancellationRequested();
            if (autoMode || slot != 0 || mode != SequenceStartMode.Resume) throw new Exception("Unexpected feeder mode.");
            TestCommands.Add("FeederResume");
            ActiveDuringTransfer.Add(_manualOutputBatchActive);
            WaferMaterial bin = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            bool load = !bin.Complete;
            bin.CurrentLocation.Kind = load ? (bin.OutputGrade == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood) : MaterialLocationKind.Cassette;
            FinishTestTransfer(load);
            return Task.FromResult(0);
        }
        private void FinishTestTransfer(bool load)
        {
            Context.Machine.OutputStageUnit.GoodStage.StageY.ActualPosition = load ? 10 : 0;
            Context.Machine.OutputStageUnit.GoodStage.StageZ.ActualPosition = load ? 10 : 0;
            SyncTestSensors();
            AfterTransfer?.Invoke(this);
        }
    }
}
