using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Motion;

namespace QMC.CDT_320
{
    public partial class Form1
    {
        private long _inputStageRunReviewRequestGeneration;
        private string _inputStageRunReviewGeometryToken = string.Empty;
        private InputStageReviewGeometryContext _inputStageRunReviewVerifiedContext;
        private InputStageReviewGeometryContext _inputStageRunReviewPendingGeometryContext;
        private double _inputStageRunReviewPendingCaptureX;
        private double _inputStageRunReviewPendingCaptureY;
        private double _inputStageRunReviewPendingCaptureT;
        private bool _inputStageRunReviewGeometryVerificationRunning;

        private static InputStageReviewOffsetLimits GetInputStageReviewOffsetLimits(InputStageUnit stage)
        {
            if (stage == null || stage.Config == null)
                return null;
            return new InputStageReviewOffsetLimits
            {
                SingleX = stage.Config.ManualDieDetectOffsetLimitX,
                SingleY = stage.Config.ManualDieDetectOffsetLimitY,
                CumulativeX = stage.Config.ManualDieDetectCumulativeOffsetLimitX,
                CumulativeY = stage.Config.ManualDieDetectCumulativeOffsetLimitY
            };
        }

        private bool TryCaptureInputStageReviewContext(
            InputStageRunReviewDialog dialog, bool allowBaselineEstablishment,
            out InputStageReviewGeometryContext context, out string reason)
        {
            context = null;
            reason = string.Empty;
            if (dialog == null || dialog.IsDisposed ||
                !ReferenceEquals(dialog, _inputStageRunReviewDialog) ||
                _inputStageRunReviewCleanedGeneration >= _inputStageRunReviewSessionGeneration ||
                Controller == null || !Controller.IsInputStageRunReviewManualActive || Controller.DryRun || Controller.GlobalDryRun)
            {
                reason = "실제 운전 Review 세션이 필요합니다. 세션 종료·변경 또는 DryRun 상태를 확인하세요.";
                return false;
            }
            try
            {
                EnsureInputStageReviewAxesStationary(Machine != null ? Machine.InputStageUnit : null);
                return MaterialStateService.TryCaptureInputStageReviewGeometryContext(
                    dialog.WaferId, dialog.DraftDieMap,
                    _inputStageRunReviewSessionGeneration, _inputStageRunReviewRequestGeneration,
                    allowBaselineEstablishment, out context, out reason);
            }
            catch (InvalidOperationException ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        private bool IsInputStageReviewRequestCurrent(
            InputStageRunReviewDialog dialog, InputStageReviewGeometryContext captured, DieMap draft)
        {
            if (captured == null || dialog == null || dialog.IsDisposed ||
                !ReferenceEquals(dialog, _inputStageRunReviewDialog) ||
                !ReferenceEquals(dialog.DraftDieMap, draft) ||
                captured.SessionGeneration != _inputStageRunReviewSessionGeneration ||
                captured.RequestGeneration != _inputStageRunReviewRequestGeneration ||
                _inputStageRunReviewCleanedGeneration >= _inputStageRunReviewSessionGeneration ||
                Controller == null || !Controller.IsInputStageRunReviewManualActive || Controller.DryRun || Controller.GlobalDryRun ||
                Controller.InputStageRunReviewActionToken.IsCancellationRequested)
                return false;
            InputStageReviewGeometryContext current;
            string reason;
            return MaterialStateService.TryCaptureInputStageReviewGeometryContext(
                       captured.WaferId, draft, captured.SessionGeneration, captured.RequestGeneration,
                       true, out current, out reason) &&
                   InputStageReviewGeometryPolicy.IsSameContext(captured, current, out reason);
        }

        private void EnsureInputStageReviewRequestCurrent(
            InputStageRunReviewDialog dialog, InputStageReviewGeometryContext captured, DieMap draft)
        {
            if (!IsInputStageReviewRequestCurrent(dialog, captured, draft))
                throw new InvalidOperationException("검출/검증 중 세션, T, 맵 또는 레시피·설정 조건이 변경되어 결과를 폐기했습니다.");
        }

        private static void EnsureInputStageReviewAxesStationary(InputStageUnit stage)
        {
            if (stage == null || stage.CameraX == null || stage.StageY == null || stage.StageT == null)
                throw new InvalidOperationException("Review 검증에 필요한 X/Y/T 축 정보가 없습니다.");
            foreach (BaseAxis axis in new BaseAxis[] { stage.CameraX, stage.StageY, stage.StageT })
            {
                if (axis.IsMoving || !axis.IsInPosition ||
                    double.IsNaN(axis.ActualPosition) || double.IsInfinity(axis.ActualPosition))
                    throw new InvalidOperationException("X/Y/T 정지 및 InPosition 확인 후 다시 검출/검증하세요.");
            }
        }

        private static double GetInputStageReviewAxisTolerance(BaseAxis axis)
        {
            double configured = axis != null && axis.Config != null ? axis.Config.InPositionTolerance : double.NaN;
            if (double.IsNaN(configured) || double.IsInfinity(configured) || configured < 0.0)
                throw new InvalidOperationException("축 InPosition 허용치가 유효하지 않습니다.");
            return Math.Max(0.000000001, configured);
        }

        private static void EnsureInputStageReviewCaptureUnchanged(
            InputStageUnit stage, double x, double y, double t)
        {
            EnsureInputStageReviewAxesStationary(stage);
            if (Math.Abs(stage.CameraX.ActualPosition - x) > GetInputStageReviewAxisTolerance(stage.CameraX) ||
                Math.Abs(stage.StageY.ActualPosition - y) > GetInputStageReviewAxisTolerance(stage.StageY) ||
                Math.Abs(stage.StageT.ActualPosition - t) > GetInputStageReviewAxisTolerance(stage.StageT))
                throw new InvalidOperationException("검출 이후 실제 X/Y/T 위치가 변경되었습니다. 다시 검출하세요.");
        }

        private bool CanApplyInputStageReviewPendingOffset(InputStageRunReviewDialog dialog, out string reason)
        {
            reason = string.Empty;
            if (!_inputStageRunReviewOffsetPending || _inputStageRunReviewPendingGeometryContext == null ||
                dialog == null || dialog.SelectedDie == null ||
                !string.Equals(dialog.SelectedDie.DieUid, _inputStageRunReviewPendingOffsetDieUid, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(BuildInputStageRunReviewDraftSignature(dialog),
                    _inputStageRunReviewPendingOffsetDraftSignature, StringComparison.Ordinal) ||
                !IsInputStageReviewRequestCurrent(dialog, _inputStageRunReviewPendingGeometryContext, dialog.DraftDieMap))
            {
                reason = "검출 기준 Die, 후보 맵 또는 장비 조건이 변경되었습니다. 다시 DIE DETECTION을 실행하세요.";
                return false;
            }
            try
            {
                InputStageUnit stage = Machine.InputStageUnit;
                EnsureInputStageReviewCaptureUnchanged(stage,
                    _inputStageRunReviewPendingCaptureX, _inputStageRunReviewPendingCaptureY,
                    _inputStageRunReviewPendingCaptureT);
                InputStageReviewOffsetCandidate candidate;
                return InputStageReviewGeometryPolicy.TryCreateOffsetCandidate(
                    _inputStageRunReviewPendingGeometryContext,
                    _inputStageRunReviewPendingOffsetX, _inputStageRunReviewPendingOffsetY,
                    GetInputStageReviewOffsetLimits(stage), out candidate, out reason);
            }
            catch (InvalidOperationException ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        private bool CanSubmitInputStageReviewGeometry(InputStageRunReviewDialog dialog, out string reason)
        {
            if (dialog == null || dialog.IsDisposed || !ReferenceEquals(dialog, _inputStageRunReviewDialog) ||
                _inputStageRunReviewCleanedGeneration >= _inputStageRunReviewSessionGeneration ||
                Controller == null || !Controller.IsInputStageRunReviewManualActive)
            {
                reason = "현재 활성 Review 세션에서만 확인할 수 있습니다.";
                return false;
            }
            bool nonProduction = MaterialStateService.IsInputStageReviewNonProductionMode(out reason);
            dialog.SetNonproductionReviewMode(nonProduction);
            if (nonProduction)
            {
                InputStageReviewGeometryContext modeContext;
                string modeToken;
                if (!MaterialStateService.TryRegisterInputStageReviewNonProductionApproval(
                    dialog.WaferId, dialog.DraftDieMap, _inputStageRunReviewSessionGeneration,
                    _inputStageRunReviewRequestGeneration, out modeContext, out modeToken, out reason))
                    return false;
                // 기존 비생산 수동 확인은 유지하되 실장비 다점 실측 증거와 구분하여 저장한다.
                _inputStageRunReviewVerifiedContext = modeContext;
                _inputStageRunReviewGeometryToken = modeToken;
                LogInputStageReviewGeometry("NONPRODUCTION-MANUAL", modeContext, reason);
                return true;
            }
            InputStageReviewGeometryContext current;
            if (!TryCaptureInputStageReviewContext(dialog, false, out current, out reason))
                return false;
            if (string.IsNullOrWhiteSpace(_inputStageRunReviewGeometryToken) ||
                _inputStageRunReviewVerifiedContext == null ||
                !InputStageReviewGeometryPolicy.IsSameContext(_inputStageRunReviewVerifiedContext, current, out reason))
            {
                reason = "현재 조건의 좌표 검증이 필요합니다. VERIFY MAP을 실행하세요. " + reason;
                dialog.SetGeometryVerified(false, reason);
                return false;
            }
            return true;
        }

        private async Task VerifyInputStageRunReviewGeometryAsync(InputStageRunReviewDialog dialog)
        {
            if (_inputStageRunReviewGeometryVerificationRunning || dialog == null || dialog.IsDisposed)
                return;
            List<DieMapEntry> selected = dialog.SelectedDies.Where(entry => entry != null).ToList();
            if (selected.Count != 3 || selected.Any(entry => !entry.IsTarget))
            {
                dialog.SetStatusMessage("하단 표에서 Ctrl 선택으로 X/Y 양쪽으로 떨어진 실제 기준 Die 3개를 선택하세요. 같은 직선의 점은 검증할 수 없습니다.");
                return;
            }
            string layoutReason;
            var layout = selected.Select(entry => new InputStageReviewMeasurement
            {
                GridX = entry.DieMapX, GridY = entry.DieMapY,
                ExpectedX = entry.PosX, ExpectedY = entry.PosY
            }).ToList();
            if (!InputStageReviewGeometryPolicy.CheckVerificationPointLayout(layout, out layoutReason))
            {
                dialog.SetStatusMessage(layoutReason);
                return;
            }
            ClearInputStageRunReviewPendingOffset();
            InputStageReviewGeometryContext context;
            string reason;
            if (!TryCaptureInputStageReviewContext(dialog, true, out context, out reason))
            {
                dialog.SetStatusMessage(reason);
                return;
            }
            if (context.IsSimulation)
            {
                dialog.SetStatusMessage("비전 미사용/시뮬레이션은 실제 좌표 검증을 승인하지 않습니다.");
                return;
            }
            foreach (DieMapEntry entry in selected)
            {
                if (!MaterialStateService.CheckInputStageReviewVerificationDie(
                    context.SessionGeneration, context.RequestGeneration, context.WaferId,
                    context.MappingRevision, entry.DieUid, out reason))
                {
                    dialog.SetStatusMessage(reason);
                    return;
                }
            }
            DieMap draft = dialog.DraftDieMap;
            string draftSignature = BuildInputStageRunReviewDraftSignature(dialog);
            var samples = new List<InputStageReviewMeasurement>();
            bool complete = false;
            bool livePaused = dialog.PauseWaferVisionLiveForAction();
            _inputStageRunReviewGeometryVerificationRunning = true;
            try
            {
                await RunInputStageReviewOneShotAsync(dialog, "Map Geometry Verification", async (stage, token) =>
                {
                    foreach (DieMapEntry entry in selected)
                    {
                        token.ThrowIfCancellationRequested();
                        EnsureInputStageReviewRequestCurrent(dialog, context, draft);
                        EnsureInputStageReviewVerificationDieAvailable(context, entry);
                        // 기존 Review 안전 이동 경로와 Manual Sequence 속도를 그대로 사용한다.
                        // 검증점마다 중심으로 재이동하지 않고 후보 좌표에서 얻은 잔차를 보존한다.
                        int moveResult = await stage.MoveVisionPointSafelyAtReadySequenceSpeedAsync(
                            entry.PosX, entry.PosY, "InputStageRunReview.GeometryVerification").ConfigureAwait(true);
                        if (moveResult != 0)
                            throw new InvalidOperationException("검증점 안전 이동 실패. result=" + moveResult);
                        token.ThrowIfCancellationRequested();
                        EnsureInputStageReviewAxesStationary(stage);
                        EnsureInputStageReviewRequestCurrent(dialog, context, draft);
                        double x = stage.CameraX.ActualPosition;
                        double y = stage.StageY.ActualPosition;
                        double t = stage.StageT.ActualPosition;
                        VisionAlignResult vision = await RequestInputStageRunReviewDieVisionAsync(
                            stage, entry, x, y, token).ConfigureAwait(true);
                        token.ThrowIfCancellationRequested();
                        EnsureInputStageReviewRequestCurrent(dialog, context, draft);
                        EnsureInputStageReviewCaptureUnchanged(stage, x, y, t);
                        if (vision == null || double.IsNaN(vision.DeltaX) || double.IsInfinity(vision.DeltaX) ||
                            double.IsNaN(vision.DeltaY) || double.IsInfinity(vision.DeltaY))
                            throw new InvalidOperationException("검증점 Vision 실측이 실패했습니다. die=" + entry.DieUid);

                        // 실측과 동일한 정지 좌표에서 별도 EXPOSE 영상을 받아 현재 검증점임을 표시한다.
                        // 이 영상은 Match 요청 ID와 연결된 영상이라고 주장하지 않는다.
                        string frameReceipt = await dialog.ShowReviewVerificationFrameAsync(token).ConfigureAwait(true);
                        token.ThrowIfCancellationRequested();
                        EnsureInputStageReviewRequestCurrent(dialog, context, draft);
                        EnsureInputStageReviewCaptureUnchanged(stage, x, y, t);
                        string point = "UID=" + entry.DieUid + ", Grid=(" + entry.DieMapX + "," + entry.DieMapY + ")";
                        var answer = await dialog.ShowReviewCorrespondenceConfirmationAsync(
                            "기준 Die 대응 확인", "검증 " + (samples.Count + 1) + "/3: " + point + "\r\n" +
                            "현재 실제 다이가 이 Map index의 다이임을 확인했습니까?\r\n" +
                            "반복 패턴이 검출되었다는 사실만으로는 index 일치를 확인할 수 없습니다.\r\n" +
                            "식별 근거나 최신 영상을 확인할 수 없으면 아니요를 선택하세요.\r\n" +
                            "예상 X/Y=" + entry.PosX.ToString("F6") + "/" + entry.PosY.ToString("F6") +
                            ", 잔차 X/Y=" + (x + vision.DeltaX - entry.PosX).ToString("F6") + "/" +
                            (y - vision.DeltaY - entry.PosY).ToString("F6"),
                            token).ConfigureAwait(true);
                        token.ThrowIfCancellationRequested();
                        EnsureInputStageReviewRequestCurrent(dialog, context, draft);
                        EnsureInputStageReviewCaptureUnchanged(stage, x, y, t);
                        if (answer != System.Windows.Forms.DialogResult.Yes)
                            throw new InvalidOperationException("기준 Die와 실제 Map index의 대응이 확인되지 않았습니다. Retry Align/Mapping으로 기준을 다시 설정하세요.");
                        EnsureInputStageReviewVerificationDieAvailable(context, entry);
                        var sample = new InputStageReviewMeasurement
                        {
                            DieUid = entry.DieUid, GridX = entry.DieMapX, GridY = entry.DieMapY,
                            ExpectedX = entry.PosX, ExpectedY = entry.PosY,
                            CaptureX = x, CaptureY = y, CaptureT = t,
                            RawVisionDeltaX = vision.DeltaX, RawVisionDeltaY = vision.DeltaY,
                            CorrespondenceConfirmed = true,
                            CorrespondenceEvidence = "Operator=" + UserSession.Name + "; " + point +
                                "; Frame=" + frameReceipt + "; UTC=" + DateTime.UtcNow.ToString("O")
                        };
                        samples.Add(sample);
                        LogInputStageReviewGeometry("SAMPLE", context,
                            point + ", capture=" + x.ToString("F6") + "/" + y.ToString("F6") + "/" + t.ToString("F6") +
                            ", raw=" + vision.DeltaX.ToString("F6") + "/" + vision.DeltaY.ToString("F6"));
                    }
                    token.ThrowIfCancellationRequested();
                    EnsureInputStageReviewRequestCurrent(dialog, context, draft);
                    complete = true;
                    return "3개 기준 Die 실측을 완료했습니다. 좌표 잔차와 T/pitch 일관성을 검사합니다.";
                }, true).ConfigureAwait(true);

                if (!complete || !IsInputStageReviewRequestCurrent(dialog, context, draft) ||
                    !string.Equals(draftSignature, BuildInputStageRunReviewDraftSignature(dialog), StringComparison.Ordinal))
                    return;
                string verificationToken;
                if (!MaterialStateService.TryRegisterInputStageReviewVerification(
                    context, draft, samples, out verificationToken, out reason))
                {
                    dialog.SetGeometryVerified(false, "좌표 검증 실패: " + reason);
                    LogInputStageReviewGeometry("REJECT", context, reason);
                    return;
                }
                // 구형 맵의 최초 검증은 기준 원점이 정립될 수 있으므로 확정된 조건을 다시 읽는다.
                InputStageReviewGeometryContext verified;
                if (!TryCaptureInputStageReviewContext(dialog, false, out verified, out reason))
                {
                    ClearInputStageRunReviewPendingOffset();
                    dialog.SetStatusMessage(reason);
                    return;
                }
                _inputStageRunReviewVerifiedContext = verified;
                _inputStageRunReviewGeometryToken = verificationToken;
                dialog.SetGeometryVerified(true, "3개 기준 Die의 좌표/T/pitch 검증을 통과했습니다. CONFIRM / CONTINUE AUTO로 확정하세요.");
                LogInputStageReviewGeometry("VERIFIED", verified, "samples=" + samples.Count);
            }
            catch (Exception ex)
            {
                if (ReferenceEquals(dialog, _inputStageRunReviewDialog) &&
                    context.SessionGeneration == _inputStageRunReviewSessionGeneration &&
                    context.RequestGeneration == _inputStageRunReviewRequestGeneration)
                {
                    ClearInputStageRunReviewPendingOffset();
                    if (!dialog.IsDisposed)
                        dialog.SetGeometryVerified(false, "좌표 검증 실패: " + ex.Message);
                }
                LogInputStageReviewGeometry("FAILED", context, ex.ToString());
            }
            finally
            {
                _inputStageRunReviewGeometryVerificationRunning = false;
                if (livePaused && !dialog.IsDisposed && ReferenceEquals(dialog, _inputStageRunReviewDialog) &&
                    context.SessionGeneration == _inputStageRunReviewSessionGeneration &&
                    context.RequestGeneration == _inputStageRunReviewRequestGeneration &&
                    _inputStageRunReviewCleanedGeneration < _inputStageRunReviewSessionGeneration &&
                    Controller != null && Controller.IsInputStageRunReviewManualActive &&
                    !Controller.InputStageRunReviewActionToken.IsCancellationRequested)
                {
                    bool resumed = dialog.StartWaferVisionLive();
                    if (!resumed)
                        dialog.SetStatusMessage("좌표 검증이 종료되었으나 Live 영상 복귀에 실패했습니다. 비전 사용을 다시 시작하세요.");
                }
            }
        }

        private static void EnsureInputStageReviewVerificationDieAvailable(
            InputStageReviewGeometryContext context, DieMapEntry entry)
        {
            string reason;
            if (!MaterialStateService.CheckInputStageReviewVerificationDie(
                context.SessionGeneration, context.RequestGeneration, context.WaferId,
                context.MappingRevision, entry.DieUid, out reason))
                throw new InvalidOperationException(reason);
        }

        private static void LogInputStageReviewGeometry(
            string action, InputStageReviewGeometryContext context, string detail)
        {
            QMC.Common.Log.Write("Main", UserSession.Name, "InputStageReviewGeometry",
                action + ": " + detail +
                (context == null ? string.Empty :
                 ", wafer=" + context.WaferId + ", revision=" + context.MappingRevision +
                 ", session/request=" + context.SessionGeneration + "/" + context.RequestGeneration +
                 ", origin=" + context.OriginX.ToString("F6") + "/" + context.OriginY.ToString("F6") +
                 ", baseline=" + context.BaselineOriginX.ToString("F6") + "/" + context.BaselineOriginY.ToString("F6") +
                 ", pitch=" + context.PitchX.ToString("F6") + "/" + context.PitchY.ToString("F6") +
                 ", T=" + context.StageTheta.ToString("F6") +
                 ", condition=" + context.ConditionSignature + ", candidate=" + context.CandidateSignature));
        }
    }
}
