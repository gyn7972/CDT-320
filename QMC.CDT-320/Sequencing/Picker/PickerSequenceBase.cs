using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Sequencing.Calibration;

namespace QMC.CDT320.Sequencing
{
    internal abstract class PickerSequenceBase<TStep> where TStep : struct
    {
        private const double DefaultAutoProcessCorrectionMaxDistance = 2.0;
        protected PickerSequenceBase(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Side = side;
            Kind = kind;
            Name = name ?? side.ToString();
        }

        protected MachineSequenceContext Context { get; private set; }
        protected PickerSequenceSide Side { get; private set; }
        protected PickerSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected PickerSequenceOptions Options { get; private set; }
        protected CalibrationMotionSettings CalibrationMotion { get; private set; }
        protected TStep CurrentStep { get; set; }
        private IDisposable pickerWorkAreaScope;
        private PickerWorkZone pickerWorkAreaZone = PickerWorkZone.Unknown;
        private bool safetyRetreatMoveActive;
        private bool runtimeBottomAutoFocusCompletedForNextGrab;

        protected PickerFrontUnit FrontPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerFrontUnit : null; }
        }

        protected PickerRearUnit RearPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerRearUnit : null; }
        }

        protected MaterialLocationKind PickerLocationKind
        {
            get { return Side == PickerSequenceSide.Front ? MaterialLocationKind.PickerFront : MaterialLocationKind.PickerRear; }
        }

        protected SequenceResourceKind PickerResourceKind
        {
            get { return Side == PickerSequenceSide.Front ? SequenceResourceKind.FrontPicker : SequenceResourceKind.RearPicker; }
        }

        public async Task<int> RunAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            Options = options ?? PickerSequenceOptions.Default();
            Options.RunMode = Options.RunMode;

            // 캘리브레이션 시퀀스는 저빈도 + 검증(속도/저장값) 필요 작업이므로 실행 동안 모든 로그를 영속한다.
            using (IsCalibrationSequenceName(GetType().Name)
                ? QMC.Common.Logging.LogPolicy.BeginVerboseScope(GetType().Name)
                : null)
            using (SequenceLog.Push(
                Side == PickerSequenceSide.Front ? QMC.Common.Logging.EventKind.FrontHeadSeq : QMC.Common.Logging.EventKind.RearHeadSeq,
                Name, () => CurrentStep.ToString(), GetType().Name, Options.RunMode.ToString()))
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options.RunMode == SequenceRunMode.Auto,
                GetType().Name + ":" + Name + ":" + Options.RunMode))
            try
            {
                ct.ThrowIfCancellationRequested();
                SequenceTrace.RunStart(GetType().Name, Options.RunMode.ToString(), "name=" + Name, "side=" + Side, "kind=" + Kind);
                WriteLog("RunAsync", Name + " sequence start. kind=" + Kind + " - Start");
                int result = await ExecuteAsync(ct).ConfigureAwait(false);
                if (result == 0)
                {
                    WriteLog("RunAsync", Name + " sequence complete. kind=" + Kind + " - Ok");
                    SequenceTrace.RunEnd(GetType().Name, "Completed", result, "name=" + Name, "side=" + Side, "kind=" + Kind);
                }
                else
                {
                    SequenceTrace.RunEnd(GetType().Name, "Failed", result, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep);
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", Name + " sequence canceled. kind=" + Kind + " - Failed");
                SequenceTrace.RunEnd(GetType().Name, "Canceled", -1, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep);
                throw;
            }
            catch (SequenceStopException ex)
            {
                WriteLog("RunAsync", Name + " sequence stopped. kind=" + Kind + ", reason=" + ex.Message + " - Stopped");
                SequenceTrace.RunEnd(GetType().Name, "Stopped", -1, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep, "reason=" + ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                int failResult = Fail("PICKER-SEQ-EX", Name, Name + " sequence exception: " + ex.Message);
                SequenceTrace.RunEnd(GetType().Name, "Failed", failResult, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep, "error=" + ex.Message);
                return failResult;
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteAsync(CancellationToken ct);

        protected bool ShouldDeferCycleStopForPickerDrain()
        {
            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return false;
                if (Context == null || !Context.IsCycleStopRequested)
                    return false;
                if (IsAlarmStopActive())
                    return false;

                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die != null && die.IsInputTarget)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        protected int ResolveTimeout()
        {
            return Options != null && Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 30000;
        }

        // 캘리브레이션 계열 시퀀스 판별(타입명 기준). Collet/COC/PickUpZ/PlaceZ/Needle/VisionCamera/AutoCal/FocusScan 포함.
        private static bool IsCalibrationSequenceName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;
            return typeName.IndexOf("Calibration", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   typeName.IndexOf("FocusScan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   typeName.IndexOf("RotationCenter", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected int ResolveVisionInspectionTimeout()
        {
            const int defaultVisionInspectionTimeoutMs = 12000;
            return defaultVisionInspectionTimeoutMs;
        }

        protected int ResolveMoveTimeout()
        {
            if (CalibrationMotion != null)
            {
                CalibrationMotion.EnsureDefaults();
                return CalibrationMotion.MoveTimeoutMs;
            }

            return ResolveTimeout();
        }

        protected int ResolveResourceTimeout()
        {
            return Options != null && Options.ResourceTimeoutMs > 0 ? Options.ResourceTimeoutMs : 30000;
        }

        protected void SetCalibrationMotion(CalibrationMotionSettings motion)
        {
            CalibrationMotion = motion != null ? motion.Clone() : null;
        }

        protected void SetOptionsForManualOperation(PickerSequenceOptions options)
        {
            Options = options ?? PickerSequenceOptions.Default();
        }

        protected void SaveRuntimeState(string reason)
        {
            try
            {
                if (Context == null || Context.Controller == null)
                    return;

                Context.Controller.SaveMachineRuntimeState(reason);
            }
            catch (Exception ex)
            {
                WriteLog("SaveRuntimeState", Name + " runtime state save failed. reason=" + reason + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        protected void RecordBottomAutoFocusPickCount(int pickerNo, DieMaterial die)
        {
            try
            {
                VisionFocusCalibrationData data = ResolveFocusCalibrationData();
                if (data == null)
                    return;

                int totalCount = data.RecordRuntimeAutoFocusPick();
                WriteLog("PickerAutoFocus",
                    Name + " 생산 AutoFocus 전체 Pick Die 누적 수를 갱신했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", totalCount=" + totalCount +
                    ", wafer=" + BuildAutoFocusWaferKey(die) + " - Check");
            }
            catch (Exception ex)
            {
                WriteLog("PickerAutoFocus",
                    Name + " Bottom AutoFocus pick count update failed. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        protected async Task<int> RunBottomRuntimeAutoFocusIfNeededAsync(
            int pickerIndex,
            int pickerNo,
            DieMaterial die,
            double defaultPosition,
            CancellationToken ct)
        {
            try
            {
                runtimeBottomAutoFocusCompletedForNextGrab = false;
                ct.ThrowIfCancellationRequested();

                VisionFocusCalibrationData data = ResolveFocusCalibrationData();
                if (data == null)
                    return 0;

                VisionFocusScanSettings settings = data.BottomDieScan;
                settings.EnsureDefaults();
                string waferKey = BuildAutoFocusWaferKey(die);
                string reason;
                RuntimeAutoFocusScanMode scanMode;
                VisionFocusPickerSide focusSide = ResolveFocusPickerSide();
                if (!data.TryReserveRuntimeAutoFocus(focusSide, pickerNo, waferKey, out scanMode, out reason))
                {
                    WriteLog("PickerAutoFocus",
                        Name + " 생산 Bottom Die AutoFocus 조건이 없어 건너뜁니다. side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", totalCount=" + data.RuntimeAutoFocusTotalPickCount +
                        ", interval=" + settings.AutoFocusPickInterval +
                        ", wafer=" + waferKey +
                        ", lastWafer=" + (data.RuntimeAutoFocusLastWaferId ?? string.Empty) +
                        ", pending=" + data.BuildRuntimeAutoFocusPendingText() +
                        " - Check");
                    return 0;
                }

                try
                {
                    if (!IsPickerSimulationOrDryRun() &&
                        !QMC.CDT320.VisionComm.VisionCommandService.IsConnected(
                            QMC.CDT320.VisionComm.AutoVisionChannel.BottomInspection))
                    {
                        return Fail("PICKER-BOTTOM-DIE-AUTOFOCUS-VISION", Name,
                            "생산 Bottom Die AutoFocus를 시작할 수 없습니다. Bottom Vision 통신이 연결되어 있지 않습니다. " +
                            "side=" + Side + ", pickerNo=" + pickerNo + ", reason=" + reason);
                    }

                    bool fineOnly = scanMode == RuntimeAutoFocusScanMode.FineOnly;
                    WriteLog("PickerAutoFocus",
                        Name + " 생산 Bottom Die AutoFocus를 시작합니다. side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", defaultZ=" + defaultPosition.ToString("F6") +
                        ", scanMode=" + scanMode +
                        ", reason=" + reason +
                        ", totalCount=" + data.RuntimeAutoFocusTotalPickCount +
                        ", interval=" + settings.AutoFocusPickInterval +
                        ", wafer=" + waferKey +
                        ", pending=" + data.BuildRuntimeAutoFocusPendingText() +
                        " - Start");

                    var request = new VisionFocusScanRequest
                    {
                        Kind = VisionFocusScanKind.BottomDie,
                        PickerSide = focusSide,
                        PickerNo = pickerNo,
                        DefaultPosition = defaultPosition,
                        MinusRange = settings.MinusRange,
                        PlusRange = settings.PlusRange,
                        Step = settings.Step,
                        FineMinusRange = settings.FineMinusRange,
                        FinePlusRange = settings.FinePlusRange,
                        FineStep = settings.FineStep,
                        RepeatCount = settings.RepeatCount,
                        MoveVelocity = settings.MoveVelocity,
                        MoveAcceleration = settings.MoveAcceleration,
                        MoveDeceleration = settings.MoveDeceleration,
                        SettleDelayMs = settings.SettleDelayMs,
                        MotionTimeoutMs = settings.MotionTimeoutMs,
                        VisionTimeoutMs = settings.VisionTimeoutMs,
                        VisionBestTimeoutMs = settings.VisionBestTimeoutMs,
                        FocusValueReceiveMode = settings.FocusValueReceiveMode,
                        ReturnToDefaultAfterScan = false,
                        SkipPrepareFocusPosition = true,
                        FineOnlyScan = fineOnly,
                        RuntimeReason = reason,
                        UpdatedBy = "RuntimeBottomDieAutoFocus"
                    };

                    var sequence = new VisionFocusScanSequence(Context.Machine, request);
                    int result = await sequence.RunAsync(ct, Options != null ? Options.RunMode : SequenceRunMode.Auto).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    double bestZ = sequence.Result.BestPosition;
                    double minZ = fineOnly
                        ? defaultPosition - settings.FineMinusRange
                        : defaultPosition - settings.MinusRange - settings.FineMinusRange;
                    double maxZ = fineOnly
                        ? defaultPosition + settings.FinePlusRange
                        : defaultPosition + settings.PlusRange + settings.FinePlusRange;
                    if (double.IsNaN(bestZ) || double.IsInfinity(bestZ) || bestZ < minZ - 0.000001 || bestZ > maxZ + 0.000001)
                    {
                        return Fail("PICKER-BOTTOM-DIE-AUTOFOCUS-BEST-Z", Name,
                            "생산 Bottom Die AutoFocus Best Z가 허용 스캔 범위를 벗어났습니다. " +
                            "side=" + Side + ", pickerNo=" + pickerNo +
                            ", bestZ=" + bestZ + ", minZ=" + minZ + ", maxZ=" + maxZ);
                    }

                    result = await MovePickerAxisAndVerifyAsync(
                        GetPickerZAxis(pickerIndex),
                        bestZ,
                        "생산 Bottom Die AutoFocus Best Z 이동",
                        ct,
                        BuildPickerTargetName("BottomDieAutoFocusBestZ", pickerIndex),
                        true).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    // Runtime AF Offset 누적: runtimeDelta = bestZ - 기존 바텀 포커스(defaultPosition).
                    // 다이를 든 상태의 실측 포커스이므로 Pick/Place Z 보정(Collet AF Z Offset)에 누적한다(다음 다이부터 적용).
                    // 한계 초과는 fail-closed(알람 중단). 레시피 파일 저장은 아래 ApplyRuntimeBottomFocusPosition의
                    // 레시피 저장에 함께 실린다.
                    double afPreviousOffset;
                    bool afOffsetCommitted;
                    bool afStaleConsumed;
                    result = AccumulateColletAfZOffsetFromRuntimeAf(
                        pickerIndex,
                        defaultPosition,
                        bestZ,
                        sequence.Result.BestScore,
                        sequence.Result.SampleCount,
                        scanMode.ToString(),
                        waferKey,
                        out afPreviousOffset,
                        out afOffsetCommitted,
                        out afStaleConsumed);
                    if (result != 0)
                        return result;

                    if (!ApplyRuntimeBottomFocusPosition(pickerIndex, bestZ))
                    {
                        // 보상 롤백: offset·stale 플래그·인메모리 BottomPosition을 함께 AF 이전 상태로 복원한다.
                        // (부분 롤백 금지 — offset만 되돌리면 레시피 저장 실패 서브경로에서 기준선/offset이 어긋난다.)
                        RollbackRuntimeAfOffsetAndBaseline(pickerIndex, afPreviousOffset, afOffsetCommitted, afStaleConsumed, defaultPosition);
                        return Fail("PICKER-BOTTOM-DIE-AUTOFOCUS-SAVE", Name,
                            "생산 Bottom Die AutoFocus Best Z를 Picker별 BottomPosition에 저장하지 못했습니다. " +
                            "side=" + Side + ", pickerNo=" + pickerNo + ", bestZ=" + bestZ.ToString("F6"));
                    }
                    data.CompleteRuntimeAutoFocus(focusSide, pickerNo, waferKey);
                    SaveVisionFocusSettings("생산 Bottom Die AutoFocus 완료");
                    runtimeBottomAutoFocusCompletedForNextGrab = true;

                    WriteLog("PickerAutoFocus",
                        Name + " 생산 Bottom Die AutoFocus를 완료하고 Best Z 이동을 확인했습니다. side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", defaultZ=" + defaultPosition.ToString("F6") +
                        ", bestZ=" + bestZ.ToString("F6") +
                        ", score=" + sequence.Result.BestScore.ToString("F4") +
                        ", sample=" + sequence.Result.SampleCount +
                        ", scanMode=" + scanMode +
                        ", wafer=" + waferKey +
                        ", pending=" + data.BuildRuntimeAutoFocusPendingText() +
                        " - Ok");

                    return 0;
                }
                finally
                {
                    data.ReleaseRuntimeAutoFocusReservation();
                }
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
                return Fail("PICKER-BOTTOM-AUTOFOCUS-EX", Name,
                    "Bottom 촬영 전 Runtime AutoFocus 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private VisionFocusCalibrationData ResolveFocusCalibrationData()
        {
            if (Context == null || Context.Machine == null ||
                Context.Machine.VisionUnit == null ||
                Context.Machine.VisionUnit.Config == null)
                return null;

            Context.Machine.VisionUnit.Config.EnsureCalibrationObjects();
            VisionFocusCalibrationData data = Context.Machine.VisionUnit.Config.FocusCalibration;
            if (data != null)
                data.EnsureObjects();
            return data;
        }

        private VisionFocusPickerSide ResolveFocusPickerSide()
        {
            return Side == PickerSequenceSide.Rear
                ? VisionFocusPickerSide.Rear
                : VisionFocusPickerSide.Front;
        }

        private string BuildAutoFocusWaferKey(DieMaterial die)
        {
            if (die != null && !string.IsNullOrWhiteSpace(die.WaferID_Input))
                return die.WaferID_Input.Trim();

            try
            {
                if (Context != null && Context.Machine != null &&
                    Context.Machine.InputStageUnit != null &&
                    !string.IsNullOrWhiteSpace(Context.Machine.InputStageUnit.CurrentWaferId))
                    return Context.Machine.InputStageUnit.CurrentWaferId.Trim();
            }
            catch
            {
            }

            return string.Empty;
        }

        private static readonly object RuntimeBottomFocusRecipeSaveLock = new object();

        private bool ApplyRuntimeBottomFocusPosition(int pickerIndex, double bestZ)
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                    FrontPicker.SetRuntimePickerZPosition(pickerIndex, "FocusPosition", bestZ);
                else
                    RearPicker.SetRuntimePickerZPosition(pickerIndex, "FocusPosition", bestZ);

                double appliedZ = GetPickerTeachingPosition(GetPickerZAxis(pickerIndex), "BottomPosition");
                if (Math.Abs(appliedZ - bestZ) > 0.000001)
                {
                    WriteLog("PickerAutoFocus",
                        Name + " Runtime Bottom AutoFocus Best Z BottomPosition readback 불일치. " +
                        "side=" + Side +
                        ", pickerNo=" + (pickerIndex + 1) +
                        ", bestZ=" + bestZ.ToString("F6") +
                        ", appliedZ=" + appliedZ.ToString("F6") + " - Failed");
                    return false;
                }

                // Runtime AutoFocus Best Z는 Picker별 Recipe BottomPosition에 반영하고 파일까지 저장해
                // 다음 검사와 프로그램 재시작 후에도 같은 Z를 시작 기준으로 사용한다.
                return SaveRuntimeBottomFocusRecipe(pickerIndex, bestZ);
            }
            catch (Exception ex)
            {
                WriteLog("PickerAutoFocus",
                    Name + " Runtime Bottom AutoFocus Best Z BottomPosition 적용 중 예외. " +
                    "side=" + Side +
                    ", pickerNo=" + (pickerIndex + 1) +
                    ", bestZ=" + bestZ.ToString("F6") +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
        }

        private bool SaveRuntimeBottomFocusRecipe(int pickerIndex, double bestZ)
        {
            try
            {
                string recipeName = Context != null && Context.Controller != null
                    ? Context.Controller.ActiveRecipeName
                    : null;
                if (string.IsNullOrWhiteSpace(recipeName) || Context.Machine == null)
                {
                    WriteLog("PickerAutoFocus",
                        Name + " Runtime Bottom AutoFocus Best Z Recipe 저장 생략. 활성 Recipe 이름을 확인할 수 없습니다. " +
                        "pickerNo=" + (pickerIndex + 1) +
                        ", bestZ=" + bestZ.ToString("F6") + " - Failed");
                    return false;
                }

                bool saved;
                lock (RuntimeBottomFocusRecipeSaveLock)
                {
                    saved = Context.Machine.SaveRecipe(recipeName);
                }

                if (!saved)
                {
                    WriteLog("PickerAutoFocus",
                        Name + " Runtime Bottom AutoFocus Best Z Recipe 저장 실패. 메모리 값은 적용되어 이번 런에는 사용되지만 " +
                        "재시작 시 이전 값으로 복원됩니다. pickerNo=" + (pickerIndex + 1) +
                        ", bestZ=" + bestZ.ToString("F6") +
                        ", recipe=" + recipeName + " - Failed");
                    return false;
                }

                WriteLog("PickerAutoFocus",
                    Name + " Runtime Bottom AutoFocus Best Z를 Recipe BottomPosition에 저장했습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + (pickerIndex + 1) +
                    ", bestZ=" + bestZ.ToString("F6") +
                    ", recipe=" + recipeName + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                WriteLog("PickerAutoFocus",
                    Name + " Runtime Bottom AutoFocus Best Z Recipe 저장 중 예외. pickerNo=" + (pickerIndex + 1) +
                    ", bestZ=" + bestZ.ToString("F6") +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private void SaveVisionFocusSettings(string reason)
        {
            try
            {
                if (Context == null || Context.Machine == null || Context.Machine.VisionUnit == null)
                    return;

                bool saved = Context.Machine.VisionUnit.SaveSettings();
                WriteLog("PickerAutoFocus",
                    Name + " Vision Focus 설정 저장. reason=" + reason +
                    ", saved=" + saved + " - Check");
            }
            catch (Exception ex)
            {
                WriteLog("PickerAutoFocus",
                    Name + " Vision Focus 설정 저장 실패. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        protected int ResolvePickerNo()
        {
            int pickerNo = Options != null && Options.PickerNo > 0 ? Options.PickerNo : 1;
            if (pickerNo < 1)
                pickerNo = 1;
            if (pickerNo > 4)
                pickerNo = 4;
            return pickerNo;
        }

        protected bool IsPickerSideEnabled()
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.UseUnit;
            return RearPicker != null && RearPicker.Config != null && RearPicker.Config.UseUnit;
        }

        protected bool IsPickerSimulationOrDryRun()
        {
            AppSettings settings = AppSettingsStore.Current;
            if (settings != null && (settings.BypassHardware || settings.DryRunMode))
                return true;

            if (Context != null && Context.Controller != null && Context.Controller.GlobalDryRun)
                return true;

            if (Side == PickerSequenceSide.Front)
                return IsFrontPickerSimulationOrDryRun();

            if (Side == PickerSequenceSide.Rear)
                return IsRearPickerSimulationOrDryRun();

            return false;
        }

        protected string AppendAutoProcessCorrectionTargetTag(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                targetName = string.Empty;

            if (!IsRealEquipmentOrDryRunAutoSequence())
                return targetName;

            if (targetName.IndexOf("AutoProcessCorrection", StringComparison.OrdinalIgnoreCase) >= 0)
                return targetName;

            return targetName +
                   ";AutoSequence;AutoProcessCorrection;AutoProcessCorrectionMax=" +
                   DefaultAutoProcessCorrectionMaxDistance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        protected bool IsRealEquipmentOrDryRunAutoSequence()
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                return false;

            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    return false;

                return settings.UseAjin && (settings.DryRunMode || !settings.SimulationMode);
            }
            catch
            {
                return false;
            }
        }

        protected bool IsPickerMotionOnlyTestMode()
        {
            return Options != null &&
                   Options.RunMode == SequenceRunMode.Auto &&
                   Options.PickerMotionOnlyTestMode;
        }

        protected bool IsFrontPickerSimulationOrDryRun()
        {
            return FrontPicker != null &&
                   ((FrontPicker.Setup != null && FrontPicker.Setup.IsSimulationMode) ||
                    (FrontPicker.Config != null && FrontPicker.Config.bDryRun));
        }

        protected bool IsRearPickerSimulationOrDryRun()
        {
            return RearPicker != null &&
                   ((RearPicker.Setup != null && RearPicker.Setup.IsSimulationMode) ||
                    (RearPicker.Config != null && RearPicker.Config.bDryRun));
        }

        protected List<int> BuildEnabledPickerIndexes()
        {
            var result = new List<int>();

            bool[] usePicker = ResolveUsePickerArray();
            PickerRunOrderMode orderMode = ResolveRunOrderMode();
            int[] order = orderMode == PickerRunOrderMode.Ascending
                ? new[] { 0, 1, 2, 3 }
                : new[] { 3, 2, 1, 0 };

            for (int i = 0; i < order.Length; i++)
            {
                int index = order[i];
                if (usePicker != null && index < usePicker.Length && usePicker[index])
                    result.Add(index);
            }

            return result;
        }

        protected List<int> BuildLoadedPickerIndexesInRunOrder(string ownerName)
        {
            var result = new List<int>();

            try
            {
                List<int> enabled = BuildEnabledPickerIndexes();
                for (int i = 0; i < enabled.Count; i++)
                {
                    int index = enabled[i];
                    int pickerNo = ToPickerNo(index);
                    if (Options != null &&
                        Options.RestrictToPickerNo > 0 &&
                        Options.RestrictToPickerNo != pickerNo)
                    {
                        continue;
                    }

                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die == null)
                    {
                        WriteLog(ownerName,
                            Name + " picker has no die. pickerNo=" + pickerNo +
                            " - Check");
                        continue;
                    }

                    result.Add(index);
                    WriteLog(ownerName,
                        Name + " picker loaded die selected. die=" + die.DieId +
                        ", pickerNo=" + pickerNo +
                        ", result=" + die.Result +
                        ", location=" + (die.CurrentLocation != null ? die.CurrentLocation.ToString() : "-") +
                        " - Check");
                }
            }
            catch (Exception ex)
            {
                WriteLog(ownerName,
                    Name + " build loaded picker list failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return result;
        }

        protected int ToPickerNo(int pickerIndex)
        {
            if (pickerIndex < 0)
                return 1;
            if (pickerIndex > 3)
                return 4;
            return pickerIndex + 1;
        }

        protected int ToPickerIndex(int pickerNo)
        {
            if (pickerNo <= 1)
                return 0;
            if (pickerNo >= 4)
                return 3;
            return pickerNo - 1;
        }

        protected string BuildPickerTargetName(string positionArrayName, int pickerIndex)
        {
            return (positionArrayName ?? string.Empty) + "[P" + ToPickerNo(pickerIndex) + "]";
        }

        protected bool IsPickerIndexEnabled(int pickerIndex)
        {
            bool[] usePicker = ResolveUsePickerArray();
            return usePicker != null &&
                   pickerIndex >= 0 &&
                   pickerIndex < usePicker.Length &&
                   usePicker[pickerIndex];
        }

        private bool[] ResolveUsePickerArray()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
            {
                FrontPicker.Config.EnsureArrays();
                return FrontPicker.Config.UsePicker;
            }

            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
            {
                RearPicker.Config.EnsureArrays();
                return RearPicker.Config.UsePicker;
            }

            return null;
        }

        private PickerRunOrderMode ResolveRunOrderMode()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                return FrontPicker.Config.RunOrderMode;
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                return RearPicker.Config.RunOrderMode;
            return PickerRunOrderMode.Descending;
        }

        protected async Task<int> MovePickerAxisAndVerifyAsync(
            PickerAxis axis,
            double target,
            string description,
            CancellationToken ct,
            string targetName = null,
            bool forceMove = false,
            bool useSafeMoveMotion = false)
        {
            Stopwatch totalWatch = Stopwatch.StartNew();
            long commandMs = 0;
            long waitMs = 0;
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                if (!forceMove && CanSkipPickerMoveCommand(axis, target))
                {
                    WriteLog("PickerMove",
                        Name + " " + description + " move skipped. Axis already in position. " +
                        BuildPickerAxisState(axis, target) + " - Ok");
                    SequenceTrace.MotionEnd("PickerMove", 0,
                        "axis=" + axis,
                        "target=" + target,
                        "description=" + description,
                        "status=AlreadyInPosition");
                    return 0;
                }

                PickerMoveAxisLogDetail axisDetail = BuildPickerMoveAxisLogDetail(axis, target);
                SequenceTrace.MotionStart("PickerMove",
                    "axis=" + axis,
                    "target=" + target,
                    "actual=" + axisDetail.Start,
                    "description=" + description,
                    "targetName=" + targetName,
                    "forceMove=" + forceMove);

                int sharedRailReadyResult = await WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
                    axis,
                    target,
                    targetName,
                    description,
                    ct,
                    forceMove).ConfigureAwait(false);
                if (sharedRailReadyResult != 0)
                    return sharedRailReadyResult;

                int yReadyResult = await WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
                    axis,
                    target,
                    null,
                    targetName,
                    description,
                    ct).ConfigureAwait(false);
                if (yReadyResult != 0)
                    return yReadyResult;

                int facingYReadyResult = await WaitPickerFacingYInterlockBeforeAutoMoveAsync(
                    axis,
                    target,
                    targetName,
                    description,
                    ct).ConfigureAwait(false);
                if (facingYReadyResult != 0)
                    return facingYReadyResult;

                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                Stopwatch commandWatch = Stopwatch.StartNew();
                int result = await SequenceAwaiter.AwaitAsync(
                    MovePickerAxisCommandAsync(axis, target, targetName, forceMove, useSafeMoveMotion),
                    -1,
                    ct).ConfigureAwait(false);
                commandMs = commandWatch.ElapsedMilliseconds;
                if (result != 0)
                {
                    //WritePickerSequenceMoveElapsed(axisDetail, targetName, description, result, commandMs, waitMs, totalWatch.ElapsedMilliseconds, null);
                    SequenceTrace.MotionEnd("PickerMove", result,
                        "axis=" + axis,
                        "target=" + target,
                        "description=" + description,
                        "status=CommandFailed");
                    return Fail("PICKER-MOVE-CMD", Name, BuildPickerMoveCommandFailureMessage(axis, target, description, result));
                }

                Stopwatch waitWatch = Stopwatch.StartNew();
                int moveTimeout = ResolveMoveTimeout();
                int waitCode = await WaitPickerAxisMoveDoneAsync(axis, target, moveTimeout, ct).ConfigureAwait(false);
                waitMs = waitWatch.ElapsedMilliseconds;
                if (waitCode != 0)
                {
                    SequenceTrace.MotionEnd("PickerMove", -1,
                        "axis=" + axis,
                        "target=" + target,
                        "description=" + description,
                        "status=WaitFailed",
                        "timeoutMs=" + moveTimeout,
                        "wait=" + waitCode);
                    return Fail("PICKER-MOVE", Name,
                        description + " move/in-position wait failed. waitCode=" + waitCode +
                        ", reason=" + BuildPickerAxisLastMotionFailure(axis) +
                        ". " + BuildPickerAxisState(axis, target));
                }

                //WritePickerSequenceMoveElapsed(axisDetail, targetName, description, result, commandMs, waitMs, totalWatch.ElapsedMilliseconds, waitResult);
                SequenceTrace.MotionEnd("PickerMove", 0,
                    "axis=" + axis,
                    "target=" + target,
                    "actual=" + (GetPickerAxis(axis) != null ? GetPickerAxis(axis).ActualPosition.ToString() : ""),
                    "description=" + description,
                    "completionVerification=StrongWait",
                    "commandMs=" + commandMs,
                    "waitMs=" + waitMs,
                    "elapsedMs=" + totalWatch.ElapsedMilliseconds);
                WriteLog("PickerMoveComplete",
                    Name + " picker axis move complete. description=" + (description ?? string.Empty) +
                    ", targetName=" + (targetName ?? string.Empty) +
                    ", commandMs=" + commandMs +
                    ", waitMs=" + waitMs +
                    ", elapsedMs=" + totalWatch.ElapsedMilliseconds +
                    ", completionVerification=StrongWait" +
                    ", " + BuildPickerAxisState(axis, target) +
                    " - Ok");
                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SequenceTrace.MotionEnd("PickerMove", -1,
                    "axis=" + axis,
                    "target=" + target,
                    "description=" + description,
                    "status=Exception",
                    "error=" + ex.Message);
                return Fail("PICKER-MOVE-EX", Name, description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MovePickerAxesAndVerifyAsync(
            IDictionary<PickerAxis, double> targets,
            string description,
            CancellationToken ct,
            string targetName = null,
            bool forceMove = false,
            bool useSafeMoveMotion = false)
        {
            Stopwatch totalWatch = Stopwatch.StartNew();
            long commandMs = 0;
            long waitMs = 0;
            try
            {
                if (targets == null || targets.Count == 0)
                    return 0;

                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                int sharedRailReadyResult = await WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
                    targets,
                    targetName,
                    description,
                    ct,
                    forceMove).ConfigureAwait(false);
                if (sharedRailReadyResult != 0)
                    return sharedRailReadyResult;

                int yReadyResult = await WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
                    targets,
                    targetName,
                    description,
                    ct,
                    forceMove).ConfigureAwait(false);
                if (yReadyResult != 0)
                    return yReadyResult;

                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                //수정포인트 1
                var commandTasks = new List<Task<int>>();
                var commandTargets = new List<KeyValuePair<PickerAxis, double>>();
                var commandDetails = new List<PickerMoveAxisLogDetail>();
                double pairedXTarget;
                double pairedYTarget;
                bool hasPairedXTarget = targets.TryGetValue(PickerAxis.PickerX, out pairedXTarget);
                bool hasPairedYTarget = targets.TryGetValue(PickerAxis.PickerY, out pairedYTarget);
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsAlarmStopActive())
                        return StopPickerMoveBecauseAlarmActive(description);

                    if (!forceMove && CanSkipPickerMoveCommand(pair.Key, pair.Value))
                    {
                        if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                        {
                            WriteLog("PickerMove",
                                Name + " " + description + " move skipped. Axis already in position. " +
                                BuildPickerAxisState(pair.Key, pair.Value) + " - Ok");
                        }
                        continue;
                    }

                    int facingYReadyResult = await WaitPickerFacingYInterlockBeforeAutoMoveAsync(
                        pair.Key,
                        pair.Value,
                        targetName,
                        description,
                        ct,
                        hasPairedXTarget ? (double?)pairedXTarget : null,
                        hasPairedYTarget ? (double?)pairedYTarget : null).ConfigureAwait(false);
                    if (facingYReadyResult != 0)
                        return facingYReadyResult;

                    commandTargets.Add(pair);
                    commandDetails.Add(BuildPickerMoveAxisLogDetail(pair.Key, pair.Value));
                    commandTasks.Add(MovePickerAxisCommandAsync(pair.Key, pair.Value, targetName, forceMove, useSafeMoveMotion));
                }

                if (commandTasks.Count > 0)
                {
                    Stopwatch commandWatch = Stopwatch.StartNew();
                    int[] commandResults = await SequenceAwaiter.AwaitAsync(
                        Task.WhenAll(commandTasks),
                        new int[0],
                        ct).ConfigureAwait(false);
                    commandMs = commandWatch.ElapsedMilliseconds;
                    for (int commandIndex = 0; commandIndex < commandTargets.Count; commandIndex++)
                    {
                        KeyValuePair<PickerAxis, double> pair = commandTargets[commandIndex];
                        if (commandResults[commandIndex] != 0)
                        {
                            WritePickerSequenceGroupMoveElapsed(targetName, description, commandDetails, commandResults[commandIndex], commandMs, waitMs, totalWatch.ElapsedMilliseconds, "CommandFailed:" + pair.Key);
                            return Fail("PICKER-MOVE-CMD", Name, BuildPickerMoveCommandFailureMessage(pair.Key, pair.Value, description, commandResults[commandIndex]));
                        }
                    }
                }

                var waitTargets = new List<KeyValuePair<PickerAxis, double>>(targets);
                var waitTasks = new List<Task<int>>();
                foreach (KeyValuePair<PickerAxis, double> pair in waitTargets)
                    waitTasks.Add(WaitPickerAxisMoveDoneAsync(pair.Key, pair.Value, ResolveMoveTimeout(), ct));

                Stopwatch waitWatch = Stopwatch.StartNew();
                int[] waitResults = await SequenceAwaiter.AwaitAsync(
                    Task.WhenAll(waitTasks),
                    new int[0],
                    ct).ConfigureAwait(false);
                waitMs = waitWatch.ElapsedMilliseconds;
                for (int waitIndex = 0; waitIndex < waitTargets.Count && waitIndex < waitResults.Length; waitIndex++)
                {
                    KeyValuePair<PickerAxis, double> pair = waitTargets[waitIndex];
                    if (waitResults[waitIndex] != 0)
                    {
                        WritePickerSequenceGroupMoveElapsed(targetName, description, commandDetails, -1, commandMs, waitMs, totalWatch.ElapsedMilliseconds, "WaitFailed:" + pair.Key + ":" + waitResults[waitIndex]);
                        return Fail("PICKER-MOVE", Name,
                            description + " move/in-position wait failed. waitCode=" + waitResults[waitIndex] +
                            ", reason=" + BuildPickerAxisLastMotionFailure(pair.Key) +
                            ". " + BuildPickerAxisState(pair.Key, pair.Value));
                    }
                }
                if (commandTargets.Count > 0 && (waitMs >= 200 || totalWatch.ElapsedMilliseconds >= 250))
                    WritePickerSequenceGroupMoveElapsed(targetName, description, commandDetails, 0, commandMs, waitMs, totalWatch.ElapsedMilliseconds, "Ok");

                WriteLog("PickerMoveComplete",
                    Name + " picker group move complete. description=" + (description ?? string.Empty) +
                    ", targetName=" + (targetName ?? string.Empty) +
                    ", commandCount=" + commandTargets.Count +
                    ", commandMs=" + commandMs +
                    ", waitMs=" + waitMs +
                    ", elapsedMs=" + totalWatch.ElapsedMilliseconds +
                    ", axisStates=" + BuildPickerAxesState(targets) +
                    " - Ok");

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-MOVE-EX", Name, description + " parallel move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
            IDictionary<PickerAxis, double> targets,
            string targetName,
            string description,
            CancellationToken ct,
            bool forceMove)
        {
            if (targets == null)
                return Task.FromResult(0);

            double pickerXTarget;
            if (!targets.TryGetValue(PickerAxis.PickerX, out pickerXTarget))
                return Task.FromResult(0);

            return WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
                PickerAxis.PickerX,
                pickerXTarget,
                targetName,
                description,
                ct,
                forceMove);
        }

        private async Task<int> WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
            PickerAxis axis,
            double target,
            string targetName,
            string description,
            CancellationToken ct,
            bool forceMove)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (axis != PickerAxis.PickerX)
                    return 0;

                if (!forceMove && CanSkipPickerMoveCommand(axis, target))
                    return 0;

                BaseAxis pickerX = GetPickerAxis(axis);
                if (pickerX == null)
                    return Fail("PICKER-SHARED-RAIL-X-AXIS-MISSING", Name,
                        description + " PickerX SharedRailX 거리 확인 실패. PickerX 축을 찾을 수 없습니다.");

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(Context != null ? Context.Machine : null);
                if (service == null || !service.IsSharedRailAxis(pickerX))
                    return 0;

                // 현재 기준: PickerX 이동은 명령 전 SharedRailX 거리 검사를 1순위로 확인한다.
                int timeoutMs = ResolveMoveTimeout();
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;
                string reason = string.Empty;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!safetyRetreatMoveActive && Context != null)
                        Context.StopIfCycleStopRequested(
                            Name + ".PickerXSharedRailDistanceWait",
                            ShouldDeferCycleStopForPickerDrain(),
                            "Picker target die drain");

                    if (IsAlarmStopActive())
                        return StopPickerMoveBecauseAlarmActive(description);

                    if (service.VerifySingleAxisMove(pickerX, target, out reason))
                        break;

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("PICKER-SHARED-RAIL-X-DISTANCE-TIMEOUT", Name,
                            description + " PickerX 이동 전 SharedRailX 거리 대기 시간 초과. " +
                            "side=" + Side +
                            ", target=" + target.ToString("F6") +
                            ", targetName=" + (targetName ?? "-") +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", reason=" + reason +
                            ", " + BuildPickerAxisState(axis, target));
                    }

                    if (!waitLogged)
                    {
                        WriteLog("PickerSharedRailXGate",
                            Name + " Auto PickerX 이동 전 SharedRailX 거리 대기. " +
                            "side=" + Side +
                            ", target=" + target.ToString("F6") +
                            ", targetName=" + (targetName ?? "-") +
                            ", description=" + description +
                            ", reason=" + reason + " - Wait");
                        WriteSharedRailXLog(
                            Name + " PickerSharedRailXGate wait. side=" + Side +
                            ", target=" + target.ToString("F6") +
                            ", targetName=" + (targetName ?? "-") +
                            ", reason=" + reason +
                            ", pickerState=" + BuildPickerAxisState(axis, target));
                        waitLogged = true;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    WriteLog("PickerSharedRailXGate",
                        Name + " Auto PickerX 이동 전 SharedRailX 거리 대기 완료. " +
                        "side=" + Side +
                        ", target=" + target.ToString("F6") +
                        ", targetName=" + (targetName ?? "-") +
                        ", elapsedMs=" + elapsedMs.ToString("0") + " - Ok");
                    WriteSharedRailXLog(
                        Name + " PickerSharedRailXGate wait complete. side=" + Side +
                        ", target=" + target.ToString("F6") +
                        ", targetName=" + (targetName ?? "-") +
                        ", elapsedMs=" + elapsedMs.ToString("0"));
                }

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
                return Fail("PICKER-SHARED-RAIL-X-GATE-EX", Name,
                    description + " PickerX SharedRailX 거리 대기 중 예외가 발생했습니다. " +
                    "side=" + Side +
                    ", target=" + target.ToString("F6") +
                    ", targetName=" + (targetName ?? "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerFacingYInterlockBeforeAutoMoveAsync(
            PickerAxis axis,
            double target,
            string targetName,
            string description,
            CancellationToken ct,
            double? pairedXTarget = null,
            double? pairedYTarget = null)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (axis != PickerAxis.PickerX && axis != PickerAxis.PickerY)
                    return 0;

                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!safetyRetreatMoveActive && Context != null)
                        Context.StopIfCycleStopRequested(
                            Name + ".WaitPickerFacingYInterlock:" + axis,
                            ShouldDeferCycleStopForPickerDrain(),
                            "Picker target die drain");

                    string detail;
                    bool clear = PickerZoneInterlockRules.CanMovePickerAxisByFacingYInterlock(
                        Context != null ? Context.Machine : null,
                        Side == PickerSequenceSide.Front,
                        axis,
                        target,
                        targetName,
                        pairedXTarget,
                        pairedYTarget,
                        out detail);
                    if (clear)
                        break;

                    if (!waitLogged)
                    {
                        WriteLog("PickerFacingYGate",
                            Name + " Auto Picker 이동 대기. Front/Rear PickerX가 마주보는 구간에서 양쪽 PickerY 돌출 간섭이 해제될 때까지 기다립니다. " +
                            "side=" + Side +
                            ", axis=" + axis +
                            ", target=" + target.ToString("0.###") +
                            ", targetName=" + (targetName ?? "-") +
                            ", description=" + description +
                            ", detail=" + detail + " - Wait");
                        WriteSharedRailXLog(
                            Name + " PickerFacingYGate wait. side=" + Side +
                            ", axis=" + axis +
                            ", target=" + target.ToString("0.###") +
                            ", targetName=" + (targetName ?? "-") +
                            ", detail=" + detail);
                        waitLogged = true;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    WriteLog("PickerFacingYGate",
                        Name + " Auto Picker 이동 대기 완료. Front/Rear PickerY 돌출 X거리 인터락 해제 확인. " +
                        "side=" + Side +
                        ", axis=" + axis +
                        ", targetName=" + (targetName ?? "-") +
                        ", description=" + description + " - Ok");
                    WriteSharedRailXLog(
                        Name + " PickerFacingYGate wait complete. side=" + Side +
                        ", axis=" + axis +
                        ", targetName=" + (targetName ?? "-"));
                }

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
                return Fail("PICKER-FACING-Y-GATE-EX", Name,
                    "Auto Picker 이동 전 Front/Rear PickerY 돌출 X거리 대기 중 예외가 발생했습니다. " +
                    "side=" + Side +
                    ", axis=" + axis +
                    ", targetName=" + (targetName ?? "-") +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MovePickerXTThenYAndVerifyAsync(
            IDictionary<PickerAxis, double> targets,
            string description,
            CancellationToken ct,
            string targetName = null,
            bool forceMove = false,
            bool useSafeMoveMotion = false)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            double pickerYTarget;
            bool hasPickerY = targets.TryGetValue(PickerAxis.PickerY, out pickerYTarget);

            var xtTargets = new Dictionary<PickerAxis, double>();
            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                if (pair.Key == PickerAxis.PickerY)
                    continue;

                xtTargets[pair.Key] = pair.Value;
            }

            int result = await MovePickerAxesAndVerifyAsync(
                xtTargets,
                description + " X/T",
                ct,
                targetName,
                forceMove,
                useSafeMoveMotion).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!hasPickerY || (!forceMove && CanSkipPickerMoveCommand(PickerAxis.PickerY, pickerYTarget)))
                return 0;

            WriteLog("PickerMove",
                Name + " " + description + " X/T 이동 완료 후 PickerY 전진을 시작합니다. " +
                "targetY=" + pickerYTarget +
                ", targetName=" + (targetName ?? "-") +
                " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                pickerYTarget,
                description + " Y",
                ct,
                targetName,
                forceMove,
                useSafeMoveMotion).ConfigureAwait(false);
        }

        private async Task<int> WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
            IDictionary<PickerAxis, double> targets,
            string targetName,
            string description,
            CancellationToken ct,
            bool forceMove = false)
        {
            if (targets == null || !targets.ContainsKey(PickerAxis.PickerY))
                return 0;

            double target = targets[PickerAxis.PickerY];
            if (!forceMove && CanSkipPickerMoveCommand(PickerAxis.PickerY, target))
                return 0;

            double pairedXTarget;
            return await WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
                PickerAxis.PickerY,
                target,
                targets.TryGetValue(PickerAxis.PickerX, out pairedXTarget) ? (double?)pairedXTarget : null,
                targetName,
                description,
                ct).ConfigureAwait(false);
        }

        private async Task<int> WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
            PickerAxis axis,
            double target,
            double? pairedXTarget,
            string targetName,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (axis != PickerAxis.PickerY)
                    return 0;

                if (!IsForwardPickerYMoveTarget(targetName))
                    return 0;

                bool waitLogged = false;
                PickerWorkZone targetZone = ResolvePickerYForwardTargetZone(targetName);
                string gateDetail;
                while (!IsOppositePickerYReadyForForwardMove(targetZone, target, pairedXTarget, targetName, out gateDetail))
                {
                    ct.ThrowIfCancellationRequested();
                    if (!safetyRetreatMoveActive && Context != null)
                        Context.StopIfCycleStopRequested(
                            Name + ".WaitOppositePickerYAvoid",
                            ShouldDeferCycleStopForPickerDrain(),
                            "Picker target die drain");

                    if (!waitLogged)
                    {
                        WriteLog("PickerYMoveGate",
                            Name + " Auto Y축 전진 이동 대기. 상대 PickerY가 Avoid 위치이고 이동 타깃이 정리될 때까지 기다립니다. " +
                            "side=" + Side +
                            ", targetName=" + (targetName ?? "-") +
                            ", targetZone=" + targetZone +
                            ", targetY=" + target.ToString("0.###") +
                            ", pairedXTarget=" + (pairedXTarget.HasValue ? pairedXTarget.Value.ToString("0.###") : "-") +
                            ", description=" + description +
                            ", gate=" + gateDetail +
                            ", opposite=" + BuildOppositePickerYState() + " - Wait");
                        WriteSharedRailXLog(
                            Name + " PickerYMoveGate wait. side=" + Side +
                            ", targetName=" + (targetName ?? "-") +
                            ", targetZone=" + targetZone +
                            ", targetY=" + target.ToString("0.###") +
                            ", pairedXTarget=" + (pairedXTarget.HasValue ? pairedXTarget.Value.ToString("0.###") : "-") +
                            ", gate=" + gateDetail +
                            ", description=" + description +
                            ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                        waitLogged = true;
                    }

                    //await Task.Delay(50, ct).ConfigureAwait(false);
                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    WriteLog("PickerYMoveGate",
                        Name + " Auto Y축 전진 이동 대기 완료. 상대 PickerY Avoid 및 이동 타깃 해제 확인. " +
                        "side=" + Side +
                        ", targetName=" + (targetName ?? "-") +
                        ", targetZone=" + targetZone +
                        ", description=" + description + " - Ok");
                    WriteSharedRailXLog(
                        Name + " PickerYMoveGate wait complete. side=" + Side +
                        ", targetName=" + (targetName ?? "-") +
                        ", targetZone=" + targetZone +
                        ", description=" + description +
                        ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                }

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
                return Fail("PICKER-Y-MOVE-GATE-EX", Name,
                    "Auto Y축 전진 이동 전 상대 PickerY Avoid 대기 중 예외가 발생했습니다. " +
                    "side=" + Side +
                    ", targetName=" + (targetName ?? "-") +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsForwardPickerYMoveTarget(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return true;

            if (targetName.IndexOf("AvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetName.IndexOf("InputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetName.IndexOf("OutputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetName.IndexOf("PickerPhase=SafeY", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return true;
        }

        private bool IsOppositePickerYReadyForForwardMove(
            PickerWorkZone targetZone,
            double targetY,
            double? pairedXTarget,
            string targetName,
            out string detail)
        {
            detail = string.Empty;

            try
            {
                bool oppositeIsFront = Side == PickerSequenceSide.Rear;

                string facingDetail;
                bool facingClear = PickerZoneInterlockRules.CanMovePickerAxisByFacingYInterlock(
                    Context != null ? Context.Machine : null,
                    Side == PickerSequenceSide.Front,
                    PickerAxis.PickerY,
                    targetY,
                    targetName,
                    pairedXTarget,
                    null,
                    out facingDetail);
                if (!facingClear)
                {
                    detail = "FacingYDistanceBlocked: " + facingDetail;
                    return false;
                }

                if (PickerZoneInterlockRules.IsProcessZone(targetZone) &&
                    IsOppositePickerInPlacePhase() &&
                    !IsOppositePickerYAtAvoidPosition())
                {
                    detail = "OppositePlacePhaseYOut";
                    return false;
                }

                PickerWorkZone workAreaZone;
                string workAreaOwner;
                bool oppositeWorkActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    oppositeIsFront,
                    out workAreaZone,
                    out workAreaOwner);
                if (PickerZoneInterlockRules.IsProcessZone(targetZone) &&
                    oppositeWorkActive &&
                    workAreaZone == PickerWorkZone.Output &&
                    !IsOppositePickerYAtAvoidPosition())
                {
                    detail = "OppositeOutputWorkAreaYOut owner=" + workAreaOwner;
                    return false;
                }

                PickerWorkZone activeTargetZone = PickerZoneInterlockRules.GetPickerYActiveTargetZone(oppositeIsFront);
                if (activeTargetZone != PickerWorkZone.Unknown)
                {
                    bool canShare = PickerZoneInterlockRules.CanShareForwardY(targetZone, activeTargetZone);
                    if (!canShare)
                        detail = "OppositeActiveYTargetZone=" + activeTargetZone;
                    return canShare;
                }

                if (IsOppositePickerYAtAvoidPosition())
                    return true;

                PickerWorkZone oppositeZone = PickerZoneInterlockRules.GetPickerCurrentXZone(
                    Context != null ? Context.Machine : null,
                    oppositeIsFront);
                bool shareByZone = PickerZoneInterlockRules.CanShareForwardY(targetZone, oppositeZone);
                if (!shareByZone)
                    detail = "OppositeCurrentZone=" + oppositeZone;
                return shareByZone;
            }
            catch
            {
                detail = "OppositePickerYReadyCheckException";
                return false;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerInPlacePhase()
        {
            try
            {
                if (Context == null || Context.PickerPhases == null)
                    return false;

                PickerPhaseSnapshot snapshot = Context.PickerPhases.GetSnapshot();
                PickerPhaseState opposite = Side == PickerSequenceSide.Front ? snapshot.Rear : snapshot.Front;
                return opposite.Phase == PickerProcessPhase.Place;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static PickerWorkZone ResolvePickerYForwardTargetZone(string targetName)
        {
            string name = (targetName ?? string.Empty).Replace(" ", string.Empty);
            if (name.Length == 0)
                return PickerWorkZone.Unknown;

            if (name.IndexOf("PickerZone=Input", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DiePick", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("PickPosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Input;
            if (name.IndexOf("PickerZone=Bottom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DieBottom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("BottomPosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Bottom;
            if (name.IndexOf("PickerZone=Side", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DieSide", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("SidePosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Side;
            if (name.IndexOf("PickerZone=Output", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DiePlace", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("PlacePosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Output;
            if (name.IndexOf("PickerZone=Avoid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("AvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("InputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("OutputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("SafeRetreat", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private bool IsOppositePickerYAtAvoidPosition()
        {
            try
            {
                BaseAxis oppositeY = ResolveOppositePickerYAxis();
                if (oppositeY == null)
                    return true;

                // 현재 기준: 상대 PickerY 안전 판단은 Home(0) 또는 실제 AvoidPosition만 인정한다.
                if (IsAxisAtHomePosition(oppositeY))
                    return true;

                if (Side == PickerSequenceSide.Front)
                {
                    if (RearPicker == null)
                        return true;

                    return RearPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                }

                if (FrontPicker == null)
                    return true;

                return FrontPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");

                // 기존 조건: Front/Rear 모두 InputAvoidPosition/OutputAvoidPosition도 PickerY 안전 위치로 보았다.
                //return OppositePicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition") ||
                //       OppositePicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "InputAvoidPosition") ||
                //       OppositePicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "OutputAvoidPosition");
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsAxisAtHomePosition(BaseAxis axis)
        {
            if (axis == null)
                return true;

            return Math.Abs(axis.ActualPosition) <= ResolveAxisInPositionTolerance(axis);
        }

        private static double ResolveAxisInPositionTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.05;
        }

        private string BuildOppositePickerYState()
        {
            try
            {
                BaseAxis axis = ResolveOppositePickerYAxis();

                if (axis == null)
                    return "PickerY=null";

                return "name=" + axis.Name +
                       ", actual=" + axis.ActualPosition +
                       ", moving=" + (axis.IsMoving ? "Y" : "N") +
                       ", activeTargetZone=" + PickerZoneInterlockRules.GetPickerYActiveTargetZone(Side == PickerSequenceSide.Rear) +
                       ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                       ", alarm=" + (axis.IsAlarm ? "ON" : "OFF");
            }
            catch (Exception ex)
            {
                return "stateError=" + ex.Message;
            }
            finally
            {
            }
        }

        private BaseAxis ResolveOppositePickerYAxis()
        {
            return ResolveOppositePickerAxis(PickerAxis.PickerY);
        }

        private BaseAxis ResolveOppositePickerAxis(PickerAxis pickerAxis)
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    BaseAxis axis;
                    if (RearPicker != null &&
                        RearPicker.Axes != null &&
                        RearPicker.Axes.TryGetValue(pickerAxis, out axis))
                        return axis;

                    return null;
                }

                BaseAxis frontAxis;
                if (FrontPicker != null &&
                    FrontPicker.Axes != null &&
                    FrontPicker.Axes.TryGetValue(pickerAxis, out frontAxis))
                    return frontAxis;

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private string BuildOppositePickerSharedRailXState(string movingAxes)
        {
            try
            {
                bool oppositeIsFront = Side == PickerSequenceSide.Rear;
                string oppositeName = oppositeIsFront ? "FrontPicker" : "RearPicker";
                BaseAxis x = ResolveOppositePickerAxis(PickerAxis.PickerX);
                BaseAxis y = ResolveOppositePickerAxis(PickerAxis.PickerY);
                PickerWorkZone currentXZone = PickerZoneInterlockRules.GetPickerCurrentXZone(
                    Context != null ? Context.Machine : null,
                    oppositeIsFront);
                PickerWorkZone activeYTargetZone = PickerZoneInterlockRules.GetPickerYActiveTargetZone(oppositeIsFront);
                PickerWorkZone workAreaZone;
                string workAreaOwner;
                bool workAreaActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    oppositeIsFront,
                    out workAreaZone,
                    out workAreaOwner);

                return "opposite=" + oppositeName +
                       ", currentXZone=" + currentXZone +
                       ", activeYTargetZone=" + activeYTargetZone +
                       ", yAtAvoid=" + (IsOppositePickerYAtAvoidPosition() ? "Y" : "N") +
                       ", workArea=" + (workAreaActive ? workAreaZone.ToString() : "None") +
                       ", owner=" + (workAreaActive ? workAreaOwner : "-") +
                       ", movingAxes=" + (string.IsNullOrWhiteSpace(movingAxes) ? "-" : movingAxes) +
                       ", x=" + FormatSharedRailAxis(x) +
                       ", y=" + FormatSharedRailAxis(y);
            }
            catch (Exception ex)
            {
                return "stateError=" + ex.Message;
            }
            finally
            {
            }
        }

        private static string FormatSharedRailAxis(BaseAxis axis)
        {
            if (axis == null)
                return "<null>";

            return axis.Name +
                   "(actual=" + axis.ActualPosition.ToString("0.###") +
                   ", moving=" + (axis.IsMoving ? "Y" : "N") +
                   ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") + ")";
        }

        private static void WriteSharedRailXLog(string message)
        {
            try
            {
                Log.Write("SharedRailX", message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        protected async Task<int> MovePickerToDiePositionAndVerifyAsync(string positionArrayName, int pickerNo, string description, CancellationToken ct)
        {
            int index = pickerNo - 1;
            string targetName = BuildPickerTargetName(positionArrayName, index);

            int result = await MoveAllPickerZToAvoidAndVerifyAsync(description + " pre Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            // Carry-zone moves use the central resolver so auto/manual/calibration targets are calculated consistently.
            PickerCalibratedZoneTarget zoneTarget = ResolvePickerZoneTarget(positionArrayName, index);
            var xyTargets = new Dictionary<PickerAxis, double>();
            xyTargets[PickerAxis.PickerX] = zoneTarget.X;
            xyTargets[PickerAxis.PickerY] = zoneTarget.Y;
            xyTargets[zoneTarget.PickerTAxis] = zoneTarget.T;

            WriteLog("PickerZoneTarget",
                Name + " picker zone target calculated. description=" + (description ?? string.Empty) +
                ", targetName=" + targetName +
                ", pickerNo=" + pickerNo +
                ", policy=CarryRuntimeAndCollet" +
                ", formula=" + zoneTarget.Formula +
                " - Calc");

            result = await MovePickerAxesAndVerifyAsync(xyTargets, description + " XYT", ct, targetName).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                zoneTarget.PickerZAxis,
                zoneTarget.Z,
                description + " Z",
                ct,
                targetName).ConfigureAwait(false);
            if (result != 0)
                return result;

            var allTargets = new Dictionary<PickerAxis, double>(xyTargets);
            allTargets[zoneTarget.PickerZAxis] = zoneTarget.Z;
            WriteLog("PickerZoneMoveComplete",
                Name + " picker zone move complete. description=" + (description ?? string.Empty) +
                ", targetName=" + targetName +
                ", pickerNo=" + pickerNo +
                ", formula=" + zoneTarget.Formula +
                ", axisStates=" + BuildPickerAxesState(allTargets) +
                " - Ok");
            return 0;
        }

        protected Task<int> MoveAllPickerZToAvoidAndVerifyAsync(string description, CancellationToken ct, bool forceMove = false)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerZ0] = GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition");
            targets[PickerAxis.PickerZ1] = GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition");
            targets[PickerAxis.PickerZ2] = GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition");
            targets[PickerAxis.PickerZ3] = GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition");
            // Avoid 이동은 안전이동 → 캘리브레이션 컨텍스트에서 SafeMovePercent(축 Default×%)를 사용한다(공정 경로는 CalibrationMotion=null이라 무영향).
            return MovePickerAxesAndVerifyAsync(targets, description, ct, "AvoidPosition", forceMove, true);
        }

        protected Task<int> MoveAllPickerTToAvoidAndVerifyAsync(string description, CancellationToken ct, bool forceMove = false)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
            targets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
            targets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
            targets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");
            // Avoid 이동은 안전이동 → 캘리브레이션 컨텍스트에서 SafeMovePercent(축 Default×%)를 사용한다.
            return MovePickerAxesAndVerifyAsync(targets, description, ct, "AvoidPosition;PickerPhase=SafeT", forceMove, true);
        }

        protected Task<int> MovePickerGroupAndVerifyAsync(string positionName, string description, CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, positionName);
            targets[PickerAxis.PickerY] = GetPickerTeachingPosition(PickerAxis.PickerY, positionName);
            targets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, positionName);
            targets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, positionName);
            targets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, positionName);
            targets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, positionName);
            targets[PickerAxis.PickerZ0] = GetPickerTeachingPosition(PickerAxis.PickerZ0, positionName);
            targets[PickerAxis.PickerZ1] = GetPickerTeachingPosition(PickerAxis.PickerZ1, positionName);
            targets[PickerAxis.PickerZ2] = GetPickerTeachingPosition(PickerAxis.PickerZ2, positionName);
            targets[PickerAxis.PickerZ3] = GetPickerTeachingPosition(PickerAxis.PickerZ3, positionName);
            return MovePickerAxesAndVerifyAsync(targets, description, ct, positionName);
        }

        protected async Task<int> MoveCurrentPickerToAvoidAndVerifyAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    description + " Z축 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    description + " Y축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY",
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition"),
                    description + " X축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeX",
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                var tTargets = new Dictionary<PickerAxis, double>();
                tTargets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
                tTargets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
                tTargets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
                tTargets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");

                result = await MovePickerAxesAndVerifyAsync(
                    tTargets,
                    description + " T축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeT").ConfigureAwait(false);
                if (result != 0)
                    return result;

                PickerAxis[] finalAxes =
                {
                    PickerAxis.PickerX,
                    PickerAxis.PickerY,
                    PickerAxis.PickerT0,
                    PickerAxis.PickerT1,
                    PickerAxis.PickerT2,
                    PickerAxis.PickerT3,
                    PickerAxis.PickerZ0,
                    PickerAxis.PickerZ1,
                    PickerAxis.PickerZ2,
                    PickerAxis.PickerZ3
                };

                foreach (PickerAxis axis in finalAxes)
                {
                    double target = GetPickerTeachingPosition(axis, "AvoidPosition");
                    if (!IsPickerAxisInPosition(axis, target))
                    {
                        return Fail("PICKER-AVOID-FINAL-POS", Name,
                            description + " 최종 Avoid 위치 확인 실패. " +
                            BuildPickerAxisState(axis, target));
                    }
                }

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-AVOID-EX", Name,
                    description + " Avoid 이동 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        // INV-6: 인터락/리스 해제·정지·Abort 전에 자기 픽커를 물리적으로 안전(Z=Avoid → Y=Avoid)하게 후퇴시키고 검증한다.
        // 순서(Z 상승 후 Y 후퇴)가 안전의 핵심이다. 공용 레일 X는 여기서 움직이지 않는다(마주보기 위험 회피).
        protected async Task<int> EnsureSelfSafeAsync(string reason, CancellationToken ct)
        {
            string label = string.IsNullOrWhiteSpace(reason) ? "EnsureSelfSafe" : reason;
            bool previousSafetyRetreatMoveActive = safetyRetreatMoveActive;
            try
            {
                safetyRetreatMoveActive = true;
                ct.ThrowIfCancellationRequested();

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    label + " Z축 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    label + " Y축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY",
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ct.ThrowIfCancellationRequested();
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
                return Fail("PICKER-ENSURE-SELF-SAFE-EX", Name,
                    label + " 자기 픽커 안전 후퇴 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                safetyRetreatMoveActive = previousSafetyRetreatMoveActive;
            }
        }

        // INV-7: 시작/재개 첫 이동 전, "자기(현재 side)" 픽커가 물리적으로 안전 배치(Avoid = Y·Z·X·T Avoid)인지 이동 없이 확인만 한다.
        // 자기 픽커만 확인하는 이유: Front/Rear 공정은 병렬로 시작되므로 "양쪽"을 확인하면 상대가 정상적으로 첫 전진을 시작한 순간
        // 오탐(false fail)이 난다. 각 픽커가 자기 CheckUnit(첫 이동 전)에서 자기 Avoid를 확인하면 전체적으로 양쪽 Avoid가 보장된다.
        // Start 흐름은 항상 Ready 시퀀스로 상부축을 Avoid로 정렬한 뒤 공정을 시작하므로 정상 시작에서는 통과한다.
        protected bool VerifySafeStartConfig(out string detail)
        {
            detail = string.Empty;
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    bool frontEnabled = FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.UseUnit;
                    if (frontEnabled && !FrontPicker.IsFrontPickerInAvoidPosition())
                    {
                        detail = "FrontPicker가 Avoid 위치가 아닙니다.";
                        return false;
                    }
                }
                else
                {
                    bool rearEnabled = RearPicker != null && RearPicker.Config != null && RearPicker.Config.UseUnit;
                    if (rearEnabled && !RearPicker.IsRearPickerInAvoidPosition())
                    {
                        detail = "RearPicker가 Avoid 위치가 아닙니다.";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                detail = "안전 시작 배치 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        protected async Task<int> MoveOppositePickerToAvoidAndVerifyAsync(
            string description,
            CancellationToken ct,
            bool allowOppositeOutputWorkZone = false)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options != null && Options.RunMode == SequenceRunMode.Auto)
                    return await WaitOppositePickerReadyForAutoAsync(
                        description,
                        ct,
                        allowOppositeOutputWorkZone).ConfigureAwait(false);

                bool fine = Options != null && Options.FineMove;

                if (Side == PickerSequenceSide.Front)
                {
                    if (RearPicker == null)
                    {
                        WriteLog("PickerOppositeAvoid",
                            Name + " opposite picker avoid skipped. RearPickerUnit is null. description=" + description + " - Check");
                        return 0;
                    }

                    // 이미 전체 Avoid면 재이동하지 않고 그대로 통과(Auto Cal 안전위치 이동과 중복 방지, 이미-안전 시 그 자리 대기).
                    if (RearPicker.IsRearPickerInAvoidPosition())
                    {
                        WriteLog("PickerOppositeAvoid",
                            Name + " opposite RearPicker already at Avoid. move skipped. description=" + description + " - Ok");
                        return 0;
                    }

                    int result = await MoveRearPickerToAvoidSequentialAsync(description, fine, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    if (!RearPicker.IsRearPickerInAvoidPosition())
                    {
                        return Fail("PICKER-OPPOSITE-AVOID-CHECK", "RearPickerUnit",
                            description + " 최종 위치 확인 실패. RearPicker가 Avoid 위치가 아닙니다.");
                    }

                    return 0;
                }

                if (FrontPicker == null)
                {
                    WriteLog("PickerOppositeAvoid",
                        Name + " opposite picker avoid skipped. FrontPickerUnit is null. description=" + description + " - Check");
                    return 0;
                }

                // 이미 전체 Avoid면 재이동하지 않고 그대로 통과(Auto Cal 안전위치 이동과 중복 방지, 이미-안전 시 그 자리 대기).
                if (FrontPicker.IsFrontPickerInAvoidPosition())
                {
                    WriteLog("PickerOppositeAvoid",
                        Name + " opposite FrontPicker already at Avoid. move skipped. description=" + description + " - Ok");
                    return 0;
                }

                int frontResult = await MoveFrontPickerToAvoidSequentialAsync(description, fine, ct).ConfigureAwait(false);
                if (frontResult != 0)
                    return frontResult;

                if (!FrontPicker.IsFrontPickerInAvoidPosition())
                {
                    return Fail("PICKER-OPPOSITE-AVOID-CHECK", "FrontPickerUnit",
                        description + " 최종 위치 확인 실패. FrontPicker가 Avoid 위치가 아닙니다.");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OPPOSITE-AVOID-EX", Name,
                    description + " 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontPickerToAvoidSequentialAsync(string description, bool fine, CancellationToken ct)
        {
            if (FrontPicker == null)
                return 0;

            ct.ThrowIfCancellationRequested();
            // 캘리브레이션 컨텍스트에서는 SafeMovePercent(각 축 Default × %) 안전이동을 사용한다.
            // 공정 경로(CalibrationMotion == null)는 기존 fine 경로 그대로.
            double safePercent = CalibrationMotion != null ? ResolveCalibrationSafeMovePercent() : 0.0;
            WriteLog("PickerOppositeAvoid",
                Name + " opposite FrontPicker avoid sequence start. order=Z all Avoid -> Y Avoid -> T all Avoid -> X Avoid. safeMovePercent=" +
                safePercent.ToString("F3") + ", description=" +
                description + " - Start");

            var zTargets = BuildFrontPickerAvoidTargets(true, false, false);
            int result = safePercent > 0.0
                ? await FrontPicker.MovePickerAxesSafeMove(
                    zTargets,
                    safePercent,
                    "AvoidPosition;PickerPhase=SafeZ;OppositeAvoid").ConfigureAwait(false)
                : await FrontPicker.MoveFrontPickerAxes(
                    zTargets,
                    fine,
                    "AvoidPosition;PickerPhase=SafeZ;OppositeAvoid").ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-Z", "FrontPickerUnit",
                    description + " 실패. FrontPicker Z Avoid 이동 실패. result=" + result);

            result = safePercent > 0.0
                ? await FrontPicker.MovePickerAxisToTeachingPositionSafeMove(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    safePercent,
                    0.0).ConfigureAwait(false)
                : await FrontPicker.MoveFrontPickerAxisToTeachingPosition(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    fine).ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-Y", "FrontPickerUnit",
                    description + " 실패. FrontPicker Y Avoid 이동 실패. result=" + result);

            if (!FrontPicker.IsFrontPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                return Fail("PICKER-OPPOSITE-AVOID-Y-CHECK", "FrontPickerUnit",
                    description + " 실패. FrontPicker Y가 Avoid 위치가 아닙니다.");

            var tTargets = BuildFrontPickerAvoidTargets(false, false, true);
            result = safePercent > 0.0
                ? await FrontPicker.MovePickerAxesSafeMove(
                    tTargets,
                    safePercent,
                    "AvoidPosition;PickerPhase=SafeT;OppositeAvoid").ConfigureAwait(false)
                : await FrontPicker.MoveFrontPickerAxes(
                    tTargets,
                    fine,
                    "AvoidPosition;PickerPhase=SafeT;OppositeAvoid").ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-T", "FrontPickerUnit",
                    description + " 실패. FrontPicker T Avoid 이동 실패. result=" + result);

            var xTargets = BuildFrontPickerAvoidTargets(false, true, false);
            result = safePercent > 0.0
                ? await FrontPicker.MovePickerAxesSafeMove(
                    xTargets,
                    safePercent,
                    "AvoidPosition;PickerPhase=SafeX;OppositeAvoid").ConfigureAwait(false)
                : await FrontPicker.MoveFrontPickerAxes(
                    xTargets,
                    fine,
                    "AvoidPosition;PickerPhase=SafeX;OppositeAvoid").ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-X", "FrontPickerUnit",
                    description + " 실패. FrontPicker X Avoid 이동 실패. result=" + result);

            if (!FrontPicker.IsFrontPickerInAvoidPosition())
                return Fail("PICKER-OPPOSITE-AVOID-FINAL", "FrontPickerUnit",
                    description + " 실패. FrontPicker 최종 전체 Avoid 위치 확인에 실패했습니다.");

            WriteLog("PickerOppositeAvoid",
                Name + " opposite FrontPicker avoid sequence complete. order=Z all Avoid -> Y Avoid -> T all Avoid -> X Avoid. description=" +
                description + " - Ok");
            return 0;
        }

        private async Task<int> MoveRearPickerToAvoidSequentialAsync(string description, bool fine, CancellationToken ct)
        {
            if (RearPicker == null)
                return 0;

            ct.ThrowIfCancellationRequested();
            // 캘리브레이션 컨텍스트에서는 SafeMovePercent(각 축 Default × %) 안전이동을 사용한다.
            // 공정 경로(CalibrationMotion == null)는 기존 fine 경로 그대로.
            double safePercent = CalibrationMotion != null ? ResolveCalibrationSafeMovePercent() : 0.0;
            WriteLog("PickerOppositeAvoid",
                Name + " opposite RearPicker avoid sequence start. order=Z all Avoid -> Y Avoid -> T all Avoid -> X Avoid. safeMovePercent=" +
                safePercent.ToString("F3") + ", description=" +
                description + " - Start");

            var zTargets = BuildRearPickerAvoidTargets(true, false, false);
            int result = safePercent > 0.0
                ? await RearPicker.MovePickerAxesSafeMove(
                    zTargets,
                    safePercent,
                    "AvoidPosition;PickerPhase=SafeZ;OppositeAvoid").ConfigureAwait(false)
                : await RearPicker.MoveRearPickerAxes(
                    zTargets,
                    fine,
                    "AvoidPosition;PickerPhase=SafeZ;OppositeAvoid").ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-Z", "RearPickerUnit",
                    description + " 실패. RearPicker Z Avoid 이동 실패. result=" + result);

            result = safePercent > 0.0
                ? await RearPicker.MovePickerAxisToTeachingPositionSafeMove(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    safePercent,
                    0.0).ConfigureAwait(false)
                : await RearPicker.MoveRearPickerAxisToTeachingPosition(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    fine).ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-Y", "RearPickerUnit",
                    description + " 실패. RearPicker Y Avoid 이동 실패. result=" + result);

            if (!RearPicker.IsRearPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                return Fail("PICKER-OPPOSITE-AVOID-Y-CHECK", "RearPickerUnit",
                    description + " 실패. RearPicker Y가 Avoid 위치가 아닙니다.");

            var tTargets = BuildRearPickerAvoidTargets(false, false, true);
            result = safePercent > 0.0
                ? await RearPicker.MovePickerAxesSafeMove(
                    tTargets,
                    safePercent,
                    "AvoidPosition;PickerPhase=SafeT;OppositeAvoid").ConfigureAwait(false)
                : await RearPicker.MoveRearPickerAxes(
                    tTargets,
                    fine,
                    "AvoidPosition;PickerPhase=SafeT;OppositeAvoid").ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-T", "RearPickerUnit",
                    description + " 실패. RearPicker T Avoid 이동 실패. result=" + result);

            var xTargets = BuildRearPickerAvoidTargets(false, true, false);
            result = safePercent > 0.0
                ? await RearPicker.MovePickerAxesSafeMove(
                    xTargets,
                    safePercent,
                    "AvoidPosition;PickerPhase=SafeX;OppositeAvoid").ConfigureAwait(false)
                : await RearPicker.MoveRearPickerAxes(
                    xTargets,
                    fine,
                    "AvoidPosition;PickerPhase=SafeX;OppositeAvoid").ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKER-OPPOSITE-AVOID-X", "RearPickerUnit",
                    description + " 실패. RearPicker X Avoid 이동 실패. result=" + result);

            if (!RearPicker.IsRearPickerInAvoidPosition())
                return Fail("PICKER-OPPOSITE-AVOID-FINAL", "RearPickerUnit",
                    description + " 실패. RearPicker 최종 전체 Avoid 위치 확인에 실패했습니다.");

            WriteLog("PickerOppositeAvoid",
                Name + " opposite RearPicker avoid sequence complete. order=Z all Avoid -> Y Avoid -> T all Avoid -> X Avoid. description=" +
                description + " - Ok");
            return 0;
        }

        private Dictionary<PickerAxis, double> BuildFrontPickerAvoidTargets(bool includeZ, bool includeX, bool includeT)
        {
            var targets = new Dictionary<PickerAxis, double>();
            if (FrontPicker == null)
                return targets;

            if (includeX)
                targets[PickerAxis.PickerX] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");
            if (includeT)
            {
                targets[PickerAxis.PickerT0] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
                targets[PickerAxis.PickerT1] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
                targets[PickerAxis.PickerT2] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
                targets[PickerAxis.PickerT3] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");
            }
            if (includeZ)
            {
                targets[PickerAxis.PickerZ0] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition");
                targets[PickerAxis.PickerZ1] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition");
                targets[PickerAxis.PickerZ2] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition");
                targets[PickerAxis.PickerZ3] = FrontPicker.GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition");
            }

            return targets;
        }

        private Dictionary<PickerAxis, double> BuildRearPickerAvoidTargets(bool includeZ, bool includeX, bool includeT)
        {
            var targets = new Dictionary<PickerAxis, double>();
            if (RearPicker == null)
                return targets;

            if (includeX)
                targets[PickerAxis.PickerX] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");
            if (includeT)
            {
                targets[PickerAxis.PickerT0] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
                targets[PickerAxis.PickerT1] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
                targets[PickerAxis.PickerT2] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
                targets[PickerAxis.PickerT3] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");
            }
            if (includeZ)
            {
                targets[PickerAxis.PickerZ0] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition");
                targets[PickerAxis.PickerZ1] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition");
                targets[PickerAxis.PickerZ2] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition");
                targets[PickerAxis.PickerZ3] = RearPicker.GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition");
            }

            return targets;
        }

        private async Task<int> WaitOppositePickerReadyForAutoAsync(
            string description,
            CancellationToken ct,
            bool allowOppositeOutputWorkZone)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string oppositeName = Side == PickerSequenceSide.Front ? "RearPicker" : "FrontPicker";
                bool loggedWait = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    PickerWorkZone oppositeZone;
                    string oppositeOwner;
                    bool oppositeWorkActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                        Side != PickerSequenceSide.Front,
                        out oppositeZone,
                        out oppositeOwner);
                    PickerWorkZone normalizedOppositeZone =
                        PickerZoneInterlockRules.NormalizeInterlockZone(oppositeZone);

                    // 반대 헤드가 Output Place 영역에 있으면 Bottom과 작업 존이 분리되므로 기존 모션 인터락에 맡기고 동시 진행한다.
                    if (allowOppositeOutputWorkZone &&
                        oppositeWorkActive &&
                        normalizedOppositeZone == PickerWorkZone.Output)
                    {
                        WriteLog("PickerOppositeWait",
                            Name + " auto continue. Opposite picker Output Place is allowed during Bottom inspection. " +
                            "description=" + description +
                            ", opposite=" + oppositeName +
                            ", zone=" + oppositeZone +
                            ", owner=" + oppositeOwner + " - Ok");
                        return 0;
                    }

                    string movingAxes;
                    if (IsOppositePickerMoving(out movingAxes))
                    {
                        if (!loggedWait)
                        {
                            WriteLog("PickerOppositeWait",
                                Name + " auto wait. Opposite picker is moving. description=" + description +
                                ", opposite=" + oppositeName +
                                ", movingAxes=" + movingAxes + " - Check");
                            WriteSharedRailXLog(
                                Name + " PickerOppositeWait wait. reason=OppositeMoving" +
                                ", description=" + description +
                                ", opposite=" + oppositeName +
                                ", oppositeState=" + BuildOppositePickerSharedRailXState(movingAxes));
                            loggedWait = true;
                        }

                        await Task.Delay(1, ct).ConfigureAwait(false);
                        continue;
                    }

                    if (oppositeWorkActive &&
                        PickerZoneInterlockRules.IsProcessZone(oppositeZone))
                    {
                        if (!loggedWait)
                        {
                            WriteLog("PickerOppositeWait",
                                Name + " auto wait. Opposite picker is using inspection area. description=" + description +
                                ", opposite=" + oppositeName +
                                ", zone=" + oppositeZone +
                                ", owner=" + oppositeOwner + " - Check");
                            WriteSharedRailXLog(
                                Name + " PickerOppositeWait wait. reason=OppositeInspectionArea" +
                                ", description=" + description +
                                ", opposite=" + oppositeName +
                                ", zone=" + oppositeZone +
                                ", owner=" + oppositeOwner +
                                ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                            loggedWait = true;
                        }

                        await Task.Delay(1, ct).ConfigureAwait(false);
                        continue;
                    }

                    if (loggedWait)
                    {
                        WriteLog("PickerOppositeWait",
                            Name + " auto wait complete. Opposite picker is ready. description=" + description +
                            ", opposite=" + oppositeName + " - Ok");
                        WriteSharedRailXLog(
                            Name + " PickerOppositeWait wait complete. description=" + description +
                            ", opposite=" + oppositeName +
                            ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                    }

                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OPPOSITE-WAIT-EX", Name,
                    description + " 자동 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsOppositePickerMoving(out string movingAxes)
        {
            movingAxes = string.Empty;

            try
            {
                IReadOnlyDictionary<PickerAxis, BaseAxis> axes = null;
                if (Side == PickerSequenceSide.Front)
                    axes = RearPicker != null ? RearPicker.Axes : null;
                else
                    axes = FrontPicker != null ? FrontPicker.Axes : null;

                if (axes == null)
                    return false;

                var moving = new List<string>();
                foreach (KeyValuePair<PickerAxis, BaseAxis> pair in axes)
                {
                    if (pair.Value != null && pair.Value.IsMoving)
                        moving.Add(pair.Value.Name);
                }

                movingAxes = moving.Count > 0 ? string.Join(",", moving.ToArray()) : string.Empty;
                return moving.Count > 0;
            }
            catch (Exception ex)
            {
                movingAxes = "opposite picker moving check failed: " + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        protected void EnsurePickerWorkAreaReserved(PickerWorkZone zone, string description)
        {
            try
            {
                if (zone == PickerWorkZone.Unknown || zone == PickerWorkZone.Avoid)
                    return;

                if (pickerWorkAreaScope != null && pickerWorkAreaZone == zone)
                    return;

                ReleasePickerWorkArea();

                pickerWorkAreaScope = PickerZoneInterlockRules.BeginPickerWorkAreaUse(
                    Side == PickerSequenceSide.Front,
                    zone,
                    Name + ":" + description);
                pickerWorkAreaZone = zone;

                WriteLog("PickerWorkArea",
                    Name + " reserved picker work area. side=" + Side +
                    ", zone=" + zone +
                    ", description=" + description + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerWorkArea",
                    Name + " picker work area reservation failed. side=" + Side +
                    ", zone=" + zone +
                    ", description=" + description +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        protected void ReleasePickerWorkArea()
        {
            try
            {
                if (pickerWorkAreaScope == null)
                    return;

                PickerWorkZone releasedZone = pickerWorkAreaZone;
                pickerWorkAreaScope.Dispose();
                pickerWorkAreaScope = null;
                pickerWorkAreaZone = PickerWorkZone.Unknown;

                WriteLog("PickerWorkArea",
                    Name + " released picker work area. side=" + Side +
                    ", zone=" + releasedZone + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerWorkArea",
                    Name + " picker work area release failed. side=" + Side +
                    ", zone=" + pickerWorkAreaZone +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        protected void SetPickerVacuum(int pickerNo, bool on)
        {
            if (Side == PickerSequenceSide.Front)
                FrontPicker.SetPickerVacuum(pickerNo, on);
            else
                RearPicker.SetPickerVacuum(pickerNo, on);
        }

        protected void SetPickerBlow(int pickerNo, bool on)
        {
            if (Side == PickerSequenceSide.Front)
                FrontPicker.SetPickerBlow(pickerNo, on);
            else
                RearPicker.SetPickerBlow(pickerNo, on);
        }

        protected async Task<int> PickerBlowAsync(int pickerNo, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int waitMs = 100;
                if (Side == PickerSequenceSide.Front && FrontPicker != null)
                    waitMs = FrontPicker.ResolvePickerBlowTimeMs(pickerNo);
                if (Side == PickerSequenceSide.Rear && RearPicker != null)
                    waitMs = RearPicker.ResolvePickerBlowTimeMs(pickerNo);

                SetPickerBlow(pickerNo, true);
                if (waitMs > 0)
                    await Task.Delay(waitMs, ct).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-BLOW", Name,
                    "Picker Blow 동작 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                try
                {
                    SetPickerBlow(pickerNo, false);
                }
                catch (Exception ex)
                {
                    WriteLog("PickerBlow",
                        Name + " Picker Blow OFF 정리 실패. side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", error=" + ex.Message + " - Failed");
                }
            }
        }

        protected async Task<int> VerifyPickerFlowStateAsync(int pickerNo, bool expected, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsPickerSimulationOrDryRun())
                {
                    WriteLog("PickerFlowCheck",
                        Name + " Flow Check 확인은 Simulation/DryRun 조건으로 통과합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", expected=" + (expected ? "ON" : "OFF") +
                        ", description=" + description + " - Bypass");
                    return 0;
                }

                int timeoutMs = ResolvePickerIoTimeoutMs(pickerNo);
                DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
                bool actual = ReadPickerFlowState(pickerNo);

                while (DateTime.Now <= deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    actual = ReadPickerFlowState(pickerNo);
                    if (actual == expected)
                        return 0;

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                actual = ReadPickerFlowState(pickerNo);
                return Fail("PICKER-FLOW-CHECK", Name,
                    "Flow Check 확인 실패. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", expected=" + (expected ? "ON" : "OFF") +
                    ", actual=" + (actual ? "ON" : "OFF") +
                    ", timeoutMs=" + timeoutMs +
                    ", description=" + description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-FLOW-CHECK-EX", Name,
                    "Flow Check 확인 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", expected=" + (expected ? "ON" : "OFF") +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        protected int ResolvePickerIoTimeoutMs(int pickerNo)
        {
            try
            {
                if (Side == PickerSequenceSide.Front && FrontPicker != null)
                    return FrontPicker.ResolvePickerIoTimeoutMs(pickerNo);

                if (Side == PickerSequenceSide.Rear && RearPicker != null)
                    return RearPicker.ResolvePickerIoTimeoutMs(pickerNo);
            }
            catch (Exception ex)
            {
                WriteLog("PickerFlowCheck",
                    Name + " Picker I/O timeout 조회 실패. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }

            return 5000;
        }

        protected bool ReadPickerFlowState(int pickerNo)
        {
            try
            {
                if (Side == PickerSequenceSide.Front && FrontPicker != null)
                    return FrontPicker.IsPickerFlowDetected(pickerNo, true);

                if (Side == PickerSequenceSide.Rear && RearPicker != null)
                    return RearPicker.IsPickerFlowDetected(pickerNo, true);
            }
            catch (Exception ex)
            {
                WriteLog("PickerFlowCheck",
                    Name + " Picker Flow 신호 읽기 실패. Unit IsPickerFlowDetected 호출 실패. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }

            return false;
        }

        protected Task<int> MovePickerAxisCommandAsync(PickerAxis axis, double target, string targetName = null, bool forceMove = false, bool useSafeMoveMotion = false)
        {
            if (CalibrationMotion != null)
            {
                CalibrationMotion.EnsureDefaults();

                // 기본: 측정 이동 = 각 캘의 Move Speed(CalibrationMotion). 안전이동(useSafeMoveMotion=true)이고
                // SafeMovePercent가 설정돼 있으면 해당 축 Config.Default × %를 명시 속도/가감속으로 사용한다.
                // (측정 속도와 완전 분리, 전역 MotionSpeedScale과 중첩되지 않음 — explicit 경로)
                double velocity = CalibrationMotion.MoveVelocity;
                double acceleration = CalibrationMotion.MoveAcceleration;
                double deceleration = CalibrationMotion.MoveDeceleration;
                bool safeMoveApplied = false;
                double safePercent = 0.0;
                if (useSafeMoveMotion)
                {
                    safePercent = ResolveCalibrationSafeMovePercent();
                    BaseAxis safeAxis = safePercent > 0.0 ? GetPickerAxis(axis) : null;
                    if (safeAxis != null && safeAxis.Config != null && safeAxis.Config.DefaultVelocity > 0.0)
                    {
                        double factor = safePercent / 100.0;
                        velocity = safeAxis.Config.DefaultVelocity * factor;
                        acceleration = safeAxis.Config.Acceleration * factor;
                        deceleration = safeAxis.Config.Deceleration * factor;
                        safeMoveApplied = true;
                    }
                }

                WriteLog("PickerMoveCommand",
                    Name + " calibration motion command. side=" + Side +
                    ", axis=" + axis +
                    ", target=" + target.ToString("F6") +
                    ", targetName=" + (targetName ?? "-") +
                    ", forceMove=" + forceMove +
                    ", velocity=" + velocity.ToString("F6") +
                    ", acceleration=" + acceleration.ToString("F6") +
                    ", deceleration=" + deceleration.ToString("F6") +
                    ", timeoutMs=" + CalibrationMotion.MoveTimeoutMs +
                    ", safeMove=" + useSafeMoveMotion +
                    ", safeMoveApplied=" + safeMoveApplied +
                    ", safeMovePercent=" + safePercent.ToString("F3") +
                    ", speedScalePercent=" + MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + MotionSpeedScale.EffectiveScaleFactor.ToString("F6") +
                    ", explicitVelocityNotDefaultScaled=True - Check");
                if (Side == PickerSequenceSide.Front)
                    return FrontPicker.MovePickerAxisCommandWithMotion(
                        axis,
                        target,
                        velocity,
                        acceleration,
                        deceleration,
                        targetName,
                        forceMove);

                return RearPicker.MovePickerAxisCommandWithMotion(
                    axis,
                    target,
                    velocity,
                    acceleration,
                    deceleration,
                    targetName,
                    forceMove);
            }

            bool fine = Options != null && Options.FineMove;
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxisCommand(axis, target, fine, targetName, forceMove);
            return RearPicker.MovePickerAxisCommand(axis, target, fine, targetName, forceMove);
        }

        // 캘리브레이션 안전이동(Avoid) 전용 퍼센트를 장비 설정에서 라이브로 읽는다. 계산은 각 축 Config.Default × (%/100).
        // 캘리브레이션 컨텍스트(CalibrationMotion != null)에서만 사용되며, 값이 없거나 오류면 0(=측정 속도 사용)으로 폴백한다.
        protected double ResolveCalibrationSafeMovePercent()
        {
            try
            {
                if (Context == null || Context.Machine == null || Context.Machine.VisionUnit == null ||
                    Context.Machine.VisionUnit.Config == null || Context.Machine.VisionUnit.Config.CalibrationData == null)
                    return 0.0;

                double percent = Context.Machine.VisionUnit.Config.CalibrationData.SafeMovePercent;
                if (double.IsNaN(percent) || percent < CalibrationData.MinSafeMovePercent)
                    return 0.0;
                if (percent > CalibrationData.MaxSafeMovePercent)
                    percent = CalibrationData.MaxSafeMovePercent;
                return percent;
            }
            catch
            {
                return 0.0;
            }
        }

        protected Task<int> MovePickerAxisCommandWithVelocityAsync(PickerAxis axis, double target, double velocity, string targetName = null)
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxisCommandWithVelocity(axis, target, velocity, targetName);
            return RearPicker.MovePickerAxisCommandWithVelocity(axis, target, velocity, targetName);
        }

        protected Task<int> MovePickerAxisCommandWithMotionAsync(PickerAxis axis, double target, double velocity, double acceleration, double deceleration, string targetName = null)
        {
            WriteLog("PickerMoveCommand",
                Name + " explicit motion command. side=" + Side +
                ", axis=" + axis +
                ", target=" + target.ToString("F6") +
                ", targetName=" + (targetName ?? "-") +
                ", velocity=" + velocity.ToString("F6") +
                ", acceleration=" + acceleration.ToString("F6") +
                ", deceleration=" + deceleration.ToString("F6") +
                ", speedScalePercent=" + MotionSpeedScale.ScalePercent.ToString("F3") +
                ", effectiveScaleFactor=" + MotionSpeedScale.EffectiveScaleFactor.ToString("F6") +
                ", explicitVelocityNotDefaultScaled=True - Check");
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration, targetName);
            return RearPicker.MovePickerAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration, targetName);
        }

        // To do: [명령 전용 절대이동] 명령 발행 즉시 리턴 - 이동 중 감시/저속 오버라이드가 필요한
        //        FastContiSegmentedPickUp 경로 전용. 파라미터는 스케일 완료된 최종값으로 전달할 것.
        protected Task<int> MovePickerAxisCommandOnlyAsync(PickerAxis axis, double target, double velocity, double acceleration, double deceleration, string targetName = null)
        {
            WriteLog("PickerMoveCommand",
                Name + " command-only motion. side=" + Side +
                ", axis=" + axis +
                ", target=" + target.ToString("F6") +
                ", targetName=" + (targetName ?? "-") +
                ", velocity=" + velocity.ToString("F6") +
                ", acceleration=" + acceleration.ToString("F6") +
                ", deceleration=" + deceleration.ToString("F6") + " - Check");
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxisCommandOnly(axis, target, velocity, acceleration, deceleration, targetName);
            return RearPicker.MovePickerAxisCommandOnly(axis, target, velocity, acceleration, deceleration, targetName);
        }

        // 기존 조건: AxisMoveWaitResult 반환 — 현재 기준: int(0=완료, 음수=실패) 반환(R3).
        protected async Task<int> WaitPickerAxisMoveDoneAsync(PickerAxis axis, double target, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Side == PickerSequenceSide.Front)
                    return await FrontPicker.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs, ct).ConfigureAwait(false);

                return await RearPicker.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("PickerMove",
                    Name + " picker axis wait exception. axis=" + axis +
                    ", target=" + target +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        protected string BuildPickerAxisLastMotionFailure(PickerAxis axis)
        {
            BaseAxis item = GetPickerAxis(axis);
            if (item == null || string.IsNullOrWhiteSpace(item.LastMotionFailureMessage))
                return string.Empty;
            return item.LastMotionFailureMessage;
        }

        protected async Task DelayBeforeVisionInspectionAsync(CancellationToken ct)
        {
            if (ShouldSkipVisionInspectionDelay())
                return;

            int delayMs = ResolveVisionInspectionSettleMs();
            if (delayMs > 0)
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        protected async Task DelayBeforeBottomVisionInspectionAsync(int pickerNo, CancellationToken ct)
        {
            if (ShouldSkipVisionInspectionDelay())
                return;

            int delayMs = ResolveBottomVisionPreGrabDelayMs();
            if (delayMs <= 0)
                return;

            WriteLog("PickerBottomVisionDelay",
                Name + " Bottom Vision 검사 전 대기를 시작합니다. side=" + Side +
                ", pickerNo=" + pickerNo +
                ", delayMs=" + delayMs +
                ", source=VisionRecipe.BottomVisionPreGrabDelayMs - Start");
            await Task.Delay(delayMs, ct).ConfigureAwait(false);
            WriteLog("PickerBottomVisionDelay",
                Name + " Bottom Vision 검사 전 대기를 완료했습니다. 다음 명령=INSPECTASYNC/Grab. side=" + Side +
                ", pickerNo=" + pickerNo +
                ", delayMs=" + delayMs + " - Ok");
        }

        protected async Task DelaySideInspectionTurnSettleAsync(CancellationToken ct)
        {
            if (ShouldSkipVisionInspectionDelay())
                return;

            int delayMs = ResolveSideInspectionTurnSettleMs();
            if (delayMs > 0)
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        private bool ShouldSkipVisionInspectionDelay()
        {
            if (Options != null && Options.SimulateVisionResult)
                return true;

            AppSettings settings = AppSettingsStore.Current;
            if (settings != null && !settings.UseVision)
                return true;

            return false;
        }

        protected bool IsDryRunWithVisionDisabled()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.DryRunMode && !settings.UseVision;
        }

        private int ResolveVisionInspectionSettleMs()
        {
            int value = 0;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                value = FrontPicker.Config.VisionInspectionSettleMs;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                value = RearPicker.Config.VisionInspectionSettleMs;

            return value > 0 ? value : 0;
        }

        private int ResolveBottomVisionPreGrabDelayMs()
        {
            if (Context == null || Context.Machine == null || Context.Machine.VisionUnit == null ||
                Context.Machine.VisionUnit.Recipe == null)
                return 0;

            Context.Machine.VisionUnit.Recipe.EnsurePositionObjects();
            return Math.Max(0, Math.Min(60000, Context.Machine.VisionUnit.Recipe.BottomVisionPreGrabDelayMs));
        }

        private int ResolveSideInspectionTurnSettleMs()
        {
            int value = 0;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                value = FrontPicker.Config.SideInspectionTurnSettleMs;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                value = RearPicker.Config.SideInspectionTurnSettleMs;

            return value > 0 ? value : 0;
        }

        protected bool IsPickerAxisInPosition(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            double tolerance = item != null && item.Config != null ? item.Config.InPositionTolerance : 0.001;
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.IsPickerAxisInPosition(axis, target, tolerance);
            return RearPicker.IsPickerAxisInPosition(axis, target, tolerance);
        }

        protected bool IsPickerAxisAlreadyInPosition(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            if (item == null || item.IsMoving)
                return false;

            return IsPickerAxisInPosition(axis, target);
        }

        protected bool CanSkipPickerMoveCommand(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            double tolerance = item != null && item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.001;
            return item != null && item.IsAtTargetPosition(target, tolerance);
        }

        protected string BuildPickerAxisState(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            if (item == null)
                return "axis=" + ToPickerAxisDisplayName(axis) + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.001;

            return "axis=" + ToPickerAxisDisplayName(axis) +
                   ", name=" + AjinAxisDefaults.ToDisplayName(item.Name) +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance;
        }

        // Formats every commanded picker axis so move-complete logs include target, actual, tolerance, and drive state.
        protected string BuildPickerAxesState(IDictionary<PickerAxis, double> targets)
        {
            if (targets == null || targets.Count == 0)
                return "-";

            string result = string.Empty;
            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                if (!string.IsNullOrEmpty(result))
                    result += " | ";

                result += BuildPickerAxisState(pair.Key, pair.Value);
            }

            return result;
        }

        protected string BuildPickerMoveCommandFailureMessage(
            PickerAxis axis,
            double target,
            string description,
            int result)
        {
            string message =
                TranslatePickerMoveDescription(description) +
                " 이동 명령 실패. 결과=" + result +
                ". " + BuildPickerAxisStateKorean(axis, target);

            string failureReason = BuildPickerLastMotionFailureReason(axis, result);
            if (!string.IsNullOrWhiteSpace(failureReason))
                message += ". " + failureReason;

            return message;
        }

        private string BuildPickerAxisStateKorean(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            if (item == null)
                return "축=" + ToPickerAxisDisplayName(axis) + ", 목표위치=" + target + ", 상태=축을 찾을 수 없음";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.001;

            return "축=" + ToPickerAxisDisplayName(axis) +
                   ", 축이름=" + AjinAxisDefaults.ToDisplayName(item.Name) +
                   ", 서보=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", 알람=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", 이동중=" + (item.IsMoving ? "Y" : "N") +
                   ", 현재위치=" + item.ActualPosition +
                   ", 목표위치=" + target +
                   ", 허용오차=" + tolerance;
        }

        private static string ToPickerAxisDisplayName(PickerAxis axis)
        {
            string name = axis.ToString();
            if ((name.StartsWith("PickerT", StringComparison.Ordinal) ||
                 name.StartsWith("PickerZ", StringComparison.Ordinal)) &&
                name.Length == 8)
            {
                char c = name[7];
                if (c >= '0' && c <= '3')
                    return name.Substring(0, 7) + ((c - '0') + 1).ToString();
            }

            return name;
        }

        private string BuildPickerLastMotionFailureReason(PickerAxis axis, int result)
        {
            BaseAxis item = GetPickerAxis(axis);
            string lastFailure = item != null ? item.LastMotionFailureMessage : string.Empty;

            if (!string.IsNullOrWhiteSpace(lastFailure))
                return TranslateMotionFailureReason(lastFailure);

            if (result == -11)
            {
                if (axis == PickerAxis.PickerX)
                {
                    return "상세 원인=인터락 차단입니다. PickerX는 공유 X 레일 축이라 이동 목표 위치 또는 이동 경로에서 " +
                           "InputVisionX, OutputVisionX, FrontPickerX, RearPickerX 중 다른 축과 안전거리가 부족하면 이동이 차단됩니다. " +
                           "방해 축을 Avoid 위치로 이동하거나 Side/Bottom/Place 티칭 위치와 공유 레일 안전거리 설정을 확인하세요.";
                }

                return "상세 원인=인터락 차단입니다. 해당 축의 이동 조건, 상대 축 위치, 실린더 상태를 확인하세요.";
            }

            if (result == -2)
                return "상세 원인=축 이동 준비 조건이 맞지 않습니다. 서보 ON, 알람 OFF, 이동중 여부를 확인하세요.";

            return string.Empty;
        }

        private static string TranslateMotionFailureReason(string failure)
        {
            if (string.IsNullOrWhiteSpace(failure))
                return string.Empty;

            if (failure.IndexOf("SharedRailX pair clearance is too close", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "상세 원인=공유 X 레일 안전거리 인터락입니다. 이동 목표 위치에서 다른 X축과 안전거리가 부족하거나 경로가 겹칩니다. " +
                       "InputVisionX, OutputVisionX, FrontPickerX, RearPickerX 위치를 확인하세요. 원문=" + failure;
            }

            if (failure.IndexOf("SharedRailX real-time clearance guard stopped motion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "상세 원인=공유 X 레일 실시간 안전거리 인터락입니다. 이동 시작/목표 위치가 가능해 보여도 이동 중 다른 X축과 안전거리가 부족해 정지했습니다. " +
                       "동시에 움직이는 OutputVisionX/InputVisionX 또는 반대 PickerX 시퀀스 점유 상태를 확인하세요. 원문=" + failure;
            }

            if (failure.IndexOf("Interlock blocked", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=인터락 차단입니다. 원문=" + failure;

            if (failure.IndexOf("Servo is OFF", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=서보가 OFF 상태입니다. 원문=" + failure;

            if (failure.IndexOf("Axis alarm is ON", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=축 알람이 ON 상태입니다. 원문=" + failure;

            if (failure.IndexOf("Axis is already moving", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=축이 이미 이동 중입니다. 원문=" + failure;

            return "상세 원인=" + failure;
        }

        private static string TranslatePickerMoveDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "Picker 축";

            if (description.IndexOf("side inspection X", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 X축";
            if (description.IndexOf("side inspection Y", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 Y축";
            if (description.IndexOf("side inspection Z", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 Z축";
            if (description.IndexOf("side inspection T", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 T축";
            if (description.IndexOf("bottom inspection X", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 X축";
            if (description.IndexOf("bottom inspection Y", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 Y축";
            if (description.IndexOf("bottom inspection Z", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 Z축";
            if (description.IndexOf("bottom inspection T", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 T축";
            if (description.IndexOf("pick", StringComparison.OrdinalIgnoreCase) >= 0)
                return "픽업 " + description;
            if (description.IndexOf("place", StringComparison.OrdinalIgnoreCase) >= 0)
                return "플레이스 " + description;

            return description;
        }

        protected string BuildRequiredPickerAxesReason()
        {
            PickerAxis[] axes =
            {
                PickerAxis.PickerX,
                PickerAxis.PickerY,
                PickerAxis.PickerT0,
                PickerAxis.PickerT1,
                PickerAxis.PickerT2,
                PickerAxis.PickerT3,
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3
            };

            var failures = new List<string>();
            foreach (PickerAxis axis in axes)
            {
                BaseAxis item = GetPickerAxis(axis);
                if (item == null)
                {
                    failures.Add(axis + "=missing");
                    continue;
                }

                if (!item.IsServoOn || item.IsAlarm)
                    failures.Add(BuildPickerAxisState(axis, item.ActualPosition));
            }

            return failures.Count == 0 ? string.Empty : string.Join("; ", failures);
        }

        protected BaseAxis GetPickerAxis(PickerAxis axis)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Axes.ContainsKey(axis))
                return FrontPicker.Axes[axis];
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Axes.ContainsKey(axis))
                return RearPicker.Axes[axis];
            return null;
        }

        protected double GetPickerTeachingPosition(PickerAxis axis, string positionName)
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.GetPickerTeachingPosition(axis, positionName);
            return RearPicker.GetPickerTeachingPosition(axis, positionName);
        }

        // ── Collet AF Z Offset (공정 Pick/Place Z 전용) ─────────────────────────────
        // 부호 규칙: 픽커 Z 위=+ / 아래=-. offset이 +면 덜 내려오고 -면 더 내려온다(적용식 = 목표 Z + offset).
        // 인덱스 정합: pickerIndex(0-base) = colletNo-1 = Recipe.ColletAfZOffset 인덱스 = PickerZ{i} 축.
        // 캘리브레이션 측정/티칭 저장/검사(Bottom/Side)/Avoid Z 경로에는 절대 사용하지 말 것(이중 적용·오염).

        /// <summary>자기 side 픽커 레시피의 ColletAfZOffset 배열/limit/기준선 stale 플래그를 얻는다. 실패 시 false.</summary>
        private bool TryGetColletAfZOffsetRecipe(out double[] offsets, out double limitMm, out bool[] staleFlags)
        {
            offsets = null;
            staleFlags = null;
            limitMm = 0.3;
            if (Side == PickerSequenceSide.Front)
            {
                if (FrontPicker == null || FrontPicker.Recipe == null)
                    return false;
                FrontPicker.Recipe.EnsurePositionObjects();
                offsets = FrontPicker.Recipe.ColletAfZOffset;
                limitMm = FrontPicker.Recipe.ColletAfZOffsetLimitMm;
                staleFlags = FrontPicker.Recipe.ColletAfZBaselineStale;
            }
            else
            {
                if (RearPicker == null || RearPicker.Recipe == null)
                    return false;
                RearPicker.Recipe.EnsurePositionObjects();
                offsets = RearPicker.Recipe.ColletAfZOffset;
                limitMm = RearPicker.Recipe.ColletAfZOffsetLimitMm;
                staleFlags = RearPicker.Recipe.ColletAfZBaselineStale;
            }

            if (double.IsNaN(limitMm) || double.IsInfinity(limitMm) || limitMm <= 0.0)
                limitMm = 0.3;
            return offsets != null && staleFlags != null;
        }

        /// <summary>
        /// 공정(오토/수동 시퀀스) Pick/Place Z 목표 전용 Collet AF Z Offset 조회.
        /// |offset| > Recipe.ColletAfZOffsetLimitMm 이면 fail-closed: limitFailReason을 채우고 0을 반환한다 —
        /// 호출부는 반드시 limitFailReason != null 이면 Fail 알람으로 중단해야 한다(한계 초과 편차로 하강 금지).
        /// </summary>
        protected double ResolveColletAfZOffset(int pickerIndex, out string limitFailReason)
        {
            limitFailReason = null;
            if (pickerIndex < 0 || pickerIndex > 3)
                return 0.0;

            double[] offsets;
            double limitMm;
            bool[] staleFlags;
            if (!TryGetColletAfZOffsetRecipe(out offsets, out limitMm, out staleFlags) || offsets.Length <= pickerIndex)
                return 0.0;

            double offset = offsets[pickerIndex];
            if (double.IsNaN(offset) || double.IsInfinity(offset))
                return 0.0;

            if (Math.Abs(offset) > limitMm)
            {
                limitFailReason =
                    "Collet AF Z Offset이 안전 한계를 초과해 공정 Z 이동을 중단합니다. side=" + Side +
                    ", colletNo=" + (pickerIndex + 1) +
                    ", offsetMm=" + offset.ToString("F6") +
                    ", limitMm=" + limitMm.ToString("F6") +
                    ". Recipe→Picker의 Offset/limit 또는 콜렛 상태를 확인하세요.";
                return 0.0;
            }

            return offset;
        }

        /// <summary>
        /// Runtime AF의 BottomPosition 적용 실패 시 보상 롤백: 누적했던 offset과 인메모리 BottomPosition(FocusPosition)을
        /// 함께 AF 이전 값으로 복원해 "offset은 새값 + 기준선은 구값" 부정합(재시도 시 이중 누적)을 차단한다.
        /// </summary>
        private void RollbackRuntimeAfOffsetAndBaseline(int pickerIndex, double previousOffset, bool offsetCommitted, bool staleConsumed, double previousBottomZ)
        {
            try
            {
                double[] offsets;
                double limitMm;
                bool[] staleFlags;
                if (TryGetColletAfZOffsetRecipe(out offsets, out limitMm, out staleFlags))
                {
                    if (offsetCommitted && offsets.Length > pickerIndex)
                        offsets[pickerIndex] = previousOffset;
                    // stale 플래그 소비도 트랜잭션의 일부: 기준선을 낡은 값으로 되돌리므로 플래그도 복원해야
                    // 다음 AF가 Z캘 티칭에 이미 흡수된 드리프트를 재누적하지 않는다.
                    if (staleConsumed && staleFlags.Length > pickerIndex)
                        staleFlags[pickerIndex] = true;
                }

                if (Side == PickerSequenceSide.Front)
                    FrontPicker.SetRuntimePickerZPosition(pickerIndex, "FocusPosition", previousBottomZ);
                else
                    RearPicker.SetRuntimePickerZPosition(pickerIndex, "FocusPosition", previousBottomZ);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletAfZOffsetRuntime",
                    "Runtime Bottom AF 적용 실패 — offset/기준선 보상 롤백 수행. side=" + Side +
                    ", colletNo=" + (pickerIndex + 1) +
                    ", offsetRolledBackTo=" + previousOffset.ToString("F6") + " (committed=" + offsetCommitted + ")" +
                    ", staleRestored=" + staleConsumed +
                    ", bottomPositionRolledBackTo=" + previousBottomZ.ToString("F6"));
            }
            catch (Exception ex)
            {
                WriteLog("ColletAfZOffset",
                    Name + " Runtime AF 보상 롤백 중 예외. colletNo=" + (pickerIndex + 1) +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        /// <summary>
        /// PickUpZ/PlaceZ 캘리브레이션이 티칭을 새로 저장하면 현재 콜렛 접촉면이 티칭에 흡수되므로,
        /// 잔존 Collet AF Z Offset을 리셋한다(캘 저장 직후 호출). 단, offset은 Pick/Place 공용이므로
        /// 단순 0 리셋 시 재캘리브레이션되지 않은 반대편 보정이 소실된다 — 리셋 전에 잔존 offset을
        /// 반대편 티칭에 폴딩(가산)해 유효 목표 Z(티칭+offset)를 보존한다.
        /// 또한 Bottom 기준선(BottomPosition)은 아직 낡은 상태이므로 stale 플래그를 세워
        /// 다음 Bottom AF 1회가 누적 없이 기준선 재설정만 하도록 한다. 실패 시 알람(리셋 미수행 방치 금지).
        /// 반환 0=성공, 음수=폴딩 검증 실패(호출한 캘리브레이션을 Fail로 중단해야 함).
        /// </summary>
        protected int ResetColletAfZOffsetAfterZCalibration(int pickerIndex, string source)
        {
            try
            {
                if (pickerIndex < 0 || pickerIndex > 3)
                    return 0;

                double[] offsets;
                double limitMm;
                bool[] staleFlags;
                if (!TryGetColletAfZOffsetRecipe(out offsets, out limitMm, out staleFlags) || offsets.Length <= pickerIndex)
                    return 0;

                double previous = offsets[pickerIndex];
                if (double.IsNaN(previous) || double.IsInfinity(previous))
                    previous = 0.0;

                // 폴딩 대상: 이번 캘이 재측정하지 "않은" 반대편 티칭.
                string foldPositionName = source == "PickUpZCalibration" ? "PlacePosition" : "PickPosition";
                string foldDetail = "none";
                if (previous != 0.0)
                {
                    PickerAxis zAxis = GetPickerZAxis(pickerIndex);
                    double foldBase = GetPickerTeachingPosition(zAxis, foldPositionName);
                    double foldTarget = foldBase + previous;
                    if (Side == PickerSequenceSide.Front)
                        FrontPicker.SetPickerAxisTeachingPosition(zAxis, foldPositionName, foldTarget);
                    else
                        RearPicker.SetPickerAxisTeachingPosition(zAxis, foldPositionName, foldTarget);

                    double readback = GetPickerTeachingPosition(zAxis, foldPositionName);
                    if (Math.Abs(readback - foldTarget) > 0.000001)
                    {
                        return Fail("COLLET-AF-ZOFFSET-FOLD-VERIFY", Name,
                            "Collet AF Z Offset 폴딩 readback 불일치 — 캘리브레이션 저장을 중단합니다. source=" + source +
                            ", side=" + Side +
                            ", colletNo=" + (pickerIndex + 1) +
                            ", position=" + foldPositionName +
                            ", expected=" + foldTarget.ToString("F6") +
                            ", readback=" + readback.ToString("F6"));
                    }

                    foldDetail = foldPositionName + "=" + foldBase.ToString("F6") + "+" + previous.ToString("F6") + "=" + foldTarget.ToString("F6");
                }

                offsets[pickerIndex] = 0.0;
                if (staleFlags.Length > pickerIndex)
                    staleFlags[pickerIndex] = true;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletAfZOffsetReset",
                    "Z 캘리브레이션 티칭 저장으로 Collet AF Z Offset을 리셋했습니다(새 티칭이 현재 콜렛 접촉면을 포함). source=" + source +
                    ", side=" + Side +
                    ", colletNo=" + (pickerIndex + 1) +
                    ", previousOffsetMm=" + previous.ToString("F6") + " -> 0.000000" +
                    ", fold(" + foldDetail + ")" +
                    ", baselineStale=true(다음 Bottom AF는 기준선 재설정 전용)");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-AF-ZOFFSET-RESET-EX", Name,
                    "Collet AF Z Offset 리셋 중 예외가 발생했습니다. source=" + source +
                    ", colletNo=" + (pickerIndex + 1) +
                    ", error=" + ex.Message);
            }
        }

        /// <summary>
        /// 공정 중 Runtime Bottom Die AutoFocus 결과로 Collet AF Z Offset을 누적한다.
        /// runtimeDelta = bestZ - 기존 바텀 포커스(defaultZ), 누적 = 기존 offset + runtimeDelta.
        /// |누적| > limit 이면 fail-closed(알람 중단). 성공 시 산식 포함 전체 데이터를 항상 영속 로그로 남긴다.
        /// 적용 시점: 이번 다이의 Pick은 이미 지났으므로 다음 다이(Place 포함)부터 반영된다.
        /// 반환 0=성공, 음수=한계 초과 Fail.
        /// </summary>
        protected int AccumulateColletAfZOffsetFromRuntimeAf(
            int pickerIndex,
            double defaultZ,
            double bestZ,
            double bestScore,
            int sampleCount,
            string scanMode,
            string waferKey,
            out double previousOffset,
            out bool offsetCommitted,
            out bool staleConsumed)
        {
            previousOffset = 0.0;
            offsetCommitted = false;
            staleConsumed = false;
            if (pickerIndex < 0 || pickerIndex > 3)
                return 0;

            double[] offsets;
            double limitMm;
            bool[] staleFlags;
            if (!TryGetColletAfZOffsetRecipe(out offsets, out limitMm, out staleFlags) || offsets.Length <= pickerIndex)
                return 0;

            // Z캘 직후 기준선(BottomPosition)이 낡은 상태의 첫 Bottom AF: 드리프트가 이미 Z캘 티칭에
            // 흡수되어 있으므로 누적하지 않고 기준선 재설정(BottomPosition 갱신은 호출부가 수행)만 한다.
            if (staleFlags.Length > pickerIndex && staleFlags[pickerIndex])
            {
                staleFlags[pickerIndex] = false;
                staleConsumed = true;
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletAfZOffsetRuntime",
                    "Runtime Bottom AF 기준선 재설정 — Z캘 직후 첫 AF이므로 누적을 건너뜁니다. side=" + Side +
                    ", colletNo=" + (pickerIndex + 1) +
                    ", skippedDelta=bestZ-defaultZ=" + bestZ.ToString("F6") + "-" + defaultZ.ToString("F6") + "=" + (bestZ - defaultZ).ToString("F6") +
                    ", offsetMm=" + offsets[pickerIndex].ToString("F6") + " (변경 없음), baselineStale=false");
                return 0;
            }

            double runtimeDelta = bestZ - defaultZ;
            if (double.IsNaN(runtimeDelta) || double.IsInfinity(runtimeDelta))
                return 0;

            double previous = offsets[pickerIndex];
            if (double.IsNaN(previous) || double.IsInfinity(previous))
                previous = 0.0;
            previousOffset = previous;
            double newTotal = previous + runtimeDelta;

            if (Math.Abs(newTotal) > limitMm)
            {
                return Fail("PICKER-RUNTIME-AF-ZOFFSET-LIMIT", Name,
                    "Runtime Bottom AF 누적 Collet AF Z Offset이 안전 한계를 초과했습니다. side=" + Side +
                    ", colletNo=" + (pickerIndex + 1) +
                    ", runtimeDelta=bestZ-defaultZ=" + bestZ.ToString("F6") + "-" + defaultZ.ToString("F6") + "=" + runtimeDelta.ToString("F6") +
                    ", previousOffsetMm=" + previous.ToString("F6") +
                    ", newTotalMm=" + newTotal.ToString("F6") +
                    ", limitMm=" + limitMm.ToString("F6") +
                    ". 콜렛/티칭 상태를 확인하세요.");
            }

            offsets[pickerIndex] = newTotal;
            offsetCommitted = true;

            string detail =
                "Runtime Bottom AF Collet AF Z Offset 누적. side=" + Side +
                ", colletNo=" + (pickerIndex + 1) +
                ", runtimeDelta=bestZ-defaultZ=" + bestZ.ToString("F6") + "-" + defaultZ.ToString("F6") + "=" + runtimeDelta.ToString("F6") +
                ", offsetMm=" + previous.ToString("F6") + " -> " + newTotal.ToString("F6") +
                ", limitMm=" + limitMm.ToString("F6") +
                ", bestScore=" + bestScore.ToString("F4") +
                ", sampleCount=" + sampleCount +
                ", scanMode=" + (scanMode ?? "-") +
                ", wafer=" + (waferKey ?? "-") +
                ", 부호규칙=+덜내려옴/-더내려옴, 적용=다음 다이 Pick/Place부터";
            // 공정 중 이벤트지만 저빈도 + 분석 필수 데이터이므로 Calibration 카테고리로 항상 영속한다.
            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletAfZOffsetRuntime", detail);
            WriteLog("ColletAfZOffset", Name + " " + detail + " - Ok");
            return 0;
        }

        protected double ResolveSideInspectionPickerZFromBottomBest(int pickerIndex, int pickerNo, string source)
        {
            double bottomTeachingZ = GetPickerTeachingPosition(GetPickerZAxis(pickerIndex), "BottomPosition");
            double sideTeachingZ = GetPickerTeachingPosition(GetPickerZAxis(pickerIndex), "SidePosition");
            double offsetMm = 0.0;

            try
            {
                VisionFocusCalibrationData focusData = Context != null &&
                    Context.Machine != null &&
                    Context.Machine.VisionUnit != null &&
                    Context.Machine.VisionUnit.Config != null
                        ? Context.Machine.VisionUnit.Config.FocusCalibration
                        : null;

                if (focusData != null)
                {
                    focusData.EnsureObjects();
                    offsetMm = SanitizeSideInspectionZOffset(focusData.BottomToSideZOffsetMm);

                    VisionFocusPositionRecord bottomRecord = focusData.GetBottomRecord(
                        VisionFocusScanKind.BottomDie,
                        Side == PickerSequenceSide.Front ? VisionFocusPickerSide.Front : VisionFocusPickerSide.Rear,
                        pickerNo);

                    if (bottomRecord != null &&
                        bottomRecord.Valid &&
                        bottomRecord.BestScore > 0.0 &&
                        IsFiniteSideInspectionZ(bottomRecord.BestPosition))
                    {
                        double finalZ = bottomRecord.BestPosition + offsetMm;
                        WriteLog(source ?? "PickerSideZ",
                            Name + " Side 검사 PickerZ 산출: Bottom AF Best Z + BottomToSideZOffsetMm 적용. " +
                            "side=" + Side +
                            ", pickerNo=" + pickerNo +
                            ", bottomBestZ=" + bottomRecord.BestPosition.ToString("F6") +
                            ", bottomToSideZOffsetMm=" + offsetMm.ToString("F6") +
                            ", finalSideZ=" + finalZ.ToString("F6") +
                            ", useFlag=" + focusData.UseBottomToSideZOffset +
                            ", sidePositionTeaching(미사용)=" + sideTeachingZ.ToString("F6") + " - Ok");
                        return finalZ;
                    }

                    double fallbackZ = bottomTeachingZ + offsetMm;
                    WriteLog(source ?? "PickerSideZ",
                        Name + " Side 검사 PickerZ 산출: Bottom AF Best가 유효하지 않아 BottomPosition + BottomToSideZOffsetMm로 대체합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", bottomPosition=" + bottomTeachingZ.ToString("F6") +
                        ", bottomToSideZOffsetMm=" + offsetMm.ToString("F6") +
                        ", finalSideZ=" + fallbackZ.ToString("F6") +
                        ", sidePositionTeaching(미사용)=" + sideTeachingZ.ToString("F6") + " - Check");
                    return fallbackZ;
                }

                WriteLog(source ?? "PickerSideZ",
                    Name + " Side 검사 PickerZ 산출: Vision Focus 설정이 없어 BottomPosition을 사용합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", bottomPosition=" + bottomTeachingZ.ToString("F6") +
                    ", sidePositionTeaching(미사용)=" + sideTeachingZ.ToString("F6") + " - Check");
                return bottomTeachingZ;
            }
            catch (Exception ex)
            {
                WriteLog(source ?? "PickerSideZ",
                    Name + " Side 검사 PickerZ 산출 중 예외가 발생해 BottomPosition을 사용합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", bottomPosition=" + bottomTeachingZ.ToString("F6") +
                    ", error=" + ex.Message + " - Failed");
                return bottomTeachingZ;
            }
        }

        protected async Task DelayAfterRuntimeAutoFocusBottomGrabAckAsync(int pickerNo, CancellationToken ct)
        {
            if (!runtimeBottomAutoFocusCompletedForNextGrab)
                return;

            runtimeBottomAutoFocusCompletedForNextGrab = false;
            int delayMs = 300;
            if (Context != null && Context.Machine != null && Context.Machine.VisionUnit != null &&
                Context.Machine.VisionUnit.Recipe != null)
            {
                Context.Machine.VisionUnit.Recipe.EnsurePositionObjects();
                delayMs = Context.Machine.VisionUnit.Recipe.RuntimeAutoFocusToBottomInspectionDelayMs;
            }

            delayMs = Math.Max(0, Math.Min(60000, delayMs));
            if (delayMs <= 0)
                return;

            WriteLog("PickerAutoFocus",
                Name + " Runtime AutoFocus 후 Bottom Grab ACK 수신 완료 대기를 시작합니다. side=" + Side +
                ", pickerNo=" + pickerNo +
                ", delayMs=" + delayMs + " - Start");
            await Task.Delay(delayMs, ct).ConfigureAwait(false);
            WriteLog("PickerAutoFocus",
                Name + " Runtime AutoFocus 후 Bottom Grab ACK 대기를 완료했습니다. side=" + Side +
                ", pickerNo=" + pickerNo +
                ", delayMs=" + delayMs + " - Ok");
        }

        private static bool IsFiniteSideInspectionZ(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double SanitizeSideInspectionZOffset(double value)
        {
            return IsFiniteSideInspectionZ(value) ? value : 0.0;
        }

        protected double ResolvePickerZoneX(string positionArrayName, int pickerIndex)
        {
            return ResolvePickerZoneTarget(positionArrayName, pickerIndex).X;
        }

        protected double ResolvePickerZoneY(string positionArrayName, int pickerIndex)
        {
            return ResolvePickerZoneTarget(positionArrayName, pickerIndex).Y;
        }

        protected double ResolvePickerZoneT(string positionArrayName, int pickerIndex)
        {
            return ResolvePickerZoneTarget(positionArrayName, pickerIndex).T;
        }

        protected PickerCalibratedZoneTarget ResolvePickerZoneTarget(string positionArrayName, int pickerIndex)
        {
            return PickerMotionTargetResolver.ResolveCarryZoneTarget(
                Context != null ? Context.Machine : null,
                Side,
                positionArrayName,
                pickerIndex);
        }

        protected string ResolveZonePositionName(string positionArrayName)
        {
            return CalibrationCoordinateService.ResolveZonePositionName(positionArrayName);
        }

        protected PickerAxis GetPickerZAxis(int index)
        {
            return CalibrationCoordinateService.ResolvePickerZAxis(index);
        }

        protected PickerAxis GetPickerTAxis(int index)
        {
            return CalibrationCoordinateService.ResolvePickerTAxis(index);
        }

        protected double ResolvePickerAlignOffsetX(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(index);
            return (offset != null ? offset.AlignOffsetX : 0.0) + calibration.X;
        }

        protected double ResolvePickerAlignOffsetY(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(index);
            return (offset != null ? offset.AlignOffsetY : 0.0) + calibration.Y;
        }

        protected double ResolvePickerAlignOffsetT(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            return offset != null ? offset.AlignOffsetT : 0.0;
        }

        protected double ResolvePickerRuntimeAlignOffsetX(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            // InputVisionToPicker X는 카메라/콜렛 캘 포함 저장값이므로 Pick 계산에서는 Runtime X만 추가한다.
            return offset != null ? offset.AlignOffsetX : 0.0;
        }

        protected double ResolvePickerRuntimeAlignOffsetY(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            // InputVisionToPicker Y는 카메라/콜렛 캘 포함 저장값이므로 Pick 계산에서는 Runtime Y만 추가한다.
            return offset != null ? offset.AlignOffsetY : 0.0;
        }

        private PickerCalibrationOffset ResolvePickerCalibrationOffset(int index)
        {
            return CalibrationCoordinateService.ResolvePickerCalibrationOffset(
                Context != null ? Context.Machine : null,
                ResolvePickerSide(),
                index);
        }

        protected double ResolveInputVisionToPickerXOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveInputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetX;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve InputVisionToPicker X offset. pickerNo=" +
                ToPickerNo(index) + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected double ResolveInputVisionToPickerYOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveInputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetY;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve InputVisionToPicker Y offset. pickerNo=" +
                ToPickerNo(index) + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected double ResolveOutputVisionToPickerXOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveOutputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetX;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve OutputVisionToPicker X offset. pickerNo=" +
                ToPickerNo(index) + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected double ResolveOutputVisionToPickerYOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveOutputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetY;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve OutputVisionToPicker Y offset. pickerNo=" +
                ToPickerNo(index) + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected bool TryResolveInputVisionToPickerOffsets(int index, out double offsetX, out double offsetY, out string reason)
        {
            return PickerCoordinateTransformHelper.TryResolveInputVisionToPickerOffsets(
                Context != null ? Context.Machine : null,
                Side,
                index,
                out offsetX,
                out offsetY,
                out reason);
        }

        protected bool TryResolveOutputVisionToPickerOffsets(int index, out double offsetX, out double offsetY, out string reason)
        {
            return PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                Context != null ? Context.Machine : null,
                Side,
                index,
                out offsetX,
                out offsetY,
                out reason);
        }

        protected bool TryResolveOutputVisionToPickerOffsets(BinSide outputSide, int index, out double offsetX, out double offsetY, out string reason)
        {
            return PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                Context != null ? Context.Machine : null,
                Side,
                index,
                outputSide,
                out offsetX,
                out offsetY,
                out reason);
        }

        private PickerAlignOffset ResolvePickerRuntimeOffset(int index)
        {
            if (index < 0)
                return null;

            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                return FrontPicker.GetRuntimePickerOffset(index);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                return RearPicker.GetRuntimePickerOffset(index);

            return null;
        }

        private VisionFocusPickerSide ResolvePickerSide()
        {
            return Side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;
        }

        protected InspectionAlignmentSnapshot BuildPickerAlignmentSnapshot(
            string name,
            int pickerIndex,
            double targetX,
            double targetY,
            double targetT,
            double targetZ,
            VisionOffset offset)
        {
            try
            {
                BaseAxis xAxis = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis yAxis = GetPickerAxis(PickerAxis.PickerY);
                BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(pickerIndex));
                BaseAxis zAxis = GetPickerAxis(GetPickerZAxis(pickerIndex));

                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    X = xAxis != null ? xAxis.ActualPosition : targetX,
                    Y = yAxis != null ? yAxis.ActualPosition : targetY,
                    T = tAxis != null ? tAxis.ActualPosition : targetT,
                    Z = zAxis != null ? zAxis.ActualPosition : targetZ,
                    XAxisName = xAxis != null ? xAxis.Name : "PickerX",
                    YAxisName = yAxis != null ? yAxis.Name : "PickerY",
                    TAxisName = tAxis != null ? tAxis.Name : GetPickerTAxis(pickerIndex).ToString(),
                    ZAxisName = zAxis != null ? zAxis.Name : GetPickerZAxis(pickerIndex).ToString(),
                    Offset = offset ?? new VisionOffset(),
                    IsValid = true
                };
            }
            catch (Exception ex)
            {
                WriteLog("PickerInspectionData",
                    Name + " picker alignment snapshot failed. snapshot=" +
                    name + ", error=" + ex.Message + " - Failed");
                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    X = targetX,
                    Y = targetY,
                    T = targetT,
                    Z = targetZ,
                    Offset = offset ?? new VisionOffset(),
                    IsValid = false
                };
            }
            finally
            {
            }
        }

        protected InspectionAlignmentSnapshot BuildInputStageAlignmentSnapshot(
            InputStageUnit stage,
            string name,
            VisionOffset offset)
        {
            try
            {
                BaseAxis xAxis = stage != null ? stage.CameraX : null;
                BaseAxis yAxis = stage != null ? stage.StageY : null;
                BaseAxis tAxis = stage != null ? stage.StageT : null;
                BaseAxis zAxis = stage != null ? stage.ExpanderZ : null;

                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    X = xAxis != null ? xAxis.ActualPosition : 0.0,
                    Y = yAxis != null ? yAxis.ActualPosition : 0.0,
                    T = tAxis != null ? tAxis.ActualPosition : 0.0,
                    Z = zAxis != null ? zAxis.ActualPosition : 0.0,
                    XAxisName = xAxis != null ? xAxis.Name : "InputVisionX",
                    YAxisName = yAxis != null ? yAxis.Name : "InputStageY",
                    TAxisName = tAxis != null ? tAxis.Name : "InputStageT",
                    ZAxisName = zAxis != null ? zAxis.Name : "InputExpandingZ",
                    Offset = offset ?? new VisionOffset(),
                    IsValid = stage != null
                };
            }
            catch (Exception ex)
            {
                WriteLog("PickerInspectionData",
                    Name + " input alignment snapshot failed. snapshot=" +
                    name + ", error=" + ex.Message + " - Failed");
                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    Offset = offset ?? new VisionOffset(),
                    IsValid = false
                };
            }
            finally
            {
            }
        }

        protected static InspectionMeasurement BuildMeasurement(
            string name,
            double value,
            string unit,
            MaterialInspectionResult result,
            string rawValue = "")
        {
            return new InspectionMeasurement
            {
                Name = name ?? "",
                Value = value,
                Unit = unit ?? "",
                RawValue = rawValue ?? "",
                Result = result
            };
        }

        protected static InspectionMeasurement BuildBooleanMeasurement(string name, bool ok)
        {
            return new InspectionMeasurement
            {
                Name = name ?? "",
                Value = ok ? 1.0 : 0.0,
                Unit = "bool",
                RawValue = ok ? "OK" : "NG",
                Result = ok ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng
            };
        }

        protected static void AppendVisionRawMeasurements(
            List<InspectionMeasurement> measurements,
            BottomVisionOffset result,
            string prefix,
            MaterialInspectionResult inspectionResult)
        {
            if (measurements == null || result == null)
                return;

            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "Vision" : prefix;
            if (!string.IsNullOrWhiteSpace(result.Raw))
            {
                measurements.Add(new InspectionMeasurement
                {
                    Name = safePrefix + "VisionRaw",
                    Value = 0.0,
                    Unit = "raw",
                    RawValue = result.Raw,
                    Result = inspectionResult
                });
            }

            AppendVisionValueMeasurements(measurements, result.Values, safePrefix, inspectionResult);
        }

        protected static void AppendVisionRawMeasurements(
            List<InspectionMeasurement> measurements,
            SideVisionResult result,
            string prefix,
            MaterialInspectionResult inspectionResult)
        {
            if (measurements == null || result == null)
                return;

            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "Vision" : prefix;
            if (!string.IsNullOrWhiteSpace(result.Raw))
            {
                measurements.Add(new InspectionMeasurement
                {
                    Name = safePrefix + "VisionRaw",
                    Value = 0.0,
                    Unit = "raw",
                    RawValue = result.Raw,
                    Result = inspectionResult
                });
            }

            if (result.Values == null || result.Values.Count == 0)
                return;

            AppendVisionValueMeasurements(measurements, result.Values, safePrefix, inspectionResult);
        }

        protected static void AppendVisionValueMeasurements(
            List<InspectionMeasurement> measurements,
            IDictionary<string, string> values,
            string prefix,
            MaterialInspectionResult defaultResult)
        {
            if (measurements == null || values == null || values.Count == 0)
                return;

            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "Vision" : prefix;
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (IsVisionPassKey(pair.Key))
                    continue;

                double value;
                QMC.CDT320.VisionComm.VisionProtocolResponse.TryParseDouble(pair.Value, out value);
                measurements.Add(new InspectionMeasurement
                {
                    Name = safePrefix + "Vision_" + NormalizeMeasurementKey(pair.Key),
                    Value = value,
                    Unit = "",
                    RawValue = pair.Value ?? "",
                    Result = ResolveVisionMeasurementResult(values, pair.Key, defaultResult)
                });
            }
        }

        protected static bool IsVisionPassKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key) &&
                   key.EndsWith("_pass", StringComparison.OrdinalIgnoreCase);
        }

        protected static MaterialInspectionResult ResolveVisionMeasurementResult(
            IDictionary<string, string> values,
            string key,
            MaterialInspectionResult defaultResult)
        {
            if (values == null || string.IsNullOrWhiteSpace(key))
                return defaultResult;

            string passText;
            if (!values.TryGetValue(key + "_pass", out passText))
                return defaultResult;

            bool pass;
            if (TryParseVisionPassValue(passText, out pass))
                return pass ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng;

            return defaultResult;
        }

        protected static bool TryParseVisionPassValue(string text, out bool pass)
        {
            pass = false;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string value = text.Trim();
            if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "ok", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "pass", StringComparison.OrdinalIgnoreCase))
            {
                pass = true;
                return true;
            }

            if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "ng", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "fail", StringComparison.OrdinalIgnoreCase))
            {
                pass = false;
                return true;
            }

            return false;
        }

        private static string NormalizeMeasurementKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "unknown";

            var chars = key.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                bool ok = (c >= 'a' && c <= 'z') ||
                          (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') ||
                          c == '_';
                if (!ok)
                    chars[i] = '_';
            }

            return new string(chars);
        }

        protected async Task<SequenceResourceLease> AcquireResourceAsync(
            SequenceResourceKind resource,
            string holder,
            CancellationToken ct)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? Name : holder;
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    SequenceResourceLease manualLease = await Context.Resources
                        .AcquireAsync(resource, safeHolder, ResolveResourceTimeout(), ct)
                        .ConfigureAwait(false);
                    if (manualLease == null)
                    {
                        Fail("PICKER-RESOURCE", safeHolder,
                            "리소스 점유 실패. resource=" + resource);
                    }

                    return manualLease;
                }

                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!safetyRetreatMoveActive)
                        Context.StopIfCycleStopRequested(
                            Name + ".AcquireResource:" + resource,
                            ShouldDeferCycleStopForPickerDrain(),
                            "Picker target die drain");

                    SequenceResourceLease autoLease = await Context.Resources
                        .AcquireAsync(resource, safeHolder, 200, ct, false)
                        .ConfigureAwait(false);
                    if (autoLease != null)
                    {
                        if (waitLogged)
                        {
                            Context.LogPublic("[SEQ] " + Name + " 리소스 대기 완료. resource=" +
                                resource + ", holder=" + safeHolder);
                        }

                        return autoLease;
                    }

                    if (!waitLogged)
                    {
                        string currentHolder = Context.Resources.GetHolder(resource);
                        Context.LogPublic("[SEQ] " + Name + " 리소스 사용 대기 중입니다. resource=" +
                            resource + ", holder=" + safeHolder + ", current=" +
                            (string.IsNullOrWhiteSpace(currentHolder) ? "-" : currentHolder));
                        waitLogged = true;
                    }

                    await Task.Delay(20, ct).ConfigureAwait(false);
                }
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
                Fail("PICKER-RESOURCE", safeHolder,
                    "리소스 점유 중 예외가 발생했습니다. resource=" + resource + ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    Context.LogPublic("[" + Name + "] STOP " + message);
                    throw new SequenceStopException(message);
                }

                SequenceFailureStore.Record(Name, Kind.ToString(), CurrentStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                if (IsAlarmStopActive())
                {
                    WriteLog(source,
                        "이미 활성 알람이 있어 피커 후속 알람 발생을 생략합니다. code=" +
                        alarmCode + ", message=" + message + " - Suppressed");
                }
                else
                {
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                }
                Context.LogPublic("[" + Name + "] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog(source, "Failure handling failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        protected bool IsAlarmStopActive()
        {
            try
            {
                if (AlarmManager.HasActive)
                    return true;

                return Context != null &&
                       Context.Controller != null &&
                       Context.Controller.Status == EquipmentStatus.Alarm;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private int StopPickerMoveBecauseAlarmActive(string description)
        {
            WriteLog("PickerMove",
                Name + " " + description +
                " 이동 명령을 중단합니다. 이미 활성 알람 상태입니다. - Stopped");
            return -1;
        }

        private sealed class PickerMoveAxisLogDetail
        {
            public PickerAxis Axis { get; set; }
            public double Start { get; set; }
            public double Target { get; set; }
            public double Distance { get; set; }
            public double Velocity { get; set; }
            public double Acceleration { get; set; }
            public double Deceleration { get; set; }
        }

        private PickerMoveAxisLogDetail BuildPickerMoveAxisLogDetail(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            double start = item != null ? item.ActualPosition : 0.0;
            double velocity = ResolvePickerMoveLogVelocity(item);
            double acceleration = ResolvePickerMoveLogAcceleration(item);
            double deceleration = ResolvePickerMoveLogDeceleration(item);

            return new PickerMoveAxisLogDetail
            {
                Axis = axis,
                Start = start,
                Target = target,
                Distance = Math.Abs(target - start),
                Velocity = velocity,
                Acceleration = acceleration,
                Deceleration = deceleration
            };
        }

        private double ResolvePickerMoveLogVelocity(BaseAxis axis)
        {
            if (axis == null || axis.Config == null)
                return 0.0;

            bool fine = Options != null && Options.FineMove;
            if (fine && axis.Config.JogFineVelocity > 0.0)
                return axis.Config.JogFineVelocity;

            return MotionSpeedScale.ApplyDefaultVelocityScale(axis.Config.DefaultVelocity);
        }

        private double ResolvePickerMoveLogAcceleration(BaseAxis axis)
        {
            if (axis == null || axis.Config == null)
                return 0.0;

            bool fine = Options != null && Options.FineMove;
            if (fine)
                return axis.Config.Acceleration;

            return MotionSpeedScale.ApplyDefaultAccelerationScale(axis.Config.Acceleration);
        }

        private double ResolvePickerMoveLogDeceleration(BaseAxis axis)
        {
            if (axis == null || axis.Config == null)
                return 0.0;

            bool fine = Options != null && Options.FineMove;
            if (fine)
                return axis.Config.Deceleration;

            return MotionSpeedScale.ApplyDefaultAccelerationScale(axis.Config.Deceleration);
        }

        private static string FormatPickerMoveAxisLogDetail(PickerMoveAxisLogDetail detail)
        {
            if (detail == null)
                return string.Empty;

            return detail.Axis +
                "(start=" + detail.Start.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", target=" + detail.Target.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", distance=" + detail.Distance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", velocity=" + detail.Velocity.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", acc=" + detail.Acceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", dec=" + detail.Deceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ")";
        }

        private static string FormatPickerMoveAxisLogDetails(IList<PickerMoveAxisLogDetail> details)
        {
            if (details == null || details.Count == 0)
                return string.Empty;

            List<string> parts = new List<string>();
            for (int i = 0; i < details.Count; i++)
                parts.Add(FormatPickerMoveAxisLogDetail(details[i]));

            return string.Join("; ", parts);
        }

        private void WritePickerSequenceMoveElapsed(
            PickerMoveAxisLogDetail axisDetail,
            string targetName,
            string description,
            int result,
            long commandMs,
            long waitMs,
            long totalMs,
            string waitSummary)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC",
                    "PK-MOVE-TIME",
                    Name + " " + (axisDetail != null ? axisDetail.Axis.ToString() : "-") +
                    " path=SequenceCommandVerify" +
                    ", runMode=" + (Options != null ? Options.RunMode.ToString() : "-") +
                    ", kind=" + Kind +
                    ", step=" + CurrentStep +
                    ", elapsedMs=" + totalMs +
                    ", commandMs=" + commandMs +
                    ", waitMs=" + waitMs +
                    ", result=" + result +
                    ", wait=" + (waitSummary ?? "-") +
                    ", start=" + (axisDetail != null ? axisDetail.Start.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", target=" + (axisDetail != null ? axisDetail.Target.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", distance=" + (axisDetail != null ? axisDetail.Distance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", targetName=" + (targetName ?? string.Empty) +
                    ", description=" + (description ?? string.Empty) +
                    ", fine=" + (Options != null && Options.FineMove) +
                    ", velocity=" + (axisDetail != null ? axisDetail.Velocity.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", acc=" + (axisDetail != null ? axisDetail.Acceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", dec=" + (axisDetail != null ? axisDetail.Deceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-"));
            }
            catch
            {
            }
        }

        private void WritePickerSequenceGroupMoveElapsed(
            string targetName,
            string description,
            IList<PickerMoveAxisLogDetail> axisDetails,
            int result,
            long commandMs,
            long waitMs,
            long totalMs,
            string status)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC",
                    "PK-MOVE-TIME",
                    Name + " group path=SequenceCommandVerify" +
                    ", runMode=" + (Options != null ? Options.RunMode.ToString() : "-") +
                    ", kind=" + Kind +
                    ", step=" + CurrentStep +
                    ", elapsedMs=" + totalMs +
                    ", commandMs=" + commandMs +
                    ", waitMs=" + waitMs +
                    ", result=" + result +
                    ", status=" + (status ?? string.Empty) +
                    ", targetName=" + (targetName ?? string.Empty) +
                    ", description=" + (description ?? string.Empty) +
                    ", fine=" + (Options != null && Options.FineMove) +
                    ", commandCount=" + (axisDetails != null ? axisDetails.Count : 0) +
                    ", axisDetails=" + FormatPickerMoveAxisLogDetails(axisDetails));
            }
            catch
            {
            }
        }

        protected void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(Side == PickerSequenceSide.Front ? QMC.Common.Logging.EventKind.FrontHeadSeq : QMC.Common.Logging.EventKind.RearHeadSeq, source, message);
        }
    }
}
