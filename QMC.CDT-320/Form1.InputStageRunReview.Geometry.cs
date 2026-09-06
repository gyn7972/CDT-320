using System;
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
        private string _inputStageRunReviewApprovalToken = string.Empty;
        private InputStageReviewGeometryContext _inputStageRunReviewPendingGeometryContext;
        private double _inputStageRunReviewPendingCaptureX;
        private double _inputStageRunReviewPendingCaptureY;
        private double _inputStageRunReviewPendingCaptureT;

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
                throw new InvalidOperationException("검출 중 세션, T, 맵 또는 레시피·설정 조건이 변경되어 결과를 폐기했습니다.");
        }

        private static void EnsureInputStageReviewAxesStationary(InputStageUnit stage)
        {
            if (stage == null || stage.CameraX == null || stage.StageY == null || stage.StageT == null)
                throw new InvalidOperationException("Review 동작에 필요한 X/Y/T 축 정보가 없습니다.");
            foreach (BaseAxis axis in new BaseAxis[] { stage.CameraX, stage.StageY, stage.StageT })
            {
                if (axis.IsMoving || !axis.IsInPosition ||
                    double.IsNaN(axis.ActualPosition) || double.IsInfinity(axis.ActualPosition))
                    throw new InvalidOperationException("X/Y/T 정지 및 InPosition 확인 후 다시 검출하세요.");
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

        private bool CanSubmitInputStageReviewConfirmation(InputStageRunReviewDialog dialog, out string reason)
        {
            if (dialog == null || dialog.IsDisposed || !ReferenceEquals(dialog, _inputStageRunReviewDialog) ||
                _inputStageRunReviewCleanedGeneration >= _inputStageRunReviewSessionGeneration ||
                Controller == null || !Controller.IsInputStageRunReviewManualActive)
            {
                reason = "현재 활성 Review 세션에서만 확인할 수 있습니다.";
                return false;
            }
            dialog.SetNonproductionReviewMode(MaterialStateService.IsInputStageReviewNonProductionMode(out reason));
            InputStageReviewGeometryContext context;
            string token;
            // 기존 CONFIRM에서 현재 자료의 일관성을 확인한다. 별도 3점 이동/촬영을 실행하지 않는다.
            if (!MaterialStateService.TryRegisterInputStageReviewConfirmation(
                dialog.WaferId, dialog.DraftDieMap, _inputStageRunReviewSessionGeneration,
                _inputStageRunReviewRequestGeneration, out context, out token, out reason))
            {
                LogInputStageReviewGeometry("CONFIRM_REJECTED", context, reason, dialog.DraftDieMap);
                return false;
            }
            _inputStageRunReviewApprovalToken = token;
            LogInputStageReviewGeometry("CONFIRM_SUBMITTED", context,
                "approval=" + token + "; selectedUid=" + (dialog.SelectedDie != null ? dialog.SelectedDie.DieUid : "<none>") +
                "; startUid=" + (dialog.StartDieUid ?? "") + "; orderedCount=" + dialog.OrderedDieIds.Count +
                "; physicalVerification=not-performed", dialog.DraftDieMap);
            return true;
        }

        private static string DescribeInputStageReviewVisionResult(VisionAlignResult vision)
        {
            if (vision == null)
                return "visionResult=unavailable";
            return FormattableString.Invariant($"visionResult=received; visionDeltaMmX={vision.DeltaX:R}; visionDeltaMmY={vision.DeltaY:R}; visionDeltaThetaDeg={vision.DeltaTheta:R}; alignResultPitchFieldX={vision.PitchX:R}; alignResultPitchFieldY={vision.PitchY:R}");
        }

        private static void LogInputStageReviewGeometry(
            string action, InputStageReviewGeometryContext context, string detail, DieMap draft = null)
        {
            MaterialStateService.WriteInputStageReviewDiagnostic(action, context, draft, detail);
        }
    }
}
