using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using QMC.Common;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    /// <summary>콜렛 1개의 클리닝/검사 진행 상태.</summary>
    internal sealed class ColletCleaningItem
    {
        public VisionFocusPickerSide Side { get; set; }
        public int ColletNo { get; set; }
        public int ColletIndex { get { return ColletNo - 1; } }
        public bool Cleaned { get; set; }
        public bool Inspected { get; set; }
        public bool InspectionOk { get; set; }
        public int RetryUsed { get; set; }
        public int UsedCellOrderIndex { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public ColletCleaningResult Result { get; set; } = ColletCleaningResult.None;
    }

    /// <summary>
    /// 콜렛 클리닝 시퀀스(Side 단위 배치).
    ///
    /// 큰 흐름(사용자 확정 2026-07-26):
    ///   Phase 1 - 선택된 콜렛 전부를 NG Stage 다이맵 셀에서 순서대로 클리닝한다(존 이동 1회).
    ///   Phase 2 - 다이 검사존으로 한 번만 이동해서 선택된 콜렛 전부를 검사한다.
    ///   Phase 3 - 검사 NG인 콜렛만 Phase 1~2를 반복한다. 재시도 소진 시 콜렛 교체 알람.
    ///
    /// Front/Rear는 상대 Picker Avoid 인터락 때문에 물리적으로 동시 작업이 불가능하므로
    /// Side 단위가 한 번에 묶을 수 있는 최대 범위다. 양쪽 실행은 AutoColletCleaningSequence가 담당한다.
    /// </summary>
    internal sealed class ColletCleaningSequence : PickerSequenceBase<ColletCleaningStep>
    {
        private const string CleaningTargetName = "ColletCleaning";

        private readonly ColletCleaningSettings _settings;
        private readonly List<ColletCleaningItem> _items = new List<ColletCleaningItem>();
        private readonly VisionFocusPickerSide _cleaningSide;

        private double _outputVisionToPickerX;
        private double _outputVisionToPickerY;
        private int _cleaningCellCursor;
        private string _cleaningCellBinWaferId = string.Empty;

        public ColletCleaningSequence(
            MachineSequenceContext context,
            PickerSequenceSide side,
            ColletCleaningSettings settings)
            : base(context, side, PickerSequenceKind.Inspect, side + "ColletCleaningSequence")
        {
            _settings = settings != null ? settings.Clone() : new ColletCleaningSettings();
            _cleaningSide = side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;
        }

        /// <summary>선택된 콜렛별 최종 결과. 호출자가 이력/표시에 사용한다.</summary>
        public IReadOnlyList<ColletCleaningItem> Items { get { return _items; } }

        /// <summary>NG Stage에 Bin이 없어 클리닝을 건너뛴 경우 true(알람 아님).</summary>
        public bool SkippedNoBin { get; private set; }

        public string SkipReason { get; private set; } = string.Empty;

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            CurrentStep = ColletCleaningStep.CheckUnit;

            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested(Name + ":" + CurrentStep);

                    int result;
                    switch (CurrentStep)
                    {
                        case ColletCleaningStep.CheckUnit:
                            result = CheckUnit();
                            break;
                        case ColletCleaningStep.CheckSafety:
                            result = await CheckSafetyAsync(ct).ConfigureAwait(false);
                            break;
                        case ColletCleaningStep.ReserveArea:
                            result = await ReserveAreaAsync(ct).ConfigureAwait(false);
                            break;
                        case ColletCleaningStep.CleanAllSelectedCollets:
                            result = await CleanAllSelectedColletsAsync(ct).ConfigureAwait(false);
                            break;
                        case ColletCleaningStep.MoveToInspectionZone:
                            result = await MoveToInspectionZoneAsync(ct).ConfigureAwait(false);
                            break;
                        case ColletCleaningStep.InspectAllSelectedCollets:
                            result = await InspectAllSelectedColletsAsync(ct).ConfigureAwait(false);
                            break;
                        case ColletCleaningStep.EvaluateAndRetry:
                            result = EvaluateAndRetry();
                            break;
                        case ColletCleaningStep.MoveAvoid:
                            result = await MoveAvoidAsync(ct).ConfigureAwait(false);
                            break;
                        case ColletCleaningStep.Complete:
                            return 0;
                        default:
                            return Fail("COLLET-CLEAN-STEP", Name,
                                "콜렛 클리닝 처리할 수 없는 Step입니다. step=" + CurrentStep);
                    }

                    if (result != 0)
                    {
                        CurrentStep = ColletCleaningStep.Error;
                        return result;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                StopAllPickerZAxesOnInterrupt("취소 요청");
                throw;
            }
            catch (SequenceStopException)
            {
                StopAllPickerZAxesOnInterrupt("시퀀스 정지 요청");
                throw;
            }
            finally
            {
                // 실패/취소/예외 경로에서도 존 점유와 lease가 남지 않도록 무조건 해제한다.
                // 성공 경로(MoveAvoidAsync)에서 이미 해제한 경우 중복 호출은 안전하다(null 확인).
                ReleasePickerWorkArea();
                ReleaseAllLeases();
            }
        }

        /// <summary>
        /// 취소/정지 시 콜렛이 NG Bin에 눌린 채 방치되지 않도록 해당 Side의 PickerZ 전체를 정지하고
        /// 하강 잔류 가능성을 명시 경고로 남긴다.
        /// </summary>
        private void StopAllPickerZAxesOnInterrupt(string reason)
        {
            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            foreach (PickerAxis zAxis in zAxes)
            {
                try
                {
                    BaseAxis axis = GetPickerAxis(zAxis);
                    if (axis == null)
                        continue;

                    axis.UpdateStatus();
                    axis.StopJog();
                    axis.Stop();
                    axis.UpdateStatus();
                }
                catch (Exception ex)
                {
                    WriteLog(Name,
                        "콜렛 클리닝 중단 - PickerZ 정지 예외. axis=" + zAxis +
                        ", error=" + ex.Message + " - Check");
                }
            }

            EventLogger.Write(EventKind.Warning, "CAL", "COLLET-CLEAN-INTERRUPTED",
                "콜렛 클리닝이 중단되었습니다(" + reason + "). side=" + Side +
                ", step=" + CurrentStep +
                ". 콜렛이 NG Bin 위 하강 위치에 남아 있을 수 있으니 PickerZ Avoid 복귀 후 작업하세요.");
        }

        // ---------------------------------------------------------------- CheckUnit

        private int CheckUnit()
        {
            try
            {
                if (Context == null || Context.Machine == null)
                    return Fail("COLLET-CLEAN-NO-CONTEXT", Name, "콜렛 클리닝 실행 Context가 없습니다.");

                if (_settings == null)
                    return Fail("COLLET-CLEAN-NO-SETTINGS", Name, "콜렛 클리닝 설정이 없습니다.");

                // 안전 위치(Avoid) 이동은 Calibration 화면의 "안전위치(Avoid) 이동 속도 %"를 따른다.
                // 클리닝 파라미터(Clean Z Speed 등)는 실제 공정(누름) 이동에만 사용한다.
                // 다른 캘리브레이션 시퀀스와 동일하게 CalibrationMotion을 주입해야
                // useSafeMoveMotion 경로와 ResolveCalibrationSafeMovePercent()가 동작한다.
                SetCalibrationMotion(ResolveCalibrationMotionFromData());

                _items.Clear();

                // NG Stage에 Bin이 없으면 클리닝 자체가 불가능하다.
                // 알람이 아니라 스킵 + 경고로 처리하고 다음 조건에서 재시도한다(사용자 확정 정책).
                // 대상 목록을 만들기 전에 확인해야 "실행하지 않았는데 이력만 남는" 오해가 생기지 않는다.
                WaferMaterial ngBinPresence = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);
                if (ngBinPresence == null)
                {
                    SkippedNoBin = true;
                    SkipReason = "NG Stage에 Bin이 없어 콜렛 클리닝을 실행할 수 없습니다. " +
                                 "먼저 NG Bin을 Stage에 공급(OUTPUT LOAD)한 뒤 다시 실행하세요.";
                    WriteLog(Name, SkipReason + " side=" + _cleaningSide + " - Skip");
                    Context.LogPublic("[COLLET-CLEAN] " + SkipReason);
                    CurrentStep = ColletCleaningStep.Complete;
                    return 0;
                }

                for (int colletNo = 4; colletNo >= 1; colletNo--)
                {
                    if (!_settings.IsColletSelected(_cleaningSide, colletNo))
                        continue;

                    if (!IsPickerIndexEnabled(colletNo - 1))
                    {
                        WriteLog(Name,
                            "선택된 콜렛이 비활성 Picker라 대상에서 제외합니다. side=" + _cleaningSide +
                            ", colletNo=" + colletNo + " - Check");
                        continue;
                    }

                    _items.Add(new ColletCleaningItem { Side = _cleaningSide, ColletNo = colletNo });
                }

                if (_items.Count == 0)
                {
                    SkippedNoBin = false;
                    SkipReason = "선택된 콜렛이 없습니다. side=" + _cleaningSide;
                    WriteLog(Name, SkipReason + " - Skip");
                    CurrentStep = ColletCleaningStep.Complete;
                    return 0;
                }

                // 콜렛에 Die가 남아 있으면 누름 가압으로 Die 파쇄/필름 손상이 나므로 시작 전에 차단한다.
                // 자동 트리거 경로는 픽커 공핍이 보장되지만 수동 실행은 이 게이트가 유일한 방어다.
                foreach (ColletCleaningItem item in _items)
                {
                    DieMaterial dieOnCollet = MaterialStateService.GetDieAtPicker(PickerLocationKind, item.ColletNo);
                    if (dieOnCollet != null)
                        return Fail("COLLET-CLEAN-DIE-ON-PICKER", Name,
                            "콜렛에 Die가 남아 있어 클리닝을 실행할 수 없습니다. side=" + _cleaningSide +
                            ", colletNo=" + item.ColletNo +
                            ", die=" + (dieOnCollet.DieId ?? "-") +
                            ". Die를 배출/제거한 뒤 다시 실행하세요.");
                }

                OutputStageUnit stage = Context.Machine.OutputStageUnit;
                if (stage == null || stage.Recipe == null)
                    return Fail("COLLET-CLEAN-OUTPUT-STAGE", "OutputStageUnit",
                        "콜렛 클리닝에 필요한 OutputStage 유닛/Recipe가 없습니다.");

                stage.Recipe.EnsurePositionObjects();

                _cleaningCellBinWaferId = ngBinPresence.WaferId ?? string.Empty;
                _cleaningCellCursor = 0;

                string offsetReason;
                if (!PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                    Context.Machine,
                    Side,
                    _items[0].ColletIndex,
                    BinSide.Ng,
                    out _outputVisionToPickerX,
                    out _outputVisionToPickerY,
                    out offsetReason))
                {
                    return Fail("COLLET-CLEAN-OFFSET", Name,
                        "콜렛 클리닝 Output vision -> Picker 오프셋 해석에 실패했습니다. side=" + Side +
                        ", reason=" + offsetReason);
                }

                // 설정이 다이얼로그 -> 러너 -> 시퀀스까지 값 손실 없이 전달됐는지 확인용.
                WriteLog(Name,
                    "콜렛 클리닝 유효 설정. side=" + _cleaningSide +
                    ", pressCount=" + _settings.CleanPressCount +
                    ", arriveDwellMs=" + _settings.ArriveDwellMs +
                    ", repeatLiftHeight=" + _settings.RepeatLiftHeight.ToString("F6") +
                    ", cleanVelocity=" + _settings.CleanVelocity.ToString("F3") +
                    ", cleanAcc=" + _settings.CleanAcceleration.ToString("F3") +
                    ", cleanDec=" + _settings.CleanDeceleration.ToString("F3") +
                    ", contactZUserOffset=" + _settings.ContactZUserOffset.ToString("F6") +
                    ", maxExtraPressDepth=" + _settings.MaxExtraPressDepth.ToString("F6") +
                    ", die=" + _settings.DieHeight.ToString("F6") +
                    ", rim=" + _settings.RimHeight.ToString("F6") +
                    ", film=" + _settings.FilmHeight.ToString("F6") +
                    ", maxRetry=" + _settings.MaxRetryCount + " - Check");

                WriteLog(Name,
                    "콜렛 클리닝 대상 확정. side=" + _cleaningSide +
                    ", collets=" + string.Join(",", _items.Select(i => i.ColletNo.ToString()).ToArray()) +
                    ", bin=" + _cleaningCellBinWaferId +
                    ", pressCount=" + _settings.CleanPressCount +
                    ", maxRetry=" + _settings.MaxRetryCount + " - Start");

                CurrentStep = ColletCleaningStep.CheckSafety;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CLEAN-CHECK-EX", Name,
                    "콜렛 클리닝 준비 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        // ---------------------------------------------------------------- CheckSafety

        private async Task<int> CheckSafetyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // 시작 안전 이동 순서는 PickerPlaceZCalibrationSequence.PrepareSafeStartPositionAsync와 동일하게 맞춘다.
                //   ① 본인 PickerZ 전체 Avoid  ② 본인 PickerY Avoid  ③ 본인 PickerT 전체 Avoid
                //   ④ 상대 Picker Avoid        ⑤ Input/Output VisionX Avoid
                //   ⑥ Input/Output 카세트 리프터 Avoid (PickerX 이동 전제 - 인터락이 무조건 요구)
                // forceMove는 쓰지 않는다: 이미 Avoid(정지+무알람+톨러런스)면 확인만 하고 통과한다.
                const string safeDescription = "콜렛 클리닝 시작 안전 위치";

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    safeDescription + " - PickerZ all Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Z 다음은 Y다. Y가 Avoid여야 이후 PickerX/T 이동이 인터락을 통과한다.
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    safeDescription + " - PickerY Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY",
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveAllPickerTToAvoidAndVerifyAsync(
                    safeDescription + " - PickerT all Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidAndVerifyAsync(
                    safeDescription + " - Opposite Picker Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureVisionAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureCassetteLifterAvoidForPickerXAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog(Name,
                    safeDescription + " 확보 완료. 순서=PickerZ Avoid -> PickerY Avoid -> PickerT Avoid -> " +
                    "상대 Picker Avoid -> VisionX Avoid -> Cassette Lifter Avoid. side=" + _cleaningSide + " - Ok");

                CurrentStep = ColletCleaningStep.ReserveArea;
                return 0;
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
                return Fail("COLLET-CLEAN-SAFETY-EX", Name,
                    "콜렛 클리닝 시작 안전 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// PickerX 이동은 Input/Output 카세트 리프터가 정지된 Avoid 위치일 것을 무조건 요구한다
        /// (PickerFrontInterlockRules / PickerRearInterlockRules).
        /// 클리닝은 PickerX를 반복 이동하므로 시작 전에 두 리프터를 Avoid로 확보한다.
        /// </summary>
        private async Task<int> EnsureCassetteLifterAvoidForPickerXAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputCassetteUnit inputCassette = Context.Machine.InputCassetteUnit;
            if (inputCassette != null && inputCassette.InputLifterZ != null)
            {
                if (!inputCassette.IsWaferLifterZInAvoidPosition())
                {
                    WriteLog(Name,
                        "PickerX 이동 조건 확보: InputCassette 리프터를 Avoid로 이동합니다. actual=" +
                        inputCassette.InputLifterZ.ActualPosition.ToString("F3") + " - Start");

                    int result = await SequenceAwaiter.AwaitAsync(
                        inputCassette.MoveToWaferCassetteAvoidPosition(false), -1, ct).ConfigureAwait(false);
                    if (result != 0 || !inputCassette.IsWaferLifterZInAvoidPosition())
                        return Fail("COLLET-CLEAN-IN-CST-AVOID", inputCassette.Name,
                            "콜렛 클리닝 시작 전 InputCassette 리프터 Avoid 확보에 실패했습니다. result=" + result +
                            ", actual=" + inputCassette.InputLifterZ.ActualPosition.ToString("F3"));
                }
            }

            OutputCassetteUnit outputCassette = Context.Machine.OutputCassetteUnit;
            if (outputCassette != null && outputCassette.OutputLifterZ != null)
            {
                if (!outputCassette.IsBinLifterZInAvoidPosition())
                {
                    WriteLog(Name,
                        "PickerX 이동 조건 확보: OutputCassette 리프터를 Avoid로 이동합니다. actual=" +
                        outputCassette.OutputLifterZ.ActualPosition.ToString("F3") + " - Start");

                    int result = await SequenceAwaiter.AwaitAsync(
                        outputCassette.MoveToBinCassetteAvoidPosition(false), -1, ct).ConfigureAwait(false);
                    if (result != 0 || !outputCassette.IsBinLifterZInAvoidPosition())
                        return Fail("COLLET-CLEAN-OUT-CST-AVOID", outputCassette.Name,
                            "콜렛 클리닝 시작 전 OutputCassette 리프터 Avoid 확보에 실패했습니다. result=" + result +
                            ", actual=" + outputCassette.OutputLifterZ.ActualPosition.ToString("F3"));
                }
            }

            return 0;
        }

        private async Task<int> EnsureVisionAvoidForStartAsync(CancellationToken ct)
        {
            InputStageUnit inputStage = Context.Machine.InputStageUnit;
            if (inputStage != null && inputStage.CameraX != null &&
                inputStage.Recipe != null && inputStage.Recipe.VisionX != null)
            {
                inputStage.Recipe.EnsurePositionObjects();
                double inputTarget = inputStage.Recipe.VisionX.AvoidPosition;
                if (!inputStage.CameraX.IsAtTargetPosition(inputTarget, 0.0))
                {
                    CalibrationMotionSettings inputMotion = ResolveCleaningStageMotion();
                    int inputResult = await inputStage.MoveInputStageAxisCommandWithMotion(
                        WaferStageAxis.VisionX,
                        inputTarget,
                        inputMotion.MoveVelocity,
                        inputMotion.MoveAcceleration,
                        inputMotion.MoveDeceleration).ConfigureAwait(false);
                    if (inputResult != 0)
                        return Fail("COLLET-CLEAN-INPUT-VISION-AVOID", inputStage.Name,
                            "콜렛 클리닝 시작 전 InputVisionX Avoid 이동에 실패했습니다. result=" + inputResult);
                }
            }

            OutputStageUnit outputStage = Context.Machine.OutputStageUnit;
            if (outputStage == null || outputStage.OutputCameraX == null ||
                outputStage.Recipe == null || outputStage.Recipe.VisionX == null)
                return Fail("COLLET-CLEAN-OUTPUT-VISION-MISSING", "OutputStageUnit",
                    "콜렛 클리닝 시작 전 OutputVisionX Avoid 이동에 필요한 축/Recipe가 없습니다.");

            double outputTarget = outputStage.Recipe.VisionX.AvoidPosition;
            if (outputStage.OutputCameraX.IsAtTargetPosition(outputTarget, 0.0))
                return 0;

            CalibrationMotionSettings motion = ResolveCleaningStageMotion();
            int result = await outputStage.MoveVisionXToAvoidAndVerifyAsync(
                ResolveMoveTimeout(),
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("COLLET-CLEAN-OUTPUT-VISION-AVOID", outputStage.Name,
                    "콜렛 클리닝 시작 전 OutputVisionX Avoid 이동에 실패했습니다. result=" + result);

            return 0;
        }

        // ---------------------------------------------------------------- ReserveArea

        private SequenceResourceLease _pickerLease;
        private SequenceResourceLease _placeLease;
        private SequenceResourceLease _ngStageLease;

        private async Task<int> ReserveAreaAsync(CancellationToken ct)
        {
            try
            {
                _pickerLease = await AcquireResourceAsync(PickerResourceKind, Name + ":" + Side, ct).ConfigureAwait(false);
                if (_pickerLease == null)
                    return Fail("COLLET-CLEAN-RESOURCE", Name, "Picker 리소스 점유에 실패했습니다. side=" + Side);

                _placeLease = await AcquireResourceAsync(
                    SequenceResourceKind.OutputPlaceArea, Name + ":OutputPlaceArea", ct).ConfigureAwait(false);
                if (_placeLease == null)
                    return Fail("COLLET-CLEAN-RESOURCE", Name, "OutputPlaceArea 리소스 점유에 실패했습니다.");

                _ngStageLease = await AcquireResourceAsync(
                    SequenceResourceKind.OutputNgStageArea, Name + ":OutputNgStageArea", ct).ConfigureAwait(false);
                if (_ngStageLease == null)
                    return Fail("COLLET-CLEAN-RESOURCE", Name, "OutputNgStageArea 리소스 점유에 실패했습니다.");

                CurrentStep = ColletCleaningStep.CleanAllSelectedCollets;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CLEAN-RESOURCE-EX", Name,
                    "콜렛 클리닝 리소스 점유 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private void ReleaseAllLeases()
        {
            try
            {
                if (_ngStageLease != null) { _ngStageLease.Dispose(); _ngStageLease = null; }
                if (_placeLease != null) { _placeLease.Dispose(); _placeLease = null; }
                if (_pickerLease != null) { _pickerLease.Dispose(); _pickerLease = null; }
            }
            catch (Exception ex)
            {
                WriteLog(Name, "콜렛 클리닝 리소스 해제 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        // ---------------------------------------------------------------- Phase 1: Clean

        private async Task<int> CleanAllSelectedColletsAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                List<ColletCleaningItem> pending = _items.Where(i => !i.Cleaned).ToList();
                if (pending.Count == 0)
                {
                    CurrentStep = ColletCleaningStep.MoveToInspectionZone;
                    return 0;
                }

                WriteLog(Name,
                    "[Phase1] 선택 콜렛 클리닝을 시작합니다. side=" + _cleaningSide +
                    ", count=" + pending.Count + " - Start");

                foreach (ColletCleaningItem item in pending)
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested(Name + ":Clean:" + item.ColletNo);

                    int result = await CleanSingleColletAsync(item, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    item.Cleaned = true;
                }

                CurrentStep = ColletCleaningStep.MoveToInspectionZone;
                return 0;
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
                return Fail("COLLET-CLEAN-PHASE1-EX", Name,
                    "콜렛 클리닝 Phase1 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> CleanSingleColletAsync(ColletCleaningItem item, CancellationToken ct)
        {
            OutputStageUnit stage = Context.Machine.OutputStageUnit;
            if (stage == null || stage.Recipe == null)
                return Fail("COLLET-CLEAN-OUTPUT-STAGE", "OutputStageUnit",
                    "콜렛 클리닝 중 OutputStage 유닛/Recipe를 찾을 수 없습니다.");

            // 1) 클리닝에 사용할 NG 다이맵 셀을 맨 끝쪽부터 선점한다.
            OutputStageReceiveTarget cell;
            string cellReason;
            if (!TryReserveCleaningCell(out cell, out cellReason))
                return Fail("COLLET-CLEAN-CELL", "Material", cellReason);

            item.UsedCellOrderIndex = cell.OrderIndex;

            // 2) 셀 위치에 대응하는 Stage Y / Picker X,Y,T 목표를 계산한다.
            double ngStageBaseY = stage.Recipe.NGStageY.ProcessPosition;
            PlaceCoordinateResult target = PickerMotionTargetResolver.CalculateOutputPlaceTarget(
                Context.Machine,
                Side,
                item.ColletIndex,
                Name,
                "ColletCleaning-" + _cleaningSide + "-" + item.ColletNo,
                BinSide.Ng,
                ngStageBaseY,
                cell.TargetX,
                cell.TargetY,
                stage.Recipe.VisionX.ProcessPosition,
                _outputVisionToPickerX,
                _outputVisionToPickerY);
            if (target == null)
                return Fail("COLLET-CLEAN-TARGET", Name,
                    "콜렛 클리닝 대상 좌표 계산에 실패했습니다. side=" + _cleaningSide +
                    ", colletNo=" + item.ColletNo + ", order=" + cell.OrderIndex);

            // 3) 접촉 Z 계산. Place 티칭 Z는 다이/림/필름 높이만큼 이미 상승한 값이므로 그만큼 빼서 더 내려간다.
            //    ContactZUserOffset 부호 규약: + 상승(덜 누름) / - 하강(더 누름).
            double contactZ;
            string contactReason;
            if (!TryResolveContactZ(item, out contactZ, out contactReason))
                return Fail("COLLET-CLEAN-CONTACT-Z", Name, contactReason);

            // 4) NG Stage Y를 셀 위치로 이동한다. (NG Clamp Lift Up 강제는 축 이동 관문이 담당한다)
            int result = await MoveNgStageYAsync(stage, target.OutputStageY, item, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            // 5) Picker 정렬. X 단독 이동 전에는 PickerY가 Avoid여야 하므로
            //    반드시 X/T를 먼저 이동하고 그 다음 Y를 전진시킨다(MovePickerXTThenYAndVerifyAsync가 이 순서를 강제).
            //    개별 축을 X -> Y 순으로 부르면 "메뉴얼/단독 X축 이동 전 PickerY가 Avoid 또는 0 위치여야 합니다" 인터락에 걸린다.
            var alignTargets = new Dictionary<PickerAxis, double>
            {
                { PickerAxis.PickerX, target.PickerX },
                { GetPickerTAxis(item.ColletIndex), target.PickerT },
                { PickerAxis.PickerY, target.PickerY }
            };
            result = await MovePickerXTThenYAndVerifyAsync(
                alignTargets,
                "콜렛 클리닝 정렬",
                ct,
                CleaningTargetName,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            // 6) 누름 사이클.
            result = await RunPressCycleAsync(item, contactZ, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            // 7) 사용한 셀을 배치 대상에서 제외할지 정책에 따라 처리한다.
            if (!_settings.AllowPlaceOnCleanedCell)
            {
                string excludeReason;
                if (!MaterialStateService.TryExcludeOutputReceiveSlotForColletCleaning(
                    BinSide.Ng, cell.OrderIndex, out excludeReason))
                {
                    WriteLog(Name,
                        "클리닝 사용 셀을 배치 대상에서 제외하지 못했습니다. order=" + cell.OrderIndex +
                        ", reason=" + excludeReason + " - Failed");
                }
            }

            WriteLog(Name,
                "[Phase1] 콜렛 클리닝 완료. side=" + _cleaningSide +
                ", colletNo=" + item.ColletNo +
                ", cellOrder=" + cell.OrderIndex +
                ", map=(" + cell.DieMapX + "," + cell.DieMapY + ")" +
                ", contactZ=" + contactZ.ToString("F6") +
                ", pressCount=" + _settings.CleanPressCount +
                ", allowPlaceOnCleanedCell=" + _settings.AllowPlaceOnCleanedCell + " - Ok");
            return 0;
        }

        private async Task<int> MoveNgStageYAsync(
            OutputStageUnit stage,
            double targetY,
            ColletCleaningItem item,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // NG StageY 이동 전제조건 확보(인터락 회피).
            // 순서: NG Clamp Lift Up -> Good Guide Down -> GoodStageZ Avoid.
            // 각 항목은 Is 함수로 현재 상태를 먼저 확인해 이미 만족하면 실린더/축을 움직이지 않는다.
            // 이 확보를 건너뛰면 MotionGuard가 -11로 차단한다.
            // (실장비 2026-07-26: "OutputNGStageY 이동 불가: OutputGoodStageZ가 정확한 Avoid 또는 0 이하 위치여야 합니다.")
            int clear = await stage.EnsureNgStageYMoveClearAsync(
                "콜렛 클리닝 NG StageY 이동",
                ResolveMoveTimeout(),
                Options != null && Options.FineMove,
                ct).ConfigureAwait(false);
            if (clear != 0)
                return Fail("COLLET-CLEAN-NG-Y-CLEAR", stage.Name,
                    "콜렛 클리닝 NG StageY 이동 전제조건(NG Clamp Lift Up / Good Guide Down / GoodStageZ Avoid) " +
                    "확보에 실패했습니다. result=" + clear +
                    ", " + stage.DescribeOutputStageInterlockState(BinSide.Ng));

            CalibrationMotionSettings motion = ResolveCleaningStageMotion();
            int result = await stage.MoveStageAxisCommandWithMotion(
                BinStageAxis.NgBinY,
                targetY,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                CleaningTargetName + ";NgStageY;colletNo=" + item.ColletNo).ConfigureAwait(false);
            if (result != 0)
                return Fail("COLLET-CLEAN-STAGE-MOVE", stage.Name,
                    "콜렛 클리닝 NG StageY 이동에 실패했습니다. result=" + result +
                    ", target=" + targetY.ToString("F6") +
                    ", " + stage.DescribeOutputStageInterlockState(BinSide.Ng));

            return 0;
        }

        /// <summary>
        /// 누름 사이클: 접촉 Z까지 하강 -> 도착 대기 -> 반복 상승 높이만큼 상승. 지정 횟수 반복 후 Z Avoid 복귀.
        /// </summary>
        private async Task<int> RunPressCycleAsync(ColletCleaningItem item, double contactZ, CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(item.ColletIndex);
            double liftZ = contactZ + Math.Abs(_settings.RepeatLiftHeight);
            int pressCount = _settings.CleanPressCount;

            // 누름 사이클은 반복 횟수/대기 시간이 설정대로 도는지 로그로 증명한다.
            WriteLog(Name,
                "[Phase1] 누름 사이클 시작. side=" + _cleaningSide +
                ", colletNo=" + item.ColletNo +
                ", zAxis=" + zAxis +
                ", pressCount=" + pressCount +
                ", contactZ=" + contactZ.ToString("F6") +
                ", liftZ=" + liftZ.ToString("F6") +
                ", repeatLiftHeight=" + _settings.RepeatLiftHeight.ToString("F6") +
                ", arriveDwellMs=" + _settings.ArriveDwellMs +
                ", cleanVelocity=" + _settings.CleanVelocity.ToString("F3") + " - Start");

            for (int press = 1; press <= pressCount; press++)
            {
                ct.ThrowIfCancellationRequested();
                Context.StopIfCycleStopRequested(Name + ":Press:" + item.ColletNo + ":" + press);

                int result = await MovePickerZWithCleaningMotionAsync(
                    zAxis, contactZ,
                    "콜렛 클리닝 누름 하강 " + press + "/" + pressCount, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog(Name,
                    "[Phase1] 누름 하강 완료. colletNo=" + item.ColletNo +
                    ", press=" + press + "/" + pressCount +
                    ", contactZ=" + contactZ.ToString("F6") +
                    ", actual=" + DescribePickerAxisActual(zAxis) + " - Ok");

                if (_settings.ArriveDwellMs > 0)
                {
                    WriteLog(Name,
                        "[Phase1] 누름 유지 대기. colletNo=" + item.ColletNo +
                        ", press=" + press + "/" + pressCount +
                        ", dwellMs=" + _settings.ArriveDwellMs + " - Wait");
                    await Task.Delay(_settings.ArriveDwellMs, ct).ConfigureAwait(false);
                }

                result = await MovePickerZWithCleaningMotionAsync(
                    zAxis, liftZ,
                    "콜렛 클리닝 누름 상승 " + press + "/" + pressCount, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog(Name,
                    "[Phase1] 누름 상승 완료. colletNo=" + item.ColletNo +
                    ", press=" + press + "/" + pressCount +
                    ", liftZ=" + liftZ.ToString("F6") +
                    ", actual=" + DescribePickerAxisActual(zAxis) + " - Ok");
            }

            WriteLog(Name,
                "[Phase1] 누름 사이클 완료. colletNo=" + item.ColletNo +
                ", 수행 횟수=" + pressCount + " - Ok");

            // 다음 셀로 넘어가기 전 반드시 Z Avoid -> Y Avoid 순서로 복귀한다.
            // PickerX 단독 이동은 PickerY가 Avoid일 때만 허용되므로 Y 복귀를 빠뜨리면
            // 다음 콜렛의 X 이동에서 인터락에 걸린다.
            return await MovePickerZThenYToAvoidAsync(zAxis, "콜렛 클리닝 후", ct).ConfigureAwait(false);
        }

        /// <summary>
        /// 한 셀 처리를 마치고 Z -> Y 순서로 Avoid에 복귀한다(안전 위치 이동 속도 사용).
        /// 다음 셀의 PickerX 이동 전제조건(PickerY Avoid)을 만들어 주는 필수 단계다.
        /// </summary>
        private async Task<int> MovePickerZThenYToAvoidAsync(
            PickerAxis zAxis,
            string description,
            CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                GetPickerTeachingPosition(zAxis, "AvoidPosition"),
                description + " PickerZ Avoid",
                ct,
                CleaningTargetName,
                false,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                description + " PickerY Avoid",
                ct,
                "AvoidPosition;PickerPhase=SafeY",
                false,
                true).ConfigureAwait(false);
        }

        /// <summary>누름 사이클 로그용 축 실위치 문자열.</summary>
        private string DescribePickerAxisActual(PickerAxis axis)
        {
            try
            {
                BaseAxis target = GetPickerAxis(axis);
                if (target == null)
                    return "null";

                return target.ActualPosition.ToString("F6") +
                       "(moving=" + (target.IsMoving ? "Y" : "N") +
                       ", inPos=" + (target.IsInPosition ? "Y" : "N") + ")";
            }
            catch (Exception ex)
            {
                return "stateFailed:" + ex.Message;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZWithCleaningMotionAsync(
            PickerAxis zAxis,
            double target,
            string description,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await MovePickerAxisCommandWithMotionAsync(
                zAxis,
                target,
                _settings.CleanVelocity,
                _settings.CleanAcceleration,
                _settings.CleanDeceleration,
                CleaningTargetName).ConfigureAwait(false);
            if (result != 0)
                return Fail("COLLET-CLEAN-Z-MOVE", Name,
                    description + " 명령에 실패했습니다. result=" + result +
                    ", " + BuildPickerAxisState(zAxis, target));

            result = await WaitPickerAxisMoveDoneAsync(zAxis, target, ResolveMoveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("COLLET-CLEAN-Z-WAIT", Name,
                    description + " 완료 대기에 실패했습니다. result=" + result +
                    ", " + BuildPickerAxisState(zAxis, target));

            return 0;
        }

        /// <summary>
        /// 접촉 Z = NG Place 티칭 Z - 다이높이 - 림높이 - 필름높이 + 사용자 오프셋.
        /// 사용자 오프셋 부호: + 상승 / - 하강. 과압 방지를 위해 추가 하강량을 MaxExtraPressDepth로 제한한다.
        /// </summary>
        private bool TryResolveContactZ(ColletCleaningItem item, out double contactZ, out string reason)
        {
            contactZ = 0.0;
            reason = string.Empty;

            PickerAxis zAxis = GetPickerZAxis(item.ColletIndex);
            double placeTeachingZ = GetPickerTeachingPosition(zAxis, BuildIndexedPlacePositionName(item.ColletIndex));
            if (double.IsNaN(placeTeachingZ) || placeTeachingZ == 0.0)
            {
                placeTeachingZ = GetPickerTeachingPosition(zAxis, "PlacePosition");
            }

            if (double.IsNaN(placeTeachingZ) || placeTeachingZ == 0.0)
            {
                reason = "콜렛 클리닝 접촉 Z 기준이 될 NG Place 티칭 Z를 찾을 수 없습니다. side=" + _cleaningSide +
                         ", colletNo=" + item.ColletNo + ", zAxis=" + zAxis;
                return false;
            }

            double heightSum = _settings.DieHeight + _settings.RimHeight + _settings.FilmHeight;
            double baseContactZ = placeTeachingZ - heightSum;
            contactZ = baseContactZ + _settings.ContactZUserOffset;

            // 과압 방지: 사용자 오프셋이 음수(하강)로 한계를 넘으면 실행하지 않는다.
            double extraDepth = baseContactZ - contactZ;
            if (extraDepth > Math.Abs(_settings.MaxExtraPressDepth) + 1e-9)
            {
                reason = "콜렛 클리닝 접촉 Z가 과압 방지 한계를 초과했습니다. side=" + _cleaningSide +
                         ", colletNo=" + item.ColletNo +
                         ", placeTeachingZ=" + placeTeachingZ.ToString("F6") +
                         ", die=" + _settings.DieHeight.ToString("F6") +
                         ", rim=" + _settings.RimHeight.ToString("F6") +
                         ", film=" + _settings.FilmHeight.ToString("F6") +
                         ", userOffset=" + _settings.ContactZUserOffset.ToString("F6") +
                         ", extraDepth=" + extraDepth.ToString("F6") +
                         ", maxExtraPressDepth=" + Math.Abs(_settings.MaxExtraPressDepth).ToString("F6");
                return false;
            }

            WriteLog(Name,
                "콜렛 클리닝 접촉 Z 계산. side=" + _cleaningSide +
                ", colletNo=" + item.ColletNo +
                ", contactZ=placeTeachingZ-die-rim-film+userOffset=" +
                placeTeachingZ.ToString("F6") + "-" + _settings.DieHeight.ToString("F6") +
                "-" + _settings.RimHeight.ToString("F6") + "-" + _settings.FilmHeight.ToString("F6") +
                "+" + _settings.ContactZUserOffset.ToString("F6") + "=" + contactZ.ToString("F6") +
                ", liftHeight=" + _settings.RepeatLiftHeight.ToString("F6") + " - Ok");
            return true;
        }

        private static string BuildIndexedPlacePositionName(int colletIndex)
        {
            return "DiePlacePosition" + colletIndex;
        }

        /// <summary>
        /// 클리닝용 NG 다이맵 셀을 맨 끝쪽부터 하나 선점한다.
        /// 생산 배치는 앞에서부터 소비하므로, 끝에서부터 쓰면 충돌이 가장 늦게 발생한다.
        /// 대상 셀에 이미 die가 있으면(=생산 커서와 만남) 알람으로 중단한다.
        /// </summary>
        private bool TryReserveCleaningCell(out OutputStageReceiveTarget cell, out string reason)
        {
            cell = null;
            reason = string.Empty;

            WaferMaterial ngBin = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);
            if (ngBin == null)
            {
                reason = "콜렛 클리닝 셀 선점 실패: NG Stage에 Bin이 없습니다.";
                return false;
            }

            // Bin이 교체되면 끝쪽 소비 커서를 리셋한다.
            string currentBinId = ngBin.WaferId ?? string.Empty;
            if (!string.Equals(currentBinId, _cleaningCellBinWaferId, StringComparison.OrdinalIgnoreCase))
            {
                WriteLog(Name,
                    "NG Bin이 교체되어 콜렛 클리닝 셀 커서를 리셋합니다. old=" + _cleaningCellBinWaferId +
                    ", new=" + currentBinId + " - Check");
                _cleaningCellBinWaferId = currentBinId;
                _cleaningCellCursor = 0;
            }

            string peekReason;
            if (!MaterialStateService.TryPeekOutputReceiveSlotFromEnd(
                BinSide.Ng, _cleaningCellCursor, out cell, out peekReason))
            {
                reason = "콜렛 클리닝에 사용할 NG 다이맵 셀이 없습니다. cursor=" + _cleaningCellCursor +
                         ", reason=" + peekReason;
                return false;
            }

            _cleaningCellCursor++;
            return true;
        }

        // ---------------------------------------------------------------- Phase 2: Inspect

        private async Task<int> MoveToInspectionZoneAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // 존 전환(NG Stage -> Bottom 검사존)도 안전 이동이 기본이다.
                // 시작 때와 동일하게 Z -> Y -> T Avoid를 확보하고, PickerX 전제조건(카세트 리프터 Avoid)까지 다시 확인한다.
                const string zoneDescription = "콜렛 검사존 진입 안전 위치";

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    zoneDescription + " - PickerZ all Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    zoneDescription + " - PickerY Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY",
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveAllPickerTToAvoidAndVerifyAsync(
                    zoneDescription + " - PickerT all Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Bottom 존 진입 전에도 상대 Picker Avoid를 반드시 다시 확인한다.
                // 두 Picker가 같은 X 레일을 공유하므로 존 전환마다 재확인이 필요하다.
                result = await MoveOppositePickerToAvoidAndVerifyAsync(
                    zoneDescription + " - Opposite Picker Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureVisionAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureCassetteLifterAvoidForPickerXAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Bottom, "ColletCleaningInspection");

                WriteLog(Name,
                    "[Phase2] 콜렛 검사존으로 이동합니다. side=" + _cleaningSide +
                    ", count=" + _items.Count(i => !i.Inspected) + " - Start");

                CurrentStep = ColletCleaningStep.InspectAllSelectedCollets;
                return 0;
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
                return Fail("COLLET-CLEAN-ZONE-EX", Name,
                    "콜렛 검사존 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> InspectAllSelectedColletsAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                foreach (ColletCleaningItem item in _items.Where(i => !i.Inspected).ToList())
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested(Name + ":Inspect:" + item.ColletNo);

                    int result = await InspectSingleColletAsync(item, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                CurrentStep = ColletCleaningStep.EvaluateAndRetry;
                return 0;
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
                return Fail("COLLET-CLEAN-PHASE2-EX", Name,
                    "콜렛 클리닝 Phase2 검사 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> InspectSingleColletAsync(ColletCleaningItem item, CancellationToken ct)
        {
            // 콜렛 Bottom 검사 위치는 레시피 티칭값을 그대로 사용한다.
            // (PickerX/PickerY의 DieBottomPosition[콜렛인덱스], 없으면 공용 BottomPosition)
            // 예: Front C4 -> X=615.021, Y=36.352 / Rear C4 -> X=614.897, Y=-32.119
            double bottomX = ResolveBottomTeachingPosition(PickerAxis.PickerX, item.ColletIndex);
            double bottomY = ResolveBottomTeachingPosition(PickerAxis.PickerY, item.ColletIndex);
            if (double.IsNaN(bottomX) || Math.Abs(bottomX) <= 1e-9)
                return Fail("COLLET-CLEAN-INSPECT-TARGET", Name,
                    "콜렛 검사 PickerX 레시피 티칭값이 없습니다. side=" + _cleaningSide +
                    ", colletNo=" + item.ColletNo);

            // 검사존 진입도 동일 규칙: X/T 먼저 -> Y 나중(단독 X 이동 전 PickerY Avoid 요구 인터락 회피).
            var inspectTargets = new Dictionary<PickerAxis, double>
            {
                { PickerAxis.PickerX, bottomX },
                { PickerAxis.PickerY, bottomY }
            };
            int result = await MovePickerXTThenYAndVerifyAsync(
                inspectTargets,
                "콜렛 검사 정렬",
                ct,
                CleaningTargetName,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            // Bottom 카메라는 콜렛을 촬영 높이까지 내려야 찍힌다. X/Y만 이동하고 Z가 Avoid에 있으면 촬영이 성립하지 않는다.
            // 촬영 Z는 레시피 티칭값(PickerZn의 DieBottomPosition[콜렛인덱스])을 사용한다.
            // 예: Front C4 = -12.187, Rear C4 = -11.5. 콜렛별 개별 티칭이 없으면 공용 BottomPosition으로 보완한다.
            PickerAxis zAxis = GetPickerZAxis(item.ColletIndex);
            double inspectionZ = GetPickerTeachingPosition(zAxis, BuildPickerTargetName("DieBottomPosition", item.ColletIndex));
            if (double.IsNaN(inspectionZ) || Math.Abs(inspectionZ) <= 1e-9)
                inspectionZ = GetPickerTeachingPosition(zAxis, "BottomPosition");

            double zAvoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            if (double.IsNaN(inspectionZ) || Math.Abs(inspectionZ - zAvoid) <= 1e-6)
            {
                return Fail("COLLET-CLEAN-INSPECT-Z-TEACH", Name,
                    "콜렛 검사 촬영 Z 레시피 티칭값이 없습니다(DieBottomPosition/BottomPosition). side=" + _cleaningSide +
                    ", colletNo=" + item.ColletNo +
                    ", zAxis=" + zAxis +
                    ", bottomZ=" + inspectionZ.ToString("F6") +
                    ", avoidZ=" + zAvoid.ToString("F6"));
            }

            // [안전이동 적용 2026-08-06] 촬영 자세 확보용 Z 하강이며 탐색(측정) 스트로크가 아니다.
            // 사용자 확정 규칙(2026-08-06): 측정 Z 스트로크만 화면 파라미터 속도, 나머지는 전부 안전이동.
            result = await MovePickerAxisAndVerifyAsync(
                zAxis, inspectionZ,
                "콜렛 검사 PickerZ 하강", ct, CleaningTargetName, true, true).ConfigureAwait(false);
            if (result != 0)
                return result;

            WriteLog(Name,
                "[Phase2] 콜렛 검사 촬영 자세 확보. side=" + _cleaningSide +
                ", colletNo=" + item.ColletNo +
                ", pickerX=" + bottomX.ToString("F6") +
                ", pickerY=" + bottomY.ToString("F6") +
                ", pickerZ=" + inspectionZ.ToString("F6") + " - Ok");

            await DelayBeforeBottomVisionInspectionAsync(item.ColletNo, ct).ConfigureAwait(false);

            // 판정은 비전이 주는 OK/NG를 그대로 사용한다(Handler는 임계값을 두지 않는다).
            InspectionResultDto inspection = await AutoVisionRequestService.InspectColletAsync(
                AutoVisionChannel.BottomInspection,
                Side == PickerSequenceSide.Front ? "FrontCollet" : "RearCollet",
                Side == PickerSequenceSide.Front ? 0 : 1,
                item.ColletNo,
                0,
                0,
                0,
                0,
                ResolveVisionInspectionTimeout(),
                ct).ConfigureAwait(false);

            // 시뮬레이션/드라이런/비전 미사용 환경에서는 통신 실패를 알람으로 올리지 않고 OK로 통과시킨다.
            // (실기 운전에서는 아래 통신 실패 알람이 그대로 유지된다.)
            if (inspection == null && IsColletInspectionSimulationAllowed())
            {
                WriteLog(Name,
                    "[Phase2] 시뮬레이션/드라이런 환경이라 콜렛 검사 결과를 OK로 간주합니다. side=" + _cleaningSide +
                    ", colletNo=" + item.ColletNo + " - Skip");
                inspection = new InspectionResultDto { IsPass = true, Raw = "SIMULATED-OK" };
            }

            // 검사 후에도 Z Avoid -> Y Avoid 순서로 복귀해야 다음 콜렛의 PickerX 이동이 가능하다.
            int zAvoidResult = await MovePickerZThenYToAvoidAsync(zAxis, "콜렛 검사 후", ct).ConfigureAwait(false);
            if (zAvoidResult != 0)
                return zAvoidResult;

            if (inspection == null)
                return Fail("COLLET-CLEAN-INSPECT-COMM", "Vision",
                    "콜렛 검사 결과를 수신하지 못했습니다(통신 실패). side=" + _cleaningSide +
                    ", colletNo=" + item.ColletNo);

            item.Inspected = true;
            item.InspectionOk = inspection.IsPass;
            item.Message = inspection.Raw ?? string.Empty;

            WriteLog(Name,
                "[Phase2] 콜렛 검사 결과. side=" + _cleaningSide +
                ", colletNo=" + item.ColletNo +
                ", judge=" + (inspection.IsPass ? "OK" : "NG") +
                ", retryUsed=" + item.RetryUsed +
                ", raw=" + (inspection.Raw ?? string.Empty) + " - Ok");
            return 0;
        }

        /// <summary>
        /// Bottom 검사 위치의 레시피 티칭값을 읽는다.
        /// 콜렛별 배열(DieBottomPosition[index])이 우선이고, 값이 없으면 공용 BottomPosition을 쓴다.
        /// </summary>
        private double ResolveBottomTeachingPosition(PickerAxis axis, int colletIndex)
        {
            double indexed = GetPickerTeachingPosition(axis, BuildPickerTargetName("DieBottomPosition", colletIndex));
            if (!double.IsNaN(indexed) && Math.Abs(indexed) > 1e-9)
                return indexed;

            return GetPickerTeachingPosition(axis, "BottomPosition");
        }

        /// <summary>
        /// 비전 미사용 / 드라이런 / 시뮬레이션 / 하드웨어 바이패스 환경인지 판정한다.
        /// 이 경우 Bottom 카메라와 통신이 안 되는 것이 정상이므로 검사 결과를 OK로 간주한다.
        /// (ColletCalibrationSequence.IsVisionResultSimulationAllowed와 동일 기준)
        /// </summary>
        private bool IsColletInspectionSimulationAllowed()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    return false;

                if (!settings.UseVision || settings.DryRunMode || settings.SimulationMode || settings.BypassHardware)
                    return true;

                // 비전을 쓰도록 되어 있어도 Bottom 채널이 실제로 연결되지 않았으면 실기 판정이 불가능하다.
                return !VisionCommandService.IsConnected(AutoVisionChannel.BottomInspection);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        // ---------------------------------------------------------------- Phase 3: Evaluate / Retry

        private int EvaluateAndRetry()
        {
            try
            {
                List<ColletCleaningItem> ngItems = _items
                    .Where(i => i.Inspected && !i.InspectionOk)
                    .ToList();

                foreach (ColletCleaningItem ok in _items.Where(i => i.Inspected && i.InspectionOk))
                {
                    if (ok.Result != ColletCleaningResult.Ok)
                    {
                        ok.Result = ColletCleaningResult.Ok;
                        WriteLog(Name,
                            "[Phase3] 콜렛 클리닝 완료(OK). side=" + _cleaningSide +
                            ", colletNo=" + ok.ColletNo + ", retryUsed=" + ok.RetryUsed + " - Ok");
                    }
                }

                if (ngItems.Count == 0)
                {
                    CurrentStep = ColletCleaningStep.MoveAvoid;
                    return 0;
                }

                var retryTargets = new List<ColletCleaningItem>();
                var replaceTargets = new List<ColletCleaningItem>();
                foreach (ColletCleaningItem ng in ngItems)
                {
                    if (ng.RetryUsed < _settings.MaxRetryCount)
                    {
                        ng.RetryUsed++;
                        ng.Cleaned = false;
                        ng.Inspected = false;
                        retryTargets.Add(ng);
                    }
                    else
                    {
                        ng.Result = ColletCleaningResult.ReplaceRequired;
                        replaceTargets.Add(ng);
                    }
                }

                if (retryTargets.Count > 0)
                {
                    WriteLog(Name,
                        "[Phase3] 검사 NG 콜렛을 다시 클리닝합니다. side=" + _cleaningSide +
                        ", collets=" + string.Join(",", retryTargets.Select(i => i.ColletNo.ToString()).ToArray()) +
                        ", maxRetry=" + _settings.MaxRetryCount + " - Check");
                    CurrentStep = ColletCleaningStep.CleanAllSelectedCollets;
                    return 0;
                }

                // 재시도를 모두 소진하고도 NG면 콜렛 교체가 필요하다.
                string replaceList = string.Join(",", replaceTargets.Select(i => i.ColletNo.ToString()).ToArray());
                if (_settings.DisablePickerOnReplaceAlarm)
                {
                    foreach (ColletCleaningItem replace in replaceTargets)
                        DisablePickerForReplace(replace);
                }

                return Fail("COLLET-CLEAN-REPLACE", Name,
                    "콜렛 클리닝 재시도를 모두 수행했지만 검사 NG가 유지됩니다. 콜렛 교체가 필요합니다. side=" +
                    _cleaningSide + ", collets=" + replaceList +
                    ", maxRetry=" + _settings.MaxRetryCount +
                    ", disablePicker=" + _settings.DisablePickerOnReplaceAlarm);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CLEAN-PHASE3-EX", Name,
                    "콜렛 클리닝 Phase3 판정 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>콜렛 교체 알람 시 해당 픽커만 생산에서 제외한다(옵션).</summary>
        private void DisablePickerForReplace(ColletCleaningItem item)
        {
            try
            {
                bool[] usePicker = null;
                if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                {
                    FrontPicker.Config.EnsureArrays();
                    usePicker = FrontPicker.Config.UsePicker;
                }
                else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                {
                    RearPicker.Config.EnsureArrays();
                    usePicker = RearPicker.Config.UsePicker;
                }

                if (usePicker == null || item.ColletIndex < 0 || item.ColletIndex >= usePicker.Length)
                    return;

                usePicker[item.ColletIndex] = false;
                WriteLog(Name,
                    "콜렛 교체 필요로 해당 Picker를 생산에서 제외했습니다. side=" + _cleaningSide +
                    ", colletNo=" + item.ColletNo + " - Check");
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-DISABLE",
                    "콜렛 교체 필요로 Picker를 생산에서 제외했습니다. side=" + _cleaningSide +
                    ", colletNo=" + item.ColletNo);
            }
            catch (Exception ex)
            {
                WriteLog(Name,
                    "콜렛 교체 시 Picker 제외 처리 중 예외가 발생했습니다. colletNo=" + item.ColletNo +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        // ---------------------------------------------------------------- MoveAvoid

        private async Task<int> MoveAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveCurrentPickerToAvoidAndVerifyAsync(
                    "콜렛 클리닝 종료 Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ReleasePickerWorkArea();
                ReleaseAllLeases();

                WriteLog(Name,
                    "콜렛 클리닝을 완료했습니다. side=" + _cleaningSide +
                    ", ok=" + _items.Count(i => i.Result == ColletCleaningResult.Ok) +
                    ", total=" + _items.Count + " - Ok");

                CurrentStep = ColletCleaningStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CLEAN-AVOID-EX", Name,
                    "콜렛 클리닝 종료 Avoid 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Calibration 공용 이동 조건(안전 위치 이동 속도 포함)을 CalibrationData에서 읽어온다.
        /// ColletCalibration 등 다른 캘리브레이션 시퀀스와 동일한 소스를 사용한다.
        /// </summary>
        private CalibrationMotionSettings ResolveCalibrationMotionFromData()
        {
            try
            {
                CalibrationData data = CalibrationCoordinateService.ResolveData(
                    Context != null ? Context.Machine : null);
                if (data != null && data.Collet != null &&
                    data.Collet.Settings != null && data.Collet.Settings.Motion != null)
                    return data.Collet.Settings.Motion.Clone();
            }
            catch (Exception ex)
            {
                WriteLog(Name,
                    "Calibration 이동 조건 로드에 실패해 기본값을 사용합니다. error=" + ex.Message + " - Check");
            }
            finally
            {
            }

            return new CalibrationMotionSettings();
        }

        /// <summary>Stage 축의 안전/준비 이동에 사용할 Calibration 이동 조건.</summary>
        private CalibrationMotionSettings ResolveCleaningStageMotion()
        {
            CalibrationMotionSettings motion = CalibrationMotion;
            if (motion != null)
                return motion;

            return new CalibrationMotionSettings();
        }
    }
}
