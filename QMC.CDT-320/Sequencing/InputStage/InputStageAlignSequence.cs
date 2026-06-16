using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;
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
        ApplyAlignResult,
        Complete,
        Error
    }

    internal sealed class InputStageAlignSequence : InputStageSequenceBase<InputStageAlignStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private WaferMapData _map;
        private WaferMaterial _wafer;
        private TapeFrameSpec _frameSpec;
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
        private bool _alignAnchorReady;
        private int _alignAnchorRow;
        private int _alignAnchorCol;
        private double _alignAnchorX;
        private double _alignAnchorY;
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
                        return Task.FromResult(CheckAlignUnit());
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
                        return Task.FromResult(CalculateAlignResult());
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

        private int CheckAlignUnit()
        {
            try
            {
                ResetAlignRuntimeState();

                int result = CheckUnit(InputStageAlignStep.MoveVisionProcessPosition);
                if (result != 0)
                    return result;

                if (Stage.Recipe == null)
                    return Fail("IN-STAGE-ALIGN-RECIPE", Stage.Name, "Input stage recipe is not available.");

                string servoReason = BuildAlignServoReason();
                if (!string.IsNullOrEmpty(servoReason))
                    return Fail("IN-STAGE-ALIGN-SERVO", Stage.Name, "Input stage servo is not on. " + servoReason);

                _wafer = Stage.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (_wafer == null)
                    return Fail("IN-STAGE-ALIGN-WAFER", "Material",
                        "InputStage wafer data was not found. CurrentWaferMaterial=null, MaterialLocation=InputStage empty.");

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
            _alignAnchorReady = false;
            _alignAnchorRow = 0;
            _alignAnchorCol = 0;
            _alignAnchorX = 0.0;
            _alignAnchorY = 0.0;
            ClearPendingVisionRequest();
        }

        private async Task<int> MoveVisionProcessPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Options.EnableMotion)
                {
                    int result = await MoveAxisAndVerifyAsync(WaferStageAxis.WaferY, Stage.Recipe.WaferY.ProcessPosition, "StageY process", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await MoveAxisAndVerifyAsync(WaferStageAxis.VisionX, Stage.Recipe.VisionX.ProcessPosition, "VisionX process", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await MoveAxisAndVerifyAsync(WaferStageAxis.WaferExpandingZ, Stage.Recipe.WaferZ.ProcessPosition, "StageZ process", ct).ConfigureAwait(false);
                    if (result != 0) return result;
                }

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
                StartVisionMarkRequest(ResolveTargetId(Options.CenterAlignTargetId, "Center"), "Center");
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
                if (_centerResult == null)
                    return Fail("IN-STAGE-ALIGN-CENTER", "Vision", "Center vision offset receive failed.");

                CaptureAlignAnchorFromVisionResult(_centerResult, "Center");
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

                double deltaTheta = _centerResult != null ? _centerResult.DeltaTheta : 0.0;
                double targetT = Stage.StageT.ActualPosition + deltaTheta;
                int result = await MoveAxisAndVerifyAsync(WaferStageAxis.WaferT, targetT, "StageT theta correction", ct).ConfigureAwait(false);
                if (result != 0) return result;

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
                StartVisionMarkRequest(ResolveTargetId(Options.CenterAlignTargetId, "Center"), "CenterVerify");
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
                if (_verifyCenterResult == null)
                    return Fail("IN-STAGE-ALIGN-THETA-VERIFY", "Vision", "Center verify vision offset receive failed.");

                CaptureAlignAnchorFromVisionResult(_verifyCenterResult, "CenterVerify");
                double theta = Math.Abs(_verifyCenterResult.DeltaTheta);
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
                StartVisionMarkRequest(ResolveTargetId(Options.Ref1AlignTargetId, "Ref1"), "Ref1");
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

                _ref1X = Stage.CameraX.ActualPosition + _ref1Result.DeltaX;
                _ref1Y = Stage.StageY.ActualPosition + _ref1Result.DeltaY;
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
                StartVisionMarkRequest(ResolveTargetId(Options.Ref2AlignTargetId, "Ref2"), "Ref2");
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

                _ref2X = Stage.CameraX.ActualPosition + _ref2Result.DeltaX;
                _ref2Y = Stage.StageY.ActualPosition + _ref2Result.DeltaY;
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

        private int CalculateAlignResult()
        {
            try
            {
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

                if (Math.Abs(_pitchX) <= 1e-9 || Math.Abs(_pitchY) <= 1e-9)
                    return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-PITCH", Stage.Name,
                        "Calculated align pitch is invalid. pitchX=" + _pitchX + ", pitchY=" + _pitchY);

                _originX = _ref1X - (_map.Ref1Col * _pitchX);
                _originY = _ref1Y - (_map.Ref1Row * _pitchY);

                _thetaFromTwoPoint = Math.Atan2(_ref2Y - _ref1Y, _ref2X - _ref1X) * 180.0 / Math.PI;
                if (Math.Abs(_thetaFromTwoPoint) > ResolveThetaTolerance())
                    return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-REF-THETA", Stage.Name,
                        "Two point theta is out of tolerance. theta=" + _thetaFromTwoPoint.ToString("F6") +
                        ", tolerance=" + ResolveThetaTolerance().ToString("F6"));

                CurrentStep = InputStageAlignStep.ApplyAlignResult;
                return 0;
            }
            catch (Exception ex)
            {
                return FailAndResetAlignRuntimeState("IN-STAGE-ALIGN-CALC-EX", Stage.Name, "Align result calculation failed: " + ex.Message);
            }
            finally
            {
            }
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
                Stage.ApplyWaferAlignResult(_originX, _originY, _pitchX, _pitchY, 0.0, 0.0);

                WaferMaterial wafer = Stage.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer != null)
                {
                    wafer.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    MaterialStateService.SaveInputStageAlignResult(wafer, _originX, _originY, _pitchX, _pitchY, 0.0, 0.0);
                }

                Context.Bus.Set("InputStageAligned");
                Context.Bus.Set("InputStageReady");
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
            if (task == null)
                return null;

            if (task.IsCompleted)
                return await task.ConfigureAwait(false);

            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(task, cancelTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, task))
                ct.ThrowIfCancellationRequested();

            return await task.ConfigureAwait(false);
        }

        private async Task<VisionAlignResult> RequestVisionPcOffsetWithRetryAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                int retryCount = Math.Max(3, Options.AlignRetryCount);
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
                WriteLog("InputStageAlignSequence", "Vision PC offset retry exception. step=" + stepName + ": " + ex.Message + " - Failed");
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
                    return await RequestSimVisionOffsetAsync(targetId, stepName, ct).ConfigureAwait(false);

                if (Stage.Vision == null)
                    return null;

                Task<VisionAlignResult> alignTask = Stage.Vision.TriggerAlignAsync(targetId);
                if (alignTask == null)
                    return null;

                if (alignTask.IsCompleted)
                    return await alignTask.ConfigureAwait(false);

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task completed = await Task.WhenAny(alignTask, cancelTask).ConfigureAwait(false);
                if (!ReferenceEquals(completed, alignTask))
                    ct.ThrowIfCancellationRequested();

                return await alignTask.ConfigureAwait(false);
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

        private async Task<VisionAlignResult> RequestSimVisionOffsetAsync(string targetId, string stepName, CancellationToken ct)
        {
            try
            {
                await Task.Delay(120, ct).ConfigureAwait(false);

                double dx;
                double dy;
                double dt;
                lock (SimVisionRandomLock)
                {
                    bool referenceMark =
                        string.Equals(stepName, "Ref1", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(stepName, "Ref2", StringComparison.OrdinalIgnoreCase);
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

        private double ResolveSimThetaOffset(string stepName)
        {
            try
            {
                double tolerance = ResolveThetaTolerance();
                if (string.Equals(stepName, "CenterVerify", StringComparison.OrdinalIgnoreCase))
                    return 0.0;

                if (string.Equals(stepName, "Center", StringComparison.OrdinalIgnoreCase))
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
                string specName = wafer != null ? wafer.TapeFrameSpecName : "";
                if (string.IsNullOrWhiteSpace(specName))
                {
                    specName = MaterialStateService.ResolveRecipeTapeFrameSpecName(0);
                    if (wafer != null && !string.IsNullOrWhiteSpace(specName))
                    {
                        wafer.TapeFrameSpecName = specName;
                        MaterialStateService.NotifyAndSave("InputStageAlignSpecResolve");
                    }
                }

                var spec = MaterialSpecs.FindFrame(specName);
                if (spec != null)
                    return spec;

                specName = MaterialStateService.ResolveRecipeTapeFrameSpecName(0);
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

                bool fallbackMap = map.RowCount <= 1 && map.ColumnCount <= 1;
                if (fallbackMap)
                {
                    map.RowCount = Math.Max(1, spec.GridY);
                    map.ColumnCount = Math.Max(1, spec.GridX);
                    map.DieMap = new bool[map.RowCount, map.ColumnCount];
                    for (int row = 0; row < map.RowCount; row++)
                    {
                        for (int col = 0; col < map.ColumnCount; col++)
                            map.DieMap[row, col] = true;
                    }
                }

                bool invalidRefPair = map.Ref1Row == map.Ref2Row && map.Ref1Col == map.Ref2Col;
                if (fallbackMap || invalidRefPair)
                {
                    int centerRow = map.RowCount / 2;
                    int leftCol = map.ColumnCount > 1 ? Math.Max(0, map.ColumnCount / 4) : 0;
                    int rightCol = map.ColumnCount > 1 ? Math.Min(map.ColumnCount - 1, (map.ColumnCount * 3) / 4) : 0;
                    if (rightCol == leftCol && map.ColumnCount > 1)
                        rightCol = map.ColumnCount - 1;

                    map.Ref1Row = centerRow;
                    map.Ref1Col = leftCol;
                    map.Ref2Row = centerRow;
                    map.Ref2Col = rightCol;
                }

                WriteLog("InputStageAlignSequence",
                    "Frame spec applied to align map. spec=" + spec.Name +
                    ", gridX=" + spec.GridX +
                    ", gridY=" + spec.GridY +
                    ", pitchX=" + spec.PitchX.ToString("F6") +
                    ", pitchY=" + spec.PitchY.ToString("F6") + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Frame spec apply to map failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private double ResolveAlignPitchX(VisionAlignResult ref1Result, VisionAlignResult ref2Result)
        {
            try
            {
                if (ref2Result != null && ref2Result.PitchX > 0.0)
                    return ref2Result.PitchX;
                if (ref1Result != null && ref1Result.PitchX > 0.0)
                    return ref1Result.PitchX;
                if (_frameSpec != null && _frameSpec.PitchX > 0.0)
                    return _frameSpec.PitchX;
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
                if (ref2Result != null && ref2Result.PitchY > 0.0)
                    return ref2Result.PitchY;
                if (ref1Result != null && ref1Result.PitchY > 0.0)
                    return ref1Result.PitchY;
                if (_frameSpec != null && _frameSpec.PitchY > 0.0)
                    return _frameSpec.PitchY;
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

        private bool IsSimulationOrDryRun()
        {
            try
            {
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

            _alignAnchorX = (Stage.CameraX != null ? Stage.CameraX.ActualPosition : 0.0) + result.DeltaX;
            _alignAnchorY = (Stage.StageY != null ? Stage.StageY.ActualPosition : 0.0) + result.DeltaY;

            WriteLog("InputStageAlignSequence",
                "Align anchor updated from vision result. description=" + description +
                ", row=" + _alignAnchorRow +
                ", col=" + _alignAnchorCol +
                ", anchorX=" + _alignAnchorX.ToString("F6") +
                ", anchorY=" + _alignAnchorY.ToString("F6") +
                ", dx=" + result.DeltaX.ToString("F6") +
                ", dy=" + result.DeltaY.ToString("F6") + " - Ok");
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

                Task<int> moveY = MoveAxisCommandAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct);
                Task<int> moveX = MoveAxisCommandAsync(WaferStageAxis.VisionX, targetX, description + " VisionX", ct);
                int[] moveResults = await Task.WhenAll(moveY, moveX).ConfigureAwait(false);
                if (moveResults[0] != 0) return moveResults[0];
                if (moveResults[1] != 0) return moveResults[1];

                Task<int> waitY = WaitAxisInPositionResultAsync(WaferStageAxis.WaferY, targetY, description + " StageY", ct);
                Task<int> waitX = WaitAxisInPositionResultAsync(WaferStageAxis.VisionX, targetX, description + " VisionX", ct);
                int[] waitResults = await Task.WhenAll(waitY, waitX).ConfigureAwait(false);
                if (waitResults[0] != 0) return waitResults[0];
                if (waitResults[1] != 0) return waitResults[1];

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

        private WaferMapData ResolveWaferMapForAlign(string waferId)
        {
            try
            {
                DieMap sourceMap = ResolveSourceInputDieMap(_wafer);
                if (IsUsableSourceMap(sourceMap))
                    return ConvertDieMapToWaferMap(sourceMap, waferId);

                return Stage.EnsureWaferMapForAlign(waferId, Options.AllowFallbackMap);
            }
            catch (Exception ex)
            {
                WriteLog("InputStageAlignSequence", "Align wafer map resolve failed: " + ex.Message + " - Failed");
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
                DieMap activeMap = LotStorage.ActiveInputDieMap;
                if (IsUsableSourceMap(activeMap))
                    return activeMap;

                DieMap materialMap = MaterialStateService.BuildDieMapFromWafer(wafer);
                if (IsUsableSourceMap(materialMap))
                    return materialMap;

                DieMap recipeMap = LoadRecipeInputDieMap();
                if (IsUsableSourceMap(recipeMap))
                    return recipeMap;

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
                if (project == null || string.IsNullOrWhiteSpace(project.InputDieMapFileName))
                    return null;

                string path = project.InputDieMapFileName;
                if (!System.IO.Path.IsPathRooted(path))
                    path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

                DieMap map = DieMapGenerator.Load(path);
                if (map != null)
                    WriteLog("InputStageAlignSequence", "Recipe input die map loaded for align. path=" + path + " - Ok");
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

        private static bool IsUsableSourceMap(DieMap map)
        {
            try
            {
                return map != null &&
                       map.GridX > 0 &&
                       map.GridY > 0 &&
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

            int gridX = Math.Max(1, map.GridX);
            int gridY = Math.Max(1, map.GridY);
            var waferMap = new WaferMapData
            {
                WaferId = string.IsNullOrWhiteSpace(waferId) ? map.FrameObjId : waferId,
                ColumnCount = gridX,
                RowCount = gridY,
                DieMap = new bool[gridY, gridX]
            };

            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null ||
                    entry.GridX < 0 || entry.GridX >= gridX ||
                    entry.GridY < 0 || entry.GridY >= gridY)
                    continue;

                waferMap.DieMap[entry.GridY, entry.GridX] = entry.IsTarget;
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
            try
            {
                int result = await MoveAxisCommandAsync(axis, target, description, ct).ConfigureAwait(false);
                if (result != 0) return result;

                return await WaitAxisInPositionResultAsync(axis, target, description, ct).ConfigureAwait(false);
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
            try
            {
                ct.ThrowIfCancellationRequested();
                string guardReason;
                if (!VerifySharedRailAxisMove(axis, target, out guardReason))
                    return Fail("IN-STAGE-ALIGN-SHARED-RAIL", Stage.Name,
                        description + " shared rail check failed. axis=" + axis + ", target=" + target + ". " + guardReason);

                int result = await AwaitStepWithCancellationAsync(Stage.MoveInputStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-ALIGN-MOVE", Stage.Name,
                        description + " move command failed. axis=" + axis + ", target=" + target +
                        ", result=" + result + ". " + BuildAxisState(axis, target));

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

        private async Task<int> WaitAxisInPositionResultAsync(WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                AxisMoveWaitResult waitResult = await AwaitStepWithCancellationAsync(
                    Stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout()),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("IN-STAGE-ALIGN-MOVE", waitResult), Stage.Name,
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
                return Options.AlignThetaToleranceDeg > 0.0
                    ? Options.AlignThetaToleranceDeg
                    : (Stage.Config != null ? Stage.Config.AlignConvergenceThresholdDeg : 0.005);
            }
            catch
            {
                return 0.005;
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
                if (stepTask == null)
                    return -1;

                if (stepTask.IsCompleted)
                    return await stepTask.ConfigureAwait(false);

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
                if (!ReferenceEquals(completed, stepTask))
                    ct.ThrowIfCancellationRequested();

                return await stepTask.ConfigureAwait(false);
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
                if (stepTask == null)
                    return null;

                if (stepTask.IsCompleted)
                    return await stepTask.ConfigureAwait(false);

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
                if (!ReferenceEquals(completed, stepTask))
                    ct.ThrowIfCancellationRequested();

                return await stepTask.ConfigureAwait(false);
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
    }
}

