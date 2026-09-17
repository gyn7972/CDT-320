using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;
using QMC.CDT320.VisionComm;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputStageAlignStep
    {
        Idle,
        CheckUnit,
        MoveVisionProcessPosition,
        MoveCenterMarkPosition,
        RequestCenterMark,
        WaitCenterMarkResult,
        CorrectTheta,
        RequestThetaVerify,
        WaitThetaVerifyResult,
        MoveRef1Position,
        RequestRef1Mark,
        WaitRef1MarkResult,
        MoveRef2Position,
        RequestRef2Mark,
        WaitRef2MarkResult,
        CalculateAlignResult,
        MoveCenterAfterRefAlign,
        ApplyAlignResult,
        Complete,
        Error
    }

    internal sealed class InputStageAlignSequence : InputStageSequenceBase<InputStageAlignStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        // 기존 조건: 얼라인 허용값 3종이 코드 상수/필드로 고정되어 UI에서 조정할 수 없었다.
        // private double AlignPitchCompareToleranceMm = 0.1;
        // private double AlignCenterToleranceMm = 0.1;
        // private double MaxEffectiveThetaToleranceDeg = 0.05;
        // 현재 기준: InputStage Config 값으로 제어한다. (미설정/0 이하이면 현재 운용값 기본 사용)
        // To do: [얼라인 허용값 파라미터화] 상수 -> Config 이관.
        private double AlignPitchCompareToleranceMm
        {
            get
            {
                return Stage != null && Stage.Config != null && Stage.Config.AlignPitchCompareToleranceMm > 0.0
                    ? Stage.Config.AlignPitchCompareToleranceMm
                    : 0.1;
            }
        }

        private double AlignCenterToleranceMm
        {
            get
            {
                return Stage != null && Stage.Config != null && Stage.Config.AlignCenterToleranceMm > 0.0
                    ? Stage.Config.AlignCenterToleranceMm
                    : 0.1;
            }
        }

        private double MaxEffectiveThetaToleranceDeg
        {
            get
            {
                return Stage != null && Stage.Config != null && Stage.Config.MaxEffectiveThetaToleranceDeg > 0.0
                    ? Stage.Config.MaxEffectiveThetaToleranceDeg
                    : 0.05;
            }
        }
        private int _alignVisionRetryCount = 3;
        private int _alignSearchPointCount = 8;
        private int _alignVisionTimeoutMs = 5000;
        private WaferMapData _map;
        private WaferMaterial _wafer;
        private TapeFrameSpec _frameSpec;
        private bool _resolvedAlignMapIsExternal;
        private VisionAlignResult _centerResult;
        private VisionAlignResult _verifyCenterResult;
        private VisionAlignResult _ref1Result;
        private VisionAlignResult _ref2Result;
        private double _ref1X;
        private double _ref1Y;
        private double _ref2X;
        private double _ref2Y;
        private double _originX;
        private double _originY;
        private double _pitchX;
        private double _pitchY;
        private double _thetaFromTwoPoint;
        private int _thetaRetryCount;
        private int _twoPointThetaRetryCount;
        private bool _alignThetaReferenceReady;
        private double _alignThetaReferenceT;
        private bool _alignAnchorReady;
        private int _alignAnchorRow;
        private int _alignAnchorCol;
        private double _alignAnchorX;
        private double _alignAnchorY;
        private bool _manualAlignFallbackActive;
        private bool _alignProcessReferenceReady;
        private double _alignProcessReferenceX;
        private double _alignProcessReferenceY;
        private bool _finalAlignOffsetReady;
        private double _finalAlignOffsetX;
        private double _finalAlignOffsetY;
        private bool _preserveThetaOnProcessMove;
        private bool _hybridVirtualFrameActive;
        private double _hybridCenterAppliedMoveX;
        private double _hybridCenterAppliedMoveY;
        private double _hybridCenterAppliedTheta;
        private double _hybridTwoPointAppliedTheta;
        private string _alignResultRunId;
        private Task<VisionAlignResult> _pendingVisionTask;
        private CancellationTokenSource _pendingVisionCts;
        private string _pendingVisionStepName;
        private string _pendingVisionTargetId;

        public InputStageAlignSequence(MachineSequenceContext context)
            : base(context, InputStageSequenceKind.Align, "InputStageAlignSequence")
        {
        }

        protected override InputStageAlignStep IdleStep { get { return InputStageAlignStep.Idle; } }
        protected override InputStageAlignStep InitialStep { get { return InputStageAlignStep.CheckUnit; } }
        protected override InputStageAlignStep CompleteStep { get { return InputStageAlignStep.Complete; } }
        protected override InputStageAlignStep ErrorStep { get { return InputStageAlignStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int stateResult = EnsureAlignRuntimeState();
                if (stateResult != 0)
                    return Task.FromResult(stateResult);

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputStageAlignStep.CheckUnit:
                        return CheckAlignUnitAsync(ct);
                    // 비전 프로세스 위치 이동
                    case InputStageAlignStep.MoveVisionProcessPosition:
                        return MoveVisionProcessPositionAsync(ct);
                    // 센터 마크 위치 이동
                    case InputStageAlignStep.MoveCenterMarkPosition:
                        return MoveCenterMarkPositionAsync(ct);
                    // 센터 마크 찾기 요청
                    case InputStageAlignStep.RequestCenterMark:
                        return Task.FromResult(RequestCenterMark());
                    // 센터 마크 결과 대기
                    case InputStageAlignStep.WaitCenterMarkResult:
                        return WaitCenterMarkResultAsync(ct);
                    // 세타 보정
                    case InputStageAlignStep.CorrectTheta:
                        return CorrectThetaAsync(ct);
                    // 세타 검증 마크 찾기 요청
                    case InputStageAlignStep.RequestThetaVerify:
                        return Task.FromResult(RequestThetaVerify());
                    // 세타 검증 결과 대기
                    case InputStageAlignStep.WaitThetaVerifyResult:
                        return WaitThetaVerifyResultAsync(ct);
                    // 기준 1 위치 이동
                    case InputStageAlignStep.MoveRef1Position:
                        return MoveRef1PositionAsync(ct);
                    // 기준 1 마크 찾기 요청
                    case InputStageAlignStep.RequestRef1Mark:
                        return Task.FromResult(RequestRef1Mark());
                    // 기준 1 마크 결과 대기
                    case InputStageAlignStep.WaitRef1MarkResult:
                        return WaitRef1MarkResultAsync(ct);
                    // 기준 2 위치 이동
                    case InputStageAlignStep.MoveRef2Position:
                        return MoveRef2PositionAsync(ct);
                    // 기준 2 마크 찾기 요청
                    case InputStageAlignStep.RequestRef2Mark:
                        return Task.FromResult(RequestRef2Mark());
                    // 기준 2 마크 결과 대기
                    case InputStageAlignStep.WaitRef2MarkResult:
                        return WaitRef2MarkResultAsync(ct);
                    // 얼라인 결과 계산
                    case InputStageAlignStep.CalculateAlignResult:
                        return CalculateAlignResultAsync(ct);
                    // Ref1/Ref2 T 보정 후 센터 복귀
                    case InputStageAlignStep.MoveCenterAfterRefAlign:
                        return MoveCenterAfterRefAlignAsync(ct);
                    // 얼라인 결과 적용
                    case InputStageAlignStep.ApplyAlignResult:
                        return Task.FromResult(ApplyAlignResult());
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
                return Task.FromResult(Fail("IN-STAGE-ALIGN-STEP-EX", "InputStageAlignSequence", "Align step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int EnsureAlignRuntimeState()
        {
            if (CurrentStep == InputStageAlignStep.Idle ||
                CurrentStep == InputStageAlignStep.CheckUnit ||
                CurrentStep == InputStageAlignStep.Complete ||
                CurrentStep == InputStageAlignStep.Error)
            {
                return 0;
            }

            if (_map != null)
                return 0;

            WriteLog("InputStageAlignSequence",
                "Align volatile runtime state was not initialized. Restart from CheckUnit. step=" + CurrentStep + " - Retry");
            CurrentStep = InputStageAlignStep.CheckUnit;
            return 0;
        }

        private async Task<int> CheckAlignUnitAsync(CancellationToken ct)
        {
            try
            {
                ResetAlignRuntimeState();

                // 바코드/맵 검사까지 성공해야 이동 Step으로 승격한다. 실패/취소 재개도 이 검사를 다시 거친다.
                int result = CheckUnit(InputStageAlignStep.CheckUnit);
                if (result != 0)
                    return result;

                CaptureAlignVisionSettings();
                result = ConfigureHybridVirtualFrameMode();
                if (result != 0)
                    return result;

                _alignResultRunId = Guid.NewGuid().ToString("N");

                if (Stage.Recipe == null)
                    return Fail("IN-STAGE-ALIGN-RECIPE", Stage.Name, "Input stage recipe is not available.");

                string servoReason = BuildAlignServoReason();
                if (!string.IsNullOrEmpty(servoReason))
                    return Fail("IN-STAGE-ALIGN-SERVO", Stage.Name, "Input stage servo is not on. " + servoReason);

                _wafer = Stage.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (_wafer == null)
                    return Fail("IN-STAGE-ALIGN-WAFER", "Material",
                        "InputStage wafer data was not found. CurrentWaferMaterial=null, MaterialLocation=InputStage empty.");

                // 수동/STEP 얼라인도 Auto의 바코드 검사를 우회하지 않는다.
                // 이 단계는 값을 정정하거나 판독 모션을 시작하지 않고, 확정된 후보만 검사한다.
                AppSettings barcodeSettings = AppSettingsStore.Current;
                if (barcodeSettings == null || Stage.Config == null)
                    return Fail("IN-STAGE-ALIGN-BARCODE-SETTING", Stage.Name, "얼라인 전 바코드 검사 설정을 확인할 수 없습니다.");
                if (RecipeInputMapSource.UsesRemoteForActiveRecipe(barcodeSettings) || Stage.Config.UseBarcodeLotPrefixCheck)
                {
                    if (!_wafer.BarcodeConfirmed || !InputFeederLoadToStageSequence.IsUsableBarcode(_wafer.BarcodeId))
                        return Fail("IN-STAGE-ALIGN-BARCODE-REQUIRED", Stage.Name,
                            "얼라인 전에 웨이퍼 바코드 확인을 완료해야 합니다. 바코드 판독/수동 입력 절차를 먼저 진행하십시오.");
                    _wafer = await InputFeederLoadToStageSequence.ValidateAndApplyInputBarcodeAsync(
                        Stage, _wafer, _wafer.BarcodeId, "InputStageAlign:Validation", _wafer.BarcodeAttemptCount,
                        ct, allowRecovery: false).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                }

                InputStageHybridResultSession.Clear();
                Stage.ClearWaferAlignThetaResult();
                MaterialStateService.ResetInputStageThetaAlignResult(_wafer, "InputStageAlignStartThetaReset");

                WriteLog("InputStageAlignSequence",
                    "Align theta tolerance resolved. requested=" + Options.AlignThetaToleranceDeg.ToString("F6") +
                    ", stageConfig=" + (Stage.Config != null
                        ? Stage.Config.AlignConvergenceThresholdDeg.ToString("F6")
                        : "-") +
                    ", effective=" + ResolveThetaTolerance().ToString("F6") +
                    ", maximumEffective=" + MaxEffectiveThetaToleranceDeg.ToString("F6") + " - Ok");

                _frameSpec = ResolveFrameSpecForWafer(_wafer);
                string waferId = !string.IsNullOrWhiteSpace(Options.WaferId) ? Options.WaferId : _wafer.WaferId;
                _map = ResolveWaferMapForAlign(waferId);
                if (_map == null)
                    return Fail("IN-STAGE-ALIGN-MAP", Stage.Name,
                        "InputStage wafer map was not found. waferId=" + waferId +
                        ", allowFallbackMap=" + Options.AllowFallbackMap);

                ApplyFrameSpecToMap(_map, _frameSpec);

                if (Options.RequireVisionAlign && Stage.Vision == null)
                    return Fail("IN-STAGE-ALIGN-VISION", Stage.Name, "Vision align is required but vision client is not available.");

                CurrentStep = InputStageAlignStep.MoveVisionProcessPosition;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-CHECK-EX", "InputStageAlignSequence", "Align unit check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void CaptureAlignVisionSettings()
        {
            var config = Stage.Config;
            if (config != null)
                config.EnsureAlignVisionDefaults();

            // 한 번 시작한 얼라인은 같은 검사 정책을 사용한다. 편집한 값은 다음 시작부터 적용한다.
            _alignVisionRetryCount = config != null ? config.AlignVisionRetryCount : 3;
            _alignSearchPointCount = config != null ? config.AlignSearchPointCount : 8;
            _alignVisionTimeoutMs = config != null ? config.AlignVisionTimeoutMs : 5000;
            WriteLog("InputStageAlignSequence",
                "얼라인 검사 설정 적용. 추가 재시도=" + _alignVisionRetryCount +
                ", 주변 탐색 위치=" + _alignSearchPointCount +
                ", 통신 단계별 응답 대기(ms)=" + _alignVisionTimeoutMs + " - Ok");
        }

        private void ResetAlignRuntimeState()
        {
            _map = null;
            _wafer = null;
            _frameSpec = null;
            _centerResult = null;
            _verifyCenterResult = null;
            _ref1Result = null;
            _ref2Result = null;
            _ref1X = 0.0;
            _ref1Y = 0.0;
            _ref2X = 0.0;
            _ref2Y = 0.0;
            _originX = 0.0;
            _originY = 0.0;
            _pitchX = 0.0;
            _pitchY = 0.0;
            _thetaFromTwoPoint = 0.0;
            _thetaRetryCount = 0;
            _twoPointThetaRetryCount = 0;
            _alignThetaReferenceReady = false;
            _alignThetaReferenceT = 0.0;
            _alignAnchorReady = false;
            _alignAnchorRow = 0;
            _alignAnchorCol = 0;
            _alignAnchorX = 0.0;
            _alignAnchorY = 0.0;
            _manualAlignFallbackActive = false;
            _alignProcessReferenceReady = false;
            _alignProcessReferenceX = 0.0;
            _alignProcessReferenceY = 0.0;
            _finalAlignOffsetReady = false;
            _finalAlignOffsetX = 0.0;
            _finalAlignOffsetY = 0.0;
            _preserveThetaOnProcessMove = false;
            _hybridVirtualFrameActive = false;
            _hybridCenterAppliedMoveX = 0.0;
            _hybridCenterAppliedMoveY = 0.0;
            _hybridCenterAppliedTheta = 0.0;
            _hybridTwoPointAppliedTheta = 0.0;
            _alignResultRunId = "";
            ClearPendingVisionRequest();
        }

        private async Task<int> MoveVisionProcessPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Options.EnableMotion)
                {
                    int result = await PrepareVisionProcessPlaneForAlignAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                if (!_alignThetaReferenceReady)
                {
                    CaptureAlignThetaReference(Options.EnableMotion ? "StageTProcessPosition" : "MotionDisabled");
                }
                else
                {
                    WriteLog("InputStageAlignSequence",
                        "Align theta reference is preserved across correction retry. referenceT=" +
                        _alignThetaReferenceT.ToString("F6") +
                        ", actualT=" + (Stage.StageT != null ? Stage.StageT.ActualPosition.ToString("F6") : "0.000000") +
                        " - Ok");
                }

                _preserveThetaOnProcessMove = false;

                CurrentStep = InputStageAlignStep.MoveCenterMarkPosition;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-VISION-POS-EX", Stage.Name, "Vision process position move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareVisionProcessPlaneForAlignAsync(CancellationToken ct)
        {
            Stage.Recipe.EnsurePositionObjects();

            int result = await MoveHeadZAxesAvoidBeforeProcessPlaneMoveAsync(ct).ConfigureAwait(false);
            if (result != 0) return result;

            bool stageZAtProcess = Stage.Recipe.WaferZ != null &&
                CanSkipAxisMoveCommand(ResolveStageAxis(WaferStageAxis.WaferExpandingZ), Stage.Recipe.WaferZ.ProcessPosition);

            if (!stageZAtProcess)
            {
                result = await EnsureStageTFixedBeforeExpanderZMoveAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                if (Stage.Recipe.WaferZ != null)
                {
                    result = await MoveAxisAndVerifyAsync(
                        WaferStageAxis.WaferExpandingZ,
                        Stage.Recipe.WaferZ.ProcessPosition,
                        "StageZ process before process plane move",
                        ct).ConfigureAwait(false);
                    if (result != 0) return result;
                }
            }
            else
            {
                WriteLog("InputStageAlignSequence",
                    "StageZ already at process position. Skip StageZ process move before align process plane move. " +
                    BuildAxisState(WaferStageAxis.WaferExpandingZ, Stage.Recipe.WaferZ.ProcessPosition) + " - Ok");
            }

            result = await MoveAxisAndVerifyAsync(
                WaferStageAxis.NeedleX,
                Stage.Recipe.NeedleX.AvoidPosition,
                "NeedleX avoid before process plane move",
                ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveVisionProcessPlaneAxesAsync(ct).ConfigureAwait(false);
            if (result != 0) return result;

            if (!IsExpanderZSafeForStageTProcessMove())
                return Fail("IN-STAGE-ALIGN-STAGE-T-SAFETY", Stage.Name,
                    "StageT process move requires ExpanderZ at Process or at/below Avoid. " +
                    BuildAxisState(WaferStageAxis.WaferExpandingZ, Stage.Recipe.WaferZ.ProcessPosition));

            if (_preserveThetaOnProcessMove)
            {
                WriteLog("InputStageAlignSequence",
                    "StageT process move skipped to preserve the applied two point correction. actualT=" +
                    (Stage.StageT != null ? Stage.StageT.ActualPosition.ToString("F6") : "0.000000") +
                    ", recipeProcessT=" + Stage.Recipe.WaferT.ProcessPosition.ToString("F6") + " - Ok");
            }
            else
            {
                result = await MoveAxisAndVerifyAsync(
                    WaferStageAxis.WaferT,
                    Stage.Recipe.WaferT.ProcessPosition,
                    "StageT process before align",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;
            }

            return 0;
        }

        private async Task<int> MoveHeadZAxesAvoidBeforeProcessPlaneMoveAsync(CancellationToken ct)
        {
            Stage.Recipe.EnsurePositionObjects();

            int result = await MoveAxisAndVerifyAsync(
                WaferStageAxis.NeedleZ,
                Stage.Recipe.NeedleZ.AvoidPosition,
                "NeedleZ avoid before process plane move",
                ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveAxisAndVerifyAsync(
                WaferStageAxis.EjectPinZ,
                Stage.Recipe.EjectPinZ.AvoidPosition,
                "EjectPinZ avoid before process plane move",
                ct).ConfigureAwait(false);
            if (result != 0) return result;

            return 0;
        }

        private async Task<int> MoveVisionProcessPlaneAxesAsync(CancellationToken ct)
        {
            int result = await MoveAxisAndVerifyAsync(
                WaferStageAxis.WaferY,
                Stage.Recipe.WaferY.ProcessPosition,
                "StageY process",
                ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveAxisAndVerifyAsync(
                WaferStageAxis.VisionX,
                Stage.Recipe.VisionX.ProcessPosition,
                "VisionX process",
                ct).ConfigureAwait(false);
            if (result != 0) return result;

            return 0;
        }

        private async Task<int> EnsureStageTFixedBeforeExpanderZMoveAsync(CancellationToken ct)
        {
            Stage.Recipe.EnsurePositionObjects();

            if (IsStageTAtExpanderZFixedPosition())
                return 0;

            WriteLog("InputStageAlignSequence",
                "StageT is not at an ExpanderZ-safe fixed position before StageZ process plane move. Move StageT home first. " +
                BuildAxisState(WaferStageAxis.WaferT, 0.0) + " - Start");

            return await MoveAxisAndVerifyAsync(
                WaferStageAxis.WaferT,
                0.0,
                "StageT home before StageZ process plane move",
                ct,
                true).ConfigureAwait(false);
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

        private bool IsExpanderZSafeForStageTProcessMove()
        {
            try
            {
                Stage.Recipe.EnsurePositionObjects();

                QMC.Common.Motion.BaseAxis expanderZ = ResolveStageAxis(WaferStageAxis.WaferExpandingZ);
                if (expanderZ == null || Stage.Recipe.WaferZ == null || expanderZ.IsMoving || expanderZ.IsAlarm)
                    return false;

                return IsAxisInPosition(expanderZ, Stage.Recipe.WaferZ.ProcessPosition) ||
                    IsAxisInPosition(expanderZ, Stage.Recipe.WaferZ.AvoidPosition) ||
                    expanderZ.ActualPosition <= 0.0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private Task<int> MoveCenterMarkPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Options.EnableMotion)
                {
                    int result = CheckAlignProcessTeaching();
                    if (result != 0)
                        return Task.FromResult(result);

                    int centerRow = _map != null ? _map.RowCount / 2 : 0;
                    int centerCol = _map != null ? _map.ColumnCount / 2 : 0;
                    CaptureAlignProcessReferenceIfNeeded("center mark process position");
                    CaptureAlignAnchorFromCurrentPosition(centerRow, centerCol, "center mark");
                }

                CurrentStep = InputStageAlignStep.RequestCenterMark;
                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-STAGE-ALIGN-CENTER-MOVE-EX", Stage.Name, "Center mark position move failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int RequestCenterMark()
        {
            try
            {
                StartVisionMarkRequest(ResolveTargetId(Options.CenterAlignTargetId, VisionAlignTargetIds.Center), VisionAlignTargetIds.Center);
                CurrentStep = InputStageAlignStep.WaitCenterMarkResult;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-CENTER-REQ-EX", "Vision", "Center align mark request exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitCenterMarkResultAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_pendingVisionTask == null)
                {
                    CurrentStep = InputStageAlignStep.RequestCenterMark;
                    return 0;
                }

                _centerResult = await WaitPendingVisionResultAsync(ct).ConfigureAwait(false);
                if (_centerResult == null && !_hybridVirtualFrameActive)
                {
                    _centerResult = await SearchVisionMarkAroundCurrentPointAsync(
                        ResolveTargetId(Options.CenterAlignTargetId, VisionAlignTargetIds.Center),
                        VisionAlignTargetIds.Center,
                        "Wafer Align Center",
                        ct).ConfigureAwait(false);
                }

                if (_centerResult == null)
                {
                    if (_hybridVirtualFrameActive)
                        return Fail("IN-STAGE-ALIGN-HYBRID-CENTER-VISION", "Vision",
                            "Wafer Align Center 다이를 찾지 못했습니다. HybridRealVisionSimMotion에서는 가상 X/Y 이동으로 실제 Vision 화면이 바뀌지 않으므로 동일 화면 통신 재시도 후 주변 탐색을 수행하지 않습니다.");

                    // 파샬 웨이퍼 등 센터 다이가 없어도 정지하지 않고 명목값 얼라인으로 완료해
                    // Review 화면(수동 Jog/T 보정/다이 검출/첫칩 선택)까지 진행시킨다.
                    if (TryEnterManualAlignFallback())
                        return 0;

                    return Fail("IN-STAGE-ALIGN-CENTER", "Vision",
                        "Wafer Align Center 다이를 찾지 못했습니다. 센터 검사와 설정된 주변 탐색에서 검출하지 못했습니다.");
                }

                int centerMoveResult = await ApplyCenterVisionCorrectionAsync(
                    _centerResult,
                    VisionAlignTargetIds.Center,
                    ct).ConfigureAwait(false);
                if (centerMoveResult != 0)
                    return centerMoveResult;

                CurrentStep = InputStageAlignStep.CorrectTheta;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-CENTER-EX", "Vision", "Center align mark wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> CorrectThetaAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (!Options.EnableMotion)
                {
                    CurrentStep = InputStageAlignStep.RequestThetaVerify;
                    return 0;
                }

                double rawDeltaTheta = _centerResult != null ? _centerResult.DeltaTheta : 0.0;
                double effectiveDeltaTheta = ResolveHybridEffectiveCenterTheta(
                    rawDeltaTheta,
                    "CenterThetaCorrection");
                double correctionTheta = -effectiveDeltaTheta;
                int limitResult = CheckThetaCorrectionLimit(correctionTheta, VisionAlignTargetIds.Center);
                if (limitResult != 0)
                    return limitResult;

                double currentT = Stage.StageT.ActualPosition;
                double targetT = currentT + correctionTheta;
                WriteLog("InputStageAlignSequence",
                    "Center theta correction formula. currentT=" + currentT.ToString("F6") +
                    ", rawVisionDeltaTheta=" + rawDeltaTheta.ToString("F6") +
                    ", virtualAppliedTheta=" + _hybridCenterAppliedTheta.ToString("F6") +
                    ", effectiveDeltaTheta=" + effectiveDeltaTheta.ToString("F6") +
                    ", correctionTheta=-effectiveDeltaTheta=" + correctionTheta.ToString("F6") +
                    ", targetT=currentT+correctionTheta=" + targetT.ToString("F6") + " - Start");

                int result = await MoveAxisAndVerifyAsync(WaferStageAxis.WaferT, targetT, "StageT theta correction", ct, true).ConfigureAwait(false);
                if (result != 0) return result;

                if (_hybridVirtualFrameActive)
                {
                    double actualT = Stage.StageT != null ? Stage.StageT.ActualPosition : currentT;
                    double appliedTheta = actualT - currentT;
                    _hybridCenterAppliedTheta = NormalizeThetaOffset(_hybridCenterAppliedTheta + appliedTheta);
                    WriteLog("InputStageAlignSequence",
                        "Hybrid virtual center theta updated. rawVisionDeltaTheta=" + rawDeltaTheta.ToString("F6") +
                        ", effectiveDeltaTheta=" + effectiveDeltaTheta.ToString("F6") +
                        ", appliedTheta=actualAfter-actualBefore=" + appliedTheta.ToString("F6") +
                        ", accumulatedVirtualTheta=" + _hybridCenterAppliedTheta.ToString("F6") +
                        ", mode=HybridRealVisionSimMotion - Ok");
                }

                CurrentStep = InputStageAlignStep.RequestThetaVerify;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-THETA-EX", Stage.Name, "Theta correction failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int RequestThetaVerify()
        {
            try
            {
                StartVisionMarkRequest(ResolveTargetId(Options.CenterAlignTargetId, VisionAlignTargetIds.Center), VisionAlignTargetIds.CenterVerify);
                CurrentStep = InputStageAlignStep.WaitThetaVerifyResult;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-THETA-REQ-EX", "Vision", "Theta verify mark request exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitThetaVerifyResultAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_pendingVisionTask == null)
                {
                    CurrentStep = InputStageAlignStep.RequestThetaVerify;
                    return 0;
                }

                _verifyCenterResult = await WaitPendingVisionResultAsync(ct).ConfigureAwait(false);
                if (_verifyCenterResult == null && !_hybridVirtualFrameActive)
                {
                    _verifyCenterResult = await SearchVisionMarkAroundCurrentPointAsync(
                        ResolveTargetId(Options.CenterAlignTargetId, VisionAlignTargetIds.Center),
                        VisionAlignTargetIds.CenterVerify,
                        "Wafer Align Center Verify",
                        ct).ConfigureAwait(false);
                }

                if (_verifyCenterResult == null)
                {
                    if (_hybridVirtualFrameActive)
                        return Fail("IN-STAGE-ALIGN-HYBRID-THETA-VERIFY-VISION", "Vision",
                            "Wafer Align T 보정 확인용 Center 다이를 찾지 못했습니다. HybridRealVisionSimMotion에서는 가상 X/Y 이동으로 실제 Vision 화면이 바뀌지 않으므로 동일 화면 통신 재시도 후 주변 탐색을 수행하지 않습니다.");

                    return Fail("IN-STAGE-ALIGN-THETA-VERIFY", "Vision",
                        "Wafer Align T 보정 확인용 Center 다이를 찾지 못했습니다. 센터 검사와 설정된 주변 탐색에서 검출하지 못했습니다.");
                }

                double inputDeltaX;
                double inputDeltaY;
                double moveDeltaX;
                double moveDeltaY;
                ResolveInputCameraMotorCorrection(
                    _verifyCenterResult,
                    VisionAlignTargetIds.CenterVerify,
                    out inputDeltaX,
                    out inputDeltaY,
                    out moveDeltaX,
                    out moveDeltaY);

                double effectiveMoveDeltaX;
                double effectiveMoveDeltaY;
                ResolveHybridEffectiveMoveCorrection(
                    moveDeltaX,
                    moveDeltaY,
                    VisionAlignTargetIds.CenterVerify,
                    out effectiveMoveDeltaX,
                    out effectiveMoveDeltaY);
                double rawTheta = _verifyCenterResult.DeltaTheta;
                double effectiveTheta = ResolveHybridEffectiveCenterTheta(rawTheta, VisionAlignTargetIds.CenterVerify);

                WriteLog("InputStageAlignSequence",
                    "Center theta verification keeps the first center XY. " +
                    "rawMoveDeltaX=" + moveDeltaX.ToString("F6") +
                    ", rawMoveDeltaY=" + moveDeltaY.ToString("F6") +
                    ", effectiveMoveDeltaX=" + effectiveMoveDeltaX.ToString("F6") +
                    ", effectiveMoveDeltaY=" + effectiveMoveDeltaY.ToString("F6") +
                    ", rawVisionDeltaTheta=" + rawTheta.ToString("F6") +
                    ", virtualAppliedTheta=" + _hybridCenterAppliedTheta.ToString("F6") +
                    ", effectiveDeltaTheta=" + effectiveTheta.ToString("F6") +
                    ", mode=" + ResolveVisionMotionModeName() +
                    ", xyCorrectionApplied=False - Check");

                double theta = Math.Abs(effectiveTheta);
                double tolerance = ResolveThetaTolerance();
                if (theta <= tolerance)
                {
                    CurrentStep = InputStageAlignStep.MoveRef1Position;
                    return 0;
                }

                if (_thetaRetryCount < Math.Max(0, Options.AlignRetryCount))
                {
                    _thetaRetryCount++;
                    _centerResult = _verifyCenterResult;
                    CurrentStep = InputStageAlignStep.CorrectTheta;
                    return 0;
                }

                return Fail("IN-STAGE-ALIGN-THETA-TOL", Stage.Name,
                    "Theta correction is out of tolerance. theta=" + theta.ToString("F6") +
                    ", tolerance=" + tolerance.ToString("F6"));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-THETA-VERIFY-EX", Stage.Name, "Theta tolerance verify failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveRef1PositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Options.EnableMotion)
                {
                    int result = await MoveVisionPointAndVerifyAsync(_map.Ref1Row, _map.Ref1Col, "ref1 mark", ct).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                CurrentStep = InputStageAlignStep.RequestRef1Mark;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-REF1-MOVE-EX", Stage.Name, "Ref1 mark position move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int RequestRef1Mark()
        {
            try
            {
                StartVisionMarkRequest(ResolveTargetId(Options.Ref1AlignTargetId, VisionAlignTargetIds.Ref1), VisionAlignTargetIds.Ref1);
                CurrentStep = InputStageAlignStep.WaitRef1MarkResult;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-REF1-REQ-EX", "Vision", "Ref1 align mark request exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitRef1MarkResultAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_pendingVisionTask == null)
                {
                    CurrentStep = InputStageAlignStep.RequestRef1Mark;
                    return 0;
                }

                _ref1Result = await WaitPendingVisionResultAsync(ct).ConfigureAwait(false);
                if (_ref1Result == null)
                    return Fail("IN-STAGE-ALIGN-REF1", "Vision", "Ref1 vision offset receive failed.");

                ResolveAlignPointFromVisionResult(
                    _ref1Result,
                    VisionAlignTargetIds.Ref1,
                    out _ref1X,
                    out _ref1Y);
                CurrentStep = InputStageAlignStep.MoveRef2Position;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-REF1-EX", "Vision", "Ref1 align mark wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveRef2PositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Options.EnableMotion)
                {
                    int result = await MoveVisionPointAndVerifyAsync(_map.Ref2Row, _map.Ref2Col, "ref2 mark", ct).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                CurrentStep = InputStageAlignStep.RequestRef2Mark;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-REF2-MOVE-EX", Stage.Name, "Ref2 mark position move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int RequestRef2Mark()
        {
            try
            {
                StartVisionMarkRequest(ResolveTargetId(Options.Ref2AlignTargetId, VisionAlignTargetIds.Ref2), VisionAlignTargetIds.Ref2);
                CurrentStep = InputStageAlignStep.WaitRef2MarkResult;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-REF2-REQ-EX", "Vision", "Ref2 align mark request exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitRef2MarkResultAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_pendingVisionTask == null)
                {
                    CurrentStep = InputStageAlignStep.RequestRef2Mark;
                    return 0;
                }

                _ref2Result = await WaitPendingVisionResultAsync(ct).ConfigureAwait(false);
                if (_ref2Result == null)
                    return Fail("IN-STAGE-ALIGN-REF2", "Vision", "Ref2 vision offset receive failed.");

                ResolveAlignPointFromVisionResult(
                    _ref2Result,
                    VisionAlignTargetIds.Ref2,
                    out _ref2X,
                    out _ref2Y);
                CurrentStep = InputStageAlignStep.CalculateAlignResult;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-REF2-EX", "Vision", "Ref2 align mark wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> CalculateAlignResultAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_map == null || _ref1Result == null || _ref2Result == null)
                {
                    WriteLog("InputStageAlignSequence",
                        "Align calculation state is incomplete. Restart from CheckUnit. map=" + (_map != null) +
                        ", ref1=" + (_ref1Result != null) +
                        ", ref2=" + (_ref2Result != null) + " - Retry");
                    CurrentStep = InputStageAlignStep.CheckUnit;
                    return 0;
                }

                int colSpan = _map.Ref2Col - _map.Ref1Col;
                int rowSpan = _map.Ref2Row - _map.Ref1Row;

                _pitchX = colSpan != 0 && Math.Abs(_ref2X - _ref1X) > 1e-9
                    ? (_ref2X - _ref1X) / colSpan
                    : ResolveAlignPitchX(_ref1Result, _ref2Result);
                _pitchY = rowSpan != 0 && Math.Abs(_ref2Y - _ref1Y) > 1e-9
                    ? (_ref2Y - _ref1Y) / rowSpan
                    : ResolveAlignPitchY(_ref1Result, _ref2Result);

                _pitchX = NormalizeResolvedAlignPitch("X", _pitchX, ResolveConfiguredAlignPitchX(), "CalculateAlignResult");
                _pitchY = NormalizeResolvedAlignPitch("Y", _pitchY, ResolveConfiguredAlignPitchY(), "CalculateAlignResult");

                if (Math.Abs(_pitchX) <= 1e-9 || Math.Abs(_pitchY) <= 1e-9)
                    return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-PITCH", Stage.Name,
                        "Calculated align pitch is invalid. pitchX=" + _pitchX + ", pitchY=" + _pitchY);

                _originX = _ref1X - (_map.Ref1Col * _pitchX);
                _originY = _ref1Y - (_map.Ref1Row * _pitchY);

                double rawTwoPointTheta = Math.Atan2(_ref2Y - _ref1Y, _ref2X - _ref1X) * 180.0 / Math.PI;
                _thetaFromTwoPoint = _hybridVirtualFrameActive
                    ? NormalizeThetaOffset(rawTwoPointTheta + _hybridTwoPointAppliedTheta)
                    : rawTwoPointTheta;
                WriteLog("InputStageAlignSequence",
                    "Two point align formula. ref1Row=" + _map.Ref1Row +
                    ", ref1Col=" + _map.Ref1Col +
                    ", ref1X=" + _ref1X.ToString("F6") +
                    ", ref1Y=" + _ref1Y.ToString("F6") +
                    ", ref2Row=" + _map.Ref2Row +
                    ", ref2Col=" + _map.Ref2Col +
                    ", ref2X=" + _ref2X.ToString("F6") +
                    ", ref2Y=" + _ref2Y.ToString("F6") +
                    ", deltaX=ref2X-ref1X=" + (_ref2X - _ref1X).ToString("F6") +
                    ", deltaY=ref2Y-ref1Y=" + (_ref2Y - _ref1Y).ToString("F6") +
                    ", rawTheta=atan2(deltaY,deltaX)=" + rawTwoPointTheta.ToString("F6") +
                    ", virtualAppliedTheta=" + _hybridTwoPointAppliedTheta.ToString("F6") +
                    ", effectiveTheta=" + _thetaFromTwoPoint.ToString("F6") +
                    ", mode=" + ResolveVisionMotionModeName() +
                    ", pitchX=" + _pitchX.ToString("F6") +
                    ", pitchY=" + _pitchY.ToString("F6") +
                    ", originX=ref1X-ref1Col*pitchX=" + _originX.ToString("F6") +
                    ", originY=ref1Y-ref1Row*pitchY=" + _originY.ToString("F6") + " - Check");

                double thetaTolerance = ResolveThetaTolerance();
                double thetaAbs = Math.Abs(_thetaFromTwoPoint);
                if (ShouldCorrectTwoPointTheta(thetaAbs, thetaTolerance))
                {
                    int result = await CorrectTwoPointThetaAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return 0;
                }

                CurrentStep = InputStageAlignStep.MoveCenterAfterRefAlign;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-CALC-EX", Stage.Name, "Align result calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool ShouldCorrectTwoPointTheta(double thetaAbs, double tolerance)
        {
            try
            {
                return thetaAbs > tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> CorrectTwoPointThetaAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                double tolerance = ResolveThetaTolerance();
                double correctionTheta = -_thetaFromTwoPoint;

                int limitResult = CheckThetaCorrectionLimit(correctionTheta, VisionAlignTargetIds.Ref1Ref2);
                if (limitResult != 0)
                    return limitResult;

                if (!Options.EnableMotion)
                    return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-REF-THETA-MOTION", Stage.Name,
                        "Two point theta correction is required but motion is disabled. theta=" + _thetaFromTwoPoint.ToString("F6") +
                        ", tolerance=" + tolerance.ToString("F6"));

                if (_twoPointThetaRetryCount >= Math.Max(0, Options.AlignRetryCount))
                    return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-REF-THETA", Stage.Name,
                        "Two point theta is out of tolerance after correction retry. theta=" + _thetaFromTwoPoint.ToString("F6") +
                        ", tolerance=" + tolerance.ToString("F6") +
                        ", retry=" + _twoPointThetaRetryCount +
                        ", maxRetry=" + Options.AlignRetryCount);

                _twoPointThetaRetryCount++;
                double currentT = Stage.StageT.ActualPosition;
                double targetT = currentT + correctionTheta;
                WriteLog("InputStageAlignSequence",
                    "Two point theta correction. theta=" + _thetaFromTwoPoint.ToString("F6") +
                    ", tolerance=" + tolerance.ToString("F6") +
                    ", correction=" + correctionTheta.ToString("F6") +
                    ", targetT=" + targetT.ToString("F6") +
                    ", mode=OutOfTolerance" +
                    ", retry=" + _twoPointThetaRetryCount +
                    "/" + Options.AlignRetryCount + " - Start");

                int moveResult = await MoveAxisAndVerifyAsync(WaferStageAxis.WaferT, targetT, "StageT two point theta correction", ct, true).ConfigureAwait(false);
                if (moveResult != 0)
                    return moveResult;

                if (_hybridVirtualFrameActive)
                {
                    double actualT = Stage.StageT != null ? Stage.StageT.ActualPosition : currentT;
                    double appliedTheta = actualT - currentT;
                    _hybridTwoPointAppliedTheta = NormalizeThetaOffset(_hybridTwoPointAppliedTheta + appliedTheta);
                    WriteLog("InputStageAlignSequence",
                        "Hybrid virtual two-point theta updated. appliedTheta=actualAfter-actualBefore=" +
                        appliedTheta.ToString("F6") +
                        ", accumulatedVirtualTheta=" + _hybridTwoPointAppliedTheta.ToString("F6") +
                        ", mode=HybridRealVisionSimMotion - Ok");
                }

                ClearRefAlignRuntimeState();
                _thetaRetryCount = 0;
                WriteLog("InputStageAlignSequence",
                    "Two point theta correction completed. Keep the first center XY and remeasure Ref1/Ref2 only. " +
                    "centerX=" + _alignAnchorX.ToString("F6") +
                    ", centerY=" + _alignAnchorY.ToString("F6") +
                    ", actualT=" + (Stage.StageT != null ? Stage.StageT.ActualPosition.ToString("F6") : "0.000000") +
                    ", centerPipelineRepeated=False - Ok");
                CurrentStep = InputStageAlignStep.MoveRef1Position;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-REF-THETA-EX", Stage.Name,
                    "Two point theta correction failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ClearRefAlignRuntimeState()
        {
            _ref1Result = null;
            _ref2Result = null;
            _ref1X = 0.0;
            _ref1Y = 0.0;
            _ref2X = 0.0;
            _ref2Y = 0.0;
            _originX = 0.0;
            _originY = 0.0;
            _pitchX = 0.0;
            _pitchY = 0.0;
            _thetaFromTwoPoint = 0.0;
        }

        private async Task<int> MoveCenterAfterRefAlignAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options.EnableMotion)
                {
                    int centerRow = _map != null ? _map.RowCount / 2 : _alignAnchorRow;
                    int centerCol = _map != null ? _map.ColumnCount / 2 : _alignAnchorCol;
                    int result = await MoveVisionPointAndVerifyAsync(
                        centerRow,
                        centerCol,
                        "Wafer Align Ref1/Ref2 T 보정 후 Center 복귀",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await VerifyFinalCenterAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }
                else
                {
                    CaptureFinalAlignOffsets("MotionDisabled");
                }

                CurrentStep = InputStageAlignStep.ApplyAlignResult;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-CENTER-RETURN-EX", Stage.Name,
                    "Wafer Align Ref1/Ref2 T 보정 후 Center 복귀 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckThetaCorrectionLimit(double correctionTheta, string source)
        {
            double limit = ResolveThetaCorrectionLimit();
            if (Math.Abs(correctionTheta) <= limit)
                return 0;

            return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-THETA-LIMIT", Stage.Name,
                "Theta correction exceeds limit. source=" + source +
                ", correction=" + correctionTheta.ToString("F6") +
                ", limit=" + limit.ToString("F6"));
        }

        private int FailAndResetAlignRuntimeState(string alarmCode, string source, string message)
        {
            ResetAlignRuntimeState();
            return Fail(alarmCode, source, message);
        }

        private int ApplyAlignResult()
        {
            try
            {
                if (!_finalAlignOffsetReady)
                    CaptureFinalAlignOffsets("ApplyAlignResultFallback");

                double offsetX = _finalAlignOffsetX;
                double offsetY = _finalAlignOffsetY;
                double referenceT = ResolveAlignThetaReference();
                double correctedT = Stage.StageT != null ? Stage.StageT.ActualPosition : referenceT;
                double offsetT = NormalizeThetaOffset(correctedT - referenceT);

                int finalThetaLimitResult = CheckThetaCorrectionLimit(offsetT, "FinalOffset");
                if (finalThetaLimitResult != 0)
                    return finalThetaLimitResult;

                string thetaReadyReason;
                if (!Stage.IsWaferAlignThetaOffsetWithinLimit(offsetT, out thetaReadyReason))
                    return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-THETA-LIMIT", Stage.Name,
                        "Final theta offset is outside limit. " + thetaReadyReason);

                WriteLog("InputStageAlignSequence",
                    "Final align result formula. processReferenceX=" + _alignProcessReferenceX.ToString("F6") +
                    ", processReferenceY=" + _alignProcessReferenceY.ToString("F6") +
                    ", finalCenterX=" + (Stage.CameraX != null ? Stage.CameraX.ActualPosition.ToString("F6") : "0.000000") +
                    ", finalCenterY=" + (Stage.StageY != null ? Stage.StageY.ActualPosition.ToString("F6") : "0.000000") +
                    ", offsetX=finalCenterX-processReferenceX=" + offsetX.ToString("F6") +
                    ", offsetY=finalCenterY-processReferenceY=" + offsetY.ToString("F6") +
                    ", referenceT=" + referenceT.ToString("F6") +
                    ", correctedT=" + correctedT.ToString("F6") +
                    ", offsetT=correctedT-referenceT=" + offsetT.ToString("F6") + " - Ok");

                Stage.ApplyWaferAlignResult(_originX, _originY, _pitchX, _pitchY, offsetX, offsetY);
                Stage.ApplyWaferAlignThetaResult(referenceT, correctedT, offsetT);

                WaferMaterial wafer = Stage.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer != null)
                {
                    string resultMode = _hybridVirtualFrameActive
                        ? InputStageResultMode.HybridRealVisionSimMotion
                        : InputStageResultMode.Standard;
                    wafer.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    if (_hybridVirtualFrameActive)
                    {
                        InputStageHybridResultSession.MarkAlign(
                            wafer.WaferId,
                            _alignResultRunId,
                            IsHybridVirtualFrameConfigurationCurrent);
                    }
                    else
                    {
                        InputStageHybridResultSession.Clear();
                    }

                    MaterialStateService.SaveInputStageAlignResult(
                        wafer,
                        _originX,
                        _originY,
                        _pitchX,
                        _pitchY,
                        offsetX,
                        offsetY,
                        true,
                        referenceT,
                        correctedT,
                        offsetT,
                        resultMode,
                        _alignResultRunId,
                        _manualAlignFallbackActive);

                    WriteLog("InputStageAlignSequence",
                        "InputStage align result provenance saved. waferId=" + wafer.WaferId +
                        ", resultMode=" + resultMode +
                        ", manualFallback=" + _manualAlignFallbackActive +
                        ", resultRunId=" + _alignResultRunId + " - Ok");
                }

                Context.Bus.Set("InputStageAligned");
                Context.Bus.Reset("InputStageDieMapped");
                Context.Bus.Reset("InputStageFinishComplete");
                Context.Bus.Reset("InputStageReady");
                CurrentStep = InputStageAlignStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-APPLY-EX", Stage.Name, "Align result apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void CaptureAlignThetaReference(string source)
        {
            try
            {
                _alignThetaReferenceT = Stage != null && Stage.StageT != null
                    ? Stage.StageT.ActualPosition
                    : 0.0;
                _alignThetaReferenceReady = true;

                WriteLog("InputStageAlignSequence",
                    "Align theta reference captured. source=" + source +
                    ", referenceT=" + _alignThetaReferenceT.ToString("F6") +
                    ", recipeProcessT=" + (Stage != null ? Stage.ResolveWaferAlignReferenceT().ToString("F6") : "0.000000") +
                    " - Ok");
            }
            catch
            {
                _alignThetaReferenceT = 0.0;
                _alignThetaReferenceReady = true;
            }
            finally
            {
            }
        }

        private double ResolveAlignThetaReference()
        {
            if (!_alignThetaReferenceReady)
                CaptureAlignThetaReference("ApplyAlignResultFallback");

            return _alignThetaReferenceT;
        }

        private static double NormalizeThetaOffset(double offsetT)
        {
            try
            {
                while (offsetT > 180.0)
                    offsetT -= 360.0;
                while (offsetT < -180.0)
                    offsetT += 360.0;
                return offsetT;
            }
            catch
            {
                return offsetT;
            }
            finally
            {
            }
        }

        private void StartVisionMarkRequest(string targetId, string stepName)
        {
            ClearPendingVisionRequest();
            _pendingVisionCts = new CancellationTokenSource();
            _pendingVisionTargetId = targetId;
            _pendingVisionStepName = stepName;
            _pendingVisionTask = RequestVisionPcOffsetWithRetryAsync(targetId, stepName, _pendingVisionCts.Token);
            WriteLog("InputStageAlignSequence",
                "Vision PC mark request started. step=" + stepName +
                ", target=" + targetId + " - Start");
        }

        private async Task<VisionAlignResult> WaitPendingVisionResultAsync(CancellationToken ct)
        {
            try
            {
                if (_pendingVisionTask == null)
                    return null;

                VisionAlignResult result = await AwaitVisionTaskWithCancellationAsync(_pendingVisionTask, ct).ConfigureAwait(false);
                WriteLog("InputStageAlignSequence",
                    "Vision PC mark request completed. step=" + _pendingVisionStepName +
                    ", target=" + _pendingVisionTargetId +
                    ", result=" + (result != null ? "OK" : "NG") + " - Done");
                return result;
            }
            finally
            {
                ClearPendingVisionRequest();
            }
        }

        private void ClearPendingVisionRequest()
        {
            if (_pendingVisionCts != null)
            {
                try
                {
                    if (_pendingVisionTask != null && !_pendingVisionTask.IsCompleted)
                        _pendingVisionCts.Cancel();
                }
                catch
                {
                }
                finally
                {
                    _pendingVisionCts.Dispose();
                    _pendingVisionCts = null;
                }
            }

            _pendingVisionTask = null;
            _pendingVisionStepName = string.Empty;
            _pendingVisionTargetId = string.Empty;
        }

        private static async Task<VisionAlignResult> AwaitVisionTaskWithCancellationAsync(Task<VisionAlignResult> task, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAsync(task, null, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> RequestVisionPcOffsetWithRetryAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                int retryCount = _alignVisionRetryCount;
                int maxAttempts = retryCount + 1;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    ct.ThrowIfCancellationRequested();
                    VisionAlignResult result = await RequestVisionPcOffsetOnceAsync(targetId, stepName, ct).ConfigureAwait(false);
                    if (result != null)
                    {
                        WriteLog("InputStageAlignSequence",
                            "Vision PC offset received. step=" + stepName +
                            ", target=" + targetId +
                            ", attempt=" + attempt +
                            ", dx=" + result.DeltaX.ToString("F6") +
                            ", dy=" + result.DeltaY.ToString("F6") +
                            ", dt=" + result.DeltaTheta.ToString("F6") + " - Ok");
                        return result;
                    }

                    WriteLog("InputStageAlignSequence",
                        "Vision PC offset receive failed. step=" + stepName +
                        ", target=" + targetId +
                        ", attempt=" + attempt + "/" + maxAttempts +
                        (attempt < maxAttempts ? " - Retry" : " - Failed"));
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Vision PC offset retry exception. step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private Task<VisionAlignResult> RequestConfiguredAlignVisionAsync(string targetId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var configurableVision = Stage.Vision as IConfigurableAlignVisionClient;
            if (configurableVision != null)
                return configurableVision.TriggerAlignAsync(targetId, _alignVisionTimeoutMs, ct);

            // 기존 외부 구현과의 호환 경로. 실장비 Wafer adapter는 위의 설정 지원 계약을 구현한다.
            return Stage.Vision.TriggerAlignAsync(targetId);
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

                Task<VisionAlignResult> alignTask = RequestConfiguredAlignVisionAsync(targetId, ct);
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
                WriteLog("InputStageAlignSequence", "Vision PC offset request exception. step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
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

                Task<VisionAlignResult> alignTask = RequestConfiguredAlignVisionAsync(targetId, ct);
                if (alignTask == null)
                    return null;

                VisionAlignResult result = await SequenceAwaiter.AwaitAsync(alignTask, null, ct).ConfigureAwait(false);
                WriteLog("InputStageAlignSequence",
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
                WriteLog("InputStageAlignSequence", "DryRun Vision GRAB request exception. step=" + stepName + ": " + ex.Message + " - SimFallback");
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
                        PitchX = ResolveAlignPitchX(null, null),
                        PitchY = ResolveAlignPitchY(null, null)
                    };
                }

                await Task.Delay(120, ct).ConfigureAwait(false);

                double dx;
                double dy;
                double dt;
                lock (SimVisionRandomLock)
                {
                    bool referenceMark =
                        string.Equals(stepName, VisionAlignTargetIds.Ref1, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(stepName, VisionAlignTargetIds.Ref2, StringComparison.OrdinalIgnoreCase);
                    dx = referenceMark ? 0.0 : (SimVisionRandom.NextDouble() - 0.5) * 0.002;
                    dy = referenceMark ? 0.0 : (SimVisionRandom.NextDouble() - 0.5) * 0.002;
                    dt = ResolveSimThetaOffset(stepName);
                }

                return new VisionAlignResult
                {
                    DeltaX = dx,
                    DeltaY = dy,
                    DeltaTheta = dt,
                    PitchX = ResolveAlignPitchX(null, null),
                    PitchY = ResolveAlignPitchY(null, null)
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Simulation vision offset failed. target=" + targetId + ", step=" + stepName + ": " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private async Task<VisionAlignResult> SearchVisionMarkAroundCurrentPointAsync(
            string targetId,
            string stepName,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (_alignSearchPointCount == 0)
                {
                    WriteLog("InputStageAlignSequence",
                        description + " 주변 탐색 설정이 0이므로 추가 탐색을 수행하지 않습니다. step=" + stepName + " - Skip");
                    return null;
                }

                if (!Options.EnableMotion)
                {
                    WriteLog("InputStageAlignSequence",
                        description + " 주변 탐색은 Motion Disable 상태라 수행하지 않습니다. step=" +
                        stepName + " - Skip");
                    return null;
                }

                double baseX = Stage != null && Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0;
                double baseY = Stage != null && Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0;
                double pitchX = Math.Abs(ResolveAlignPitchX(null, null));
                double pitchY = Math.Abs(ResolveAlignPitchY(null, null));
                if (pitchX <= 1e-9 || pitchY <= 1e-9)
                {
                    WriteLog("InputStageAlignSequence",
                        description + " 주변 탐색 실패. Pitch 값이 유효하지 않습니다. pitchX=" +
                        pitchX.ToString("F6") +
                        ", pitchY=" + pitchY.ToString("F6") + " - Failed");
                    return null;
                }

                SearchOffset[] offsets = BuildOnePitchSearchOffsets(pitchX, pitchY, _alignSearchPointCount);
                for (int i = 0; i < offsets.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    double targetX = baseX + offsets[i].X;
                    double targetY = baseY + offsets[i].Y;
                    string areaReason;
                    if (!Stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    {
                        WriteLog("InputStageAlignSequence",
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
                        WriteLog("InputStageAlignSequence",
                            description + " 주변 탐색에서 다이를 찾았습니다. direction=" +
                            offsets[i].Name +
                            ", baseX=" + baseX.ToString("F6") +
                            ", baseY=" + baseY.ToString("F6") +
                            ", targetX=" + targetX.ToString("F6") +
                            ", targetY=" + targetY.ToString("F6") +
                            ", dx=" + result.DeltaX.ToString("F6") +
                            ", dy=" + result.DeltaY.ToString("F6") +
                            ", dt=" + result.DeltaTheta.ToString("F6") + " - Ok");
                        return result;
                    }
                }

                WriteLog("InputStageAlignSequence",
                    description + " 설정된 주변 " + _alignSearchPointCount + "곳 탐색에서 다이를 찾지 못했습니다. baseX=" +
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
                WriteLog("InputStageAlignSequence",
                    description + " 주변 탐색 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static SearchOffset[] BuildOnePitchSearchOffsets(double pitchX, double pitchY, int pointCount)
        {
            var offsets = new[]
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
            Array.Resize(ref offsets, pointCount);
            return offsets;
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

        private double ResolveSimThetaOffset(string stepName)
        {
            try
            {
                double tolerance = ResolveThetaTolerance();
                if (string.Equals(stepName, VisionAlignTargetIds.CenterVerify, StringComparison.OrdinalIgnoreCase))
                    return 0.0;

                if (string.Equals(stepName, VisionAlignTargetIds.Center, StringComparison.OrdinalIgnoreCase))
                    return tolerance * 0.2;

                return 0.0;
            }
            catch
            {
                return 0.0;
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
                        MaterialStateService.NotifyAndSave("InputStageAlignSpecResolve");
                    }
                }
                else if (wafer != null &&
                         !string.Equals(wafer.TapeFrameSpecName, specName, StringComparison.OrdinalIgnoreCase))
                {
                    wafer.TapeFrameSpecName = specName;
                    MaterialStateService.NotifyAndSave("InputStageAlignSpecNormalize");
                }

                var spec = MaterialSpecs.FindFrame(specName);
                if (spec != null)
                    return spec;

                specName = MaterialStateService.ResolveInputTapeFrameSpecName(0);
                return MaterialSpecs.FindFrame(specName);
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Frame spec resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private void ApplyFrameSpecToMap(WaferMapData map, TapeFrameSpec spec)
        {
            try
            {
                if (map == null || spec == null)
                    return;

                bool externalMap = _resolvedAlignMapIsExternal ||
                                   (!string.IsNullOrWhiteSpace(spec.EdgeSkipMode) &&
                                    spec.EdgeSkipMode.IndexOf("External", StringComparison.OrdinalIgnoreCase) >= 0);
                bool fallbackMap = map.RowCount <= 1 && map.ColumnCount <= 1;
                int pitchGridX = 0;
                int pitchGridY = 0;
                bool pitchGridResolved = !externalMap && TryResolvePitchBasedGrid(spec, out pitchGridX, out pitchGridY);
                bool pitchGridMismatch = !externalMap && pitchGridResolved &&
                                         (map.ColumnCount != pitchGridX || map.RowCount != pitchGridY);
                if (!externalMap && (fallbackMap || pitchGridMismatch))
                {
                    int columnCount = pitchGridResolved ? pitchGridX : Math.Max(1, spec.DieMapX);
                    int rowCount = pitchGridResolved ? pitchGridY : Math.Max(1, spec.DieMapY);
                    RebuildFullWaferMap(map, rowCount, columnCount);
                }

                bool invalidRefPair = map.Ref1Row == map.Ref2Row && map.Ref1Col == map.Ref2Col;
                if ((!externalMap && (fallbackMap || pitchGridMismatch)) || invalidRefPair)
                    ApplyDefaultRefPair(map);

                WriteLog("InputStageAlignSequence",
                    "Frame spec applied to align map. spec=" + spec.Name +
                    ", dieMapX=" + spec.DieMapX +
                    ", dieMapY=" + spec.DieMapY +
                    ", alignMapX=" + map.ColumnCount +
                    ", alignMapY=" + map.RowCount +
                    ", externalMap=" + externalMap +
                    ", pitchGapX=" + spec.PitchX.ToString("F6") +
                    ", pitchGapY=" + spec.PitchY.ToString("F6") +
                    ", centerStepX=" + DieMapGenerator.CalculateCenterStep(spec.DieSizeX, spec.PitchX).ToString("F6") +
                    ", centerStepY=" + DieMapGenerator.CalculateCenterStep(spec.DieSizeY, spec.PitchY).ToString("F6") + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Frame spec apply to map failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool TryResolvePitchBasedGrid(TapeFrameSpec spec, out int gridX, out int gridY)
        {
            gridX = 0;
            gridY = 0;
            if (spec == null ||
                spec.OuterDiameterMm <= 0.0 ||
                spec.DieSizeX <= 0.0 ||
                spec.DieSizeY <= 0.0 ||
                double.IsNaN(spec.PitchX) || double.IsInfinity(spec.PitchX) || spec.PitchX < 0.0 ||
                double.IsNaN(spec.PitchY) || double.IsInfinity(spec.PitchY) || spec.PitchY < 0.0)
                return false;

            double centerStepX = DieMapGenerator.CalculateCenterStep(spec.DieSizeX, spec.PitchX);
            double centerStepY = DieMapGenerator.CalculateCenterStep(spec.DieSizeY, spec.PitchY);
            gridX = DieMapGenerator.CalculateWaferGridCount(spec.OuterDiameterMm, centerStepX, spec.DieSizeX);
            gridY = DieMapGenerator.CalculateWaferGridCount(spec.OuterDiameterMm, centerStepY, spec.DieSizeY);
            return gridX > 0 && gridY > 0;
        }

        private static void RebuildFullWaferMap(WaferMapData map, int rowCount, int columnCount)
        {
            if (map == null)
                return;

            map.RowCount = Math.Max(1, rowCount);
            map.ColumnCount = Math.Max(1, columnCount);
            map.DieMap = new bool[map.RowCount, map.ColumnCount];
            for (int row = 0; row < map.RowCount; row++)
            {
                for (int col = 0; col < map.ColumnCount; col++)
                    map.DieMap[row, col] = true;
            }
        }

        private double ResolveAlignPitchX(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            try
            {
                double configured = ResolveConfiguredAlignPitchX();
                double visionPitch = SelectVisionPitchX(ref1Result, ref2Result);
                if (visionPitch > 0.0)
                    return NormalizeResolvedAlignPitch("X", visionPitch, configured, "VisionPitch");
                LogVisionPitchUnavailable("X", ref1Result, ref2Result, configured);
                if (configured > 0.0)
                    return configured;
                return Stage.ResolveAlignPitchX(ref1Result, ref2Result);
            }
            catch
            {
                return Stage.ResolveAlignPitchX(ref1Result, ref2Result);
            }
            finally
            {
            }
        }

        private double ResolveAlignPitchY(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            try
            {
                double configured = ResolveConfiguredAlignPitchY();
                double visionPitch = SelectVisionPitchY(ref1Result, ref2Result);
                if (visionPitch > 0.0)
                    return NormalizeResolvedAlignPitch("Y", visionPitch, configured, "VisionPitch");
                LogVisionPitchUnavailable("Y", ref1Result, ref2Result, configured);
                if (configured > 0.0)
                    return configured;
                return Stage.ResolveAlignPitchY(ref1Result, ref2Result);
            }
            catch
            {
                return Stage.ResolveAlignPitchY(ref1Result, ref2Result);
            }
            finally
            {
            }
        }

        private double ResolveConfiguredAlignPitchX()
        {
            if (_frameSpec != null && _frameSpec.DieSizeX > 0.0 &&
                !double.IsNaN(_frameSpec.PitchX) && !double.IsInfinity(_frameSpec.PitchX) && _frameSpec.PitchX >= 0.0)
                return DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeX, _frameSpec.PitchX);
            return Stage != null ? Stage.ResolveAlignPitchX(null, null) : 0.0;
        }

        private double ResolveConfiguredAlignPitchY()
        {
            if (_frameSpec != null && _frameSpec.DieSizeY > 0.0 &&
                !double.IsNaN(_frameSpec.PitchY) && !double.IsInfinity(_frameSpec.PitchY) && _frameSpec.PitchY >= 0.0)
                return DieMapGenerator.CalculateCenterStep(_frameSpec.DieSizeY, _frameSpec.PitchY);
            return Stage != null ? Stage.ResolveAlignPitchY(null, null) : 0.0;
        }

        private static double SelectVisionPitchX(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            if (ref2Result != null && ref2Result.PitchX > 0.0)
                return ref2Result.PitchX;
            if (ref1Result != null && ref1Result.PitchX > 0.0)
                return ref1Result.PitchX;
            return 0.0;
        }

        private static double SelectVisionPitchY(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            if (ref2Result != null && ref2Result.PitchY > 0.0)
                return ref2Result.PitchY;
            if (ref1Result != null && ref1Result.PitchY > 0.0)
                return ref1Result.PitchY;
            return 0.0;
        }

        private double NormalizeResolvedAlignPitch(string axis, double candidate, double configured, string source)
        {
            if (candidate <= 0.0)
                return configured;
            if (configured <= 0.0)
                return candidate;

            double delta = Math.Abs(candidate - configured);
            if (delta <= AlignPitchCompareToleranceMm)
                return candidate;

            WriteLog("InputStageAlignSequence",
                "Align pitch mismatch. configured data is used. axis=" + axis +
                ", source=" + source +
                ", candidate=" + candidate.ToString("F6") +
                ", configured=" + configured.ToString("F6") +
                ", delta=" + delta.ToString("F6") +
                ", tolerance=" + AlignPitchCompareToleranceMm.ToString("F6") + " - Check");
            return configured;
        }

        private void LogVisionPitchUnavailable(
            string axis,
            VisionAlignResult ref1Result,
            VisionAlignResult ref2Result,
            double configured)
        {
            try
            {
                if (ref1Result == null && ref2Result == null)
                    return;

                WriteLog("InputStageAlignSequence",
                    "Vision align pitch was not supplied. configured map/frame pitch is used. axis=" + axis +
                    ", configured=" + configured.ToString("F6") + " - Check");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private bool IsSimulationOrDryRun()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
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

        /// <summary>
        /// 센터 다이 미검출 시(파샬 웨이퍼 등) 센터 지령 위치와 설정 피치로 명목 얼라인을 완성하고
        /// ApplyAlignResult로 직행한다. 세타/오프셋 보정은 0으로 남아 Review 화면에서 수동 보정한다.
        /// 앵커 미캡처(모션 비활성)나 설정 피치 무효면 false를 반환해 기존 알람 경로를 유지한다.
        /// </summary>
        private bool TryEnterManualAlignFallback()
        {
            if (!_alignAnchorReady)
            {
                WriteLog("InputStageAlignSequence",
                    "Manual align fallback unavailable. reason=anchor not captured - Skip");
                return false;
            }

            double pitchX = ResolveConfiguredAlignPitchX();
            double pitchY = ResolveConfiguredAlignPitchY();
            if (pitchX <= 0.0 || pitchY <= 0.0)
            {
                WriteLog("InputStageAlignSequence",
                    "Manual align fallback unavailable. reason=configured pitch invalid" +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") + " - Skip");
                return false;
            }

            _pitchX = pitchX;
            _pitchY = pitchY;
            _originX = _alignAnchorX - (_alignAnchorCol * _pitchX);
            _originY = _alignAnchorY - (_alignAnchorRow * _pitchY);
            _manualAlignFallbackActive = true;

            WriteLog("InputStageAlignSequence",
                "Manual align fallback engaged. center die not found." +
                " anchorRow=" + _alignAnchorRow +
                ", anchorCol=" + _alignAnchorCol +
                ", anchorX=" + _alignAnchorX.ToString("F6") +
                ", anchorY=" + _alignAnchorY.ToString("F6") +
                ", pitchX=" + _pitchX.ToString("F6") +
                ", pitchY=" + _pitchY.ToString("F6") +
                ", originX=anchorX-anchorCol*pitchX=" + _originX.ToString("F6") +
                ", originY=anchorY-anchorRow*pitchY=" + _originY.ToString("F6") + " - Check");
            QMC.Common.Logging.EventLogger.Write(
                QMC.Common.Logging.EventKind.Warning,
                "SYS",
                "IN-STAGE-ALIGN-MANUAL-FALLBACK",
                Stage != null ? Stage.Name : "InputStageUnit",
                "Wafer Align Center 다이를 찾지 못해 명목값 얼라인으로 진행합니다. Review 화면에서 Jog 정렬과 T 보정을 완료해야 확정할 수 있습니다.");

            CurrentStep = InputStageAlignStep.ApplyAlignResult;
            return true;
        }

        private void CaptureAlignAnchorFromCurrentPosition(int row, int col, string description)
        {
            _alignAnchorRow = row;
            _alignAnchorCol = col;
            _alignAnchorX = Stage != null && Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0;
            _alignAnchorY = Stage != null && Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0;
            _alignAnchorReady = true;

            WriteLog("InputStageAlignSequence",
                "Align anchor captured from current motor position. description=" + description +
                ", row=" + row +
                ", col=" + col +
                ", anchorX=" + _alignAnchorX.ToString("F6") +
                ", anchorY=" + _alignAnchorY.ToString("F6") + " - Ok");
        }

        private void CaptureAlignProcessReferenceIfNeeded(string description)
        {
            if (_alignProcessReferenceReady)
                return;

            _alignProcessReferenceX = Stage != null && Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0;
            _alignProcessReferenceY = Stage != null && Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0;
            _alignProcessReferenceReady = true;

            WriteLog("InputStageAlignSequence",
                "Align process reference captured. description=" + description +
                ", processReferenceX=" + _alignProcessReferenceX.ToString("F6") +
                ", processReferenceY=" + _alignProcessReferenceY.ToString("F6") + " - Ok");
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
                WriteLog("InputStageAlignSequence",
                    "Align Vision/motion mode resolved. realVisionInSimulation=False" +
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
                return Fail("IN-STAGE-ALIGN-HYBRID-AXIS-MODE", Stage != null ? Stage.Name : "InputStageUnit",
                    "Real Vision simulation mode requires CameraX/StageY/StageT to be all simulated or all real. " +
                    "cameraXSimulated=" + cameraXSimulated +
                    ", stageYSimulated=" + stageYSimulated +
                    ", stageTSimulated=" + stageTSimulated);
            }

            _hybridVirtualFrameActive = simulatedAxisCount == 3;
            if (_hybridVirtualFrameActive && !Options.EnableMotion)
            {
                _hybridVirtualFrameActive = false;
                return Fail("IN-STAGE-ALIGN-HYBRID-MOTION-DISABLED", Stage != null ? Stage.Name : "InputStageUnit",
                    "HybridRealVisionSimMotion requires simulated X/Y/T motion commands to be enabled.");
            }

            WriteLog("InputStageAlignSequence",
                "Align Vision/motion mode resolved. realVisionInSimulation=True" +
                ", cameraXSimulated=" + cameraXSimulated +
                ", stageYSimulated=" + stageYSimulated +
                ", stageTSimulated=" + stageTSimulated +
                ", mode=" + ResolveVisionMotionModeName() + " - Ok");
            return 0;
        }

        private bool IsHybridVirtualFrameConfigurationCurrent()
        {
            return Options != null &&
                Options.EnableMotion &&
                AutoVisionRequestService.IsRealVisionInSimulationActive() &&
                IsSimulatedAxis(Stage != null ? Stage.CameraX : null) &&
                IsSimulatedAxis(Stage != null ? Stage.StageY : null) &&
                IsSimulatedAxis(Stage != null ? Stage.StageT : null);
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

        private void ResolveHybridEffectiveMoveCorrection(
            double rawMoveDeltaX,
            double rawMoveDeltaY,
            string description,
            out double effectiveMoveDeltaX,
            out double effectiveMoveDeltaY)
        {
            effectiveMoveDeltaX = rawMoveDeltaX;
            effectiveMoveDeltaY = rawMoveDeltaY;
            if (!_hybridVirtualFrameActive)
                return;

            effectiveMoveDeltaX = rawMoveDeltaX - _hybridCenterAppliedMoveX;
            effectiveMoveDeltaY = rawMoveDeltaY - _hybridCenterAppliedMoveY;
            WriteLog("InputStageAlignSequence",
                "Hybrid virtual XY residual. description=" + description +
                ", rawMoveDeltaX=" + rawMoveDeltaX.ToString("F6") +
                ", rawMoveDeltaY=" + rawMoveDeltaY.ToString("F6") +
                ", virtualAppliedMoveX=" + _hybridCenterAppliedMoveX.ToString("F6") +
                ", virtualAppliedMoveY=" + _hybridCenterAppliedMoveY.ToString("F6") +
                ", effectiveMoveDeltaX=raw-applied=" + effectiveMoveDeltaX.ToString("F6") +
                ", effectiveMoveDeltaY=raw-applied=" + effectiveMoveDeltaY.ToString("F6") +
                ", mode=HybridRealVisionSimMotion - Check");
        }

        private double ResolveHybridEffectiveCenterTheta(double rawVisionTheta, string description)
        {
            if (!_hybridVirtualFrameActive)
                return rawVisionTheta;

            double effectiveTheta = NormalizeThetaOffset(rawVisionTheta + _hybridCenterAppliedTheta);
            WriteLog("InputStageAlignSequence",
                "Hybrid virtual center theta residual. description=" + description +
                ", rawVisionTheta=" + rawVisionTheta.ToString("F6") +
                ", virtualAppliedTheta=" + _hybridCenterAppliedTheta.ToString("F6") +
                ", effectiveTheta=raw+applied=" + effectiveTheta.ToString("F6") +
                ", mode=HybridRealVisionSimMotion - Check");
            return effectiveTheta;
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

            WriteLog("InputStageAlignSequence",
                "Input camera motor correction formula. description=" + description +
                ", rawVisionDx=" + result.DeltaX.ToString("F6") +
                ", rawVisionDy=" + result.DeltaY.ToString("F6") +
                ", inputDeltaX=rawDx=" + inputDeltaX.ToString("F6") +
                ", inputDeltaY=rawDy=" + inputDeltaY.ToString("F6") +
                ", moveDeltaX=inputDeltaX=" + moveDeltaX.ToString("F6") +
                ", moveDeltaY=-inputDeltaY=" + moveDeltaY.ToString("F6") + " - Ok");
        }

        private void CaptureAlignAnchorFromVisionResult(VisionAlignResult result, string description)
        {
            if (result == null || Stage == null)
                return;

            if (!_alignAnchorReady)
            {
                int centerRow = _map != null ? _map.RowCount / 2 : 0;
                int centerCol = _map != null ? _map.ColumnCount / 2 : 0;
                CaptureAlignAnchorFromCurrentPosition(centerRow, centerCol, description);
            }

            double inputDeltaX;
            double inputDeltaY;
            double moveDeltaX;
            double moveDeltaY;
            ResolveInputCameraMotorCorrection(
                result,
                description,
                out inputDeltaX,
                out inputDeltaY,
                out moveDeltaX,
                out moveDeltaY);

            double currentX = Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0;
            double currentY = Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0;
            _alignAnchorX = currentX + moveDeltaX;
            _alignAnchorY = currentY + moveDeltaY;

            WriteLog("InputStageAlignSequence",
                "Align anchor updated from vision result. description=" + description +
                ", row=" + _alignAnchorRow +
                ", col=" + _alignAnchorCol +
                ", currentX=" + currentX.ToString("F6") +
                ", currentY=" + currentY.ToString("F6") +
                ", anchorX=" + _alignAnchorX.ToString("F6") +
                ", anchorY=" + _alignAnchorY.ToString("F6") +
                ", formulaX=currentX+inputDeltaX" +
                ", formulaY=currentY-inputDeltaY" +
                ", inputDeltaX=" + inputDeltaX.ToString("F6") +
                ", inputDeltaY=" + inputDeltaY.ToString("F6") + " - Ok");
        }

        private async Task<int> ApplyCenterVisionCorrectionAsync(
            VisionAlignResult result,
            string description,
            CancellationToken ct)
        {
            CaptureAlignAnchorFromVisionResult(result, description);
            if (!Options.EnableMotion)
                return 0;

            double actualBeforeX = Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0;
            double actualBeforeY = Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0;

            string areaReason;
            if (!Stage.IsInputStageWorkPointInArea(_alignAnchorX, _alignAnchorY, out areaReason))
                return Fail("IN-STAGE-ALIGN-CENTER-WORK-AREA", Stage.Name,
                    description + " center correction target is outside input stage work area. targetX=" +
                    _alignAnchorX.ToString("F6") +
                    ", targetY=" + _alignAnchorY.ToString("F6") +
                    ", reason=" + areaReason);

            WriteLog("InputStageAlignSequence",
                "Apply center motor correction. description=" + description +
                ", targetX=currentX+inputDeltaX=" + _alignAnchorX.ToString("F6") +
                ", targetY=currentY-inputDeltaY=" + _alignAnchorY.ToString("F6") + " - Start");

            int resultCode = await MoveVisionXYPointSafelyAsync(
                _alignAnchorX,
                _alignAnchorY,
                description + " center correction",
                ct).ConfigureAwait(false);
            if (resultCode != 0)
                return resultCode;

            if (_hybridVirtualFrameActive)
            {
                double actualAfterX = Stage.CameraX != null ? Stage.CameraX.ActualPosition : actualBeforeX;
                double actualAfterY = Stage.StageY != null ? Stage.StageY.ActualPosition : actualBeforeY;
                double appliedMoveX = actualAfterX - actualBeforeX;
                double appliedMoveY = actualAfterY - actualBeforeY;
                _hybridCenterAppliedMoveX += appliedMoveX;
                _hybridCenterAppliedMoveY += appliedMoveY;
                WriteLog("InputStageAlignSequence",
                    "Hybrid virtual center XY updated. appliedMoveX=actualAfter-actualBefore=" +
                    appliedMoveX.ToString("F6") +
                    ", appliedMoveY=actualAfter-actualBefore=" + appliedMoveY.ToString("F6") +
                    ", accumulatedVirtualMoveX=" + _hybridCenterAppliedMoveX.ToString("F6") +
                    ", accumulatedVirtualMoveY=" + _hybridCenterAppliedMoveY.ToString("F6") +
                    ", mode=HybridRealVisionSimMotion - Ok");
            }

            WriteLog("InputStageAlignSequence",
                "Center motor correction completed. description=" + description +
                ", actualX=" + (Stage.CameraX != null ? Stage.CameraX.ActualPosition.ToString("F6") : "0.000000") +
                ", actualY=" + (Stage.StageY != null ? Stage.StageY.ActualPosition.ToString("F6") : "0.000000") +
                ", targetX=" + _alignAnchorX.ToString("F6") +
                ", targetY=" + _alignAnchorY.ToString("F6") + " - Ok");
            return 0;
        }

        private void ResolveAlignPointFromVisionResult(
            VisionAlignResult result,
            string description,
            out double pointX,
            out double pointY)
        {
            double inputDeltaX;
            double inputDeltaY;
            double moveDeltaX;
            double moveDeltaY;
            ResolveInputCameraMotorCorrection(
                result,
                description,
                out inputDeltaX,
                out inputDeltaY,
                out moveDeltaX,
                out moveDeltaY);

            double effectiveMoveDeltaX;
            double effectiveMoveDeltaY;
            ResolveHybridEffectiveMoveCorrection(
                moveDeltaX,
                moveDeltaY,
                description,
                out effectiveMoveDeltaX,
                out effectiveMoveDeltaY);

            double currentX = Stage != null && Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0;
            double currentY = Stage != null && Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0;
            pointX = currentX + effectiveMoveDeltaX;
            pointY = currentY + effectiveMoveDeltaY;

            WriteLog("InputStageAlignSequence",
                "Align reference point formula. description=" + description +
                ", pointX=currentX+inputDeltaX=" + pointX.ToString("F6") +
                ", pointY=currentY-inputDeltaY=" + pointY.ToString("F6") +
                ", currentX=" + currentX.ToString("F6") +
                ", currentY=" + currentY.ToString("F6") +
                ", inputDeltaX=" + inputDeltaX.ToString("F6") +
                ", inputDeltaY=" + inputDeltaY.ToString("F6") +
                ", rawMoveDeltaX=" + moveDeltaX.ToString("F6") +
                ", rawMoveDeltaY=" + moveDeltaY.ToString("F6") +
                ", effectiveMoveDeltaX=" + effectiveMoveDeltaX.ToString("F6") +
                ", effectiveMoveDeltaY=" + effectiveMoveDeltaY.ToString("F6") +
                ", mode=" + ResolveVisionMotionModeName() + " - Ok");
        }

        private async Task<int> VerifyFinalCenterAsync(CancellationToken ct)
        {
            string targetId = ResolveTargetId(Options.CenterAlignTargetId, VisionAlignTargetIds.Center);
            ct.ThrowIfCancellationRequested();
            VisionAlignResult result = await RequestVisionPcOffsetWithRetryAsync(
                targetId,
                "FinalCenterVerify",
                ct).ConfigureAwait(false);
            if (result == null)
                return Fail("IN-STAGE-ALIGN-FINAL-CENTER-VISION", "Vision",
                    "Final center verification vision result was not received.");

            double inputDeltaX;
            double inputDeltaY;
            double moveDeltaX;
            double moveDeltaY;
            ResolveInputCameraMotorCorrection(
                result,
                "FinalCenterVerify",
                out inputDeltaX,
                out inputDeltaY,
                out moveDeltaX,
                out moveDeltaY);

            double effectiveMoveDeltaX;
            double effectiveMoveDeltaY;
            ResolveHybridEffectiveMoveCorrection(
                moveDeltaX,
                moveDeltaY,
                "FinalCenterVerify",
                out effectiveMoveDeltaX,
                out effectiveMoveDeltaY);

            double thetaTolerance = ResolveThetaTolerance();
            bool centerOk = Math.Abs(effectiveMoveDeltaX) <= AlignCenterToleranceMm &&
                Math.Abs(effectiveMoveDeltaY) <= AlignCenterToleranceMm;
            bool refThetaOk = Math.Abs(_thetaFromTwoPoint) <= thetaTolerance;

            WriteLog("InputStageAlignSequence",
                "Final center verification without correction. rawMoveDeltaX=" + moveDeltaX.ToString("F6") +
                ", rawMoveDeltaY=" + moveDeltaY.ToString("F6") +
                ", effectiveMoveDeltaX=" + effectiveMoveDeltaX.ToString("F6") +
                ", effectiveMoveDeltaY=" + effectiveMoveDeltaY.ToString("F6") +
                ", centerTolerance=" + AlignCenterToleranceMm.ToString("F6") +
                ", centerVisionDeltaTheta=" + result.DeltaTheta.ToString("F6") +
                ", finalRefTheta=" + _thetaFromTwoPoint.ToString("F6") +
                ", thetaTolerance=" + thetaTolerance.ToString("F6") +
                ", centerOk=" + centerOk +
                ", refThetaOk=" + refThetaOk +
                ", mode=" + ResolveVisionMotionModeName() +
                ", xyCorrectionApplied=False - Check");

            if (!refThetaOk)
                return Fail("IN-STAGE-ALIGN-FINAL-REF-THETA-TOL", Stage.Name,
                    "Final Ref1/Ref2 theta is out of tolerance. theta=" + _thetaFromTwoPoint.ToString("F6") +
                    ", tolerance=" + thetaTolerance.ToString("F6"));

            if (!centerOk)
                return Fail("IN-STAGE-ALIGN-FINAL-CENTER-TOL", Stage.Name,
                    "Final center offset is out of tolerance. XY correction is not applied after the first center. " +
                    "rawMoveDeltaX=" + moveDeltaX.ToString("F6") +
                    ", rawMoveDeltaY=" + moveDeltaY.ToString("F6") +
                    ", effectiveMoveDeltaX=" + effectiveMoveDeltaX.ToString("F6") +
                    ", effectiveMoveDeltaY=" + effectiveMoveDeltaY.ToString("F6") +
                    ", tolerance=" + AlignCenterToleranceMm.ToString("F6"));

            int centerRow = _map != null ? _map.RowCount / 2 : _alignAnchorRow;
            int centerCol = _map != null ? _map.ColumnCount / 2 : _alignAnchorCol;
            CaptureAlignAnchorFromCurrentPosition(centerRow, centerCol, "final center verified without correction");
            CaptureFinalAlignOffsets("FinalCenterVerify");
            return 0;
        }

        private void CaptureFinalAlignOffsets(string description)
        {
            if (!_alignProcessReferenceReady)
                CaptureAlignProcessReferenceIfNeeded(description + " fallback");

            double finalX = Stage != null && Stage.CameraX != null ? Stage.CameraX.ActualPosition : _alignAnchorX;
            double finalY = Stage != null && Stage.StageY != null ? Stage.StageY.ActualPosition : _alignAnchorY;
            _finalAlignOffsetX = finalX - _alignProcessReferenceX;
            _finalAlignOffsetY = finalY - _alignProcessReferenceY;
            _finalAlignOffsetReady = true;

            WriteLog("InputStageAlignSequence",
                "Final align offsets captured. description=" + description +
                ", finalX=" + finalX.ToString("F6") +
                ", finalY=" + finalY.ToString("F6") +
                ", processReferenceX=" + _alignProcessReferenceX.ToString("F6") +
                ", processReferenceY=" + _alignProcessReferenceY.ToString("F6") +
                ", offsetX=finalX-processReferenceX=" + _finalAlignOffsetX.ToString("F6") +
                ", offsetY=finalY-processReferenceY=" + _finalAlignOffsetY.ToString("F6") + " - Ok");
        }

        private void ResolveVisionPointTarget(int row, int col, out double targetX, out double targetY)
        {
            double pitchX = ResolveAlignPitchX(null, null);
            double pitchY = ResolveAlignPitchY(null, null);

            if (!_alignAnchorReady)
            {
                int centerRow = _map != null ? _map.RowCount / 2 : row;
                int centerCol = _map != null ? _map.ColumnCount / 2 : col;
                CaptureAlignAnchorFromCurrentPosition(centerRow, centerCol, "fallback");
            }

            targetX = _alignAnchorX + ((double)col - _alignAnchorCol) * pitchX;
            targetY = _alignAnchorY + ((double)row - _alignAnchorRow) * pitchY;
        }

        private async Task<int> MoveVisionPointAndVerifyAsync(int row, int col, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                double targetX;
                double targetY;
                ResolveVisionPointTarget(row, col, out targetX, out targetY);

                string areaReason;
                if (!Stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    return Fail("IN-STAGE-ALIGN-WORK-AREA", Stage.Name,
                        description + " target is outside input stage work area. " + areaReason);

                WriteLog("InputStageAlignSequence",
                    "Move align vision point. description=" + description +
                    ", row=" + row +
                    ", col=" + col +
                    ", anchorRow=" + _alignAnchorRow +
                    ", anchorCol=" + _alignAnchorCol +
                    ", anchorX=" + _alignAnchorX.ToString("F6") +
                    ", anchorY=" + _alignAnchorY.ToString("F6") +
                    ", targetX=" + targetX.ToString("F6") +
                    ", targetY=" + targetY.ToString("F6") + " - Start");

                int result = await MoveVisionXYPointSafelyAsync(targetX, targetY, description, ct).ConfigureAwait(false);
                if (result != 0) return result;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-POINT-MOVE-EX", Stage.Name, description + " move failed: " + ex.Message);
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
                int result = await MoveAxisAndVerifyAsync(WaferStageAxis.VisionX, targetX, description + " VisionX", ct).ConfigureAwait(false);
                if (result != 0) return result;
                return await MoveAxisAndVerifyAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct).ConfigureAwait(false);
            }

            string yFirstReason;
            if (Stage.IsInputStageWorkPointInArea(currentX, targetY, out yFirstReason))
            {
                int result = await MoveAxisAndVerifyAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct).ConfigureAwait(false);
                if (result != 0) return result;
                return await MoveAxisAndVerifyAsync(WaferStageAxis.VisionX, targetX, description + " VisionX", ct).ConfigureAwait(false);
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

            return Fail("IN-STAGE-ALIGN-WORK-AREA-PATH", Stage.Name,
                description + " has no safe L-path inside input stage work area. currentX=" + currentX.ToString("F3") +
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
                    WriteLog("InputStageAlignSequence",
                        description + " work area center entry is not available. entryX=" + entryX.ToString("F6") +
                        ", entryY=" + entryY.ToString("F6") +
                        ", reason=" + entryReason +
                        ", xFirst=" + xFirstReason +
                        ", yFirst=" + yFirstReason + " - Skip");
                    return int.MinValue;
                }

                WriteLog("InputStageAlignSequence",
                    description + " has no direct L-path. Enter work center first. entryX=" +
                    entryX.ToString("F6") +
                    ", entryY=" + entryY.ToString("F6") +
                    ", targetX=" + targetX.ToString("F6") +
                    ", targetY=" + targetY.ToString("F6") + " - Start");

                int result = await MoveAxisAndVerifyAsync(WaferStageAxis.WaferY, entryY, description + " Entry StageY", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveAxisAndVerifyAsync(WaferStageAxis.VisionX, targetX, description + " Entry VisionX", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveAxisAndVerifyAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-WORK-AREA-ENTRY-EX", Stage != null ? Stage.Name : "InputStageUnit",
                    description + " 작업영역 진입 경유 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private WaferMapData ResolveWaferMapForAlign(string waferId)
        {
            try
            {
                _resolvedAlignMapIsExternal = false;
                DieMap sourceMap = ResolveSourceInputDieMap(_wafer);
                if (IsUsableSourceMap(sourceMap))
                {
                    _resolvedAlignMapIsExternal = RecipeDieMapResolver.IsExternalMap(sourceMap);
                    return ConvertDieMapToWaferMap(sourceMap, waferId);
                }

                if (IsManagedInputMapApprovalRequired())
                {
                    WriteLog("InputStageAlignSequence",
                        "Managed Recipe input map is not FINAL APPLY approved. Align fallback map is blocked. - Failed");
                    return null;
                }

                return Stage.EnsureWaferMapForAlign(waferId, Options.AllowFallbackMap);
            }
            catch (Exception ex)
            {
                _resolvedAlignMapIsExternal = false;
                WriteLog("InputStageAlignSequence", "Align wafer map resolve failed: " + ex.Message + " - Failed");
                if (IsManagedInputMapApprovalRequired())
                    return null;
                return Stage.EnsureWaferMapForAlign(waferId, Options.AllowFallbackMap);
            }
            finally
            {
            }
        }

        private static DieMap ResolveSourceInputDieMap(WaferMaterial wafer)
        {
            try
            {
                // 현재 기준: 새 wafer align 소스는 recipe/current wafer를 우선하고 이전 active map은 마지막 fallback으로만 사용한다.
                RecipeProject project = RecipeStore.LoadLastOrDefaultCached();
                WaferMapProcessSettings settings = project != null ? project.InputMapProcessing : null;
                bool network = AppSettingsStore.Current != null && RecipeInputMapSource.UsesRemoteForActiveRecipe(AppSettingsStore.Current);
                string barcode = wafer != null ? wafer.BarcodeId ?? "" : "";
                DieMap prepared = MaterialStateService.GetPreparedInputMap(wafer, barcode, network);
                if (prepared != null)
                    return WaferMapProcessService.Prepare(prepared, settings, "Input");
                if (network)
                    throw new InvalidOperationException("얼라인 전에 바코드 웨이퍼맵 사전 확인과 준비를 완료해야 합니다. 등록 맵으로 대체할 수 없습니다.");
                DieMap recipeMap = LoadRecipeInputDieMap();
                if (IsUsableSourceMap(recipeMap))
                {
                    recipeMap = WaferMapProcessService.Prepare(recipeMap, settings, "Input");
                    if (wafer != null && !wafer.HasInputStageDieMappingResult)
                        MaterialStateService.PinPreparedInputMap(wafer, barcode, false, recipeMap);
                    return recipeMap;
                }

                if (IsManagedInputMapApprovalRequired())
                    return null;

                DieMap materialMap = MaterialStateService.BuildDieMapFromWafer(wafer);
                if (IsUsableSourceMap(materialMap))
                    return materialMap;

                DieMap activeMap = LotStorage.ActiveInputDieMap;
                if (IsUsableSourceMap(activeMap))
                    return activeMap;

                return null;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Source input die map resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static DieMap LoadRecipeInputDieMap()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return null;

                string path;
                string reason;
                DieMap map = RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.Input, out path, out reason);
                if (map != null)
                    WriteLog("InputStageAlignSequence", "Recipe input die map loaded for align. path=" + path + " - Ok");
                else if (!string.IsNullOrWhiteSpace(reason))
                    WriteLog("InputStageAlignSequence", "Recipe input die map blocked for align. reason=" + reason + " - Failed");
                return map;
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Recipe input die map load for align failed: " + ex.Message + " - Failed");
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
                return (project != null && (project.MapApprovalVersion > 0 || project.InputMapProcessing != null)) ||
                    (AppSettingsStore.Current != null && RecipeInputMapSource.UsesRemoteForActiveRecipe(AppSettingsStore.Current));
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "맵 승인 조건을 읽지 못해 얼라인 대체 맵 사용을 차단합니다. " + ex.Message + " - Failed");
                return true;
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

        private static WaferMapData ConvertDieMapToWaferMap(DieMap map, string waferId)
        {
            if (map == null)
                return null;

            int dieMapX = Math.Max(1, map.DieMapX);
            int dieMapY = Math.Max(1, map.DieMapY);
            var waferMap = new WaferMapData
            {
                WaferId = string.IsNullOrWhiteSpace(waferId) ? map.FrameObjId : waferId,
                ColumnCount = dieMapX,
                RowCount = dieMapY,
                DieMap = new bool[dieMapY, dieMapX]
            };

            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null ||
                    entry.DieMapX < 0 || entry.DieMapX >= dieMapX ||
                    entry.DieMapY < 0 || entry.DieMapY >= dieMapY)
                    continue;

                waferMap.DieMap[entry.DieMapY, entry.DieMapX] = entry.IsTarget;
            }

            ApplyDefaultRefPair(waferMap);
            return waferMap;
        }

        private static void ApplyDefaultRefPair(WaferMapData map)
        {
            if (map == null)
                return;

            int centerRow = map.RowCount > 0 ? map.RowCount / 2 : 0;
            int leftCol = map.ColumnCount > 1 ? Math.Max(0, map.ColumnCount / 4) : 0;
            int rightCol = map.ColumnCount > 1 ? Math.Min(map.ColumnCount - 1, (map.ColumnCount * 3) / 4) : 0;
            if (rightCol == leftCol && map.ColumnCount > 1)
                rightCol = map.ColumnCount - 1;

            map.Ref1Row = centerRow;
            map.Ref1Col = leftCol;
            map.Ref2Row = centerRow;
            map.Ref2Col = rightCol;
        }

        private int CheckAlignProcessTeaching()
        {
            try
            {
                double targetX = Stage != null && Stage.Recipe != null && Stage.Recipe.VisionX != null
                    ? Stage.Recipe.VisionX.ProcessPosition
                    : 0.0;
                double targetY = Stage != null && Stage.Recipe != null && Stage.Recipe.WaferY != null
                    ? Stage.Recipe.WaferY.ProcessPosition
                    : 0.0;

                if (Math.Abs(targetX) <= 1e-9 && Math.Abs(targetY) <= 1e-9)
                    return Fail("IN-STAGE-ALIGN-PROCESS-TEACH", Stage.Name,
                        "InputStage align process position is not taught. VisionX.ProcessPosition=0, WaferY.ProcessPosition=0.");

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-PROCESS-TEACH-EX", Stage.Name,
                    "InputStage align process teaching check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisAndVerifyAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            return await MoveAxisAndVerifyAsync(axis, target, description, ct, false).ConfigureAwait(false);
        }

        private async Task<int> MoveAxisAndVerifyAsync(WaferStageAxis axis, double target, string description, CancellationToken ct, bool forceMove)
        {
            try
            {
                int result = await MoveAxisCommandAsync(axis, target, description, ct, forceMove).ConfigureAwait(false);
                if (result != 0) return result;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-MOVE-VERIFY-EX", Stage.Name, description + " move verify failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisCommandAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            return await MoveAxisCommandAsync(axis, target, description, ct, false).ConfigureAwait(false);
        }

        private async Task<int> MoveAxisCommandAsync(WaferStageAxis axis, double target, string description, CancellationToken ct, bool forceMove)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string guardReason;
                if (!VerifySharedRailAxisMove(axis, target, out guardReason))
                    return Fail("IN-STAGE-ALIGN-SHARED-RAIL", Stage.Name,
                        description + " shared rail check failed. axis=" + axis + ", target=" + target + ". " + guardReason);

                QMC.Common.Motion.BaseAxis stageAxis = ResolveStageAxis(axis);
                string targetName = "InputStageAlign;Axis=" + axis + ";" + description;
                int result;
                using (MotionGuardRuntime.BeginAxisTeachingMove(stageAxis, target, targetName))
                {
                    result = await AwaitStepWithCancellationAsync(Stage.MoveInputStageAxis(axis, target, Options.FineMove, forceMove), ct).ConfigureAwait(false);
                }
                if (result != 0)
                    return Fail("IN-STAGE-ALIGN-MOVE", Stage.Name,
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
                return Fail("IN-STAGE-ALIGN-MOVE-EX", Stage.Name, description + " move command exception: " + ex.Message);
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

        private string FormatLastStageMoveFailure()
        {
            if (Stage == null || string.IsNullOrWhiteSpace(Stage.LastStageMoveFailureMessage))
                return string.Empty;

            return ", lastStageMoveFailure=" + Stage.LastStageMoveFailureMessage;
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
                    return Fail("IN-STAGE-ALIGN-MOVE", Stage.Name,
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
                return Fail("IN-STAGE-ALIGN-WAIT-EX", Stage.Name, description + " move wait exception: " + ex.Message);
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
                    return Fail("IN-STAGE-ALIGN-AXIS", Stage.Name,
                        description + " axis is not available. " + BuildAxisState(axis, target));

                if (item.IsMoving || item.IsAlarm || !IsAxisInPosition(item, target))
                    return Fail("IN-STAGE-ALIGN-POSITION", Stage.Name,
                        description + " final position check failed. axis=" + axis + ", target=" + target +
                        ". " + BuildAxisState(axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-ALIGN-POSITION-EX", Stage.Name, description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
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

        private string BuildAlignServoReason()
        {
            string reason = string.Empty;
            AppendServoOff(ref reason, WaferStageAxis.WaferY, Stage.StageY);
            AppendServoOff(ref reason, WaferStageAxis.WaferT, Stage.StageT);
            AppendServoOff(ref reason, WaferStageAxis.WaferExpandingZ, Stage.ExpanderZ);
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

        private static string ResolveTargetId(string value, string fallback)
        {
            try
            {
                return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            }
            catch
            {
                return fallback;
            }
            finally
            {
            }
        }

        private double ResolveThetaTolerance()
        {
            try
            {
                double configured = Options.AlignThetaToleranceDeg > 0.0
                    ? Options.AlignThetaToleranceDeg
                    : (Stage.Config != null ? Stage.Config.AlignConvergenceThresholdDeg : 0.005);
                if (configured <= 0.0)
                    configured = 0.005;

                return Math.Min(configured, MaxEffectiveThetaToleranceDeg);
            }
            catch
            {
                return 0.005;
            }
            finally
            {
            }
        }

        private double ResolveThetaCorrectionLimit()
        {
            try
            {
                return Options.AlignThetaCorrectionLimitDeg > 0.0
                    ? Options.AlignThetaCorrectionLimitDeg
                    : (Stage.Config != null ? Stage.Config.AlignThetaCorrectionLimitDeg : 1.0);
            }
            catch
            {
                return 1.0;
            }
            finally
            {
            }
        }

        private int ResolveTimeout()
        {
            try
            {
                return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
            }
            catch
            {
                return 10000;
            }
            finally
            {
            }
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
    }
}
