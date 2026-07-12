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
        // Legacy 4-point mapping steps are kept for reference. The active sequence now uses Center die search.
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
        private const double AlignPitchCompareToleranceMm = 0.05;

        private readonly Dictionary<string, MappedMarkPoint> _mappedPoints = new Dictionary<string, MappedMarkPoint>(StringComparer.OrdinalIgnoreCase);
        private WaferMaterial _wafer;
        private TapeFrameSpec _frameSpec;
        private DieMap _dieMap;
        private WaferMapData _waferMap;
        private int _createdDieCount;
        private double _dieMapCenterX;
        private double _dieMapCenterY;
        private double _dieMapCenterSearchOffsetX;
        private double _dieMapCenterSearchOffsetY;
        private double _centerDieTargetX;
        private double _centerDieTargetY;

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
                int result = CheckUnit(InputStageDieMappingStep.MoveNeedleZSafeBeforeMapping);
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
                // Center die를 먼저 찾고 그 오프셋을 전체 DieMap 좌표에 반영한다.

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

                _frameSpec = ResolveFrameSpecForWafer(_wafer);

                result = RestoreStageAlignRuntimeResultFromMaterial();
                if (result != 0) return result;

                string thetaReason;
                if (!MaterialStateService.IsInputStageThetaAlignComplete(_wafer, out thetaReason))
                    return Fail("IN-STAGE-DIEMAP-THETA-ALIGN", Stage.Name,
                        "Die Mapping 전에 InputStage T 보정이 완료되어야 합니다. " + thetaReason);

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

                if (Options.RequireVisionAlign && Stage.Vision == null && !IsSimulationOrDryRun())
                    return Fail("IN-STAGE-DIEMAP-VISION", Stage.Name, "Vision client is required but not available.");

                _mappedPoints.Clear();
                _dieMapCenterX = 0.0;
                _dieMapCenterY = 0.0;
                _dieMapCenterSearchOffsetX = 0.0;
                _dieMapCenterSearchOffsetY = 0.0;
                _centerDieTargetX = 0.0;
                _centerDieTargetY = 0.0;
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

                if (!Stage.IsNeedleZInSafePosition())
                {
                    double target = Stage.Recipe.NeedleZ.AvoidPosition;
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

                bool restoreAlign = Stage.PitchX <= 0.0 || Stage.PitchY <= 0.0;
                if (restoreAlign &&
                    _wafer.HasInputStageAlignResult &&
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
                IsAxisInPosition(ResolveStageAxis(WaferStageAxis.WaferExpandingZ), Stage.Recipe.WaferZ.ProcessPosition);

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
                    double centerX;
                    double centerY;
                    ResolveProcessCenter(out centerX, out centerY);

                    string areaReason;
                    if (!Stage.IsInputStageWorkPointInArea(centerX, centerY, out areaReason))
                        return Fail("IN-STAGE-DIEMAP-CENTER-WORK-AREA", Stage.Name,
                            "Die Mapping Center 위치가 작업 영역 밖입니다. centerX=" +
                            centerX.ToString("F6") +
                            ", centerY=" + centerY.ToString("F6") +
                            ", reason=" + areaReason);

                    int result = await MoveVisionXYPointSafelyAsync(
                        centerX,
                        centerY,
                        "Die Mapping Center",
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
                    "Die Mapping Center 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
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

                double baseX = Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0;
                double baseY = Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0;
                VisionAlignResult vision = await RequestVisionPcOffsetWithRetryAsync(ResolveTargetId(), VisionAlignTargetIds.Center, ct).ConfigureAwait(false);
                if (vision == null)
                {
                    vision = await SearchVisionMarkAroundCurrentPointAsync(
                        ResolveTargetId(),
                        VisionAlignTargetIds.Center,
                        "Die Mapping Center",
                        baseX,
                        baseY,
                        ct).ConfigureAwait(false);
                }

                if (vision == null)
                    return Fail("IN-STAGE-DIEMAP-CENTER-VISION", "Vision",
                        "Die Mapping Center 다이를 찾지 못했습니다. 센터와 주변 8방향 탐색을 모두 실패했습니다.");

                _dieMapCenterX = (Stage.CameraX != null ? Stage.CameraX.ActualPosition : baseX) + vision.DeltaX;
                _dieMapCenterY = (Stage.StageY != null ? Stage.StageY.ActualPosition : baseY) + vision.DeltaY;
                _dieMapCenterSearchOffsetX = _dieMapCenterX - baseX;
                _dieMapCenterSearchOffsetY = _dieMapCenterY - baseY;

                _mappedPoints[VisionAlignTargetIds.Center] = new MappedMarkPoint
                {
                    Name = VisionAlignTargetIds.Center,
                    X = _dieMapCenterX,
                    Y = _dieMapCenterY,
                    OffsetX = _dieMapCenterSearchOffsetX,
                    OffsetY = _dieMapCenterSearchOffsetY
                };

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping Center 다이를 찾았습니다. baseX=" + baseX.ToString("F6") +
                    ", baseY=" + baseY.ToString("F6") +
                    ", centerX=" + _dieMapCenterX.ToString("F6") +
                    ", centerY=" + _dieMapCenterY.ToString("F6") +
                    ", mapOffsetX=" + _dieMapCenterSearchOffsetX.ToString("F6") +
                    ", mapOffsetY=" + _dieMapCenterSearchOffsetY.ToString("F6") +
                    ", visionDx=" + vision.DeltaX.ToString("F6") +
                    ", visionDy=" + vision.DeltaY.ToString("F6") + " - Ok");

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
                    "Die Mapping Center 다이 탐색 중 예외가 발생했습니다. error=" + ex.Message);
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

            return await WaitAxisInPositionResultAsync(axis, target, description, ct).ConfigureAwait(false);
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

                if (Stage.IsWaferAlignThetaInPosition())
                    return 0;

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

                SearchOffset[] offsets = BuildOnePitchSearchOffsets(pitchX, pitchY);
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
                            description + " 주변 탐색에서 다이를 찾았습니다. direction=" +
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
                    description + " 주변 8방향 탐색에서 다이를 찾지 못했습니다. baseX=" +
                    baseX.ToString("F6") +
                    ", baseY=" + baseY.ToString("F6") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") + " - Failed");
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
                MappedMarkPoint center;
                if (!TryGetMappedPoint(VisionAlignTargetIds.Center, out center))
                {
                    return Fail("IN-STAGE-DIEMAP-CENTER-POINT", "Vision",
                        "Die Mapping 계산에는 Center die 탐색 결과가 필요합니다.");
                }

                DieMap sourceMap = ResolveSourceInputDieMap(_wafer, _frameSpec);
                if (!IsUsableSourceMap(sourceMap))
                {
                    return Fail("IN-STAGE-DIEMAP-SOURCE-MAP", "InputStageDieMappingSequence",
                        "Input die map is not available. Create and save an input wafer map from Recipe > INPUT MAP CREATE first. " +
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
                double centerX = center.X;
                double centerY = center.Y;
                string centerSource = "CenterDieSearch";
                double originX = sourceMapIsExternal
                    ? centerX + (signX * sourceMap.OriginX)
                    : centerX - (signX * pitchX * Math.Max(0, dieMapX - 1) / 2.0);
                double originY = sourceMapIsExternal
                    ? centerY + (signY * sourceMap.OriginY)
                    : centerY - (signY * pitchY * Math.Max(0, dieMapY - 1) / 2.0);
                double waferRadius = ResolveWaferRadiusFromSpecOrMap(_frameSpec, dieMapX, dieMapY, pitchX, pitchY);
                int centerCol = Math.Max(0, Math.Min(dieMapX - 1, dieMapX / 2));
                int centerRow = Math.Max(0, Math.Min(dieMapY - 1, dieMapY / 2));
                _centerDieTargetX = centerX;
                _centerDieTargetY = centerY;

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
                    SourceFormat = sourceMap != null ? sourceMap.SourceFormat : "",
                    SourcePitchFromFile = sourceMap != null && sourceMap.SourcePitchFromFile,
                    SourceDeclaredCount = sourceMap != null ? sourceMap.SourceDeclaredCount : 0,
                    SourceFirstX = sourceMap != null ? sourceMap.SourceFirstX : -1,
                    SourceFirstY = sourceMap != null ? sourceMap.SourceFirstY : -1,
                    SourceFirstPosX = sourceMap != null ? sourceMap.SourceFirstPosX : double.NaN,
                    SourceFirstPosY = sourceMap != null ? sourceMap.SourceFirstPosY : double.NaN,
                    CreatedAt = DateTime.Now
                };

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
                            ? centerX + signX * sourceEntry.PosX
                            : originX + signX * pitchX * col;
                        double y = sourceMapIsExternal && sourceEntry != null
                            ? centerY + signY * sourceEntry.PosY
                            : originY + signY * pitchY * row;
                        if (col == centerCol && row == centerRow)
                        {
                            _centerDieTargetX = x;
                            _centerDieTargetY = y;
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
                            EquipmentGridX = sourceEntry != null ? sourceEntry.EquipmentGridX : mapX - Math.Max(0, dieMapX - 1) / 2.0,
                            EquipmentGridY = sourceEntry != null ? sourceEntry.EquipmentGridY : Math.Max(0, dieMapY - 1) / 2.0 - mapY,
                            PosX = x,
                            PosY = y,
                            DieUid = sourceEntry != null && !string.IsNullOrWhiteSpace(sourceEntry.DieUid)
                                ? sourceEntry.DieUid
                                : BuildDieId(_wafer, mapY, mapX)
                        });
                    }
                }

                ApplyInputPickupSequence(_dieMap);
                int orderedCount = CountSequencedTargets(_dieMap);

                WriteLog("InputStageDieMappingSequence",
                    "Die map coordinate mapping calculated. grid=" + dieMapX + "x" + dieMapY +
                    ", sourceMap=" + (sourceMap != null ? sourceMap.FrameObjId : "none") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") +
                    ", centerSource=" + centerSource +
                    ", centerX=" + centerX.ToString("F6") +
                    ", centerY=" + centerY.ToString("F6") +
                    ", centerSearchOffsetX=" + _dieMapCenterSearchOffsetX.ToString("F6") +
                    ", centerSearchOffsetY=" + _dieMapCenterSearchOffsetY.ToString("F6") +
                    ", centerDieTargetX=" + _centerDieTargetX.ToString("F6") +
                    ", centerDieTargetY=" + _centerDieTargetY.ToString("F6") +
                    ", signX=" + signX.ToString("F1") +
                    ", signY=" + signY.ToString("F1") +
                    ", originX=" + originX.ToString("F6") +
                    ", originY=" + originY.ToString("F6") +
                    ", radius=" + waferRadius.ToString("F6") +
                    ", outerDiameter=" + (_frameSpec != null ? _frameSpec.OuterDiameterMm.ToString("F6") : "0") +
                    ", targetCount=" + targetCount +
                    ", orderedCount=" + orderedCount + " - Ok");

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
                            "Die Mapping 계산 후 Center die 위치가 작업 영역 밖입니다. targetX=" +
                            _centerDieTargetX.ToString("F6") +
                            ", targetY=" + _centerDieTargetY.ToString("F6") +
                            ", reason=" + areaReason);
                    }

                    int result = await MoveVisionXYPointSafelyAsync(
                        _centerDieTargetX,
                        _centerDieTargetY,
                        "Die Mapping 계산 후 Center die 위치",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                WriteLog("InputStageDieMappingSequence",
                    "Die Mapping 계산 후 Center die 위치로 이동했습니다. targetX=" +
                    _centerDieTargetX.ToString("F6") +
                    ", targetY=" + _centerDieTargetY.ToString("F6") +
                    ", centerOffsetX=" + _dieMapCenterSearchOffsetX.ToString("F6") +
                    ", centerOffsetY=" + _dieMapCenterSearchOffsetY.ToString("F6") + " - Ok");

                CurrentStep = InputStageDieMappingStep.ApplyDieMap;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-DIEMAP-CENTER-DIE-MOVE-EX", Stage != null ? Stage.Name : "InputStageUnit",
                    "Die Mapping 계산 후 Center die 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
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

                InputStageDieMapApplyResult applyResult = InputStageDieMapApplyService.Apply(
                    new InputStageDieMapApplyRequest
                    {
                        Stage = Stage,
                        Controller = Context != null ? Context.Controller : null,
                        Bus = Context != null ? Context.Bus : null,
                        DieMap = _dieMap,
                        WaferMap = _waferMap,
                        ExpectedWafer = _wafer,
                        PickupOptions = ResolveInputPickupSubset(),
                        Source = "InputStageDieMappingSequence.ApplyDieMap",
                        SaveReason = "InputStageDieMapping",
                        PublishReadySignals = true
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
                    ", dieCount=" + _createdDieCount + " - Ok");

                CurrentStep = ShouldMoveVisionXAvoidAfterManualDieMapping()
                    ? InputStageDieMappingStep.MoveVisionXAvoidAfterManual
                    : InputStageDieMappingStep.Complete;
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
                    CurrentStep = InputStageDieMappingStep.Complete;
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

                CurrentStep = InputStageDieMappingStep.Complete;
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
                bool recipeMapConfigured = IsRecipeInputDieMapConfigured();
                DieMap recipeMap = LoadRecipeInputDieMap(frameSpec);
                if (IsUsableSourceMap(recipeMap))
                    return ApplyInputPickupSequence(recipeMap);

                if (IsManagedInputMapApprovalRequired())
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
                WriteLog("InputStageDieMappingSequence", "Source input die map resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
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

        private static DieMap ApplyInputPickupSequence(DieMap map)
        {
            try
            {
                if (map == null)
                    return null;

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

        private static PickupSubset ResolveInputPickupSubset()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return new PickupSubset();

                if (project.InputPickup != null)
                    return project.InputPickup;
                if (project.Pickup != null)
                    return project.Pickup;
                return new PickupSubset();
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

                if (_wafer != null &&
                    !string.IsNullOrWhiteSpace(_wafer.WaferId) &&
                    !string.Equals(_wafer.WaferId, stateWafer.WaferId, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("IN-STAGE-DIEMAP-WAFER-MISMATCH", "Material",
                        "Die Mapping 대상 Wafer와 MaterialState InputStage Wafer가 다릅니다. " +
                        "sequenceWafer=" + _wafer.WaferId +
                        ", stateWafer=" + stateWafer.WaferId);
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
                    string dieId = string.IsNullOrWhiteSpace(entry.DieUid) ? BuildDieId(wafer, mapY, mapX) : entry.DieUid;
                    entry.DieUid = dieId;
                    entry.DieMapX = mapX;
                    entry.DieMapY = mapY;
                    entry.OriginalMapX = originalX;
                    entry.OriginalMapY = originalY;
                    DieMaterial die = MaterialStateService.GetOrCreateDieMaterial(dieId);
                    die.WaferID_Input = wafer.WaferId;
                    die.WaferID_Output = "";
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
                if (IsSimulationOrDryRun())
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
                AxisMoveWaitResult waitResult = await Stage.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("IN-STAGE-DIEMAP-MOVE", waitResult), Stage.Name,
                        description + " move/in-position wait failed. axis=" + axis + ", target=" + target +
                        ". " + FormatAxisMoveWaitResult(waitResult, BuildAxisState(axis, target)));

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
            try
            {
                if (!string.IsNullOrWhiteSpace(Options.DieMapVisionTargetId))
                    return Options.DieMapVisionTargetId.Trim();
                if (Stage != null && Stage.Recipe != null && Stage.Recipe.DieMap != null && !string.IsNullOrWhiteSpace(Stage.Recipe.DieMap.VisionTargetId))
                    return Stage.Recipe.DieMap.VisionTargetId.Trim();
            }
            catch
            {
            }
            finally
            {
            }

            return "DieMapMark";
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

        private void ResolveProcessCenter(out double centerX, out double centerY)
        {
            centerX = Stage != null ? Stage.ResolveWorkAreaCenterX() : 0.0;
            centerY = Stage != null ? Stage.ResolveWorkAreaCenterY() : 0.0;

            try
            {
                if (Stage != null && Stage.Recipe != null)
                {
                    Stage.Recipe.EnsurePositionObjects();
                    centerX = Stage.Recipe.VisionX.ProcessPosition;
                    centerY = Stage.Recipe.WaferY.ProcessPosition;
                }
            }
            catch
            {
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

        private static string BuildDieId(WaferMaterial wafer, int row, int col)
        {
            string waferId = wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId) ? wafer.WaferId : "WAFER";
            return waferId + "-D" + row.ToString("000") + "-" + col.ToString("000");
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

        private static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAxisWaitAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
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

        private static SearchOffset[] BuildOnePitchSearchOffsets(double pitchX, double pitchY)
        {
            return new[]
            {
                new SearchOffset("Up", 0.0, pitchY),
                new SearchOffset("Down", 0.0, -pitchY),
                new SearchOffset("Left", -pitchX, 0.0),
                new SearchOffset("Right", pitchX, 0.0),
                new SearchOffset("LeftUp", -pitchX, pitchY),
                new SearchOffset("RightUp", pitchX, pitchY),
                new SearchOffset("LeftDown", -pitchX, -pitchY),
                new SearchOffset("RightDown", pitchX, -pitchY)
            };
        }
    }
}

