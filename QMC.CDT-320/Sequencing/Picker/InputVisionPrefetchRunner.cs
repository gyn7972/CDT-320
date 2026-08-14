using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Input Vision 촬영 선행 실행자(Prefetch Runner).
    /// 기존 경계 트리거(PickUp/Place 완료 시 1회성 시작)와 달리, Auto run 동안 주기적으로
    /// 안전 조건을 재판정해 InputCamera 선행검사(InputCameraPreInspectionCoordinator)를 기동한다.
    /// 목적: Front가 Bottom 검사, Rear가 Place 중처럼 픽커가 Input 존을 비운 시간에
    /// 다음 픽업 배치의 촬영+옵셋 산출을 미리 끝내 픽업 허가를 준비해 두는 오버랩.
    /// 촬영 본체/lease/허가 발행은 전부 기존 코디네이터·시퀀스가 수행하며, 이 러너는 기동 판단만 한다.
    /// VisionConfig.UseInputVisionPrefetch(기본 OFF)로만 활성화된다.
    /// </summary>
    internal static class InputVisionPrefetchRunner
    {
        /// <summary>Prefetch 러너 활성 여부 (VisionConfig.UseInputVisionPrefetch).</summary>
        public static bool IsEnabled(MachineSequenceContext context)
        {
            try
            {
                VisionUnit vision = context != null && context.Machine != null ? context.Machine.VisionUnit : null;
                return vision != null && vision.Config != null && vision.Config.UseInputVisionPrefetch;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 순수 트리거 판정 (하네스 검증용): 외부 상태를 인자로만 받아 기동 시도 여부를 판정한다.
        /// </summary>
        public static bool ShouldAttemptPrefetch(
            bool prefetchEnabled,
            bool inputStageReady,
            bool hasReadyPickTarget,
            bool sideHasPermission,
            bool drainRequested,
            out string skipReason)
        {
            skipReason = string.Empty;

            if (!prefetchEnabled)
            {
                skipReason = "prefetch 비활성";
                return false;
            }

            if (drainRequested)
            {
                skipReason = "웨이퍼 완료 드레인 중";
                return false;
            }

            if (!inputStageReady)
            {
                skipReason = "InputStageReady 신호 없음";
                return false;
            }

            if (!hasReadyPickTarget)
            {
                skipReason = "픽업 예약 가능 다이 없음";
                return false;
            }

            if (sideHasPermission)
            {
                skipReason = "해당 사이드 미소비 허가 잔존";
                return false;
            }

            return true;
        }

        /// <summary>Auto run 동안 도는 러너 루프. AutoSequenceCoordinator가 기동/취소를 소유한다.</summary>
        public static async Task RunAsync(
            MachineSequenceContext context,
            bool frontActive,
            bool rearActive,
            CancellationToken ct)
        {
            if (context == null)
                return;

            WriteLog("InputVisionPrefetchRunner",
                "Input Vision Prefetch 러너 시작. frontActive=" + frontActive +
                ", rearActive=" + rearActive + " - Start");

            DateTime frontHoldUntil = DateTime.MinValue;
            DateTime rearHoldUntil = DateTime.MinValue;
            string frontLastSkipReason = null;
            string rearLastSkipReason = null;

            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (context.IsCycleStopRequested)
                    {
                        WriteLog("InputVisionPrefetchRunner",
                            "정상 Cycle Stop 요청으로 새 Input Vision 선행검사 시작을 차단하고 러너를 종료합니다. - Stopped");
                        return;
                    }

                    int idlePollMs = ResolveIdlePollMs(context);
                    if (!IsEnabled(context))
                    {
                        // 런타임에 플래그가 꺼지면 아무것도 하지 않고 대기만 한다.
                        await Task.Delay(idlePollMs, ct).ConfigureAwait(false);
                        continue;
                    }

                    if (frontActive)
                    {
                        frontHoldUntil = TryStartForSide(
                            context,
                            PickerSequenceSide.Front,
                            frontHoldUntil,
                            ref frontLastSkipReason,
                            ct);
                    }

                    if (rearActive)
                    {
                        rearHoldUntil = TryStartForSide(
                            context,
                            PickerSequenceSide.Rear,
                            rearHoldUntil,
                            ref rearLastSkipReason,
                            ct);
                    }

                    await Task.Delay(idlePollMs, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("InputVisionPrefetchRunner", "Input Vision Prefetch 러너가 취소로 종료되었습니다. - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("InputVisionPrefetchRunner",
                    "Input Vision Prefetch 러너 루프 예외로 종료합니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static DateTime TryStartForSide(
            MachineSequenceContext context,
            PickerSequenceSide side,
            DateTime holdUntil,
            ref string lastSkipReason,
            CancellationToken ct)
        {
            if (DateTime.UtcNow < holdUntil)
                return holdUntil;

            bool inputStageReady = context.Bus != null && context.Bus.IsSet("InputStageReady");
            bool hasReadyPickTarget = QMC.CDT320.Materials.MaterialStateService.HasReadyInputStagePickTarget();
            bool sideHasPermission = InputCameraPickUpPermissionStore.HasPermission(side);
            // 드레인 관찰(ObserveCompletionSignals)은 WaferCompletion 모니터/픽커 시퀀스 소관 — 여기서는 읽기만 한다.
            bool drainRequested = context.WaferCompletion != null &&
                                  context.WaferCompletion.Enabled &&
                                  context.WaferCompletion.IsDrainRequested;

            string skipReason;
            if (!ShouldAttemptPrefetch(
                true,
                inputStageReady,
                hasReadyPickTarget,
                sideHasPermission,
                drainRequested,
                out skipReason))
            {
                LogSkipReasonIfChanged(side, "판정:" + skipReason, ref lastSkipReason);
                return holdUntil;
            }

            string blockReason;
            if (!InputCameraPreInspectionStartGate.CanStart(context, out blockReason))
            {
                LogSkipReasonIfChanged(side, "게이트:" + blockReason, ref lastSkipReason);
                return holdUntil;
            }

            bool started = InputCameraPreInspectionCoordinator.EnsureStarted(
                context,
                side,
                BuildPrefetchOptions(context, side),
                ct,
                "InputVisionPrefetchRunner:" + side);

            if (started)
            {
                lastSkipReason = null;
                WriteLog("InputVisionPrefetchRunner",
                    side + " Input Vision 선행 촬영을 기동했습니다(주기 재시도 실행자). - Start");
                // 방금 기동한 백그라운드 촬영이 진행/실패 정리할 시간을 주고 재판정한다 (연속 재기동 폭주 방지).
                return DateTime.UtcNow.AddMilliseconds(ResolveFailureHoldMs(context));
            }

            // EnsureStarted false = 이미 실행 중/허가 존재/시작 보류 — 다음 주기에 재판정.
            return holdUntil;
        }

        private static PickerSequenceOptions BuildPrefetchOptions(MachineSequenceContext context, PickerSequenceSide side)
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Auto;
            options.SimulateVisionResult = ShouldSimulateVisionResult(context, side);
            options.PickerMotionOnlyTestMode = IsPickerMotionOnlyTestModeEnabled();
            options.RequireInputCameraMarkInspectionPermission = false;
            options.InputCameraPreInspectionMode = true;
            options.PickerNo = 0;
            options.RestrictToPickerNo = 0;
            options.ApplyInputStageVisionPolicy(context != null ? context.Machine : null);
            return options;
        }

        // FrontPickerSequence.ShouldSimulateVisionResult와 동일 정책 (side만 파라미터화).
        private static bool ShouldSimulateVisionResult(MachineSequenceContext context, PickerSequenceSide side)
        {
            try
            {
                if (QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive())
                    return false;

                AppSettings settings = AppSettingsStore.Current;
                if (settings != null &&
                    (settings.SimulationMode || settings.BypassHardware || !settings.UseAjin))
                    return true;

                if (context != null && context.Controller != null && context.Controller.GlobalDryRun)
                    return false;

                if (context == null || context.Machine == null)
                    return false;

                if (side == PickerSequenceSide.Front)
                {
                    PickerFrontUnit front = context.Machine.PickerFrontUnit;
                    return front != null &&
                           ((front.Setup != null && front.Setup.IsSimulationMode) ||
                            (front.Config != null && front.Config.IsSimulationMode));
                }

                PickerRearUnit rear = context.Machine.PickerRearUnit;
                return rear != null &&
                       ((rear.Setup != null && rear.Setup.IsSimulationMode) ||
                        (rear.Config != null && rear.Config.IsSimulationMode));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPickerMotionOnlyTestModeEnabled()
        {
            try
            {
                return AppSettingsStore.Current != null &&
                       AppSettingsStore.Current.PickerMotionOnlyTestMode;
            }
            catch
            {
                return false;
            }
        }

        private static int ResolveIdlePollMs(MachineSequenceContext context)
        {
            try
            {
                VisionUnit vision = context != null && context.Machine != null ? context.Machine.VisionUnit : null;
                int value = vision != null && vision.Config != null ? vision.Config.InputVisionPrefetchIdlePollMs : 200;
                return value > 0 ? value : 200;
            }
            catch
            {
                return 200;
            }
        }

        private static int ResolveFailureHoldMs(MachineSequenceContext context)
        {
            try
            {
                VisionUnit vision = context != null && context.Machine != null ? context.Machine.VisionUnit : null;
                int value = vision != null && vision.Config != null ? vision.Config.InputVisionPrefetchFailureHoldMs : 5000;
                return value > 0 ? value : 5000;
            }
            catch
            {
                return 5000;
            }
        }

        private static void LogSkipReasonIfChanged(PickerSequenceSide side, string reason, ref string lastSkipReason)
        {
            // 폴링 루프의 로그 폭주 방지: 사유가 바뀔 때만 남긴다.
            if (string.Equals(lastSkipReason, reason, StringComparison.Ordinal))
                return;

            lastSkipReason = reason;
            WriteLog("InputVisionPrefetchRunner",
                side + " Input Vision 선행 촬영 대기. reason=" + reason + " - Wait");
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
        }
    }
}
