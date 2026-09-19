using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;
using QMC.CDT320.VisionComm;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputStageDieMappingStep
    {
        Idle,
        CheckUnit,
        MoveNeedleZSafeBeforeMapping,
        MoveVisionProcessBeforeMapping,
        MoveCenterPoint,
        FindCenterPoint,
        MoveCenterDiePoint,
        // Legacy 4-point mapping steps are kept for reference. The active sequence uses an Align-predicted map anchor.
        MoveTopPoint,
        FindTopPoint,
        MoveBottomPoint,
        FindBottomPoint,
        MoveLeftPoint,
        FindLeftPoint,
        MoveRightPoint,
        FindRightPoint,
        CalculateDieMap,
        ApplyDieMap,
        MoveVisionXAvoidAfterManual,
        Complete,
        Error
    }

    internal sealed class InputStageDieMappingSequence : InputStageSequenceBase<InputStageDieMappingStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private static string LastSourceInputDieMapFailure = "";
        // [P4 2026-08-22] LOT 네트워크 맵 실패를 구분 알람 코드로 세우기 위한 부가 정보(비면 기존 코드 사용).
        private static string LastSourceInputDieMapFailureCode = "";
        private const double AlignPitchCompareToleranceMm = InputWaferMapPreflightService.PitchCompareToleranceMm;

        private readonly Dictionary<string, MappedMarkPoint> _mappedPoints = new Dictionary<string, MappedMarkPoint>(StringComparer.OrdinalIgnoreCase);
        private WaferMaterial _wafer;
        private string _expectedWaferId = "";
        private string _expectedWaferInstanceId = "";
        private TapeFrameSpec _frameSpec;
        private DieMap _sourceMap;
        private DieMap _dieMap;
        private WaferMapData _waferMap;
        private DieMapEntry _mappingAnchorEntry;
        private int _mappingAnchorMapX;
        private int _mappingAnchorMapY;
        private double _mappingAnchorExpectedX;
        private double _mappingAnchorExpectedY;
        private double _mappingAnchorDetectedX;
        private double _mappingAnchorDetectedY;
        private double _mappingFineOffsetX;
        private double _mappingFineOffsetY;
        private int _createdDieCount;
        private double _dieMapCenterX;
        private double _dieMapCenterY;
        private double _centerDieTargetX;
        private double _centerDieTargetY;
        private bool _hybridVirtualFrameActive;

        public InputStageDieMappingSequence(MachineSequenceContext context)
            : base(context, InputStageSequenceKind.DieMapping, "InputStageDieMappingSequence")
        {
        }

        protected override InputStageDieMappingStep IdleStep { get { return InputStageDieMappingStep.Idle; } }
        protected override InputStageDieMappingStep InitialStep { get { return InputStageDieMappingStep.CheckUnit; } }
        protected override InputStageDieMappingStep CompleteStep { get { return InputStageDieMappingStep.Complete; } }
        protected override InputStageDieMappingStep ErrorStep { get { return InputStageDieMappingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputStageDieMappingStep.CheckUnit:
                        return Task.FromResult(CheckDieMappingUnit());
                    // 다이 맵핑 전 NeedleZ 안전 위치 복귀
                    case InputStageDieMappingStep.MoveNeedleZSafeBeforeMapping:
                        return MoveNeedleZSafeBeforeMappingAsync(ct);
                    // 다이 맵핑 시작 전 Vision/Stage 작업 기준 위치 진입
                    case InputStageDieMappingStep.MoveVisionProcessBeforeMapping:
                        return MoveVisionProcessBeforeMappingAsync(ct);
                    // 센터 포인트 이동
                    case InputStageDieMappingStep.MoveCenterPoint:
                        return MoveCenterPointAsync(ct);
                    // 센터 포인트 찾기
                    case InputStageDieMappingStep.FindCenterPoint:
                        return FindCenterPointAsync(ct);
                    // 상단 포인트 이동
                    case InputStageDieMappingStep.MoveTopPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Top, InputStageDieMappingStep.FindTopPoint, ct);
                    // 상단 포인트 찾기
                    case InputStageDieMappingStep.FindTopPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Top, InputStageDieMappingStep.MoveBottomPoint, ct);
                    // 하단 포인트 이동
                    case InputStageDieMappingStep.MoveBottomPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Bottom, InputStageDieMappingStep.FindBottomPoint, ct);
                    // 하단 포인트 찾기
                    case InputStageDieMappingStep.FindBottomPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Bottom, InputStageDieMappingStep.MoveLeftPoint, ct);
                    // 좌측 포인트 이동
                    case InputStageDieMappingStep.MoveLeftPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Left, InputStageDieMappingStep.FindLeftPoint, ct);
                    // 좌측 포인트 찾기
                    case InputStageDieMappingStep.FindLeftPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Left, InputStageDieMappingStep.MoveRightPoint, ct);
                    // 우측 포인트 이동
                    case InputStageDieMappingStep.MoveRightPoint:
                        return MoveMarkPointAsync(Stage.Recipe.DieMap.Right, InputStageDieMappingStep.FindRightPoint, ct);
                    // 우측 포인트 찾기
                    case InputStageDieMappingStep.FindRightPoint:
                        return FindMarkPointAsync(Stage.Recipe.DieMap.Right, InputStageDieMappingStep.CalculateDieMap, ct);
                    // 다이 맵 계산
                    case InputStageDieMappingStep.CalculateDieMap:
                        return Task.FromResult(CalculateDieMap());
                    // 계산된 다이맵 Center 위치 이동
                    case InputStageDieMappingStep.MoveCenterDiePoint:
                        return MoveCenterDiePointAsync(ct);
                    // 다이 맵 적용
                    case InputStageDieMappingStep.ApplyDieMap:
                        return Task.FromResult(ApplyDieMap());
                    // 메뉴얼 Die Mapping 완료 후 카메라 X축 안전 위치 복귀
                    case InputStageDieMappingStep.MoveVisionXAvoidAfterManual:
                        return MoveVisionXAvoidAfterManualDieMappingAsync(ct);
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-STAGE-DIEMAP-STEP-EX", "InputStageDieMappingSequence", "Die mapping step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckDieMappingUnit()
        {
            try
            {
                _hybridVirtualFrameActive = false;
                // 바코드/원격맵 검사 실패 후 재개도 CheckUnit을 거쳐야 한다.
                int result = CheckUnit(InputStageDieMappingStep.CheckUnit);
                if (result != 0)
                    return result;

                result = ConfigureHybridVirtualFrameMode();
                if (result != 0)
                    return result;

                if (Stage.Recipe == null)
                    return Fail("IN-STAGE-DIEMAP-RECIPE", Stage.Name, "Input stage recipe is not available.");

                string servoReason = BuildDieMappingServoReason();
                if (!string.IsNullOrEmpty(servoReason))
                    return Fail("IN-STAGE-DIEMAP-SERVO", Stage.Name, "Input stage servo is not on. " + servoReason);

                Stage.Recipe.EnsurePositionObjects();
                if (Stage.Recipe.DieMap == null)
                    return Fail("IN-STAGE-DIEMAP-RECIPE", Stage.Name, "Input stage die map recipe is not available.");

                Stage.Recipe.DieMap.EnsurePoints();
                // 기존 Top/Bottom/Left/Right 4점 방식은 현재 시퀀스에서 사용하지 않는다.
                // Align Origin으로 예상 Anchor를 구하고 Vision 잔차만 전체 DieMap 좌표에 반영한다.

                if (!Stage.HasWaferOnStage())
                    return Fail("IN-STAGE-DIEMAP-WAFER", "Material",
                        "InputStage wafer data was not found. HasWaferOnStage=false, CurrentWaferMaterial=" +
                        (Stage.CurrentWaferMaterial != null ? Stage.CurrentWaferMaterial.WaferId : "null"));

                _wafer = Stage.GetCurrentStageWaferMaterial();
                if (_wafer == null)
                    _wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (_wafer == null)
                    return Fail("IN-STAGE-DIEMAP-WAFER", "Material",
                        "InputStage wafer material is not available. CurrentWaferMaterial=null, MaterialLocation=InputStage empty.");

                RecipeInputMapSource.BeginWafer(_wafer);
                // 독립 Manual/Step Mapping도 자동 투입과 같은 LOT 접두부 검사를 통과해야 한다.
                // 이 경계에서는 재입력이나 복구 모션을 수행하지 않고 잘못된 자재만 차단한다.
                if (Stage.Config != null && Stage.Config.UseBarcodeLotPrefixCheck)
                {
                    string barcodeReason;
                    if (!_wafer.BarcodeConfirmed)
                        return Fail("IN-STAGE-DIEMAP-BARCODE-LOT", "InputStageDieMappingSequence",
                            "LOT 접두부 검사가 켜져 있지만 입력 웨이퍼 바코드가 확정되지 않았습니다.");
                    if (!InputFeederLoadToStageSequence.TryValidateInputBarcodePolicy(Stage, _wafer.BarcodeId, out barcodeReason))
                        return Fail("IN-STAGE-DIEMAP-BARCODE-LOT", "InputStageDieMappingSequence", barcodeReason);
                }

                _expectedWaferId = _wafer.WaferId ?? "";
                _expectedWaferInstanceId = MaterialStateService.EnsureWaferInstanceId(_wafer);
                if (string.IsNullOrWhiteSpace(_expectedWaferInstanceId))
                    return Fail("IN-STAGE-DIEMAP-WAFER-INSTANCE", "Material",
                        "Die Mapping 시작 Wafer의 물리 세대 ID를 확인할 수 없습니다. wafer=" + _expectedWaferId);

                result = ValidateStoredAlignResultForCurrentMode();
                if (result != 0)
                    return result;

                _frameSpec = ResolveFrameSpecForWafer(_wafer);
                _sourceMap = null;
                _dieMap = null;
                _waferMap = null;
                _mappingAnchorEntry = null;
                _mappingAnchorMapX = -1;
                _mappingAnchorMapY = -1;
                _mappingAnchorExpectedX = 0.0;
                _mappingAnchorExpectedY = 0.0;
                _createdDieCount = 0;

                result = RestoreStageAlignRuntimeResultFromMaterial();
                if (result != 0) return result;

                string thetaReason;
                if (!MaterialStateService.IsInputStageThetaAlignComplete(_wafer, out thetaReason))
                {
                    // 수동 폴백 웨이퍼는 리뷰까지 가야 사용자가 T CORRECTION을 할 수 있는데
                    // 리뷰는 매핑된 맵을 전제한다. 알람으로 세우지 않고 현재 StageT 기준 임시
                    // T를 채워 매핑을 진행한다 — 확정 잠금은 유지되어 Review에서 T CORRECTION을
                    // 완료해야만 CONFIRM이 열린다.
                    if (_wafer != null && _wafer.InputStageAlignManualFallback)
                    {
                        double nominalReferenceT = Stage.ResolveWaferAlignReferenceT();
                        double nominalCorrectedT = Stage.StageT != null
                            ? Stage.StageT.ActualPosition
                            : nominalReferenceT;
                        double nominalOffsetT = nominalCorrectedT - nominalReferenceT;
                        Stage.ApplyWaferAlignThetaResult(nominalReferenceT, nominalCorrectedT, nominalOffsetT);
                        MaterialStateService.SaveInputStageThetaAlignNominal(
                            _wafer, nominalReferenceT, nominalCorrectedT, nominalOffsetT);
                        WriteLog("InputStageDieMappingSequence",
                            "Manual-fallback wafer without theta result — nominal theta engaged for mapping." +
                            " referenceT=" + nominalReferenceT.ToString("F6") +
                            ", correctedT=" + nominalCorrectedT.ToString("F6") +
                            ", offsetT=" + nominalOffsetT.ToString("F6") +
                            ", reason=" + thetaReason + " - Check");
                        EventLogger.Write(EventKind.Warning, "SYS", "IN-STAGE-DIEMAP-THETA-NOMINAL", Name,
                            "수동 폴백 웨이퍼 — T 보정 결과가 없어 임시 T로 매핑을 진행합니다. Review에서 T CORRECTION을 완료해야 확정할 수 있습니다.");
                    }
                    else
                    {
                        return Fail("IN-STAGE-DIEMAP-THETA-ALIGN", Stage.Name,
                            "Die Mapping 전에 InputStage T 보정이 완료되어야 합니다. " + thetaReason);
                    }
                }

                if (!Stage.IsWaferAlignThetaResultReady(out thetaReason))
                    return Fail("IN-STAGE-DIEMAP-THETA-RUNTIME", Stage.Name,
                        "Die Mapping 전에 저장된 T 보정값을 Stage 런타임에서 검증할 수 있어야 합니다. " + thetaReason);

                if (Stage.PitchX == 0.0 || Stage.PitchY == 0.0)
                    return Fail("IN-STAGE-DIEMAP-ALIGN", Stage.Name,
                        BuildStageAlignMissingReason());

                if (_frameSpec == null)
                    return Fail("IN-STAGE-DIEMAP-SPEC", "Material",
                        "TapeFrame spec was not found. waferId=" + _wafer.WaferId +
                        ", specName=" + _wafer.TapeFrameSpecName);
                if (_frameSpec.DieMapX <= 0 || _frameSpec.DieMapY <= 0)
                    return Fail("IN-STAGE-DIEMAP-SPEC", "Material",
                        "TapeFrame die map is invalid. specName=" + _frameSpec.Name +
                        ", dieMapX=" + _frameSpec.DieMapX + ", dieMapY=" + _frameSpec.DieMapY);

                _sourceMap = ResolveSourceInputDieMap(_wafer, _frameSpec);
                if (!IsUsableSourceMap(_sourceMap))
                {
                    return Fail(
                        string.IsNullOrWhiteSpace(LastSourceInputDieMapFailureCode)
                            ? "IN-STAGE-DIEMAP-SOURCE-MAP"
                            : LastSourceInputDieMapFailureCode,
                        "InputStageDieMappingSequence",
                        (string.IsNullOrWhiteSpace(LastSourceInputDieMapFailureCode)
                            ? "Input die map is not available. Create and save an input wafer map from Recipe > INPUT MAP CREATE first. "
                            : "") +
                        LastSourceInputDieMapFailure);
                }

                string anchorReason;
                if (!TryResolveMappingAnchor(_sourceMap, out anchorReason))
                    return Fail("IN-STAGE-DIEMAP-ANCHOR", "InputStageDieMappingSequence", anchorReason);

                if (Options.RequireVisionAlign && Stage.Vision == null &&
                    (!IsSimulationOrDryRun() || AutoVisionRequestService.IsRealVisionInSimulationActive()))
                    return Fail("IN-STAGE-DIEMAP-VISION", Stage.Name, "Vision client is required but not available.");

                _mappedPoints.Clear();
                _dieMapCenterX = 0.0;
                _dieMapCenterY = 0.0;
                _centerDieTargetX = 0.0;
                _centerDieTargetY = 0.0;
                _mappingAnchorDetectedX = 0.0;
                _mappingAnchorDetectedY = 0.0;
                _mappingFineOffsetX = 0.0;
                _mappingFineOffsetY = 0.0;
                if (Context != null && Context.Bus != null)
                {
                    Context.Bus.Reset("InputStageDieMapped");
                    Context.Bus.Reset("InputStageFinishComplete");
                    Context.Bus.Reset("InputStageReady");
                }
                CurrentStep = InputStageDieMappingStep.MoveNeedleZSafeBeforeMapping;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CHECK-EX", "InputStageDieMappingSequence", "Die mapping unit check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleZSafeBeforeMappingAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!Options.EnableMotion || Stage == null || Stage.Recipe == null || Stage.Recipe.NeedleZ == null)
                {
                    CurrentStep = InputStageDieMappingStep.MoveVisionProcessBeforeMapping;
                    return 0;
                }

                double target = Stage.Recipe.NeedleZ.AvoidPosition;
                if (!CanSkipAxisMoveCommand(Stage.NeedleZ, target))
                {
                    int result = await MoveAxisAndWaitAsync(
                        WaferStageAxis.NeedleZ,
                        target,
                        "Die Mapping 시작 전 NeedleZ Avoid",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    WriteLog("InputStageDieMappingSequence",
                        "Die Mapping 시작 전 NeedleZ를 Avoid 위치로 복귀했습니다. target=" +
                        target.ToString("F3") + " - Ok");
                }

                CurrentStep = InputStageDieMappingStep.MoveVisionProcessBeforeMapping;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "IN-STAGE-DIEMAP-NEEDLEZ-AVOID-EX",
                    Stage != null ? Stage.Name : "InputStageUnit",
                    "Die Mapping 시작 전 NeedleZ Avoid 이동 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        private int RestoreStageAlignRuntimeResultFromMaterial()
        {
            try
            {
                if (Stage == null)
                    return 0;

                if (_wafer == null)
                    return 0;

                if (_wafer.HasInputStageAlignResult &&
                    _wafer.InputStageAlignPitchX > 0.0 &&
                    _wafer.InputStageAlignPitchY > 0.0)
                {
                    NormalizeStoredAlignPitchToFrameSpec();
                    Stage.ApplyWaferAlignResult(
                        _wafer.InputStageAlignOriginX,
                        _wafer.InputStageAlignOriginY,
                        _wafer.InputStageAlignPitchX,
                        _wafer.InputStageAlignPitchY,
                        _wafer.InputStageAlignOffsetX,
                        _wafer.InputStageAlignOffsetY);
                }

                if (_wafer.HasInputStageThetaAlignResult)
                    Stage.ApplyWaferAlignThetaResult(
                        _wafer.InputStageAlignReferenceT,
                        _wafer.InputStageAlignCorrectedT,
                        _wafer.InputStageAlignOffsetT);

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping 시작 전 저장된 InputStage Align 결과를 Stage 런타임에 복구했습니다. waferId=" +
                    _wafer.WaferId +
                    ", originX=" + _wafer.InputStageAlignOriginX.ToString("F6") +
                    ", originY=" + _wafer.InputStageAlignOriginY.ToString("F6") +
                    ", pitchX=" + _wafer.InputStageAlignPitchX.ToString("F6") +
                    ", pitchY=" + _wafer.InputStageAlignPitchY.ToString("F6") +
                    ", offsetX=" + _wafer.InputStageAlignOffsetX.ToString("F6") +
                    ", offsetY=" + _wafer.InputStageAlignOffsetY.ToString("F6") + " - Ok");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-ALIGN-RESTORE", Stage != null ? Stage.Name : "InputStageUnit",
                    "저장된 InputStage Align 결과를 Stage 런타임에 복구하지 못했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildStageAlignMissingReason()
        {
            try
            {
                string waferId = _wafer != null ? _wafer.WaferId : "-";
                bool hasAlign = _wafer != null && _wafer.HasInputStageAlignResult;
                double waferPitchX = _wafer != null ? _wafer.InputStageAlignPitchX : 0.0;
                double waferPitchY = _wafer != null ? _wafer.InputStageAlignPitchY : 0.0;
                double framePitchX = _frameSpec != null
                    ? DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeX, _frameSpec.PitchX)
                    : 0.0;
                double framePitchY = _frameSpec != null
                    ? DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeY, _frameSpec.PitchY)
                    : 0.0;

                return "InputStage Align 결과가 없어 Die Mapping을 시작할 수 없습니다. " +
                       "Die Mapping 전에 Wafer Align을 완료해야 합니다. " +
                       "waferId=" + waferId +
                       ", stagePitchX=" + Stage.PitchX +
                       ", stagePitchY=" + Stage.PitchY +
                       ", waferHasAlign=" + hasAlign +
                       ", waferPitchX=" + waferPitchX +
                       ", waferPitchY=" + waferPitchY +
                       ", frameSpec=" + (_frameSpec != null ? _frameSpec.Name : "-") +
                       ", frameGapX=" + (_frameSpec != null ? _frameSpec.PitchX : 0.0) +
                       ", frameGapY=" + (_frameSpec != null ? _frameSpec.PitchY : 0.0) +
                       ", frameCenterStepX=" + framePitchX +
                       ", frameCenterStepY=" + framePitchY;
            }
            catch (Exception ex)
            {
                return "InputStage Align 결과가 없어 Die Mapping을 시작할 수 없습니다. 상세 원인 생성 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        private int CheckMarkPoint(InputStageDieMapMarkPoint point)
        {
            try
            {
                if (point == null || !point.Enabled)
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point is disabled or missing.");
                if (string.IsNullOrWhiteSpace(point.Name))
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point name is empty.");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-POINT-EX", Stage.Name, "Die map mark point check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveVisionProcessBeforeMappingAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options == null || !Options.EnableMotion || Stage == null || Stage.Recipe == null)
                {
                    CurrentStep = InputStageDieMappingStep.MoveCenterPoint;
                    return 0;
                }

                Stage.Recipe.EnsurePositionObjects();

                int result = await PrepareVisionProcessPlaneForMappingAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureWaferAlignThetaPositionAsync("Die Mapping 시작 전 StageT 보정 위치", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping 시작 전 InputStage를 Process 기준 위치로 이동했습니다. visionX=" +
                    Stage.Recipe.VisionX.ProcessPosition.ToString("F3") +
                    ", stageY=" + Stage.Recipe.WaferY.ProcessPosition.ToString("F3") + " - Ok");

                CurrentStep = InputStageDieMappingStep.MoveCenterPoint;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "IN-STAGE-DIEMAP-PROCESS-POS-EX",
                    Stage != null ? Stage.Name : "InputStageUnit",
                    "Die Mapping 시작 전 Process 위치 이동 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareVisionProcessPlaneForMappingAsync(CancellationToken ct)
        {
            Stage.Recipe.EnsurePositionObjects();

            int result = await MoveHeadZAxesAvoidBeforeProcessPlaneMoveAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            bool stageZAtProcess = Stage.Recipe.WaferZ != null &&
                CanSkipAxisMoveCommand(ResolveStageAxis(WaferStageAxis.WaferExpandingZ), Stage.Recipe.WaferZ.ProcessPosition);

            if (stageZAtProcess)
            {
                result = await MoveNeedleXToMappingCenterAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping 시작 전 StageZ가 이미 Process 위치입니다. StageZ Avoid/Process 재이동을 생략합니다. " +
                    BuildAxisState(WaferStageAxis.WaferExpandingZ, Stage.Recipe.WaferZ.ProcessPosition) + " - Ok");

                return await MoveVisionProcessPlaneAxesAsync(ct).ConfigureAwait(false);
            }

            result = await EnsureStageTFixedBeforeExpanderZMoveAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (Stage.Recipe.WaferZ != null)
            {
                result = await MoveAxisAndWaitAsync(
                    WaferStageAxis.WaferExpandingZ,
                    Stage.Recipe.WaferZ.AvoidPosition,
                    "Die Mapping 시작 전 StageZ Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            result = await MoveNeedleXToMappingCenterAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveVisionProcessPlaneAxesAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (Stage.Recipe.WaferZ != null)
            {
                result = await MoveAxisAndWaitAsync(
                    WaferStageAxis.WaferExpandingZ,
                    Stage.Recipe.WaferZ.ProcessPosition,
                    "Die Mapping 시작 전 StageZ Process",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private async Task<int> MoveNeedleXToMappingCenterAsync(CancellationToken ct)
        {
            double target = Stage.ResolveNeedleWorkAreaCenterX();
            return await MoveAxisAndWaitAsync(
                WaferStageAxis.NeedleX,
                target,
                "Die Mapping 시작 전 NeedleX Stage Center",
                ct).ConfigureAwait(false);
        }

        private async Task<int> MoveHeadZAxesAvoidBeforeProcessPlaneMoveAsync(CancellationToken ct)
        {
            Stage.Recipe.EnsurePositionObjects();

            int result = await MoveAxisAndWaitAsync(
                WaferStageAxis.NeedleZ,
                Stage.Recipe.NeedleZ.AvoidPosition,
                "Die Mapping 시작 전 NeedleZ Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveAxisAndWaitAsync(
                WaferStageAxis.EjectPinZ,
                Stage.Recipe.EjectPinZ.AvoidPosition,
                "Die Mapping 시작 전 EjectPinZ Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private async Task<int> MoveVisionProcessPlaneAxesAsync(CancellationToken ct)
        {
            int result = await MoveAxisAndWaitAsync(
                WaferStageAxis.WaferY,
                Stage.Recipe.WaferY.ProcessPosition,
                "Die Mapping 시작 전 StageY Process",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveAxisAndWaitAsync(
                WaferStageAxis.VisionX,
                Stage.Recipe.VisionX.ProcessPosition,
                "Die Mapping 시작 전 VisionX Process",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private async Task<int> EnsureStageTFixedBeforeExpanderZMoveAsync(CancellationToken ct)
        {
            Stage.Recipe.EnsurePositionObjects();

            if (IsStageTAtExpanderZFixedPosition())
                return 0;

            WriteLog("InputStageDieMappingSequence",
                "Die Mapping 시작 전 StageZ 이동을 위해 StageT를 고정 위치로 복귀합니다. " +
                BuildAxisState(WaferStageAxis.WaferT, 0.0) + " - Start");

            return await MoveAxisAndWaitAsync(
                WaferStageAxis.WaferT,
                0.0,
                "Die Mapping 시작 전 StageT Home",
                ct).ConfigureAwait(false);
        }

        private bool IsStageTAtExpanderZFixedPosition()
        {
            try
            {
                Stage.Recipe.EnsurePositionObjects();

                QMC.Common.Motion.BaseAxis stageT = ResolveStageAxis(WaferStageAxis.WaferT);
                if (stageT == null || Stage.Recipe.WaferT == null)
                    return false;

                return IsAxisInPosition(stageT, 0.0) ||
                    IsAxisInPosition(stageT, Stage.Recipe.WaferT.AvoidPosition) ||
                    IsAxisInPosition(stageT, Stage.Recipe.WaferT.LoadPosition) ||
                    IsAxisInPosition(stageT, Stage.Recipe.WaferT.UnloadPosition) ||
                    IsAxisInPosition(stageT, Stage.Recipe.WaferT.ReadyPosition) ||
                    IsAxisInPosition(stageT, Stage.Recipe.WaferT.ProcessPosition);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> MoveMarkPointAsync(InputStageDieMapMarkPoint point, InputStageDieMappingStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (point == null)
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point is null.");

                if (Options.EnableMotion)
                {
                    int thetaResult = await EnsureWaferAlignThetaPositionAsync(point.Name + " mark 이동 전 StageT 보정 위치", ct).ConfigureAwait(false);
                    if (thetaResult != 0)
                        return thetaResult;

                    string areaReason;
                    if (!Stage.IsInputStageWorkPointInArea(point.VisionXPosition, point.StageYPosition, out areaReason))
                        return Fail("IN-STAGE-DIEMAP-WORK-AREA", Stage.Name,
                            point.Name + " mark target is outside input stage work area. " + areaReason);

                    int result = await MoveVisionXYPointSafelyAsync(
                        point.VisionXPosition,
                        point.StageYPosition,
                        point.Name,
                        ct).ConfigureAwait(false);
                    if (result != 0) return result;

                }

                CurrentStep = nextStep;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-MOVE-EX", Stage.Name, point != null ? point.Name + " move failed: " + ex.Message : "Mark point move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveVisionXYPointSafelyAsync(double targetX, double targetY, string description, CancellationToken ct)
        {
            double currentX = Stage.CameraX != null ? Stage.CameraX.ActualPosition : targetX;
            double currentY = Stage.StageY != null ? Stage.StageY.ActualPosition : targetY;

            string xFirstReason;
            if (Stage.IsInputStageWorkPointInArea(targetX, currentY, out xFirstReason))
            {
                int result = await MoveAxisAndWaitAsync(WaferStageAxis.VisionX, targetX, description + " VisionX", ct).ConfigureAwait(false);
                if (result != 0) return result;
                return await MoveAxisAndWaitAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct).ConfigureAwait(false);
            }

            string yFirstReason;
            if (Stage.IsInputStageWorkPointInArea(currentX, targetY, out yFirstReason))
            {
                int result = await MoveAxisAndWaitAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct).ConfigureAwait(false);
                if (result != 0) return result;
                return await MoveAxisAndWaitAsync(WaferStageAxis.VisionX, targetX, description + " VisionX", ct).ConfigureAwait(false);
            }

            int entryResult = await MoveVisionXYPointViaWorkCenterAsync(
                targetX,
                targetY,
                description,
                xFirstReason,
                yFirstReason,
                ct).ConfigureAwait(false);
            if (entryResult != int.MinValue)
                return entryResult;

            return Fail("IN-STAGE-DIEMAP-WORK-AREA-PATH", Stage.Name,
                description + " mark has no safe L-path inside input stage work area. currentX=" + currentX.ToString("F3") +
                ", currentY=" + currentY.ToString("F3") +
                ", targetX=" + targetX.ToString("F3") +
                ", targetY=" + targetY.ToString("F3") +
                ", xFirst=" + xFirstReason +
                ", yFirst=" + yFirstReason);
        }

        private async Task<int> MoveVisionXYPointViaWorkCenterAsync(
            double targetX,
            double targetY,
            string description,
            string xFirstReason,
            string yFirstReason,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                double entryX = Stage.ResolveWorkAreaCenterX();
                double entryY = Stage.ResolveWorkAreaCenterY();

                string entryReason;
                if (!Stage.IsInputStageWorkPointInArea(entryX, entryY, out entryReason))
                {
                    WriteLog("InputStageDieMappingSequence",
                        description + " work area center entry is not available. entryX=" + entryX.ToString("F6") +
                        ", entryY=" + entryY.ToString("F6") +
                        ", reason=" + entryReason +
                        ", xFirst=" + xFirstReason +
                        ", yFirst=" + yFirstReason + " - Skip");
                    return int.MinValue;
                }

                WriteLog("InputStageDieMappingSequence",
                    description + " mark has no direct L-path. Enter work center first. entryX=" +
                    entryX.ToString("F6") +
                    ", entryY=" + entryY.ToString("F6") +
                    ", targetX=" + targetX.ToString("F6") +
                    ", targetY=" + targetY.ToString("F6") + " - Start");

                int result = await MoveAxisAndWaitAsync(WaferStageAxis.WaferY, entryY, description + " Entry StageY", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveAxisAndWaitAsync(WaferStageAxis.VisionX, targetX, description + " Entry VisionX", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveAxisAndWaitAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-WORK-AREA-ENTRY-EX", Stage != null ? Stage.Name : "InputStageUnit",
                    description + " 작업영역 진입 경유 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveCenterPointAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options.EnableMotion)
                {
                    double centerX = _mappingAnchorExpectedX;
                    double centerY = _mappingAnchorExpectedY;

                    string areaReason;
                    if (!Stage.IsInputStageWorkPointInArea(centerX, centerY, out areaReason))
                        return Fail("IN-STAGE-DIEMAP-CENTER-WORK-AREA", Stage.Name,
                            "Die Mapping 예상 Anchor 위치가 작업 영역 밖입니다. anchorMapX=" + _mappingAnchorMapX +
                            ", anchorMapY=" + _mappingAnchorMapY +
                            ", targetX=" +
                            centerX.ToString("F6") +
                            ", targetY=" + centerY.ToString("F6") +
                            ", reason=" + areaReason);

                    WriteLog("InputStageDieMappingSequence",
                        "Move to predicted Die Mapping anchor. formula=" +
                        (IsExternalInputDieMap(_sourceMap)
                            ? "expected=alignOrigin+(anchorSourcePos-sourceMapOrigin)"
                            : "expected=alignOrigin+anchorMapIndex*pitch") +
                        ", anchorMapX=" + _mappingAnchorMapX +
                        ", anchorMapY=" + _mappingAnchorMapY +
                        ", alignOriginX=" + _wafer.InputStageAlignOriginX.ToString("F6") +
                        ", alignOriginY=" + _wafer.InputStageAlignOriginY.ToString("F6") +
                        ", sourceMapOriginX=" + _sourceMap.OriginX.ToString("F6") +
                        ", sourceMapOriginY=" + _sourceMap.OriginY.ToString("F6") +
                        ", anchorSourcePosX=" + (_mappingAnchorEntry != null ? _mappingAnchorEntry.PosX.ToString("F6") : "-") +
                        ", anchorSourcePosY=" + (_mappingAnchorEntry != null ? _mappingAnchorEntry.PosY.ToString("F6") : "-") +
                        ", targetX=" + centerX.ToString("F6") +
                        ", targetY=" + centerY.ToString("F6") + " - Start");

                    int result = await MoveVisionXYPointSafelyAsync(
                        centerX,
                        centerY,
                        "Die Mapping predicted Anchor",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                CurrentStep = InputStageDieMappingStep.FindCenterPoint;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CENTER-MOVE-EX", Stage != null ? Stage.Name : "InputStageUnit",
                    "Die Mapping 예상 Anchor 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> FindCenterPointAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                double baseX = _mappingAnchorExpectedX;
                double baseY = _mappingAnchorExpectedY;

                // 수동 폴백 얼라인 웨이퍼는 비전이 다이를 잡지 못해 작업자가 수동 정렬한 상태다.
                // Anchor 검출/재시도/주변 탐색은 성립하지 않으므로 시도 없이 예상 위치를 Anchor로
                // 채택하고(fineOffset=0), 좌표 보정은 Review의 다이 검출/Offset 적용이 담당한다.
                if (_wafer != null && _wafer.InputStageAlignManualFallback)
                {
                    _mappingAnchorDetectedX = baseX;
                    _mappingAnchorDetectedY = baseY;
                    _mappingFineOffsetX = 0.0;
                    _mappingFineOffsetY = 0.0;
                    ResolveDetectedMapCenter(out _dieMapCenterX, out _dieMapCenterY);
                    _mappedPoints[VisionAlignTargetIds.Center] = new MappedMarkPoint
                    {
                        Name = VisionAlignTargetIds.Center,
                        X = _mappingAnchorDetectedX,
                        Y = _mappingAnchorDetectedY,
                        OffsetX = 0.0,
                        OffsetY = 0.0
                    };

                    WriteLog("InputStageDieMappingSequence",
                        "Die Mapping anchor manual fallback engaged. anchor vision skipped on manual-fallback aligned wafer." +
                        " anchorMapX=" + _mappingAnchorMapX +
                        ", anchorMapY=" + _mappingAnchorMapY +
                        ", adoptedAnchorX=expectedX=" + baseX.ToString("F6") +
                        ", adoptedAnchorY=expectedY=" + baseY.ToString("F6") +
                        ", fineOffsetX=0, fineOffsetY=0 - Check");
                    EventLogger.Write(EventKind.Warning, "SYS", "IN-STAGE-DIEMAP-MANUAL-FALLBACK", Name,
                        "수동 폴백 얼라인 웨이퍼 — Anchor 검출을 생략하고 예상 위치 그대로 진행합니다. Review 화면에서 다이 검출과 Offset 적용으로 좌표를 보정하십시오.");

                    CurrentStep = InputStageDieMappingStep.CalculateDieMap;
                    return 0;
                }

                double requestStartX = Stage.CameraX != null ? Stage.CameraX.ActualPosition : baseX;
                double requestStartY = Stage.StageY != null ? Stage.StageY.ActualPosition : baseY;
                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping anchor vision request. anchorMapX=" + _mappingAnchorMapX +
                    ", anchorMapY=" + _mappingAnchorMapY +
                    ", expectedX=" + baseX.ToString("F6") +
                    ", expectedY=" + baseY.ToString("F6") +
                    ", requestStartActualX=" + requestStartX.ToString("F6") +
                    ", requestStartActualY=" + requestStartY.ToString("F6") +
                    ", targetId=" + ResolveTargetId() + " - Start");
                VisionAlignResult vision = await RequestVisionPcOffsetWithRetryAsync(ResolveTargetId(), VisionAlignTargetIds.Center, ct).ConfigureAwait(false);
                if (vision == null && !_hybridVirtualFrameActive)
                {
                    vision = await SearchVisionMarkAroundCurrentPointAsync(
                        ResolveTargetId(),
                        VisionAlignTargetIds.Center,
                        "Die Mapping predicted Anchor",
                        baseX,
                        baseY,
                        ct).ConfigureAwait(false);
                }

                if (vision == null)
                {
                    if (_hybridVirtualFrameActive)
                        return Fail("IN-STAGE-DIEMAP-HYBRID-VISION", "Vision",
                            "Die Mapping 예상 Anchor 다이를 찾지 못했습니다. HybridRealVisionSimMotion에서는 가상 X/Y 이동으로 실제 Vision 화면이 바뀌지 않으므로 동일 화면 통신 재시도 후 주변 탐색을 수행하지 않습니다.");

                    return Fail("IN-STAGE-DIEMAP-CENTER-VISION", "Vision",
                        "Die Mapping 예상 Anchor 다이를 찾지 못했습니다. Align Origin 예상 위치와 반 피치 미만 Fine 탐색을 모두 실패했습니다. " +
                        "anchorMapX=" + _mappingAnchorMapX +
                        ", anchorMapY=" + _mappingAnchorMapY +
                        ", expectedX=" + baseX.ToString("F6") +
                        ", expectedY=" + baseY.ToString("F6"));
                }

                double inputDeltaX;
                double inputDeltaY;
                double moveDeltaX;
                double moveDeltaY;
                ResolveInputCameraMotorCorrection(
                    vision,
                    "Die Mapping predicted Anchor",
                    out inputDeltaX,
                    out inputDeltaY,
                    out moveDeltaX,
                    out moveDeltaY);

                double effectiveMoveDeltaX = moveDeltaX;
                double effectiveMoveDeltaY = moveDeltaY;
                double savedAlignOffsetX = 0.0;
                double savedAlignOffsetY = 0.0;
                if (_hybridVirtualFrameActive)
                {
                    if (_wafer == null ||
                        !_wafer.HasInputStageAlignResult ||
                        !IsFiniteNumber(_wafer.InputStageAlignOffsetX) ||
                        !IsFiniteNumber(_wafer.InputStageAlignOffsetY))
                    {
                        return Fail("IN-STAGE-DIEMAP-HYBRID-ALIGN-OFFSET", "Material",
                            "Hybrid Die Mapping에 필요한 저장 Align X/Y 보정값이 없거나 유효하지 않습니다.");
                    }

                    savedAlignOffsetX = _wafer.InputStageAlignOffsetX;
                    savedAlignOffsetY = _wafer.InputStageAlignOffsetY;
                    effectiveMoveDeltaX = moveDeltaX - savedAlignOffsetX;
                    effectiveMoveDeltaY = moveDeltaY - savedAlignOffsetY;
                }

                double currentX = Stage.CameraX != null ? Stage.CameraX.ActualPosition : baseX;
                double currentY = Stage.StageY != null ? Stage.StageY.ActualPosition : baseY;
                _mappingAnchorDetectedX = currentX + effectiveMoveDeltaX;
                _mappingAnchorDetectedY = currentY + effectiveMoveDeltaY;
                _mappingFineOffsetX = _mappingAnchorDetectedX - baseX;
                _mappingFineOffsetY = _mappingAnchorDetectedY - baseY;
                string fineLimitReason;
                if (!IsMappingFineOffsetWithinLimit(
                    _mappingFineOffsetX,
                    _mappingFineOffsetY,
                    out fineLimitReason))
                {
                    return Fail("IN-STAGE-DIEMAP-FINE-OFFSET-LIMIT", "InputStageDieMappingSequence",
                        "Die Mapping 예상 Anchor와 검출 Anchor의 미세 보정값이 허용 범위를 벗어났습니다. " +
                        fineLimitReason +
                        ", anchorMapX=" + _mappingAnchorMapX +
                        ", anchorMapY=" + _mappingAnchorMapY +
                        ", expectedX=" + baseX.ToString("F6") +
                        ", expectedY=" + baseY.ToString("F6") +
                        ", detectedX=" + _mappingAnchorDetectedX.ToString("F6") +
                        ", detectedY=" + _mappingAnchorDetectedY.ToString("F6") +
                        ". Wafer Align 결과 또는 수동 Map Anchor를 확인하십시오.");
                }

                ResolveDetectedMapCenter(out _dieMapCenterX, out _dieMapCenterY);

                _mappedPoints[VisionAlignTargetIds.Center] = new MappedMarkPoint
                {
                    Name = VisionAlignTargetIds.Center,
                    X = _mappingAnchorDetectedX,
                    Y = _mappingAnchorDetectedY,
                    OffsetX = _mappingFineOffsetX,
                    OffsetY = _mappingFineOffsetY
                };

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping Anchor를 찾았습니다. anchorMapX=" + _mappingAnchorMapX +
                    ", anchorMapY=" + _mappingAnchorMapY +
                    ", expectedAnchorX=" + baseX.ToString("F6") +
                    ", expectedAnchorY=" + baseY.ToString("F6") +
                    ", visionRequestActualX=" + currentX.ToString("F6") +
                    ", visionRequestActualY=" + currentY.ToString("F6") +
                    ", visionDx=" + vision.DeltaX.ToString("F6") +
                    ", visionDy=" + vision.DeltaY.ToString("F6") +
                    ", inputDeltaX=" + inputDeltaX.ToString("F6") +
                    ", inputDeltaY=" + inputDeltaY.ToString("F6") +
                    ", rawMoveDeltaX=" + moveDeltaX.ToString("F6") +
                    ", rawMoveDeltaY=" + moveDeltaY.ToString("F6") +
                    ", savedAlignOffsetX=" + savedAlignOffsetX.ToString("F6") +
                    ", savedAlignOffsetY=" + savedAlignOffsetY.ToString("F6") +
                    ", effectiveMoveDeltaX=rawMoveDeltaX-savedAlignOffsetX=" + effectiveMoveDeltaX.ToString("F6") +
                    ", effectiveMoveDeltaY=rawMoveDeltaY-savedAlignOffsetY=" + effectiveMoveDeltaY.ToString("F6") +
                    ", detectedAnchorX=currentX+effectiveMoveDeltaX=" + _mappingAnchorDetectedX.ToString("F6") +
                    ", detectedAnchorY=currentY+effectiveMoveDeltaY=" + _mappingAnchorDetectedY.ToString("F6") +
                    ", fineOffsetX=detectedAnchorX-expectedAnchorX=" + _mappingFineOffsetX.ToString("F6") +
                    ", fineOffsetY=detectedAnchorY-expectedAnchorY=" + _mappingFineOffsetY.ToString("F6") +
                    ", resolvedMapCenterX=" + _dieMapCenterX.ToString("F6") +
                    ", resolvedMapCenterY=" + _dieMapCenterY.ToString("F6") +
                    ", mode=" + ResolveVisionMotionModeName() +
                    ", limit=" + fineLimitReason + " - Ok");

                CurrentStep = InputStageDieMappingStep.CalculateDieMap;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CENTER-VISION-EX", "Vision",
                    "Die Mapping 예상 Anchor 다이 탐색 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisAndWaitAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            int result = await MoveAxisCommandAsync(axis, target, description, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private async Task<int> EnsureWaferAlignThetaPositionAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Stage == null)
                    return 0;

                string readyReason;
                if (!Stage.IsWaferAlignThetaResultReady(out readyReason))
                    return Fail("IN-STAGE-DIEMAP-THETA-ALIGN", Stage.Name,
                        description + " 실패. " + readyReason);

                double targetT;
                if (!Stage.TryResolveWaferAlignThetaTarget(out targetT))
                    return Fail("IN-STAGE-DIEMAP-THETA-TARGET", Stage.Name,
                        description + " 실패. StageT 보정 목표값을 찾을 수 없습니다.");

                if (Stage.StageT == null)
                    return Fail("IN-STAGE-DIEMAP-THETA-AXIS", "InputStageUnit",
                        description + " 실패. StageT 축 정보가 없습니다.");

                int result = await MoveAxisAndWaitAsync(
                    WaferStageAxis.WaferT,
                    targetT,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("InputStageDieMappingSequence",
                    description + " 복귀 완료. targetT=" + targetT.ToString("F6") +
                    ", actualT=" + Stage.StageT.ActualPosition.ToString("F6") +
                    ", offsetT=" + Stage.WaferAlignOffsetT.ToString("F6") + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-THETA-POS-EX", Stage != null ? Stage.Name : "InputStageUnit",
                    description + " 확인/복귀 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ResolveInputCameraMotorCorrection(
            VisionAlignResult result,
            string description,
            out double inputDeltaX,
            out double inputDeltaY,
            out double moveDeltaX,
            out double moveDeltaY)
        {
            inputDeltaX = 0.0;
            inputDeltaY = 0.0;
            moveDeltaX = 0.0;
            moveDeltaY = 0.0;
            if (result == null)
                return;

            // Wafer 채널 라이브 Delta는 카메라 순수 오프셋(raw)이므로 InputToBottomOffset 감산 없이 그대로 사용한다.
            inputDeltaX = result.DeltaX;
            inputDeltaY = result.DeltaY;
            moveDeltaX = inputDeltaX;
            moveDeltaY = -inputDeltaY;

            WriteLog("InputStageDieMappingSequence",
                "Input camera motor correction formula. description=" + description +
                ", rawVisionDx=" + result.DeltaX.ToString("F6") +
                ", rawVisionDy=" + result.DeltaY.ToString("F6") +
                ", inputDeltaX=rawDx=" + inputDeltaX.ToString("F6") +
                ", inputDeltaY=rawDy=" + inputDeltaY.ToString("F6") +
                ", moveDeltaX=inputDeltaX=" + moveDeltaX.ToString("F6") +
                ", moveDeltaY=-inputDeltaY=" + moveDeltaY.ToString("F6") + " - Ok");
        }

        private bool TryResolveMappingAnchor(DieMap sourceMap, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (!IsUsableSourceMap(sourceMap))
                {
                    reason = "Die Mapping Anchor를 계산할 Source Map이 없습니다.";
                    return false;
                }

                DieMapGenerator.Normalize(sourceMap);
                bool external = IsExternalInputDieMap(sourceMap);
                double pitchX = sourceMap.PitchX > 0.0 ? sourceMap.PitchX : Math.Abs(ResolvePitchX());
                double pitchY = sourceMap.PitchY > 0.0 ? sourceMap.PitchY : Math.Abs(ResolvePitchY());
                int centerMapX = Math.Max(0, sourceMap.DieMapX / 2);
                int centerMapY = Math.Max(0, sourceMap.DieMapY / 2);
                DieMapEntry best = null;
                int bestMapX = -1;
                int bestMapY = -1;
                double bestDistance = double.MaxValue;

                foreach (DieMapEntry entry in sourceMap.Entries)
                {
                    if (entry == null || !entry.IsTarget)
                        continue;

                    int mapX = DieMapGenerator.ResolveMapIndexX(entry);
                    int mapY = DieMapGenerator.ResolveMapIndexY(entry);
                    if (mapX < 0 || mapX >= sourceMap.DieMapX || mapY < 0 || mapY >= sourceMap.DieMapY)
                        continue;

                    double dx;
                    double dy;
                    if (external && IsFinite(entry.PosX) && IsFinite(entry.PosY))
                    {
                        dx = entry.PosX;
                        dy = entry.PosY;
                    }
                    else
                    {
                        dx = (mapX - centerMapX) * pitchX;
                        dy = (mapY - centerMapY) * pitchY;
                    }

                    double distance = (dx * dx) + (dy * dy);
                    bool better = best == null || distance < bestDistance - 1e-12;
                    if (!better && Math.Abs(distance - bestDistance) <= 1e-12)
                        better = mapY < bestMapY || (mapY == bestMapY && mapX < bestMapX);
                    if (!better)
                        continue;

                    best = entry;
                    bestMapX = mapX;
                    bestMapY = mapY;
                    bestDistance = distance;
                }

                if (best == null)
                {
                    reason = "Die Mapping Source Map에서 Anchor로 사용할 Target Die를 찾지 못했습니다.";
                    return false;
                }

                double alignOriginX = _wafer != null && _wafer.HasInputStageAlignResult
                    ? _wafer.InputStageAlignOriginX
                    : Stage.OriginX;
                double alignOriginY = _wafer != null && _wafer.HasInputStageAlignResult
                    ? _wafer.InputStageAlignOriginY
                    : Stage.OriginY;
                double sourceRelativeX = external
                    ? best.PosX - sourceMap.OriginX
                    : bestMapX * pitchX;
                double sourceRelativeY = external
                    ? best.PosY - sourceMap.OriginY
                    : bestMapY * pitchY;
                double expectedX = alignOriginX + sourceRelativeX;
                double expectedY = alignOriginY + sourceRelativeY;
                if (!IsFinite(expectedX) || !IsFinite(expectedY))
                {
                    reason = "Die Mapping 예상 Anchor 좌표가 유효하지 않습니다. expectedX=" + expectedX +
                             ", expectedY=" + expectedY;
                    return false;
                }

                _mappingAnchorEntry = best;
                _mappingAnchorMapX = bestMapX;
                _mappingAnchorMapY = bestMapY;
                _mappingAnchorExpectedX = expectedX;
                _mappingAnchorExpectedY = expectedY;

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping Anchor resolved. selection=nearest target to map geometric center" +
                    ", sourceMapExternal=" + external +
                    ", dieMapX=" + sourceMap.DieMapX +
                    ", dieMapY=" + sourceMap.DieMapY +
                    ", centerMapX=" + centerMapX +
                    ", centerMapY=" + centerMapY +
                    ", anchorMapX=" + bestMapX +
                    ", anchorMapY=" + bestMapY +
                    ", anchorIsCenterCell=" + (bestMapX == centerMapX && bestMapY == centerMapY) +
                    ", anchorSourcePosX=" + best.PosX.ToString("F6") +
                    ", anchorSourcePosY=" + best.PosY.ToString("F6") +
                    ", sourceMapOriginX=" + sourceMap.OriginX.ToString("F6") +
                    ", sourceMapOriginY=" + sourceMap.OriginY.ToString("F6") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") +
                    ", alignOriginX=" + alignOriginX.ToString("F6") +
                    ", alignOriginY=" + alignOriginY.ToString("F6") +
                    ", sourceRelativeX=" +
                    (external ? "anchorSourcePosX-sourceMapOriginX=" : "anchorMapX*pitchX=") +
                    sourceRelativeX.ToString("F6") +
                    ", sourceRelativeY=" +
                    (external ? "anchorSourcePosY-sourceMapOriginY=" : "anchorMapY*pitchY=") +
                    sourceRelativeY.ToString("F6") +
                    ", expectedAnchorX=alignOriginX+sourceRelativeX=" + expectedX.ToString("F6") +
                    ", expectedAnchorY=alignOriginY+sourceRelativeY=" + expectedY.ToString("F6") + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                reason = "Die Mapping Anchor 계산 중 예외가 발생했습니다: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private void ResolveDetectedMapCenter(out double centerX, out double centerY)
        {
            double alignOriginX = _wafer != null && _wafer.HasInputStageAlignResult
                ? _wafer.InputStageAlignOriginX
                : Stage.OriginX;
            double alignOriginY = _wafer != null && _wafer.HasInputStageAlignResult
                ? _wafer.InputStageAlignOriginY
                : Stage.OriginY;
            double finalOriginX = alignOriginX + _mappingFineOffsetX;
            double finalOriginY = alignOriginY + _mappingFineOffsetY;
            bool external = IsExternalInputDieMap(_sourceMap);
            double pitchX = _sourceMap != null && _sourceMap.PitchX > 0.0 ? _sourceMap.PitchX : Math.Abs(ResolvePitchX());
            double pitchY = _sourceMap != null && _sourceMap.PitchY > 0.0 ? _sourceMap.PitchY : Math.Abs(ResolvePitchY());

            centerX = external && _sourceMap != null
                ? finalOriginX - _sourceMap.OriginX
                : finalOriginX + pitchX * Math.Max(0, (_sourceMap != null ? _sourceMap.DieMapX : 1) - 1) / 2.0;
            centerY = external && _sourceMap != null
                ? finalOriginY - _sourceMap.OriginY
                : finalOriginY + pitchY * Math.Max(0, (_sourceMap != null ? _sourceMap.DieMapY : 1) - 1) / 2.0;

            WriteLog("InputStageDieMappingSequence",
                "Detected map center resolved. sourceMapExternal=" + external +
                ", alignOriginX=" + alignOriginX.ToString("F6") +
                ", alignOriginY=" + alignOriginY.ToString("F6") +
                ", fineOffsetX=" + _mappingFineOffsetX.ToString("F6") +
                ", fineOffsetY=" + _mappingFineOffsetY.ToString("F6") +
                ", finalOriginX=alignOriginX+fineOffsetX=" + finalOriginX.ToString("F6") +
                ", finalOriginY=alignOriginY+fineOffsetY=" + finalOriginY.ToString("F6") +
                ", centerFormula=" + (external
                    ? "finalOrigin-sourceMapOrigin"
                    : "finalOrigin+pitch*(mapCount-1)/2") +
                ", sourceMapOriginX=" + (_sourceMap != null ? _sourceMap.OriginX.ToString("F6") : "-") +
                ", sourceMapOriginY=" + (_sourceMap != null ? _sourceMap.OriginY.ToString("F6") : "-") +
                ", centerX=" + centerX.ToString("F6") +
                ", centerY=" + centerY.ToString("F6") + " - Ok");
        }

        private bool IsMappingFineOffsetWithinLimit(double offsetX, double offsetY, out string reason)
        {
            double pitchX = _sourceMap != null && _sourceMap.PitchX > 0.0 ? _sourceMap.PitchX : Math.Abs(ResolvePitchX());
            double pitchY = _sourceMap != null && _sourceMap.PitchY > 0.0 ? _sourceMap.PitchY : Math.Abs(ResolvePitchY());
            double configLimitX;
            double configLimitY;
            double effectiveLimitX;
            double effectiveLimitY;
            ResolveMappingFineOffsetLimits(
                pitchX,
                pitchY,
                out configLimitX,
                out configLimitY,
                out effectiveLimitX,
                out effectiveLimitY);

            bool finite = IsFinite(offsetX) && IsFinite(offsetY);
            bool within = finite && Math.Abs(offsetX) <= effectiveLimitX && Math.Abs(offsetY) <= effectiveLimitY;
            reason = "formula=effectiveLimit=min(configLimit,pitch/2-guard)" +
                     ", offsetX=" + offsetX.ToString("F6") +
                     ", offsetY=" + offsetY.ToString("F6") +
                     ", pitchX=" + pitchX.ToString("F6") +
                     ", pitchY=" + pitchY.ToString("F6") +
                     ", configLimitX=" + configLimitX.ToString("F6") +
                     ", configLimitY=" + configLimitY.ToString("F6") +
                     ", effectiveLimitX=" + effectiveLimitX.ToString("F6") +
                     ", effectiveLimitY=" + effectiveLimitY.ToString("F6") +
                     ", finite=" + finite +
                     ", within=" + within;
            WriteLog("InputStageDieMappingSequence", "Die Mapping Fine Offset limit check. " + reason +
                (within ? " - Ok" : " - Failed"));
            return within;
        }

        private void ResolveMappingFineOffsetLimits(
            double pitchX,
            double pitchY,
            out double configLimitX,
            out double configLimitY,
            out double effectiveLimitX,
            out double effectiveLimitY)
        {
            configLimitX = Stage != null ? Stage.ResolveDieMapFineOffsetLimitX() : 2.0;
            configLimitY = Stage != null ? Stage.ResolveDieMapFineOffsetLimitY() : 2.0;
            double halfPitchLimitX = Math.Max(0.001, Math.Abs(pitchX) / 2.0 - 0.001);
            double halfPitchLimitY = Math.Max(0.001, Math.Abs(pitchY) / 2.0 - 0.001);
            effectiveLimitX = Math.Min(configLimitX, halfPitchLimitX);
            effectiveLimitY = Math.Min(configLimitY, halfPitchLimitY);
        }

        private void WriteDieMapCoordinateAudit(
            DieMap sourceMap,
            Dictionary<string, DieMapEntry> sourceCellMap,
            bool sourceMapIsExternal,
            double alignOriginX,
            double alignOriginY,
            double finalOriginX,
            double finalOriginY,
            double pitchX,
            double pitchY)
        {
            if (_dieMap == null || _dieMap.Entries == null)
                return;

            WriteLog("InputStageDieMappingSequence",
                "Die Map coordinate audit start. entryCount=" + _dieMap.Entries.Count +
                ", external=" + sourceMapIsExternal +
                ", formulaOriginX=alignOriginX+fineOffsetX=" + finalOriginX.ToString("F6") +
                ", formulaOriginY=alignOriginY+fineOffsetY=" + finalOriginY.ToString("F6") +
                ", alignOriginX=" + alignOriginX.ToString("F6") +
                ", alignOriginY=" + alignOriginY.ToString("F6") +
                ", fineOffsetX=" + _mappingFineOffsetX.ToString("F6") +
                ", fineOffsetY=" + _mappingFineOffsetY.ToString("F6") +
                ", sourceMapOriginX=" + (sourceMap != null ? sourceMap.OriginX.ToString("F6") : "-") +
                ", sourceMapOriginY=" + (sourceMap != null ? sourceMap.OriginY.ToString("F6") : "-") +
                ", pitchX=" + pitchX.ToString("F6") +
                ", pitchY=" + pitchY.ToString("F6") + " - Start");

            for (int i = 0; i < _dieMap.Entries.Count; i++)
            {
                DieMapEntry entry = _dieMap.Entries[i];
                if (entry == null)
                    continue;

                int mapX = DieMapGenerator.ResolveMapIndexX(entry);
                int mapY = DieMapGenerator.ResolveMapIndexY(entry);
                DieMapEntry sourceEntry = FindSourceCell(sourceCellMap, mapX, mapY);
                double sourcePosX = sourceEntry != null ? sourceEntry.PosX : double.NaN;
                double sourcePosY = sourceEntry != null ? sourceEntry.PosY : double.NaN;
                double relativeX = sourceMapIsExternal && sourceEntry != null
                    ? sourcePosX - sourceMap.OriginX
                    : pitchX * mapX;
                double relativeY = sourceMapIsExternal && sourceEntry != null
                    ? sourcePosY - sourceMap.OriginY
                    : pitchY * mapY;

                WriteLog("InputStageDieMappingSequence",
                    "Die Map coordinate audit. no=" + i +
                    ", mapX=" + mapX +
                    ", mapY=" + mapY +
                    ", target=" + entry.IsTarget +
                    ", sourcePosX=" + (IsFinite(sourcePosX) ? sourcePosX.ToString("F6") : "-") +
                    ", sourcePosY=" + (IsFinite(sourcePosY) ? sourcePosY.ToString("F6") : "-") +
                    ", relativeFormula=" + (sourceMapIsExternal
                        ? "sourcePos-sourceMapOrigin"
                        : "mapIndex*pitch") +
                    ", relativeX=" + relativeX.ToString("F6") +
                    ", relativeY=" + relativeY.ToString("F6") +
                    ", finalX=finalOriginX+relativeX=" + entry.PosX.ToString("F6") +
                    ", finalY=finalOriginY+relativeY=" + entry.PosY.ToString("F6") +
                    ", dieUid=" + (entry.DieUid ?? "") + " - Audit");
            }

            WriteLog("InputStageDieMappingSequence",
                "Die Map coordinate audit complete. entryCount=" + _dieMap.Entries.Count + " - Ok");
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private async Task<int> FindMarkPointAsync(InputStageDieMapMarkPoint point, InputStageDieMappingStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (point == null)
                    return Fail("IN-STAGE-DIEMAP-POINT", Stage.Name, "Die map mark point is null.");

                VisionAlignResult vision = await RequestVisionPcOffsetWithRetryAsync(ResolveTargetId(), point.Name, ct).ConfigureAwait(false);
                if (vision == null)
                    return Fail("IN-STAGE-DIEMAP-VISION", "Vision", point.Name + " vision offset receive failed.");

                point.VisionOffsetX = vision.DeltaX;
                point.VisionOffsetY = vision.DeltaY;
                _mappedPoints[point.Name] = new MappedMarkPoint
                {
                    Name = point.Name,
                    X = Stage.CameraX.ActualPosition + vision.DeltaX,
                    Y = Stage.StageY.ActualPosition + vision.DeltaY,
                    OffsetX = vision.DeltaX,
                    OffsetY = vision.DeltaY
                };

                WriteLog("InputStageDieMappingSequence",
                    "Die map mark found. point=" + point.Name +
                    ", x=" + _mappedPoints[point.Name].X.ToString("F6") +
                    ", y=" + _mappedPoints[point.Name].Y.ToString("F6") +
                    ", dx=" + vision.DeltaX.ToString("F6") +
                    ", dy=" + vision.DeltaY.ToString("F6") + " - Ok");

                CurrentStep = nextStep;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-VISION-EX", "Vision", point != null ? point.Name + " vision failed: " + ex.Message : "Vision mark find failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> SearchVisionMarkAroundCurrentPointAsync(
            string targetId,
            string stepName,
            string description,
            double baseX,
            double baseY,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!Options.EnableMotion)
                {
                    WriteLog("InputStageDieMappingSequence",
                        description + " 주변 탐색은 Motion Disable 상태라 수행하지 않습니다. step=" +
                        stepName + " - Skip");
                    return null;
                }

                double pitchX = Math.Abs(ResolvePitchX());
                double pitchY = Math.Abs(ResolvePitchY());
                if (pitchX <= 1e-9 || pitchY <= 1e-9)
                {
                    WriteLog("InputStageDieMappingSequence",
                        description + " 주변 탐색 실패. Pitch 값이 유효하지 않습니다. pitchX=" +
                        pitchX.ToString("F6") +
                        ", pitchY=" + pitchY.ToString("F6") + " - Failed");
                    return null;
                }

                double configLimitX;
                double configLimitY;
                double effectiveLimitX;
                double effectiveLimitY;
                ResolveMappingFineOffsetLimits(
                    pitchX,
                    pitchY,
                    out configLimitX,
                    out configLimitY,
                    out effectiveLimitX,
                    out effectiveLimitY);
                SearchOffset[] offsets = BuildFineSearchOffsets(effectiveLimitX, effectiveLimitY);
                WriteLog("InputStageDieMappingSequence",
                    description + " Fine 주변 탐색을 시작합니다. baseX=" + baseX.ToString("F6") +
                    ", baseY=" + baseY.ToString("F6") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") +
                    ", configLimitX=" + configLimitX.ToString("F6") +
                    ", configLimitY=" + configLimitY.ToString("F6") +
                    ", effectiveLimitX=min(configLimitX,pitchX/2)=" + effectiveLimitX.ToString("F6") +
                    ", effectiveLimitY=min(configLimitY,pitchY/2)=" + effectiveLimitY.ToString("F6") +
                    ", candidateCount=" + offsets.Length + " - Start");
                for (int i = 0; i < offsets.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    double targetX = baseX + offsets[i].X;
                    double targetY = baseY + offsets[i].Y;
                    string areaReason;
                    if (!Stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    {
                        WriteLog("InputStageDieMappingSequence",
                            description + " 주변 탐색 후보 위치가 작업 영역 밖이라 건너뜁니다. direction=" +
                            offsets[i].Name +
                            ", targetX=" + targetX.ToString("F6") +
                            ", targetY=" + targetY.ToString("F6") +
                            ", reason=" + areaReason + " - Skip");
                        continue;
                    }

                    int moveResult = await MoveVisionXYPointSafelyAsync(
                        targetX,
                        targetY,
                        description + " 주변 탐색 " + offsets[i].Name,
                        ct).ConfigureAwait(false);
                    if (moveResult != 0)
                        return null;

                    VisionAlignResult result = await RequestVisionPcOffsetWithRetryAsync(
                        targetId,
                        stepName + "_Search_" + offsets[i].Name,
                        ct).ConfigureAwait(false);
                    if (result != null)
                    {
                        WriteLog("InputStageDieMappingSequence",
                            description + " Fine 주변 탐색에서 다이를 찾았습니다. direction=" +
                            offsets[i].Name +
                            ", baseX=" + baseX.ToString("F6") +
                            ", baseY=" + baseY.ToString("F6") +
                            ", targetX=" + targetX.ToString("F6") +
                            ", targetY=" + targetY.ToString("F6") +
                            ", dx=" + result.DeltaX.ToString("F6") +
                            ", dy=" + result.DeltaY.ToString("F6") + " - Ok");
                        return result;
                    }
                }

                WriteLog("InputStageDieMappingSequence",
                    description + " Fine 주변 탐색에서 다이를 찾지 못했습니다. baseX=" +
                    baseX.ToString("F6") +
                    ", baseY=" + baseY.ToString("F6") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") +
                    ", effectiveLimitX=" + effectiveLimitX.ToString("F6") +
                    ", effectiveLimitY=" + effectiveLimitY.ToString("F6") + " - Failed");
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence",
                    description + " 주변 탐색 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private int CalculateDieMap()
        {
            try
            {
                MappedMarkPoint anchor;
                if (!TryGetMappedPoint(VisionAlignTargetIds.Center, out anchor))
                {
                    return Fail("IN-STAGE-DIEMAP-CENTER-POINT", "Vision",
                        "Die Mapping 계산에는 예상 Anchor die 탐색 결과가 필요합니다.");
                }

                DieMap sourceMap = _sourceMap ?? ResolveSourceInputDieMap(_wafer, _frameSpec);
                if (!IsUsableSourceMap(sourceMap))
                {
                    return Fail(
                        string.IsNullOrWhiteSpace(LastSourceInputDieMapFailureCode)
                            ? "IN-STAGE-DIEMAP-SOURCE-MAP"
                            : LastSourceInputDieMapFailureCode,
                        "InputStageDieMappingSequence",
                        (string.IsNullOrWhiteSpace(LastSourceInputDieMapFailureCode)
                            ? "Input die map is not available. Create and save an input wafer map from Recipe > INPUT MAP CREATE first. "
                            : "") +
                        LastSourceInputDieMapFailure);
                }

                int dieMapX = sourceMap != null && sourceMap.DieMapX > 0 ? sourceMap.DieMapX : Math.Max(1, _frameSpec.DieMapX);
                int dieMapY = sourceMap != null && sourceMap.DieMapY > 0 ? sourceMap.DieMapY : Math.Max(1, _frameSpec.DieMapY);
                double pitchX = sourceMap != null && sourceMap.PitchX > 0.0 ? sourceMap.PitchX : ResolvePitchX();
                double pitchY = sourceMap != null && sourceMap.PitchY > 0.0 ? sourceMap.PitchY : ResolvePitchY();
                bool sourceMapIsExternal = IsExternalInputDieMap(sourceMap);
                Dictionary<string, DieMapEntry> sourceCellMap = BuildSourceCellMap(sourceMap);
                if (pitchX <= 0.0 && _frameSpec != null)
                    pitchX = DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeX, _frameSpec.PitchX);
                if (pitchY <= 0.0 && _frameSpec != null)
                    pitchY = DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeY, _frameSpec.PitchY);
                if (pitchX <= 0.0 || pitchY <= 0.0)
                    return Fail("IN-STAGE-DIEMAP-PITCH", "InputStageDieMappingSequence", "Die map pitch is invalid.");

                double signX = 1.0;
                double signY = 1.0;
                double centerX = _dieMapCenterX;
                double centerY = _dieMapCenterY;
                string centerSource = "AlignOriginPredictedAnchor+VisionFineOffset";
                double alignOriginX = _wafer != null && _wafer.HasInputStageAlignResult
                    ? _wafer.InputStageAlignOriginX
                    : Stage.OriginX;
                double alignOriginY = _wafer != null && _wafer.HasInputStageAlignResult
                    ? _wafer.InputStageAlignOriginY
                    : Stage.OriginY;
                double originX = alignOriginX + _mappingFineOffsetX;
                double originY = alignOriginY + _mappingFineOffsetY;
                double waferRadius = ResolveWaferRadiusFromSpecOrMap(_frameSpec, dieMapX, dieMapY, pitchX, pitchY);
                _centerDieTargetX = anchor.X;
                _centerDieTargetY = anchor.Y;

                _dieMap = new DieMap
                {
                    FrameObjId = BuildFrameObjId(_wafer),
                    DieMapX = dieMapX,
                    DieMapY = dieMapY,
                    PitchX = pitchX,
                    PitchY = pitchY,
                    DieSizeX = sourceMap != null && sourceMap.DieSizeX > 0.0 ? sourceMap.DieSizeX : pitchX,
                    DieSizeY = sourceMap != null && sourceMap.DieSizeY > 0.0 ? sourceMap.DieSizeY : pitchY,
                    OuterDiameterMm = sourceMap != null ? sourceMap.OuterDiameterMm : 0.0,
                    EdgeSkipMode = sourceMapIsExternal ? sourceMap.EdgeSkipMode : "",
                    SideEdgeSkip = sourceMap != null ? sourceMap.SideEdgeSkip : 0.0,
                    TopBottomEdgeSkip = sourceMap != null ? sourceMap.TopBottomEdgeSkip : 0.0,
                    OriginX = originX,
                    OriginY = originY,
                    SourceFileName = sourceMap != null ? sourceMap.SourceFileName : "",
                    SourceContentHash = sourceMap != null ? sourceMap.SourceContentHash : null,
                    ProcessTransform = WaferMapProcessService.CloneTransform(sourceMap != null ? sourceMap.ProcessTransform : null),
                    SourceFormat = sourceMap != null ? sourceMap.SourceFormat : "",
                    SourcePitchFromFile = sourceMap != null && sourceMap.SourcePitchFromFile,
                    SourceDeclaredCount = sourceMap != null ? sourceMap.SourceDeclaredCount : 0,
                    SourceFirstX = sourceMap != null ? sourceMap.SourceFirstX : -1,
                    SourceFirstY = sourceMap != null ? sourceMap.SourceFirstY : -1,
                    SourceFirstPosX = sourceMap != null ? sourceMap.SourceFirstPosX : double.NaN,
                    SourceFirstPosY = sourceMap != null ? sourceMap.SourceFirstPosY : double.NaN,
                    CreatedAt = DateTime.Now
                };

                if (_dieMap.ProcessTransform != null) _dieMap.ProcessTransform.IsAbsolutePosition = true;

                _waferMap = new WaferMapData
                {
                    WaferId = _wafer != null ? _wafer.WaferId : "",
                    ColumnCount = dieMapX,
                    RowCount = dieMapY,
                    DieMap = new bool[dieMapY, dieMapX],
                    Ref1Row = dieMapY / 2,
                    Ref1Col = Math.Max(0, dieMapX / 4),
                    Ref2Row = dieMapY / 2,
                    Ref2Col = dieMapX > 1 ? Math.Min(dieMapX - 1, (dieMapX * 3) / 4) : 0
                };

                int index = 0;
                int targetCount = 0;
                bool anchorCoordinateMapped = false;
                for (int row = 0; row < dieMapY; row++)
                {
                    for (int col = 0; col < dieMapX; col++)
                    {
                        DieMapEntry sourceEntry = FindSourceCell(sourceCellMap, col, row);
                        if (sourceMapIsExternal && sourceEntry == null)
                        {
                            _waferMap.DieMap[row, col] = false;
                            continue;
                        }

                        double x = sourceMapIsExternal && sourceEntry != null
                            ? originX + signX * (sourceEntry.PosX - sourceMap.OriginX)
                            : originX + signX * pitchX * col;
                        double y = sourceMapIsExternal && sourceEntry != null
                            ? originY + signY * (sourceEntry.PosY - sourceMap.OriginY)
                            : originY + signY * pitchY * row;
                        if (col == _mappingAnchorMapX && row == _mappingAnchorMapY)
                        {
                            _centerDieTargetX = x;
                            _centerDieTargetY = y;
                            anchorCoordinateMapped = true;
                        }
                        bool target = sourceEntry != null
                            ? sourceEntry.IsTarget
                            : (!sourceMapIsExternal && IsInsideWaferCircle(x, y, centerX, centerY, waferRadius));
                        if (target)
                            targetCount++;

                        // 현재 기준: 외부 웨이퍼맵 X/Y 인덱스는 표시/제어/MaterialState까지 그대로 유지한다.
                        int mapX = sourceEntry != null ? DieMapGenerator.ResolveMapIndexX(sourceEntry) : col;
                        int mapY = sourceEntry != null ? DieMapGenerator.ResolveMapIndexY(sourceEntry) : row;
                        int originalX = sourceEntry != null ? DieMapGenerator.ResolveOriginalMapIndexX(sourceEntry) : mapX;
                        int originalY = sourceEntry != null ? DieMapGenerator.ResolveOriginalMapIndexY(sourceEntry) : mapY;
                        _waferMap.DieMap[row, col] = target;
                        _dieMap.Entries.Add(new DieMapEntry
                        {
                            Index = index++,
                            SequenceNo = sourceEntry != null ? sourceEntry.SequenceNo : 0,
                            DieMapX = mapX,
                            DieMapY = mapY,
                            OriginalMapX = originalX,
                            OriginalMapY = originalY,
                            IsTarget = target,
                            Result = DieResult.Unknown,
                            BinCode = target ? (sourceEntry != null ? sourceEntry.BinCode : 0) : 0,
                            SourceBinCode = sourceEntry != null ? sourceEntry.SourceBinCode : null,
                            SourceToken = sourceEntry != null ? sourceEntry.SourceToken : null,
                            LogicalGridX = sourceEntry != null ? sourceEntry.LogicalGridX : null,
                            LogicalGridY = sourceEntry != null ? sourceEntry.LogicalGridY : null,
                            EquipmentGridX = sourceEntry != null ? sourceEntry.EquipmentGridX : mapX - Math.Max(0, dieMapX - 1) / 2.0,
                            EquipmentGridY = sourceEntry != null
                                ? sourceEntry.EquipmentGridY
                                : DieMapGenerator.CalculateEquipmentGridY(mapY, dieMapY),
                            PosX = x,
                            PosY = y,
                            DieUid = MaterialStateService.BuildPhysicalDieId(_wafer, originalX, originalY)
                        });
                    }
                }

                if (!anchorCoordinateMapped)
                {
                    return Fail("IN-STAGE-DIEMAP-ANCHOR-CELL", "InputStageDieMappingSequence",
                        "계산된 Die Map에서 검출 Anchor 셀을 찾지 못했습니다. anchorMapX=" + _mappingAnchorMapX +
                        ", anchorMapY=" + _mappingAnchorMapY);
                }

                double anchorRecalculationDeltaX = _centerDieTargetX - _mappingAnchorDetectedX;
                double anchorRecalculationDeltaY = _centerDieTargetY - _mappingAnchorDetectedY;
                WriteLog("InputStageDieMappingSequence",
                    "Die Map Anchor coordinate cross-check. anchorMapX=" + _mappingAnchorMapX +
                    ", anchorMapY=" + _mappingAnchorMapY +
                    ", detectedAnchorX=" + _mappingAnchorDetectedX.ToString("F6") +
                    ", detectedAnchorY=" + _mappingAnchorDetectedY.ToString("F6") +
                    ", mapCalculatedAnchorX=" + _centerDieTargetX.ToString("F6") +
                    ", mapCalculatedAnchorY=" + _centerDieTargetY.ToString("F6") +
                    ", deltaX=mapCalculatedAnchorX-detectedAnchorX=" + anchorRecalculationDeltaX.ToString("F6") +
                    ", deltaY=mapCalculatedAnchorY-detectedAnchorY=" + anchorRecalculationDeltaY.ToString("F6") + " - Check");
                if (Math.Abs(anchorRecalculationDeltaX) > 0.001 || Math.Abs(anchorRecalculationDeltaY) > 0.001)
                {
                    return Fail("IN-STAGE-DIEMAP-ANCHOR-CROSS-CHECK", "InputStageDieMappingSequence",
                        "검출 Anchor와 최종 Die Map Anchor 좌표가 일치하지 않습니다. deltaX=" +
                        anchorRecalculationDeltaX.ToString("F6") +
                        ", deltaY=" + anchorRecalculationDeltaY.ToString("F6"));
                }

                // [픽업 BIN 필터 2026-08-27] 재적용에서 필터 결과 대상 0이면 "0개 픽업 완주" 대신 정지.
                if (ApplyInputPickupSequence(_dieMap) == null)
                {
                    return Fail("IN-STAGE-DIEMAP-BIN-NO-TARGET", "InputStageDieMappingSequence",
                        string.IsNullOrWhiteSpace(LastSourceInputDieMapFailure)
                            ? "픽업 BIN 필터 적용 후 픽업 대상 다이가 없습니다."
                            : LastSourceInputDieMapFailure);
                }
                int orderedCount = CountSequencedTargets(_dieMap);

                WriteLog("InputStageDieMappingSequence",
                    "Die map coordinate mapping calculated. grid=" + dieMapX + "x" + dieMapY +
                    ", sourceMap=" + (sourceMap != null ? sourceMap.FrameObjId : "none") +
                    ", sourceMapExternal=" + sourceMapIsExternal +
                    ", sourceMapOriginX=" + sourceMap.OriginX.ToString("F6") +
                    ", sourceMapOriginY=" + sourceMap.OriginY.ToString("F6") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") +
                    ", centerSource=" + centerSource +
                    ", centerX=" + centerX.ToString("F6") +
                    ", centerY=" + centerY.ToString("F6") +
                    ", anchorMapX=" + _mappingAnchorMapX +
                    ", anchorMapY=" + _mappingAnchorMapY +
                    ", expectedAnchorX=" + _mappingAnchorExpectedX.ToString("F6") +
                    ", expectedAnchorY=" + _mappingAnchorExpectedY.ToString("F6") +
                    ", detectedAnchorX=" + _mappingAnchorDetectedX.ToString("F6") +
                    ", detectedAnchorY=" + _mappingAnchorDetectedY.ToString("F6") +
                    ", fineOffsetX=detectedAnchorX-expectedAnchorX=" + _mappingFineOffsetX.ToString("F6") +
                    ", fineOffsetY=detectedAnchorY-expectedAnchorY=" + _mappingFineOffsetY.ToString("F6") +
                    ", anchorFinalX=" + _centerDieTargetX.ToString("F6") +
                    ", anchorFinalY=" + _centerDieTargetY.ToString("F6") +
                    ", signX=" + signX.ToString("F1") +
                    ", signY=" + signY.ToString("F1") +
                    ", alignOriginX=" + alignOriginX.ToString("F6") +
                    ", alignOriginY=" + alignOriginY.ToString("F6") +
                    ", originX=alignOriginX+fineOffsetX=" + originX.ToString("F6") +
                    ", originY=alignOriginY+fineOffsetY=" + originY.ToString("F6") +
                    ", mappingOffsetX=originX-alignOriginX=" + (originX - alignOriginX).ToString("F6") +
                    ", mappingOffsetY=originY-alignOriginY=" + (originY - alignOriginY).ToString("F6") +
                    ", radius=" + waferRadius.ToString("F6") +
                    ", outerDiameter=" + (_frameSpec != null ? _frameSpec.OuterDiameterMm.ToString("F6") : "0") +
                    ", targetCount=" + targetCount +
                    ", orderedCount=" + orderedCount + " - Ok");

                WriteDieMapCoordinateAudit(
                    sourceMap,
                    sourceCellMap,
                    sourceMapIsExternal,
                    alignOriginX,
                    alignOriginY,
                    originX,
                    originY,
                    pitchX,
                    pitchY);

                CurrentStep = InputStageDieMappingStep.MoveCenterDiePoint;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CALC-EX", "InputStageDieMappingSequence", "Die map calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveCenterDiePointAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options.EnableMotion)
                {
                    string areaReason;
                    if (!Stage.IsInputStageWorkPointInArea(_centerDieTargetX, _centerDieTargetY, out areaReason))
                    {
                        return Fail("IN-STAGE-DIEMAP-CENTER-DIE-WORK-AREA", Stage.Name,
                            "Die Mapping 계산 후 Anchor die 위치가 작업 영역 밖입니다. targetX=" +
                            _centerDieTargetX.ToString("F6") +
                            ", targetY=" + _centerDieTargetY.ToString("F6") +
                            ", reason=" + areaReason);
                    }

                    int result = await MoveVisionXYPointSafelyAsync(
                        _centerDieTargetX,
                        _centerDieTargetY,
                        "Die Mapping 계산 후 Anchor die 최종 위치",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping 계산 후 Anchor die 최종 위치로 이동했습니다. anchorMapX=" + _mappingAnchorMapX +
                    ", anchorMapY=" + _mappingAnchorMapY +
                    ", targetX=" +
                    _centerDieTargetX.ToString("F6") +
                    ", targetY=" + _centerDieTargetY.ToString("F6") +
                    ", formulaX=alignExpectedAnchorX+fineOffsetX" +
                    ", formulaY=alignExpectedAnchorY+fineOffsetY" +
                    ", fineOffsetX=" + _mappingFineOffsetX.ToString("F6") +
                    ", fineOffsetY=" + _mappingFineOffsetY.ToString("F6") + " - Ok");

                CurrentStep = ShouldMoveVisionXAvoidAfterManualDieMapping()
                    ? InputStageDieMappingStep.MoveVisionXAvoidAfterManual
                    : InputStageDieMappingStep.ApplyDieMap;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CENTER-DIE-MOVE-EX", Stage != null ? Stage.Name : "InputStageUnit",
                    "Die Mapping 계산 후 Anchor die 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyDieMap()
        {
            try
            {
                if (_dieMap == null || _waferMap == null)
                    return Fail("IN-STAGE-DIEMAP-APPLY", "InputStageDieMappingSequence", "Die map result is not available.");

                int result = ResolveMaterialStateWaferForDieMapApply();
                if (result != 0) return result;

                result = ValidateStoredAlignResultForCurrentMode();
                if (result != 0) return result;

                string resultMode = _hybridVirtualFrameActive
                    ? InputStageResultMode.HybridRealVisionSimMotion
                    : InputStageResultMode.Standard;

                InputStageDieMapApplyResult applyResult = InputStageDieMapApplyService.Apply(
                    new InputStageDieMapApplyRequest
                    {
                        Stage = Stage,
                        Controller = Context != null ? Context.Controller : null,
                        Bus = Context != null ? Context.Bus : null,
                        DieMap = _dieMap,
                        WaferMap = _waferMap,
                        ExpectedWafer = _wafer,
                        ExpectedWaferId = _expectedWaferId,
                        ExpectedWaferInstanceId = _expectedWaferInstanceId,
                        PickupOptions = ResolveInputPickupSubset(),
                        ResultMode = resultMode,
                        AlignResultRunId = _wafer != null ? _wafer.InputStageAlignResultRunId : "",
                        Source = "InputStageDieMappingSequence.ApplyDieMap",
                        SaveReason = "InputStageDieMapping",
                        PublishReadySignals = Options.PublishReadySignals
                    });
                if (applyResult == null || !applyResult.Success)
                    return Fail("IN-STAGE-DIEMAP-APPLY-SERVICE", "InputStageDieMappingSequence",
                        "Die map apply service failed: " + (applyResult != null ? applyResult.ErrorMessage : ""));

                _wafer = applyResult.Wafer;
                _waferMap = applyResult.WaferMap;
                _dieMap = applyResult.DieMap;
                _createdDieCount = applyResult.CreatedDieCount;
                WriteLog("InputStageDieMappingSequence",
                    "Input stage die mapping applied. wafer=" + (_wafer != null ? _wafer.WaferId : "") +
                    ", dieMapX=" + _dieMap.DieMapX +
                    ", dieMapY=" + _dieMap.DieMapY +
                    ", dieCount=" + _createdDieCount +
                    ", resultMode=" + resultMode +
                    ", alignResultRunId=" + (_wafer != null ? _wafer.InputStageAlignResultRunId : "") + " - Ok");

                CurrentStep = InputStageDieMappingStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-APPLY-EX", "InputStageDieMappingSequence", "Die map apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool ShouldMoveVisionXAvoidAfterManualDieMapping()
        {
            return Options != null &&
                   Options.RunMode == SequenceRunMode.Manual &&
                   Options.EnableMotion &&
                   Stage != null &&
                   Stage.Recipe != null &&
                   Stage.Recipe.VisionX != null;
        }

        private async Task<int> MoveVisionXAvoidAfterManualDieMappingAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!ShouldMoveVisionXAvoidAfterManualDieMapping())
                {
                    CurrentStep = InputStageDieMappingStep.ApplyDieMap;
                    return 0;
                }

                double target = Stage.Recipe.VisionX.AvoidPosition;
                int result = await MoveAxisAndWaitAsync(
                    WaferStageAxis.VisionX,
                    target,
                    "Manual Die Mapping 완료 후 VisionX Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("InputStageDieMappingSequence",
                    "메뉴얼 Die Mapping 완료 후 VisionX를 Avoid 위치로 복귀했습니다. target=" +
                    target.ToString("F3") + " - Ok");

                CurrentStep = InputStageDieMappingStep.ApplyDieMap;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "IN-STAGE-DIEMAP-VISIONX-AVOID-EX",
                    Stage != null ? Stage.Name : "InputStageUnit",
                    "메뉴얼 Die Mapping 완료 후 VisionX Avoid 이동 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        private DieMap ResolveSourceInputDieMap(WaferMaterial wafer, TapeFrameSpec frameSpec)
        {
            try
            {
                LastSourceInputDieMapFailure = "";
                LastSourceInputDieMapFailureCode = "";

                RecipeProject processProject = RecipeStore.LoadLastOrDefaultCached();
                WaferMapProcessSettings processSettings = processProject != null ? processProject.InputMapProcessing : null;
                bool networkMode = IsLotNetworkWaferMapModeActive();
                string processBarcode = wafer != null ? (wafer.BarcodeId ?? "") : "";
                DieMap prepared = MaterialStateService.GetPreparedInputMap(wafer, processBarcode, networkMode);
                if (prepared != null)
                {
                    prepared = WaferMapProcessService.Prepare(prepared, processSettings, "Input");
                    if (networkMode)
                    {
                        string code;
                        string reason;
                        if (!InputWaferMapPreflightService.TryValidateParsedMap(prepared, frameSpec, Stage, out code, out reason))
                        {
                            LastSourceInputDieMapFailureCode = code;
                            LastSourceInputDieMapFailure = reason;
                            return null;
                        }
                        ApplyPickupBinSelectionFilter(prepared);
                    }
                    return ApplyInputPickupSequence(prepared);
                }

                // [P4 2026-08-22] LOT 네트워크 웨이퍼맵 모드: 이 웨이퍼 슬롯의 다운로드 맵을 사용한다.
                // 모드 ON에서 로트 맵 확보 실패는 레시피/기타 소스로 폴백하지 않는다(잘못된 맵 진행 금지,
                // 팀장님 확정 — 파일 없음/불일치는 알람 정지).
                if (networkMode)
                    return ResolveLotNetworkInputDieMap(wafer, frameSpec);

                bool recipeMapConfigured = IsRecipeInputDieMapConfigured();
                DieMap recipeMap = LoadRecipeInputDieMap(frameSpec);
                if (IsUsableSourceMap(recipeMap))
                {
                    recipeMap = WaferMapProcessService.Prepare(recipeMap, processSettings, "Input");
                    MaterialStateService.PinPreparedInputMap(wafer, processBarcode, false, recipeMap);
                    return ApplyInputPickupSequence(recipeMap);
                }

                if (IsManagedInputMapApprovalRequired() || processSettings != null)
                {
                    WriteLog("InputStageDieMappingSequence",
                        "Managed Recipe input map is not FINAL APPLY approved. Material/Active fallback is blocked. reason=" +
                        LastSourceInputDieMapFailure + " - Failed");
                    return null;
                }

                DieMap materialMap = MaterialStateService.BuildDieMapFromWafer(wafer);
                if (IsUsableSourceMap(materialMap))
                {
                    if (recipeMapConfigured)
                    {
                        WriteLog("InputStageDieMappingSequence",
                            "Recipe input die map was not usable. Current wafer material map is used instead. reason=" +
                            LastSourceInputDieMapFailure + " - Check");
                    }

                    return ApplyInputPickupSequence(materialMap);
                }

                DieMap activeMap = LotStorage.ActiveInputDieMap;
                if (IsUsableSourceMap(activeMap))
                {
                    if (recipeMapConfigured)
                    {
                        WriteLog("InputStageDieMappingSequence",
                            "Recipe input die map was not usable. Active input die map is used instead. reason=" +
                            LastSourceInputDieMapFailure + " - Check");
                    }

                    return ApplyInputPickupSequence(activeMap);
                }

                return null;
            }
            catch (Exception ex)
            {
                LastSourceInputDieMapFailureCode = "IN-STAGE-MAP-PREPARE";
                LastSourceInputDieMapFailure = ex.Message;
                WriteLog("InputStageDieMappingSequence", "Source input die map resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        // 폴더 누락도 원격 모드 ON으로 판정한다. 확보 단계에서 알람을 내며 등록 맵으로 대체하지 않는다.
        internal static bool IsLotNetworkWaferMapModeActive()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null) throw new InvalidOperationException("웨이퍼맵 사용 모드 설정을 읽을 수 없습니다.");
                return RecipeInputMapSource.UsesRemoteForActiveRecipe(settings);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "SYSTEM", "LOT-MAP-MODE",
                    "LOT 네트워크 맵 모드 판정 실패 — 다른 소스 맵으로 대체하지 않고 중단합니다. error=" + ex.Message);
                throw;
            }
        }

        // [P5 2026-08-24] LOT 네트워크 맵 확보 — 바코드=파일명(1:1, 팀장님 확정):
        // 바코드 필수 확인 → <폴더>\<바코드> 파일 수신(캐시/원격) → 신선 파싱(공유 객체 변형 금지)
        // → 피치 대조(오제품 차단) → BIN 선택 필터 → 픽업 순서 부여.
        // 실패 시 null + 구분 알람 코드/사유를 남긴다(호출자가 해당 코드로 Fail).
        // 슬롯 번호/LOT ID는 파일 특정에 쓰지 않는다(실측: 구 테스트 파일 번호≠웨이퍼 번호) —
        // 그래서 2단(Input2) 차단도 함께 해제했다(차단 사유였던 슬롯 번호 규칙 자체가 소멸).
        private DieMap ResolveLotNetworkInputDieMap(WaferMaterial wafer, TapeFrameSpec frameSpec)
        {
            string lotId = MaterialStateService.GetProductionLotId();

            // 바코드가 곧 파일 키다 — 판독 확정값이 없으면 맵을 특정할 수 없다(fail-closed).
            // UseInputWaferBarcode OFF + 모드 ON 조합의 강제 지점이기도 하다.
            string barcode = wafer != null && wafer.BarcodeConfirmed ? (wafer.BarcodeId ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(barcode) ||
                !InputFeederLoadToStageSequence.IsUsableBarcode(barcode))
            {
                LastSourceInputDieMapFailureCode = "LOT-MAP-BARCODE-REQUIRED";
                LastSourceInputDieMapFailure =
                    "LOT 네트워크 맵 모드는 웨이퍼 바코드가 필수입니다(맵 파일명=바코드). " +
                    "설정 → BARCODE의 'USE BARCODE'를 켜고 판독(또는 수동 입력) 후 진행하세요. waferId=" +
                    (wafer != null ? wafer.WaferId : "-") +
                    ", barcodeConfirmed=" + (wafer != null && wafer.BarcodeConfirmed);
                return null;
            }

            QMC.CDT320.Lots.LotWaferMapSlotInfo slotInfo;
            string fetchReason;
            if (!QMC.CDT320.Lots.LotWaferMapFetchService.TryFetchWaferMapByBarcode(barcode, out slotInfo, out fetchReason))
            {
                LastSourceInputDieMapFailureCode = "LOT-MAP-FILE-MISSING";
                LastSourceInputDieMapFailure =
                    "이 웨이퍼의 맵 파일을 확보하지 못했습니다. barcode=" + barcode +
                    ", lot=" + (string.IsNullOrWhiteSpace(lotId) ? "-" : lotId) +
                    ", reason=" + fetchReason +
                    " — 네트워크 폴더에 바코드와 같은 이름의 파일이 있는지 확인하세요.";
                return null;
            }

            DieMap map;
            try
            {
                // 캐시된 통계가 아니라 로컬 캐시 파일을 매번 새로 파싱한다 — 웨이퍼별 사본이라
                // BIN 필터로 IsTarget을 바꿔도 원본/타 웨이퍼에 영향이 없다.
                // [캠택맵 2026-08-27] 설정 포맷(Rad/Camtek)에 따라 파서 선택(공통 헬퍼).
                map = QMC.CDT320.Lots.LotWaferMapFetchService.LoadConfiguredFormatOrThrow(slotInfo.LocalPath);
            }
            catch (Exception ex)
            {
                LastSourceInputDieMapFailureCode = "LOT-MAP-FORMAT";
                LastSourceInputDieMapFailure =
                    "LOT 웨이퍼맵 파싱에 실패했습니다. file=" + slotInfo.LocalPath + ", error=" + ex.Message;
                return null;
            }

            if (!IsUsableSourceMap(map))
            {
                LastSourceInputDieMapFailureCode = "LOT-MAP-FORMAT";
                LastSourceInputDieMapFailure =
                    "LOT 웨이퍼맵이 비어 있습니다. file=" + slotInfo.LocalPath;
                return null;
            }

            // 얼라인 전 바코드 사전 확인과 동일한 제품/BIN 조건을 실제 적용 직전에도 다시 검증한다.
            string validationCode;
            string validationReason;
            if (!InputWaferMapPreflightService.TryValidateParsedMap(map, frameSpec, Stage, out validationCode, out validationReason))
            {
                LastSourceInputDieMapFailureCode = validationCode;
                LastSourceInputDieMapFailure = validationReason + ", file=" + slotInfo.LocalPath;
                return null;
            }

            // [P5 2026-08-24] 파일 특정은 이미 바코드=파일명으로 끝났다. 헤더 내부 ID는 파일명과
            // 다른 사례가 실측 확인돼(구 테스트 파일) 기록만 남긴다 — 불일치가 반복 관찰되면
            // 팹 데이터 이상 신호이므로 로그로 추적한다(알람 판정 금지 정책 유지).
            string internalMapId = (slotInfo.InternalMapId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(internalMapId) &&
                !string.Equals(internalMapId, barcode, StringComparison.OrdinalIgnoreCase))
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "SYSTEM", "LOT-MAP-BARCODE-CHECK",
                    "맵 헤더 내부 ID가 파일명(바코드)과 다릅니다(기록만 — 파일 특정은 파일명 기준). " +
                    "mapInternalId=" + internalMapId + ", barcode=" + barcode +
                    ", lot=" + (string.IsNullOrWhiteSpace(lotId) ? "-" : lotId));
            }

            RecipeProject processProject = RecipeStore.LoadLastOrDefaultCached();
            map = WaferMapProcessService.Prepare(map, processProject != null ? processProject.InputMapProcessing : null, "Input");
            MaterialStateService.PinPreparedInputMap(wafer, barcode, true, map);
            int filteredOut = ApplyPickupBinSelectionFilter(map);

            // [검토수정 2026-08-22] 선택 BIN이 이 웨이퍼에 하나도 없으면 "0개 픽업으로 조용히 완주"가
            // 아니라 알람으로 세운다 — 작업자가 선택을 확인하고 가야 한다(fail-closed).
            if (filteredOut > 0 && !HasAnyTargetEntry(map))
            {
                LastSourceInputDieMapFailureCode = "LOT-MAP-BIN-NO-TARGET";
                LastSourceInputDieMapFailure =
                    "BIN 선택 필터 적용 후 이 웨이퍼에 픽업 대상 다이가 없습니다. barcode=" + barcode +
                    ", lot=" + (string.IsNullOrWhiteSpace(lotId) ? "-" : lotId) +
                    ", binSelection=" + MaterialStateService.DescribePickupBinSelection() +
                    " — [BIN] 선택을 확인하세요.";
                return null;
            }

            QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "InputStageDieMappingSequence",
                "LOT 네트워크 웨이퍼맵 적용. barcode=" + barcode +
                ", lot=" + (string.IsNullOrWhiteSpace(lotId) ? "-" : lotId) +
                ", mapInternalId=" + (string.IsNullOrWhiteSpace(internalMapId) ? "-" : internalMapId) +
                ", dies=" + (map.Entries != null ? map.Entries.Count : 0) +
                ", binSelection=" + MaterialStateService.DescribePickupBinSelection() +
                ", binFilteredOut=" + filteredOut + " - Ok");

            return ApplyInputPickupSequence(map);
        }

        // [검토수정 2026-08-22] 필터 후 픽업 대상 존재 여부.
        private static bool HasAnyTargetEntry(DieMap map)
        {
            if (map == null || map.Entries == null)
                return false;

            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry != null && entry.IsTarget)
                    return true;
            }

            return false;
        }

        // [P4 2026-08-22] BIN 선택 필터: Selected 모드면 선택 bin 외 다이를 SKIP(IsTarget=false) 처리한다.
        // All 모드면 무동작(기존 동작 동일). 반환값 = 이번에 제외된 다이 수.
        private static int ApplyPickupBinSelectionFilter(DieMap map)
        {
            HashSet<int> selectedBins;
            if (!MaterialStateService.IsPickupBinFilterActive(out selectedBins))
                return 0;

            if (map == null || map.Entries == null)
                return 0;

            int filteredOut = 0;
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null || !entry.IsTarget)
                    continue;

                if (!selectedBins.Contains(entry.BinCode))
                {
                    entry.IsTarget = false;
                    filteredOut++;
                }
            }

            return filteredOut;
        }

        private static bool IsRecipeInputDieMapConfigured()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                return project != null && !string.IsNullOrWhiteSpace(project.InputDieMapFileName);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private DieMap LoadRecipeInputDieMap(TapeFrameSpec frameSpec)
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return null;

                string path;
                string reason;
                // 현재 기준: 실제 Die Mapping도 Process Test와 같은 레시피/외부맵 로더를 사용한다.
                DieMap map = RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.Input, out path, out reason);
                if (map != null)
                {
                    ApplyInputPickupSequence(map);
                    WriteLog("InputStageDieMappingSequence",
                        "Recipe input die map loaded. path=" + path +
                        ", dieMap=" + map.DieMapX + "x" + map.DieMapY + " - Ok");
                }
                else if (!string.IsNullOrWhiteSpace(reason))
                {
                    LastSourceInputDieMapFailure = reason;
                    WriteLog("InputStageDieMappingSequence", "Recipe input die map load skipped: " + reason + " - Check");
                }
                return map;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Recipe input die map load failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static bool IsUsableSourceMap(DieMap map)
        {
            try
            {
                return map != null &&
                       map.DieMapX > 0 &&
                       map.DieMapY > 0 &&
                       map.Entries != null &&
                       map.Entries.Count > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private DieMap ApplyInputPickupSequence(DieMap map)
        {
            try
            {
                if (map == null)
                    return null;

                // [픽업 BIN 필터 2026-08-27 팀장님 지시] 레시피 BIN 필터를 맵 소스(LOT 네트워크/레시피
                // 맵/Material/Active) 무관하게 공통 적용한다. LOT [BIN] 선택 필터(네트워크 경로 전용)와는
                // 순차 강등이라 자연히 교집합. IsTarget=true만 강등하므로 재호출(매핑 재적용)에도 멱등.
                int recipeFilteredOut = ApplyRecipePickupBinFilter(map, out string recipeFilterCsv);
                if (recipeFilteredOut > 0 && !HasAnyTargetEntry(map))
                {
                    LastSourceInputDieMapFailureCode = "MAP-RECIPE-BIN-NO-TARGET";
                    LastSourceInputDieMapFailure =
                        "레시피 픽업 BIN 필터 적용 후 이 웨이퍼에 픽업 대상 다이가 없습니다. " +
                        "recipeBinFilter=" + recipeFilterCsv +
                        " — 레시피 PICKUP BIN FILTER 설정을 확인하세요.";
                    WriteLog("InputStageDieMappingSequence", LastSourceInputDieMapFailure + " - Failed");
                    return null;
                }

                if (recipeFilteredOut > 0)
                {
                    int remainingTargets = 0;
                    foreach (DieMapEntry entry in map.Entries)
                    {
                        if (entry != null && entry.IsTarget)
                            remainingTargets++;
                    }
                    WriteLog("InputStageDieMappingSequence",
                        "레시피 픽업 BIN 필터 적용. recipeBinFilter=" + recipeFilterCsv +
                        ", filteredOut=" + recipeFilteredOut +
                        ", remainingTargets=" + remainingTargets + " - Ok");
                }

                PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickupSubset());
                return DieMapGenerator.Normalize(map);
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Input pickup sequence apply failed: " + ex.Message + " - Failed");
                return map;
            }
            finally
            {
            }
        }

        // [픽업 BIN 필터 2026-08-27] 레시피(InputStage DieMap) CSV 필터 — 지정 BIN 외 다이를
        // IsTarget=false로 강등한다. 필터 미설정(빈 값/유효 항목 0)이면 무동작. 반환=이번 강등 수.
        private int ApplyRecipePickupBinFilter(DieMap map, out string filterCsv)
        {
            filterCsv = "-";
            try
            {
                if (map == null || map.Entries == null || Stage == null || Stage.Recipe == null || Stage.Recipe.DieMap == null)
                    return 0;

                System.Collections.Generic.HashSet<int> bins;
                if (!Stage.Recipe.DieMap.TryGetPickupBinFilter(out bins))
                    return 0;

                var sortedBins = new List<int>(bins);
                sortedBins.Sort();
                filterCsv = string.Join(",", sortedBins);
                int filteredOut = 0;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null || !entry.IsTarget)
                        continue;

                    if (!bins.Contains(entry.BinCode))
                    {
                        entry.IsTarget = false;
                        filteredOut++;
                    }
                }

                return filteredOut;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence",
                    "레시피 픽업 BIN 필터 적용 실패(필터 없이 진행하지 않고 전체 제외). error=" + ex.Message + " - Failed");
                // fail-closed: 필터 해석 실패를 "전량 픽업"으로 조용히 바꾸지 않는다 — 상위 no-target 검사로 정지.
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry != null)
                        entry.IsTarget = false;
                }
                return map.Entries.Count;
            }
        }

        /// <summary>
        /// 매핑이 사용한 픽업 옵션이 런타임 적용값(Controller.PickupOptions = 활성 레시피 적용 결과)과
        /// 다르면 경고를 남긴다. 값 자체는 바꾸지 않는다 — 원인(마커/활성 레시피 불일치)을 드러내는 것이 목적이다.
        /// </summary>
        private void WarnIfInputPickupSubsetDiffersFromRuntime(PickupSubset resolved)
        {
            try
            {
                if (resolved == null || Context == null || Context.Controller == null)
                    return;

                PickupSubset runtime = Context.Controller.PickupOptions;
                if (runtime == null)
                    return;

                if (runtime.StartCorner == resolved.StartCorner &&
                    runtime.Direction == resolved.Direction &&
                    runtime.Pattern == resolved.Pattern)
                    return;

                EventLogger.Write(EventKind.Warning, "SYS", "INPUT-PICKUP-SUBSET-MISMATCH", Name,
                    "Die Mapping이 사용한 픽업 옵션이 런타임 적용값과 다릅니다. " +
                    "마커(last project)와 활성 레시피가 어긋났을 수 있습니다. " +
                    "mapping=(" + resolved.StartCorner + "," + resolved.Direction + "," + resolved.Pattern + ")" +
                    ", runtime=(" + runtime.StartCorner + "," + runtime.Direction + "," + runtime.Pattern + ")");
            }
            catch
            {
                // 진단 목적이므로 실패해도 매핑을 막지 않는다.
            }
        }

        private PickupSubset ResolveInputPickupSubset()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return new PickupSubset();

                PickupSubset resolved = project.InputPickup ?? project.Pickup ?? new PickupSubset();

                // 이 시퀀스는 last-project 마커(LoadLastOrDefault)를, Review 창은 활성 레시피를 읽는다.
                // 정상 흐름(ApplyMachineRecipe → SaveLastProjectName)에서는 일치하지만 어긋날 수 있어
                // 런타임 적용값과 대조해 경고만 남긴다. 반환값은 바꾸지 않는다
                // (여기서 소스를 바꾸면 실장비 픽업 순서가 달라질 수 있고, 생산 순서는 어차피
                //  Review 승인 OrderedDieIds가 지배하므로 이 불일치는 Review 전 표시에만 영향한다).
                WarnIfInputPickupSubsetDiffersFromRuntime(resolved);
                return resolved;
            }
            catch
            {
                return new PickupSubset();
            }
            finally
            {
            }
        }

        private static int CountSequencedTargets(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return 0;

                int count = 0;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry != null && entry.IsTarget && entry.SequenceNo > 0)
                        count++;
                }
                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private static bool IsExternalInputDieMap(DieMap map)
        {
            return map != null &&
                   !string.IsNullOrWhiteSpace(map.EdgeSkipMode) &&
                   string.Equals(map.EdgeSkipMode, "ExternalMap", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, DieMapEntry> BuildSourceCellMap(DieMap map)
        {
            var lookup = new Dictionary<string, DieMapEntry>(StringComparer.Ordinal);
            if (map == null || map.Entries == null)
                return lookup;

            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                // 현재 기준: 소스맵 조회 키도 외부 웨이퍼맵 원본 인덱스로 맞춘다.
                string key = BuildSourceCellKey(
                    DieMapGenerator.ResolveMapIndexX(entry),
                    DieMapGenerator.ResolveMapIndexY(entry));
                if (!lookup.ContainsKey(key))
                    lookup.Add(key, entry);
            }

            return lookup;
        }

        private static DieMapEntry FindSourceCell(Dictionary<string, DieMapEntry> lookup, int x, int y)
        {
            if (lookup == null)
                return null;

            DieMapEntry entry;
            return lookup.TryGetValue(BuildSourceCellKey(x, y), out entry) ? entry : null;
        }

        private static string BuildSourceCellKey(int x, int y)
        {
            return x.ToString() + "," + y.ToString();
        }

        private int ResolveMaterialStateWaferForDieMapApply()
        {
            try
            {
                WaferMaterial stateWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (stateWafer == null)
                {
                    return Fail("IN-STAGE-DIEMAP-WAFER-SYNC", "Material",
                        "Die Mapping 결과를 저장할 InputStage Material을 찾을 수 없습니다. " +
                        "stageWafer=" + (_wafer != null ? _wafer.WaferId : "-"));
                }

                string stateWaferInstanceId = MaterialStateService.EnsureWaferInstanceId(stateWafer);
                if (string.IsNullOrWhiteSpace(_expectedWaferInstanceId) ||
                    !string.Equals(
                        _expectedWaferInstanceId,
                        stateWaferInstanceId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("IN-STAGE-DIEMAP-WAFER-MISMATCH", "Material",
                        "Die Mapping 도중 InputStage의 물리 Wafer 세대가 변경되었습니다. " +
                        "sequenceWafer=" + _expectedWaferId +
                        ", sequenceInstance=" + _expectedWaferInstanceId +
                        ", stateWafer=" + stateWafer.WaferId +
                        ", stateInstance=" + stateWaferInstanceId);
                }

                _wafer = stateWafer;
                if (Stage != null)
                    Stage.SetCurrentWaferMaterial(_wafer);

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping 결과 저장 대상 Wafer를 MaterialState 기준으로 동기화했습니다. waferId=" +
                    _wafer.WaferId + " - Ok");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-WAFER-SYNC-EX", "Material",
                    "Die Mapping 결과 저장 대상 Wafer 동기화 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyDieMaterials(DieMap map, WaferMaterial wafer)
        {
            try
            {
                if (map == null || wafer == null)
                    return 0;

                if (wafer.DieIds == null)
                    wafer.DieIds = new List<string>();
                wafer.DieIds.Clear();

                int count = 0;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;

                    int mapX = DieMapGenerator.ResolveMapIndexX(entry);
                    int mapY = DieMapGenerator.ResolveMapIndexY(entry);
                    int originalX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                    int originalY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
                    string dieId = MaterialStateService.BuildPhysicalDieId(wafer, originalX, originalY);
                    entry.DieUid = dieId;
                    entry.DieMapX = mapX;
                    entry.DieMapY = mapY;
                    entry.OriginalMapX = originalX;
                    entry.OriginalMapY = originalY;
                    DieMaterial die = MaterialStateService.GetOrCreateDieMaterial(dieId);
                    die.WaferID_Input = wafer.WaferId;
                    die.InputWaferInstanceId = MaterialStateService.EnsureWaferInstanceId(wafer);
                    die.WaferID_Output = "";
                    die.OutputWaferInstanceId = "";
                    die.Wafer_IndexX = mapX;
                    die.Wafer_IndexY = mapY;
                    die.Wafer_OriginalIndexX = originalX;
                    die.Wafer_OriginalIndexY = originalY;
                    die.InputSequenceNo = entry.SequenceNo;
                    die.Input_BinCode = entry.IsTarget ? entry.BinCode : 0;
                    die.IsInputTarget = entry.IsTarget;
                    die.Output_BinCode = 0;
                    die.Bin_IndexX = -1;
                    die.Bin_IndexY = -1;
                    die.CurrentLocation = new MaterialLocation { Kind = entry.IsTarget ? MaterialLocationKind.InputStage : MaterialLocationKind.Unknown };
                    die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                    die.ReservedPickerNo = -1;
                    // 현재 기준: 새 Input 맵 생성 시 이전 wafer의 Pick/검사 이력은 초기화한다.
                    die.PickedPickerLocation = MaterialLocationKind.Unknown;
                    die.PickedPickerNo = -1;
                    die.PickedAt = DateTime.MinValue;
                    die.Result = DieResult.Unknown;
                    if (die.NgCodes == null)
                        die.NgCodes = new List<string>();
                    else
                        die.NgCodes.Clear();
                    if (die.Inspections == null)
                        die.Inspections = new List<DieInspectionRecord>();
                    else
                        die.Inspections.Clear();
                    if (die.WaferOffset == null)
                        die.WaferOffset = new VisionOffset();
                    die.WaferOffset.X = entry.PosX;
                    die.WaferOffset.Y = entry.PosY;
                    die.WaferOffset.R = 0.0;
                    die.WaferOffset.IsValid = true;
                    if (die.BinOffset == null)
                        die.BinOffset = new VisionOffset();
                    die.BinOffset.X = 0.0;
                    die.BinOffset.Y = 0.0;
                    die.BinOffset.R = 0.0;
                    die.BinOffset.IsValid = false;
                    die.UpdatedAt = DateTime.Now;
                    wafer.DieIds.Add(dieId);
                    if (entry.IsTarget)
                        count++;
                }

                return count;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Die material apply failed: " + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestVisionPcOffsetWithRetryAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                int retryCount = Math.Max(3, ResolveDieMapRetryCount());
                int maxAttempts = retryCount + 1;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    ct.ThrowIfCancellationRequested();
                    VisionAlignResult result = await RequestVisionPcOffsetOnceAsync(targetId, stepName, ct).ConfigureAwait(false);
                    if (result != null)
                        return result;

                    WriteLog("InputStageDieMappingSequence",
                        "Vision PC offset receive failed. step=" + stepName +
                        ", target=" + targetId +
                        ", attempt=" + attempt + "/" + maxAttempts + " - Retry");
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Vision offset retry exception. step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestVisionPcOffsetOnceAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsSimulationOrDryRun() && !AutoVisionRequestService.IsRealVisionInSimulationActive())
                {
                    VisionAlignResult dryRunVisionResult = await RequestDryRunVisionOffsetAsync(targetId, stepName, ct).ConfigureAwait(false);
                    if (dryRunVisionResult != null)
                        return dryRunVisionResult;

                    return await RequestSimVisionOffsetAsync(targetId, stepName, ct).ConfigureAwait(false);
                }

                if (Stage.Vision == null)
                    return null;

                Task<VisionAlignResult> alignTask = Stage.Vision.TriggerAlignAsync(targetId);
                if (alignTask == null)
                    return null;

                return await SequenceAwaiter.AwaitAsync(alignTask, null, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Vision offset request exception. step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static bool IsManagedInputMapApprovalRequired()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                return project != null && project.MapApprovalVersion > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<VisionAlignResult> RequestDryRunVisionOffsetAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (!IsDryRunWithVisionConnected())
                    return null;

                if (Stage == null || Stage.Vision == null)
                    return null;

                Task<VisionAlignResult> alignTask = Stage.Vision.TriggerAlignAsync(targetId);
                if (alignTask == null)
                    return null;

                VisionAlignResult result = await SequenceAwaiter.AwaitAsync(alignTask, null, ct).ConfigureAwait(false);
                WriteLog("InputStageDieMappingSequence",
                    "DryRun Vision GRAB request completed. step=" + stepName +
                    ", target=" + targetId +
                    ", result=" + (result != null ? "OK" : "NG"));
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "DryRun Vision GRAB request exception. step=" + stepName + ": " + ex.Message + " - SimFallback");
                return null;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestSimVisionOffsetAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                if (IsDryRunWithVisionDisabled())
                {
                    return new VisionAlignResult
                    {
                        DeltaX = 0.0,
                        DeltaY = 0.0,
                        DeltaTheta = 0.0,
                        PitchX = ResolvePitchX(),
                        PitchY = ResolvePitchY()
                    };
                }

                await Task.Delay(120, ct).ConfigureAwait(false);
                bool ok;
                double dx;
                double dy;
                lock (SimVisionRandomLock)
                {
                    ok = SimVisionRandom.Next(0, 100) >= 20;
                    dx = (SimVisionRandom.NextDouble() - 0.5) * 0.02;
                    dy = (SimVisionRandom.NextDouble() - 0.5) * 0.02;
                }

                if (!ok)
                    return null;

                return new VisionAlignResult
                {
                    DeltaX = dx,
                    DeltaY = dy,
                    DeltaTheta = 0.0,
                    PitchX = ResolvePitchX(),
                    PitchY = ResolvePitchY()
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Simulation vision offset failed. target=" + targetId + ", step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static bool IsDryRunWithVisionDisabled()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.DryRunMode && !settings.UseVision;
        }

        private static bool IsDryRunWithVisionConnected()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null || !settings.DryRunMode || !settings.UseVision)
                    return false;

                return QMC.CDT320.VisionComm.VisionCommandService.IsConnected(
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisCommandAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string guardReason;
                if (!VerifySharedRailAxisMove(axis, target, out guardReason))
                    return Fail("IN-STAGE-DIEMAP-SHARED-RAIL", Stage.Name,
                        description + " shared rail check failed. axis=" + axis + ", target=" + target + ". " + guardReason);

                int result = await AwaitStepWithCancellationAsync(Stage.MoveInputStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-DIEMAP-MOVE", Stage.Name,
                        description + " move command failed. axis=" + axis + ", target=" + target +
                        ", result=" + result + ". " + BuildAxisState(axis, target) + FormatLastStageMoveFailure());

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-MOVE-EX", Stage.Name, description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitAxisInPositionResultAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int waitCode = await Stage.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return Fail("IN-STAGE-DIEMAP-MOVE", Stage.Name,
                        description + " move/in-position wait failed. axis=" + axis + ", target=" + target +
                        ", waitCode=" + waitCode + ". " + BuildAxisState(axis, target));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-WAIT-EX", Stage.Name, description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckStageAxisInPosition(WaferStageAxis axis, double target, string description)
        {
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveStageAxis(axis);
                if (item == null)
                    return Fail("IN-STAGE-DIEMAP-AXIS", Stage.Name,
                        description + " axis is not available. " + BuildAxisState(axis, target));

                if (item.IsMoving || item.IsAlarm || !IsAxisInPosition(item, target))
                    return Fail("IN-STAGE-DIEMAP-POSITION", Stage.Name,
                        description + " final position check failed. axis=" + axis + ", target=" + target +
                        ". " + BuildAxisState(axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-POSITION-EX", Stage.Name, description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool VerifySharedRailAxisMove(WaferStageAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveStageAxis(axis);
                if (item == null)
                {
                    reason = "Axis is not available.";
                    return false;
                }

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(Context.Machine);
                if (service == null || !service.IsSharedRailAxis(item))
                    return true;

                return service.VerifySingleAxisMove(item, target, out reason);
            }
            catch (Exception ex)
            {
                reason = "SharedRailX check exception: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static bool IsRecipeInputDieMapMatchedToFrame(TapeFrameSpec frameSpec, RecipeProject project, DieMap map, string path)
        {
            try
            {
                if (map == null)
                    return true;

                if (IsExternalInputDieMap(map))
                {
                    // 현재 기준: 외부 웨이퍼맵 TXT는 파일 인덱스를 기준으로 사용하므로 Frame grid 검증을 건너뛴다.
                    return true;
                }

                int frameDieMapX = 0;
                int frameDieMapY = 0;
                double framePitchX = 0.0;
                double framePitchY = 0.0;
                string frameName = "";

                TapeFrameSubset inputFrame = project != null ? project.InputFrame : null;
                if (inputFrame != null)
                {
                    // 현재 기준: Recipe Input DieMap은 분리 저장된 InputFrame 기준으로 먼저 검증한다.
                    frameDieMapX = Math.Max(1, inputFrame.DieMapX);
                    frameDieMapY = Math.Max(1, inputFrame.DieMapY);
                    double dieSizeX = project != null && project.Die != null && project.Die.WidthMm > 0.0
                        ? project.Die.WidthMm
                        : inputFrame.DieSizeX;
                    double dieSizeY = project != null && project.Die != null && project.Die.HeightMm > 0.0
                        ? project.Die.HeightMm
                        : inputFrame.DieSizeY;
                    framePitchX = DieMapGenerator.CalculateCenterStep(dieSizeX, inputFrame.PitchX);
                    framePitchY = DieMapGenerator.CalculateCenterStep(dieSizeY, inputFrame.PitchY);
                    frameName = inputFrame.FrameSpecName ?? "";
                }
                else if (frameSpec != null)
                {
                    frameDieMapX = Math.Max(1, frameSpec.DieMapX);
                    frameDieMapY = Math.Max(1, frameSpec.DieMapY);
                    framePitchX = DieMapGenerator.CalculateCenterStep(frameSpec.DieSizeX, frameSpec.PitchX);
                    framePitchY = DieMapGenerator.CalculateCenterStep(frameSpec.DieSizeY, frameSpec.PitchY);
                    frameName = frameSpec.Name ?? "";
                }
                else if (project != null && project.Frame != null)
                {
                    frameDieMapX = Math.Max(1, project.Frame.DieMapX);
                    frameDieMapY = Math.Max(1, project.Frame.DieMapY);
                    double dieSizeX = project.Die != null && project.Die.WidthMm > 0.0
                        ? project.Die.WidthMm
                        : project.Frame.DieSizeX;
                    double dieSizeY = project.Die != null && project.Die.HeightMm > 0.0
                        ? project.Die.HeightMm
                        : project.Frame.DieSizeY;
                    framePitchX = DieMapGenerator.CalculateCenterStep(dieSizeX, project.Frame.PitchX);
                    framePitchY = DieMapGenerator.CalculateCenterStep(dieSizeY, project.Frame.PitchY);
                    frameName = project.Frame.FrameSpecName ?? "";
                }

                if (frameDieMapX <= 0 || frameDieMapY <= 0)
                    return true;

                bool mismatch =
                    map.DieMapX != frameDieMapX ||
                    map.DieMapY != frameDieMapY ||
                    (framePitchX > 0.0 && Math.Abs(map.PitchX - framePitchX) > 1e-6) ||
                    (framePitchY > 0.0 && Math.Abs(map.PitchY - framePitchY) > 1e-6);

                if (!mismatch)
                    return true;

                LastSourceInputDieMapFailure =
                    "Recipe input die map does not match frame spec. path=" + path +
                    ", frameSpecName=" + frameName +
                    ", mapDie=" + map.DieMapX + "x" + map.DieMapY +
                    ", frameDieMap=" + frameDieMapX + "x" + frameDieMapY +
                    ", mapPitch=(" + map.PitchX.ToString("F6") + "," + map.PitchY.ToString("F6") + ")" +
                    ", frameCenterStep=(" + framePitchX.ToString("F6") + "," + framePitchY.ToString("F6") + ").";
                WriteLog("InputStageDieMappingSequence", LastSourceInputDieMapFailure + " - Failed");
                return false;
            }
            catch (Exception ex)
            {
                LastSourceInputDieMapFailure = "Recipe input die map frame match check failed: " + ex.Message + ".";
                WriteLog("InputStageDieMappingSequence", LastSourceInputDieMapFailure + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private string FormatLastStageMoveFailure()
        {
            if (Stage == null || string.IsNullOrWhiteSpace(Stage.LastStageMoveFailureMessage))
                return string.Empty;

            return ", lastStageMoveFailure=" + Stage.LastStageMoveFailureMessage;
        }

        private QMC.Common.Motion.BaseAxis ResolveStageAxis(WaferStageAxis axis)
        {
            try
            {
                switch (axis)
                {
                    // 웨이퍼 Y축 반환
                    case WaferStageAxis.WaferY: return Stage.StageY;
                    // 웨이퍼 T축 반환
                    case WaferStageAxis.WaferT: return Stage.StageT;
                    // 웨이퍼 확장 Z축 반환
                    case WaferStageAxis.WaferExpandingZ: return Stage.ExpanderZ;
                    // 비전 X축 반환
                    case WaferStageAxis.VisionX: return Stage.CameraX;
                    // 니들 X축 반환
                    case WaferStageAxis.NeedleX: return Stage.NeedleBlockX;
                    // 니들 Z축 반환
                    case WaferStageAxis.NeedleZ: return Stage.NeedleZ;
                    // 이젝트 핀 Z축 반환
                    case WaferStageAxis.EjectPinZ: return Stage.EjectPinZ;
                    default: return null;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private string BuildDieMappingServoReason()
        {
            string reason = string.Empty;
            AppendServoOff(ref reason, WaferStageAxis.WaferY, Stage.StageY);
            AppendServoOff(ref reason, WaferStageAxis.VisionX, Stage.CameraX);
            return reason;
        }

        private void AppendServoOff(ref string reason, WaferStageAxis axis, QMC.Common.Motion.BaseAxis item)
        {
            if (item == null || item.IsServoOn)
                return;

            if (reason.Length > 0)
                reason += " ";
            reason += BuildAxisState(axis, item.ActualPosition) + ";";
        }

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            try
            {
                if (axis == null)
                    return false;

                double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.05;
                return Math.Abs(axis.ActualPosition - target) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool CanSkipAxisMoveCommand(QMC.Common.Motion.BaseAxis axis, double target)
        {
            double tolerance = axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
            return axis.IsAtTargetPosition(target, tolerance);
        }

        private TapeFrameSpec ResolveFrameSpecForWafer(WaferMaterial wafer)
        {
            try
            {
                string specName = wafer != null ? MaterialStateService.NormalizeInputTapeFrameSpecName(wafer.TapeFrameSpecName) : "";
                if (string.IsNullOrWhiteSpace(specName))
                {
                    specName = MaterialStateService.ResolveInputTapeFrameSpecName(0);
                    if (wafer != null && !string.IsNullOrWhiteSpace(specName))
                    {
                        wafer.TapeFrameSpecName = specName;
                        MaterialStateService.NotifyAndSave("InputStageDieMapSpecResolve");
                    }
                }
                else if (wafer != null &&
                         !string.Equals(wafer.TapeFrameSpecName, specName, StringComparison.OrdinalIgnoreCase))
                {
                    wafer.TapeFrameSpecName = specName;
                    MaterialStateService.NotifyAndSave("InputStageDieMapSpecNormalize");
                }

                TapeFrameSpec spec = MaterialSpecs.FindFrame(specName);
                if (spec != null)
                    return spec;

                specName = MaterialStateService.ResolveInputTapeFrameSpecName(0);
                return MaterialSpecs.FindFrame(specName);
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence", "Frame spec resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private void NormalizeStoredAlignPitchToFrameSpec()
        {
            try
            {
                if (_wafer == null || _frameSpec == null)
                    return;

                bool changed = false;
                double frameStepX = DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeX, _frameSpec.PitchX);
                double frameStepY = DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeY, _frameSpec.PitchY);
                if (ShouldUseFramePitch(_wafer.InputStageAlignPitchX, frameStepX))
                {
                    WriteLog("InputStageDieMappingSequence",
                        "Stored align pitch X is outside configured tolerance. frame pitch is used. wafer=" +
                        _wafer.WaferId +
                        ", stored=" + _wafer.InputStageAlignPitchX.ToString("F6") +
                        ", frameGap=" + _frameSpec.PitchX.ToString("F6") +
                        ", frameCenterStep=" + frameStepX.ToString("F6") +
                        ", tolerance=" + AlignPitchCompareToleranceMm.ToString("F6") + " - Check");
                    _wafer.InputStageAlignPitchX = frameStepX;
                    changed = true;
                }

                if (ShouldUseFramePitch(_wafer.InputStageAlignPitchY, frameStepY))
                {
                    WriteLog("InputStageDieMappingSequence",
                        "Stored align pitch Y is outside configured tolerance. frame pitch is used. wafer=" +
                        _wafer.WaferId +
                        ", stored=" + _wafer.InputStageAlignPitchY.ToString("F6") +
                        ", frameGap=" + _frameSpec.PitchY.ToString("F6") +
                        ", frameCenterStep=" + frameStepY.ToString("F6") +
                        ", tolerance=" + AlignPitchCompareToleranceMm.ToString("F6") + " - Check");
                    _wafer.InputStageAlignPitchY = frameStepY;
                    changed = true;
                }

                if (changed)
                    _wafer.UpdatedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageDieMappingSequence",
                    "Stored align pitch normalize failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool ShouldUseFramePitch(double stored, double frame)
        {
            if (frame <= 0.0)
                return false;
            if (stored <= 0.0)
                return true;

            return Math.Abs(stored - frame) > AlignPitchCompareToleranceMm;
        }

        private bool TryGetMappedPoint(string name, out MappedMarkPoint point)
        {
            return _mappedPoints.TryGetValue(name, out point);
        }

        private string ResolveTargetId()
        {
            // Center 기반 Die Mapping은 Wafer Align과 동일한 AlignDieFinder 정책 키만 사용한다.
            return VisionAlignTargetIds.Center;
        }

        private int ResolveDieMapRetryCount()
        {
            try
            {
                if (Options.DieMapVisionRetryCount > 0)
                    return Options.DieMapVisionRetryCount;
                if (Stage != null && Stage.Recipe != null && Stage.Recipe.DieMap != null && Stage.Recipe.DieMap.VisionRetryCount > 0)
                    return Stage.Recipe.DieMap.VisionRetryCount;
            }
            catch
            {
            }
            finally
            {
            }

            return 3;
        }

        private int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
        }

        private double ResolvePitchX()
        {
            try
            {
                if (_frameSpec != null && _frameSpec.DieSizeX > 0.0 &&
                    !double.IsNaN(_frameSpec.PitchX) && !double.IsInfinity(_frameSpec.PitchX) && _frameSpec.PitchX >= 0.0)
                    return DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeX, _frameSpec.PitchX);
                if (Stage != null && Stage.PitchX > 0.0)
                    return Stage.PitchX;
            }
            catch
            {
            }
            finally
            {
            }

            return 1.0;
        }

        private double ResolvePitchY()
        {
            try
            {
                if (_frameSpec != null && _frameSpec.DieSizeY > 0.0 &&
                    !double.IsNaN(_frameSpec.PitchY) && !double.IsInfinity(_frameSpec.PitchY) && _frameSpec.PitchY >= 0.0)
                    return DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeY, _frameSpec.PitchY);
                if (Stage != null && Stage.PitchY > 0.0)
                    return Stage.PitchY;
            }
            catch
            {
            }
            finally
            {
            }

            return 1.0;
        }

        private int ValidateStoredAlignResultForCurrentMode()
        {
            if (_wafer == null || !_wafer.HasInputStageAlignResult)
            {
                InputStageHybridResultSession.Clear();
                return Fail("IN-STAGE-DIEMAP-ALIGN-RESULT-MODE", "Material",
                    "Die Mapping에 필요한 InputStage Align 결과가 없습니다.");
            }

            string storedMode = _wafer.InputStageAlignResultMode ?? "";
            if (!InputStageResultMode.IsKnown(storedMode))
            {
                InputStageHybridResultSession.Clear();
                return Fail("IN-STAGE-DIEMAP-ALIGN-RESULT-MODE", "Material",
                    "알 수 없는 InputStage Align 결과 모드입니다. mode=" + storedMode);
            }

            if (_hybridVirtualFrameActive)
            {
                if (!InputStageResultMode.IsHybrid(storedMode) ||
                    string.IsNullOrWhiteSpace(_wafer.InputStageAlignResultRunId) ||
                    !InputStageHybridResultSession.IsCurrentAlign(
                        _wafer.WaferId,
                        _wafer.InputStageAlignResultRunId))
                {
                    InputStageHybridResultSession.Clear();
                    return Fail("IN-STAGE-DIEMAP-HYBRID-ALIGN-SESSION", "Material",
                        "Hybrid Die Mapping은 현재 프로그램 실행에서 같은 Wafer로 완료한 Hybrid Align 결과만 사용할 수 있습니다. " +
                        "waferId=" + _wafer.WaferId +
                        ", storedMode=" + storedMode +
                        ", alignRunId=" + (_wafer.InputStageAlignResultRunId ?? "") +
                        ". Align을 다시 실행하십시오.");
                }
            }
            else if (InputStageResultMode.IsHybrid(storedMode))
            {
                InputStageHybridResultSession.Clear();
                return Fail("IN-STAGE-DIEMAP-HYBRID-RESULT-REUSE", "Material",
                    "HybridRealVisionSimMotion에서 생성된 Align 결과는 현재 모션/Vision 모드에서 재사용할 수 없습니다. " +
                    "waferId=" + _wafer.WaferId + ". 현재 모드에서 Align을 다시 실행하십시오.");
            }

            return 0;
        }

        private int ConfigureHybridVirtualFrameMode()
        {
            bool realVisionInSimulation = AutoVisionRequestService.IsRealVisionInSimulationActive();
            bool cameraXSimulated = IsSimulatedAxis(Stage != null ? Stage.CameraX : null);
            bool stageYSimulated = IsSimulatedAxis(Stage != null ? Stage.StageY : null);
            bool stageTSimulated = IsSimulatedAxis(Stage != null ? Stage.StageT : null);

            _hybridVirtualFrameActive = false;
            if (!realVisionInSimulation)
            {
                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping Vision/motion mode resolved. realVisionInSimulation=False" +
                    ", cameraXSimulated=" + cameraXSimulated +
                    ", stageYSimulated=" + stageYSimulated +
                    ", stageTSimulated=" + stageTSimulated +
                    ", mode=" + ResolveVisionMotionModeName() + " - Ok");
                return 0;
            }

            int simulatedAxisCount = (cameraXSimulated ? 1 : 0) +
                (stageYSimulated ? 1 : 0) +
                (stageTSimulated ? 1 : 0);
            if (simulatedAxisCount != 0 && simulatedAxisCount != 3)
            {
                InputStageHybridResultSession.Clear();
                return Fail("IN-STAGE-DIEMAP-HYBRID-AXIS-MODE", Stage != null ? Stage.Name : "InputStageUnit",
                    "Real Vision simulation mode requires CameraX/StageY/StageT to be all simulated or all real. " +
                    "cameraXSimulated=" + cameraXSimulated +
                    ", stageYSimulated=" + stageYSimulated +
                    ", stageTSimulated=" + stageTSimulated);
            }

            _hybridVirtualFrameActive = simulatedAxisCount == 3;
            if (_hybridVirtualFrameActive && !Options.EnableMotion)
            {
                _hybridVirtualFrameActive = false;
                InputStageHybridResultSession.Clear();
                return Fail("IN-STAGE-DIEMAP-HYBRID-MOTION-DISABLED", Stage != null ? Stage.Name : "InputStageUnit",
                    "HybridRealVisionSimMotion requires simulated X/Y/T motion commands to be enabled.");
            }

            WriteLog("InputStageDieMappingSequence",
                "Die Mapping Vision/motion mode resolved. realVisionInSimulation=True" +
                ", cameraXSimulated=" + cameraXSimulated +
                ", stageYSimulated=" + stageYSimulated +
                ", stageTSimulated=" + stageTSimulated +
                ", mode=" + ResolveVisionMotionModeName() + " - Ok");
            return 0;
        }

        private static bool IsSimulatedAxis(BaseAxis axis)
        {
            return axis != null &&
                (axis is QMC.CDT320.SimAxis ||
                 (axis.Config != null && axis.Config.IsSimulationMode));
        }

        private string ResolveVisionMotionModeName()
        {
            if (_hybridVirtualFrameActive)
                return "HybridRealVisionSimMotion";
            if (AutoVisionRequestService.IsRealVisionInSimulationActive())
                return "ExistingRealVisionMotion";
            return IsSimulationOrDryRun() ? "ExistingSyntheticVisionSimulation" : "ExistingRealMotionRealVision";
        }

        private static bool IsFiniteNumber(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private bool IsSimulationOrDryRun()
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings != null && (settings.SimulationMode || settings.DryRunMode || settings.BypassHardware))
                    return true;

                if (Context != null && Context.Controller != null &&
                    (Context.Controller.GlobalDryRun || Context.Controller.DryRun))
                    return true;

                return Stage != null && Stage.IsInputStageSimulationOrDryRun();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsAxisSimulationMode()
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings != null)
                {
                    if (settings.SimulationMode || settings.BypassHardware || !settings.UseAjin)
                        return true;

                    if (settings.DryRunMode)
                        return false;
                }

                return Stage != null &&
                       Stage.Setup != null &&
                       Stage.Setup.IsSimulationMode;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static double ResolveWaferRadiusFromSpecOrMarks(TapeFrameSpec spec, MappedMarkPoint left, MappedMarkPoint right, MappedMarkPoint top, MappedMarkPoint bottom)
        {
            try
            {
                if (spec != null && spec.OuterDiameterMm > 0.0)
                    return spec.OuterDiameterMm / 2.0;

                double radiusX = Math.Abs(right.X - left.X) / 2.0;
                double radiusY = Math.Abs(bottom.Y - top.Y) / 2.0;
                double radius = (radiusX + radiusY) / 2.0;
                return radius > 0.0 ? radius : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private static double ResolveWaferRadiusFromSpecOrMap(
            TapeFrameSpec spec,
            int dieMapX,
            int dieMapY,
            double pitchX,
            double pitchY)
        {
            try
            {
                if (spec != null && spec.OuterDiameterMm > 0.0)
                    return spec.OuterDiameterMm / 2.0;

                double radiusX = Math.Abs(pitchX) * Math.Max(0, dieMapX - 1) / 2.0;
                double radiusY = Math.Abs(pitchY) * Math.Max(0, dieMapY - 1) / 2.0;
                double radius = Math.Max(radiusX, radiusY);
                return radius > 0.0 ? radius : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private static bool IsInsideWaferCircle(double x, double y, double centerX, double centerY, double radius)
        {
            try
            {
                if (radius <= 0.0)
                    return true;

                double dx = x - centerX;
                double dy = y - centerY;
                double distanceSquared = (dx * dx) + (dy * dy);
                double radiusWithTolerance = radius + 0.0001;
                return distanceSquared <= radiusWithTolerance * radiusWithTolerance;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static string BuildFrameObjId(WaferMaterial wafer)
        {
            string waferId = wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId) ? wafer.WaferId : "InputStage";
            return waferId + "-DIEMAP-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitIntAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return -1;
            }
            finally
            {
            }
        }

        private sealed class MappedMarkPoint
        {
            public string Name { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double OffsetX { get; set; }
            public double OffsetY { get; set; }
        }

        private sealed class SearchOffset
        {
            public SearchOffset(string name, double x, double y)
            {
                Name = name;
                X = x;
                Y = y;
            }

            public string Name { get; private set; }
            public double X { get; private set; }
            public double Y { get; private set; }
        }

        private static SearchOffset[] BuildFineSearchOffsets(double limitX, double limitY)
        {
            double innerX = limitX / 2.0;
            double innerY = limitY / 2.0;
            return new[]
            {
                new SearchOffset("InnerUp", 0.0, innerY),
                new SearchOffset("InnerDown", 0.0, -innerY),
                new SearchOffset("InnerLeft", -innerX, 0.0),
                new SearchOffset("InnerRight", innerX, 0.0),
                new SearchOffset("InnerLeftUp", -innerX, innerY),
                new SearchOffset("InnerRightUp", innerX, innerY),
                new SearchOffset("InnerLeftDown", -innerX, -innerY),
                new SearchOffset("InnerRightDown", innerX, -innerY),
                new SearchOffset("OuterUp", 0.0, limitY),
                new SearchOffset("OuterDown", 0.0, -limitY),
                new SearchOffset("OuterLeft", -limitX, 0.0),
                new SearchOffset("OuterRight", limitX, 0.0),
                new SearchOffset("OuterLeftUp", -limitX, limitY),
                new SearchOffset("OuterRightUp", limitX, limitY),
                new SearchOffset("OuterLeftDown", -limitX, -limitY),
                new SearchOffset("OuterRightDown", limitX, -limitY)
            };
        }
    }
}
