using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    public partial class OutputSequence
    {
        private bool _manualOutputBatchActive;

        public string LastManualOutputBatchMessage { get; private set; }

        public async Task<int> ExecuteManualOutputBatchAsync(
            bool load, CancellationToken ct, Action<string> saveCheckpoint, IProgress<string> progress = null)
        {
            var runner = new ManualOutputBatchRunner();
            bool completed = false;
            bool ownsBatch = false;
            try
            {
                if (Mode != SequenceRunMode.Manual || _manualOutputBatchActive)
                    throw new InvalidOperationException("OUTPUT ALL은 실행 중인 배치가 없는 Manual 모드에서만 시작할 수 있습니다.");
                if (saveCheckpoint == null)
                    throw new ArgumentNullException(nameof(saveCheckpoint));

                SequenceFailureStore.Clear();
                _manualOutputBatchActive = true;
                ownsBatch = true;
                var actions = new ManualOutputBatchActions(this, load, saveCheckpoint, progress);
                int result = await runner.RunAsync(
                    load ? ManualOutputBatchOperation.Load : ManualOutputBatchOperation.Unload,
                    actions, ct).ConfigureAwait(false);
                completed = result == 0;
                if (!completed && !AlarmManager.HasActive)
                    Fail("OUT-MANUAL-ALL", "OutputSequence", runner.LastMessage);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string detail = string.IsNullOrWhiteSpace(runner.LastMessage) ? ex.Message : runner.LastMessage;
                LastManualOutputBatchMessage = detail;
                return Fail("OUT-MANUAL-ALL-EX", "OutputSequence", detail);
            }
            finally
            {
                if (ownsBatch)
                    _manualOutputBatchActive = false;
                if (!string.IsNullOrWhiteSpace(runner.LastMessage))
                    LastManualOutputBatchMessage = runner.LastMessage;
                if (ownsBatch && !completed && Context != null && Context.Bus != null)
                {
                    Context.Bus.Reset("OutputGoodStageReady");
                    Context.Bus.Reset("OutputNgStageReady");
                    Context.Bus.Reset("OutputStageReady");
                }
            }
        }

        // 순서 결정은 장비와 독립된 Runner가 담당하고, 이 어댑터는 기존 이송/인터락만 호출한다.
        private sealed class ManualOutputBatchActions : IManualOutputBatchActions
        {
            private readonly OutputSequence _owner;
            private readonly bool _load;
            private readonly Action<string> _saveCheckpoint;
            private readonly IProgress<string> _progress;
            private string _verifiedIdentity;

            public ManualOutputBatchActions(OutputSequence owner, bool load, Action<string> saveCheckpoint, IProgress<string> progress)
            {
                _owner = owner;
                _load = load;
                _saveCheckpoint = saveCheckpoint;
                _progress = progress;
            }

            public void CheckCanContinue(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                _owner.Context.StopIfCycleStopRequested("ManualOutputAll", false, string.Empty);
                MachineController controller = _owner.Context.Controller;
                if (controller == null)
                    throw new InvalidOperationException("OUTPUT ALL 장비 제어기를 찾을 수 없습니다.");
                if (controller.Status != EquipmentStatus.ManualRunning || AlarmManager.HasActive)
                    throw new SequenceStopException("OUTPUT ALL 진행 조건이 해제되었습니다. status=" + controller.Status);
            }

            public async Task<ManualOutputBatchState> ReadStateAsync(CancellationToken ct)
            {
                CheckCanContinue(ct);
                var machine = _owner.Context.Machine;
                var feeder = machine != null ? machine.OutputFeederUnit : null;
                var stage = machine != null ? machine.OutputStageUnit : null;
                if (feeder == null || stage == null || machine.OutputCassetteUnit == null)
                    throw new InvalidOperationException("OUTPUT ALL의 Feeder/Stage/Cassette 장치를 찾을 수 없습니다.");

                OutputBatchMaterialSnapshot before = ReadMaterialSnapshot();
                bool hasFeeder = before.State.FeederSide.HasValue;
                bool globalDryRun = _owner.Context.Controller.GlobalDryRun;

                // 건너뛰는 Stage도 실센서와 Material이 일치해야 한다. 기존 이송의 안정 확인 API를 사용한다.
                bool goodConfirmed = await feeder.WaitTransportRingStatesConfirmedAsync(
                    hasFeeder, stage.GoodBinRingSensor, before.State.GoodPresent, globalDryRun,
                    hasFeeder, before.State.GoodPresent, 3000, ct).ConfigureAwait(false);
                if (!goodConfirmed)
                    throw new InvalidOperationException("GOOD/Feeder 센서와 Material이 일치하지 않습니다. " + feeder.LastTransportRingConfirmationFailure);
                bool ngConfirmed = await feeder.WaitTransportRingStatesConfirmedAsync(
                    hasFeeder, stage.NgBinRingSensor, before.State.NgPresent, globalDryRun,
                    hasFeeder, before.State.NgPresent, 3000, ct).ConfigureAwait(false);
                if (!ngConfirmed)
                    throw new InvalidOperationException("NG/Feeder 센서와 Material이 일치하지 않습니다. " + feeder.LastTransportRingConfirmationFailure);

                CheckCanContinue(ct);
                OutputBatchMaterialSnapshot after = ReadMaterialSnapshot();
                if (before.Identity != after.Identity)
                    throw new InvalidOperationException("OUTPUT ALL 센서 확인 중 자재 또는 NG 사용 설정이 변경되었습니다.");
                if (_load)
                {
                    if (after.State.GoodPresent)
                        VerifyLoadedStage(stage, BinSide.Good);
                    if (after.State.NgPresent)
                        VerifyLoadedStage(stage, BinSide.Ng);
                }
                _verifiedIdentity = after.Identity;
                return after.State;
            }

            public Task<int> ExecuteFeederAsync(ManualOutputBatchOperation operation, BinSide side, CancellationToken ct)
            {
                CheckCanContinue(ct);
                VerifyMaterialUnchangedBeforeMove();
                // 같은 Side Stage가 빈 경우에만 잔류 Feeder를 먼저 처리한다(Runner 선행 검사).
                return _owner.ExecuteOccupiedFeederActionAsync(ct, false, 0, SequenceStartMode.Resume);
            }

            public Task<int> ExecuteSideAsync(ManualOutputBatchOperation operation, BinSide side, CancellationToken ct)
            {
                CheckCanContinue(ct);
                VerifyMaterialUnchangedBeforeMove();
                return operation == ManualOutputBatchOperation.Load
                    ? _owner.ExecuteManualOutputLoadAsync(ct, side)
                    : _owner.ExecuteManualOutputUnloadAsync(ct, side);
            }

            public void SaveCheckpoint(string description)
            {
                _saveCheckpoint(description);
            }

            public Task<int> CompleteAsync(ManualOutputBatchOperation operation, bool anyWork, CancellationToken ct)
            {
                CheckCanContinue(ct);
                if (anyWork)
                    VerifyCompletePosture(operation);
                if (anyWork && operation == ManualOutputBatchOperation.Load)
                    _owner.SetOutputStageReadySignals();
                return Task.FromResult(0);
            }

            public string GetFailureReason()
            {
                SequenceFailureInfo failure = SequenceFailureStore.GetLast();
                return failure != null ? failure.Message : "하위 이송을 완료하지 못했습니다.";
            }

            public void Report(string message)
            {
                WriteLog("ManualOutputAll", message);
                if (_progress != null)
                    _progress.Report(message);
            }

            private sealed class OutputBatchMaterialSnapshot
            {
                public string Identity;
                public ManualOutputBatchState State;
            }

            private OutputBatchMaterialSnapshot ReadMaterialSnapshot()
            {
                // 세 위치를 같은 Material 잠금 안에서 읽어 UI/저장 작업의 변경과 섞이지 않게 한다.
                return MaterialStateService.ReadState(snapshot =>
                {
                    WaferMaterial feeder = ReadOutputBin(MaterialLocationKind.OutputFeeder, null);
                    WaferMaterial good = ReadOutputBin(MaterialLocationKind.OutputStageGood, BinSide.Good);
                    WaferMaterial ng = ReadOutputBin(MaterialLocationKind.OutputStageNg, BinSide.Ng);
                    bool ngEnabled = _owner.IsNgCassetteUsed();
                    BinSide feederSide;
                    bool resolved = TryResolveBinSide(feeder, out feederSide);
                    return new OutputBatchMaterialSnapshot
                    {
                        Identity = ngEnabled + ";" + DescribeIdentity(feeder) + ";" + DescribeIdentity(good) + ";" + DescribeIdentity(ng),
                        State = new ManualOutputBatchState
                        {
                            NgEnabled = ngEnabled,
                            GoodPresent = good != null,
                            NgPresent = ng != null,
                            FeederSide = resolved ? (BinSide?)feederSide : null,
                            FeederComplete = feeder != null && IsOutputBinReceiveComplete(feeder)
                        }
                    };
                });
            }

            private void VerifyMaterialUnchangedBeforeMove()
            {
                if (string.IsNullOrWhiteSpace(_verifiedIdentity) || ReadMaterialSnapshot().Identity != _verifiedIdentity)
                    throw new InvalidOperationException("OUTPUT ALL 이송 시작 직전 자재 상태가 변경되었습니다. 다시 확인하세요.");
            }

            private static WaferMaterial ReadOutputBin(MaterialLocationKind location, BinSide? expectedSide)
            {
                MaterialSnapshot state = MaterialStateService.State;
                if (state == null || state.Wafers == null)
                    throw new InvalidOperationException("OUTPUT ALL Material 상태를 읽을 수 없습니다.");
                var bins = state.Wafers.Where(wafer => wafer != null && wafer.CurrentLocation != null &&
                    wafer.CurrentLocation.Kind == location && WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty).ToArray();
                if (bins.Length > 1)
                    throw new InvalidOperationException(location + " 위치에 중복 Material이 있습니다.");
                WaferMaterial bin = MaterialStateService.GetWaferAtLocation(location);
                if (bin == null)
                    return null;
                if (string.IsNullOrWhiteSpace(bin.WaferInstanceId) || bin.SourceSlotNumber < 0)
                    throw new InvalidOperationException(location + " 자재의 식별자 또는 원본 슬롯이 유효하지 않습니다. bin=" + bin.WaferId);
                BinSide sourceSide;
                if (bin.SourceCassetteRole == CassetteMaterialRole.Ng1)
                    sourceSide = BinSide.Ng;
                else if (bin.SourceCassetteRole == CassetteMaterialRole.Good1 || bin.SourceCassetteRole == CassetteMaterialRole.Good2)
                    sourceSide = BinSide.Good;
                else
                    throw new InvalidOperationException(location + " 자재의 원본 카세트가 출력 카세트가 아닙니다.");
                BinSide resolvedSide;
                if (!TryResolveBinSide(bin, out resolvedSide) || resolvedSide != sourceSide ||
                    (expectedSide.HasValue && expectedSide.Value != sourceSide))
                    throw new InvalidOperationException(location + " 자재의 GOOD/NG 구분과 원본 카세트가 일치하지 않습니다.");
                return bin;
            }

            private static string DescribeIdentity(WaferMaterial bin)
            {
                return bin == null ? "-" : bin.WaferInstanceId + ":" + bin.SourceCassetteRole + ":" +
                    bin.SourceSlotNumber + ":" + bin.State + ":" + bin.OutputGrade + ":" + IsOutputBinReceiveComplete(bin) +
                    ":" + bin.BarcodeConfirmed + ":" + bin.BarcodeId;
            }

            private static void VerifyLoadedStage(OutputStageUnit stage, BinSide side)
            {
                // 중단된 Stage를 단순 점유만 보고 완료로 간주하지 않는다. 기존 로딩 완료 조건을 확인만 한다.
                if (!stage.IsBinGuideDown(side) || !stage.IsBinGuideClampLiftUp(side) || !stage.IsBinGuideClamped(side))
                    throw new InvalidOperationException(side + " Stage 자재의 Guide Down/Clamp Lift Up/Clamp 상태가 확인되지 않았습니다. 복구 후 다시 실행하세요.");
                bool barcodeReady = MaterialStateService.ReadState(snapshot =>
                {
                    WaferMaterial wafer = ReadOutputBin(side == BinSide.Ng
                        ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood, side);
                    AppSettings settings = AppSettingsStore.Current;
                    return wafer == null || settings == null || !settings.UseOutputBinBarcode ||
                        (wafer.BarcodeConfirmed && OutputFeederLoadToStageSequence.IsUsableBarcode(wafer.BarcodeId));
                });
                if (!barcodeReady)
                    throw new InvalidOperationException(side + " Stage의 바코드 확인이 완료되지 않았습니다. 기존 바코드 복구를 완료한 뒤 다시 실행하세요.");
            }

            private void VerifyCompletePosture(ManualOutputBatchOperation operation)
            {
                var machine = _owner.Context.Machine;
                OutputStageUnit stage = machine.OutputStageUnit;
                OutputFeederUnit feeder = machine.OutputFeederUnit;
                OutputCassetteUnit cassette = machine.OutputCassetteUnit;
                VerifyStoppedAxis(feeder.FeederY, "OutputFeederY");
                VerifyStoppedAxis(cassette.OutputLifterZ, "OutputLifterZ");
                if (!feeder.IsBinFeederYInAvoidPosition() || !feeder.IsBinFeederAvoidPositionCheck() ||
                    !feeder.IsFeederDown() || !feeder.IsFeederUnclamped() || !cassette.IsBinLifterZInAvoidPosition())
                    throw new InvalidOperationException("OUTPUT ALL 이송 후 Feeder/Cassette의 Avoid·Down·Unclamp 상태가 확인되지 않았습니다.");

                string goodPosition = operation == ManualOutputBatchOperation.Load ? "Process" : "Avoid";
                VerifyStagePosition(stage, stage.GoodStage != null ? stage.GoodStage.StageY : null, BinStageAxis.GoodBinY, goodPosition);
                VerifyStagePosition(stage, stage.GoodStage != null ? stage.GoodStage.StageZ : null, BinStageAxis.GoodBinZ, goodPosition);
                VerifyStagePosition(stage, stage.NgStage != null ? stage.NgStage.StageY : null, BinStageAxis.NgBinY, "Avoid");
                VerifyStagePosition(stage, stage.OutputCameraX, BinStageAxis.VisionX, "Avoid");
                string pickerReason;
                if (!_owner.AreOutputPickersAvoidAndStopped(out pickerReason))
                    throw new InvalidOperationException("OUTPUT ALL 완료 시 Picker Avoid 상태가 아닙니다. " + pickerReason);
            }

            private static void VerifyStagePosition(OutputStageUnit stage, BaseAxis axis, BinStageAxis kind, string position)
            {
                VerifyStoppedAxis(axis, kind.ToString());
                double target = stage.GetStageTeachingPosition(kind, position);
                double tolerance = axis.Config != null ? axis.Config.InPositionTolerance : 0.01;
                if (!stage.IsStageAxisInPosition(kind, target, tolerance))
                    throw new InvalidOperationException("OUTPUT ALL 완료 위치 불일치: " + kind + ", position=" + position +
                        ", actual=" + axis.ActualPosition + ", target=" + target);
            }

            private static void VerifyStoppedAxis(BaseAxis axis, string name)
            {
                if (axis != null)
                    axis.UpdateStatus();
                if (axis == null || axis.IsAlarm || !axis.IsServoOn || !axis.IsHomeDone || axis.IsMoving)
                    throw new InvalidOperationException("OUTPUT ALL 완료 시 " + name + " 축의 정상 정지를 확인하지 못했습니다.");
            }
        }
    }
}
