using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    public enum AutoVisionChannel
    {
        Wafer,
        Bottom,
        Bin,
        Main,
        FrontSide,
        RearSide
    }

    public static class AutoVisionRequestService
    {
        public static Task<bool> GrabAsync(AutoVisionChannel channel, int index, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // UseVision=false는 Vision 요청 자체를 생략한다.
                // DryRun은 실제 Vision 연결이 있으면 GRAB만 요청하고 결과 요청은 생략한다.
                if (IsVisionDisabled())
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB-BYPASS",
                        BypassReason() + " Vision GRAB 요청을 생략합니다. channel=" + channel + ", index=" + index);
                    return Task.FromResult(true);
                }

                if (IsDryRunMode())
                    return RunDryRunGrabAsync(channel, index, timeoutMs, ct);

                if (!IsReady(channel, VisionProtocolCommand.Grab, string.Empty, index))
                    return Task.FromResult(false);

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB",
                    "Vision GRAB 요청. channel=" + channel + ", index=" + index + ", timeoutMs=" + timeoutMs);

                return VisionCommandService.GrabAsync(channel, index, timeoutMs, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-GRAB",
                    "Vision GRAB 예외 발생. channel=" + channel + ", index=" + index + ", error=" + ex.Message);
                return Task.FromResult(false);
            }
            finally
            {
            }
        }

        public static async Task<MatchResultDto> MatchAsync(
            AutoVisionChannel channel,
            string finder,
            int index,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return BuildBypassMatchResult(channel, finder, index);

                bool started = await StartMatchAsync(channel, finder, index, timeoutMs, ct).ConfigureAwait(false);
                if (!started)
                    return BuildMatchFailure("MATCHASYNC STARTED ACK failed.");

                MatchResultDto result = await WaitMatchResultAsync(channel, finder, timeoutMs, ct).ConfigureAwait(false);
                if (result == null || !result.Success)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCH",
                        "Vision MATCHRESULT 실패. channel=" + channel +
                        ", finder=" + finder +
                        ", index=" + index +
                        ", raw=" + (result != null ? result.RawError : "null"));
                }
                else
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH",
                        "Vision MATCHRESULT 완료. channel=" + channel +
                        ", finder=" + finder +
                        ", index=" + index +
                        ", pixelX=" + result.X.ToString("F6") +
                        ", pixelY=" + result.Y.ToString("F6") +
                        ", t=" + result.AngleDeg.ToString("F6") +
                        ", score=" + result.Score.ToString("F6"));
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCH",
                    "Vision MATCHASYNC/MATCHRESULT 예외 발생. channel=" + channel +
                    ", finder=" + finder +
                    ", index=" + index +
                    ", error=" + ex.Message);
                return BuildMatchFailure(ex.Message);
            }
            finally
            {
            }
        }

        public static async Task<bool> StartMatchAsync(
            AutoVisionChannel channel,
            string finder,
            int index,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH-BYPASS",
                        BypassReason() + " Vision MATCHASYNC 시작 요청을 생략합니다. channel=" + channel +
                        ", finder=" + finder +
                        ", index=" + index);
                    return true;
                }

                if (!IsReady(channel, VisionProtocolCommand.MatchAsync, finder, index))
                    return false;

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCHASYNC",
                    "Vision MATCHASYNC 시작 요청. channel=" + channel +
                    ", finder=" + finder +
                    ", index=" + index +
                    ", timeoutMs=" + timeoutMs);

                bool started = await VisionCommandService.StartMatchAsync(channel, finder, index, timeoutMs, ct).ConfigureAwait(false);
                if (!started)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCHASYNC",
                        "Vision MATCHASYNC STARTED 응답 실패. channel=" + channel +
                        ", finder=" + finder +
                        ", index=" + index);
                }

                return started;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCHASYNC",
                    "Vision MATCHASYNC 시작 예외 발생. channel=" + channel +
                    ", finder=" + finder +
                    ", index=" + index +
                    ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        public static async Task<AsyncMatchPoll> PollMatchResultAsync(
            AutoVisionChannel channel,
            string finder,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                {
                    return new AsyncMatchPoll
                    {
                        Done = true,
                        Error = false,
                        Raw = "BYPASS",
                        Result = BuildBypassMatchResult(channel, finder, 0)
                    };
                }

                if (!IsReady(channel, VisionProtocolCommand.MatchResult, finder, 0))
                    return new AsyncMatchPoll { Error = true, Raw = "Vision client is not connected." };

                return await VisionCommandService.GetMatchResultAsync(channel, finder, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCHRESULT",
                    "Vision MATCHRESULT 폴링 예외 발생. channel=" + channel +
                    ", finder=" + finder +
                    ", error=" + ex.Message);
                return new AsyncMatchPoll { Error = true, Raw = ex.Message };
            }
            finally
            {
            }
        }

        public static async Task<MatchResultDto> WaitMatchResultAsync(
            AutoVisionChannel channel,
            string finder,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                DateTime timeoutAt = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (DateTime.UtcNow < timeoutAt)
                {
                    ct.ThrowIfCancellationRequested();

                    int remainMs = (int)Math.Max(1, (timeoutAt - DateTime.UtcNow).TotalMilliseconds);
                    int pollTimeoutMs = Math.Min(1000, remainMs);
                    AsyncMatchPoll poll = await PollMatchResultAsync(channel, finder, pollTimeoutMs, ct).ConfigureAwait(false);
                    if (poll == null)
                        return BuildMatchFailure("MATCHRESULT response is null.");

                    if (poll.Error)
                        return BuildMatchFailure(poll.Raw);

                    if (poll.Done)
                    {
                        if (poll.Result != null)
                            return poll.Result;

                        return BuildMatchFailure("MATCHRESULT completed but result is null.");
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                return BuildMatchFailure("MATCHRESULT timeout. channel=" + channel + ", finder=" + finder + ", timeoutMs=" + timeoutMs);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return BuildMatchFailure(ex.Message);
            }
            finally
            {
            }
        }

        public static async Task<VisionAlignResult> MatchAlignAsync(
            AutoVisionChannel channel,
            string finder,
            int index,
            double pitchMm,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return BuildBypassAlignResult(channel, finder, index, pitchMm);

                MatchResultDto match = await MatchAsync(channel, finder, index, timeoutMs, ct).ConfigureAwait(false);
                VisionAlignResult align = VisionCameraCalibrationTransform.ToAlignResult(channel, match, pitchMm);
                if (align == null)
                    return null;

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH-CAL",
                    "Vision 매칭 보정 완료. channel=" + channel +
                    ", finder=" + finder +
                    ", index=" + index +
                    ", dxMm=" + align.DeltaX.ToString("F6") +
                    ", dyMm=" + align.DeltaY.ToString("F6") +
                    ", dt=" + align.DeltaTheta.ToString("F6"));
                return align;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCH-CAL-EX",
                    "Vision 매칭 보정 예외 발생. channel=" + channel +
                    ", finder=" + finder +
                    ", index=" + index +
                    ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        public static async Task<BottomVisionOffset> MatchBottomOffsetAsync(
            int pickerNo,
            string finder,
            int index,
            double scoreThreshold,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return new BottomVisionOffset { PickerNo = pickerNo, OffsetX = 0.0, OffsetY = 0.0, OffsetT = 0.0, IsOk = true };

                MatchResultDto match = await MatchAsync(AutoVisionChannel.Bottom, finder, index, timeoutMs, ct).ConfigureAwait(false);
                BottomVisionOffset offset = VisionCameraCalibrationTransform.ToBottomVisionOffset(pickerNo, match, scoreThreshold);
                if (offset != null)
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-CAL",
                        "Bottom Vision 보정 완료. pickerNo=" + pickerNo +
                        ", index=" + index +
                        ", ok=" + offset.IsOk +
                        ", dxMm=" + offset.OffsetX.ToString("F6") +
                        ", dyMm=" + offset.OffsetY.ToString("F6") +
                        ", dt=" + offset.OffsetT.ToString("F6"));
                }

                return offset;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-CAL-EX",
                    "Bottom Vision 보정 예외 발생. pickerNo=" + pickerNo + ", index=" + index + ", error=" + ex.Message);
                return new BottomVisionOffset { PickerNo = pickerNo, IsOk = false };
            }
            finally
            {
            }
        }

        public static async Task<InspectionResultDto> InspectAsync(
            AutoVisionChannel channel,
            string inspector,
            int index,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return BuildBypassInspectionResult(channel, inspector, index);

                if (!IsReady(channel, VisionProtocolCommand.Inspect, inspector, index))
                    return new InspectionResultDto { IsPass = false, Raw = "Vision client is not connected." };

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT",
                    "Vision INSPECT 요청. channel=" + channel +
                    ", inspector=" + inspector +
                    ", index=" + index +
                    ", timeoutMs=" + timeoutMs);

                InspectionResultDto result = await VisionCommandService.InspectAsync(channel, inspector, index, timeoutMs, ct).ConfigureAwait(false);
                if (result == null || !result.IsPass)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-INSPECT",
                        "Vision INSPECT 실패/NG. channel=" + channel +
                        ", inspector=" + inspector +
                        ", index=" + index +
                        ", raw=" + (result != null ? result.Raw : "null"));
                }
                else
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT",
                        "Vision INSPECT 완료. channel=" + channel +
                        ", inspector=" + inspector +
                        ", index=" + index +
                        ", pass=" + result.IsPass +
                        ", pixelX=" + result.OffsetX.ToString("F6") +
                        ", pixelY=" + result.OffsetY.ToString("F6") +
                        ", offsetT=" + result.OffsetT.ToString("F6") +
                        ", score=" + result.Score.ToString("F6"));
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-INSPECT",
                    "Vision INSPECT 예외 발생. channel=" + channel +
                    ", inspector=" + inspector +
                    ", index=" + index +
                    ", error=" + ex.Message);
                return new InspectionResultDto { IsPass = false, Raw = ex.Message };
            }
            finally
            {
            }
        }

        public static async Task<InspectionResultDto> InspectCalibratedAsync(
            AutoVisionChannel channel,
            string inspector,
            int index,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return BuildBypassInspectionResult(channel, inspector, index);

                InspectionResultDto raw = await InspectAsync(channel, inspector, index, timeoutMs, ct).ConfigureAwait(false);
                InspectionResultDto calibrated = VisionCameraCalibrationTransform.ToInspectionResult(channel, raw);
                if (calibrated != null && calibrated.HasOffset)
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT-CAL",
                        "Vision INSPECT 보정 완료. channel=" + channel +
                        ", inspector=" + inspector +
                        ", index=" + index +
                        ", offsetXmm=" + calibrated.OffsetX.ToString("F6") +
                        ", offsetYmm=" + calibrated.OffsetY.ToString("F6") +
                        ", offsetT=" + calibrated.OffsetT.ToString("F6"));
                }

                return calibrated;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-INSPECT-CAL-EX",
                    "Vision INSPECT 보정 예외 발생. channel=" + channel +
                    ", inspector=" + inspector +
                    ", index=" + index +
                    ", error=" + ex.Message);
                return new InspectionResultDto { IsPass = false, Raw = ex.Message };
            }
            finally
            {
            }
        }

        public static VisionAlignResult ToAlignResult(MatchResultDto match, double imageCenterX, double imageCenterY, double pixelToMm, double pitchMm)
        {
            if (match == null || !match.Success)
                return null;

            return new VisionAlignResult
            {
                DeltaX = (match.X - imageCenterX) * pixelToMm,
                DeltaY = (match.Y - imageCenterY) * pixelToMm,
                DeltaTheta = match.AngleDeg,
                PitchX = pitchMm,
                PitchY = pitchMm
            };
        }

        private static bool IsReady(AutoVisionChannel channel, VisionProtocolCommand command, string toolName, int index)
        {
            if (VisionCommandService.IsConnected(channel))
                return true;

            EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-NOT-CONNECTED",
                "Vision 연결이 없어 요청을 수행할 수 없습니다. channel=" + channel +
                ", command=" + VisionProtocolCommands.ToText(command) +
                ", tool=" + toolName +
                ", index=" + index);
            return false;
        }

        private static async Task<bool> RunDryRunGrabAsync(
            AutoVisionChannel channel,
            int index,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!VisionCommandService.IsConnected(channel))
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB-DRYRUN-SKIP",
                        "DryRun 모드지만 Vision 연결이 없어 GRAB 요청을 생략하고 진행합니다. channel=" + channel +
                        ", index=" + index);
                    return true;
                }

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB-DRYRUN",
                    "DryRun 모드 Vision GRAB 요청만 수행합니다. 결과 요청은 생략합니다. channel=" + channel +
                    ", index=" + index +
                    ", timeoutMs=" + timeoutMs);

                bool result = await VisionCommandService.GrabAsync(channel, index, timeoutMs, ct).ConfigureAwait(false);
                if (!result)
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB-DRYRUN-FAIL",
                        "DryRun 모드 Vision GRAB 응답 실패를 기록하고 시퀀스는 계속 진행합니다. channel=" + channel +
                        ", index=" + index);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB-DRYRUN-EX",
                    "DryRun 모드 Vision GRAB 예외를 기록하고 시퀀스는 계속 진행합니다. channel=" + channel +
                    ", index=" + index +
                    ", error=" + ex.Message);
                return true;
            }
            finally
            {
            }
        }

        private static bool IsSimulationVisionBypassed()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && (settings.SimulationMode || settings.BypassHardware);
        }

        private static bool IsDryRunMode()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.DryRunMode;
        }

        /// <summary>비전 미사용 설정(UseVision=false)에서는 연결/요청 없이 통과 처리한다.</summary>
        private static bool IsVisionDisabled()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && !settings.UseVision;
        }

        private static bool ShouldBypassVisionResultRequests()
        {
            return IsDryRunMode() || IsVisionDisabled();
        }

        /// <summary>바이패스 로그에 사용할 사유 문자열.</summary>
        private static string BypassReason()
        {
            if (IsVisionDisabled()) return "\uBE44\uC804 \uBBF8\uC0AC\uC6A9 \uC124\uC815\uC774\uB77C";
            if (IsDryRunMode()) return "DryRun \uBAA8\uB4DC\uB77C";
            return "\uBC14\uC774\uD328\uC2A4 \uC124\uC815\uC774\uB77C";
        }

        private static MatchResultDto BuildBypassMatchResult(AutoVisionChannel channel, string finder, int index)
        {
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH-BYPASS",
                BypassReason() + " Vision 매칭 결과 요청을 생략합니다. channel=" + channel +
                ", finder=" + finder +
                ", index=" + index);

            return new MatchResultDto
            {
                Success = true,
                X = 320.0,
                Y = 240.0,
                AngleDeg = 0.0,
                Score = 1.0,
                HasImageSize = true,
                ImageWidthPixel = 640.0,
                ImageHeightPixel = 480.0,
                RawError = "BYPASS:SimulationOrDryRun"
            };
        }

        private static VisionAlignResult BuildBypassAlignResult(AutoVisionChannel channel, string finder, int index, double pitchMm)
        {
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH-CAL-BYPASS",
                BypassReason() + " Vision 매칭 보정을 0으로 통과합니다. channel=" + channel +
                ", finder=" + finder +
                ", index=" + index);

            return new VisionAlignResult
            {
                DeltaX = 0.0,
                DeltaY = 0.0,
                DeltaTheta = 0.0,
                PitchX = pitchMm,
                PitchY = pitchMm
            };
        }

        private static InspectionResultDto BuildBypassInspectionResult(AutoVisionChannel channel, string inspector, int index)
        {
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT-BYPASS",
                BypassReason() + " Vision INSPECT 결과 요청을 생략합니다. channel=" + channel +
                ", inspector=" + inspector +
                ", index=" + index);

            return new InspectionResultDto
            {
                IsPass = true,
                HasOffset = true,
                OffsetX = 0.0,
                OffsetY = 0.0,
                OffsetT = 0.0,
                Score = 1.0,
                Raw = "BYPASS:SimulationOrDryRun"
            };
        }

        private static MatchResultDto BuildMatchFailure(string reason)
        {
            return new MatchResultDto
            {
                Success = false,
                RawError = reason
            };
        }
    }
}
