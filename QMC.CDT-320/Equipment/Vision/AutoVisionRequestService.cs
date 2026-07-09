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
        BottomInspection,
        Bin,
        Main,
        FrontSide,
        RearSide
    }

    public static class AutoVisionRequestService
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private const double SimVisionMaxPixelOffset = 25.0;
        private const double SimVisionMaxAngleDeg = 0.08;

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

                MatchResultDto result = await WaitMatchResultAsync(channel, finder, index, timeoutMs, ct).ConfigureAwait(false);
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
            int index,
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
                        Result = BuildBypassMatchResult(channel, finder, index)
                    };
                }

                if (!IsReady(channel, VisionProtocolCommand.MatchResult, finder, index))
                    return new AsyncMatchPoll { Error = true, Raw = "Vision client is not connected." };

                return await VisionCommandService.GetMatchResultAsync(channel, finder, index, timeoutMs, ct).ConfigureAwait(false);
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
            int index,
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
                    AsyncMatchPoll poll = await PollMatchResultAsync(channel, finder, index, pollTimeoutMs, ct).ConfigureAwait(false);
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

                return BuildMatchFailure("MATCHRESULT timeout. channel=" + channel + ", finder=" + finder + ", index=" + index + ", timeoutMs=" + timeoutMs);
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
                {
                    MatchResultDto simulated = BuildBypassMatchResult(channel, finder, index);
                    VisionAlignResult bypassAlign = VisionCameraCalibrationTransform.ToAlignResult(channel, simulated, pitchMm);
                    LogSimulatedAlignResult(channel, finder, index, bypassAlign);
                    return bypassAlign;
                }

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
                {
                    MatchResultDto simulated = BuildBypassMatchResult(AutoVisionChannel.BottomInspection, finder, index);
                    BottomVisionOffset bypassOffset = VisionCameraCalibrationTransform.ToBottomVisionOffset(pickerNo, simulated, scoreThreshold);
                    LogSimulatedBottomOffset(pickerNo, finder, index, bypassOffset);
                    return bypassOffset;
                }

                MatchResultDto match = await MatchAsync(AutoVisionChannel.BottomInspection, finder, index, timeoutMs, ct).ConfigureAwait(false);
                BottomVisionOffset offset = VisionCameraCalibrationTransform.ToBottomVisionOffset(pickerNo, match, scoreThreshold);
                if (offset != null)
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-CAL",
                        "Bottom Vision 보정 완료. pickerNo=" + pickerNo +
                        ", index=" + index +
                        ", ok=" + offset.IsOk +
                        ", dxMm=" + offset.OffsetX.ToString("F6") +
                        ", dyMm=" + offset.OffsetY.ToString("F6") +
                        ", dt=" + offset.OffsetT.ToString("F6") +
                        ", sideVisionYOffsetMm=" + offset.SideVisionYOffset.ToString("F6") +
                        ", pickerZOffsetMm=" + offset.PickerZOffset.ToString("F6") +
                        ", sideCorrectionValid=" + offset.HasSideInspectionCorrection);
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

        // ── 8콜렛(Front4+Back4) 신형 규약 — 고정 8파트 "tool|fb|collet|die_index|channel|chip_uid" ──
        //  fb=0(Front)/1(Back), collet=1~4, die_index=픽업 순서 1-base(0=없음, -1=다이 없는 메뉴얼 테스트),
        //  channel=항상 0/1 — Side 0(0°)/1(90°), Bottom/Bin 은 0°로 간주해 0. chip_uid=자재 고유 ID(결과 매칭 키, 맨 뒤).

        /// <summary>비동기 매칭(신형) — MATCHASYNC(fb/collet 명시) 시작 후 die_index 로 MATCHRESULT 회수(2026-07-06).
        /// gridX/gridY = 웨이퍼 격자 인덱스(비전 맵 조회 대체, 모름=-1).</summary>
        public static async Task<MatchResultDto> MatchColletAsync(
            AutoVisionChannel channel,
            string finder,
            int fb,
            int collet,
            int dieIndex,
            int gridX,
            int gridY,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return BuildBypassMatchResult(channel, finder, fb * 4 + collet);

                if (!IsReady(channel, VisionProtocolCommand.MatchAsync, finder, fb * 4 + collet))
                    return BuildMatchFailure("Vision client is not connected.");

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCHASYNC",
                    "Vision MATCHASYNC(8콜렛) 시작 요청. channel=" + channel +
                    ", finder=" + finder +
                    ", fb=" + fb + ", collet=" + collet +
                    ", dieIndex=" + dieIndex +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                bool started = await VisionCommandService.StartMatchAsync(channel, finder, fb, collet, dieIndex, 0, gridX, gridY, timeoutMs, ct).ConfigureAwait(false);   // 채널은 항상 0/1 — Bottom 은 0°로 간주해 0
                if (!started)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCHASYNC",
                        "Vision MATCHASYNC(8콜렛) STARTED 응답 실패. channel=" + channel +
                        ", finder=" + finder + ", fb=" + fb + ", collet=" + collet + ", dieIndex=" + dieIndex);
                    return BuildMatchFailure("MATCHASYNC STARTED ACK failed.");
                }

                MatchResultDto result = await WaitMatchResultByDieAsync(channel, finder, dieIndex, timeoutMs, ct).ConfigureAwait(false);
                if (result == null || !result.Success)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCH",
                        "Vision MATCHRESULT(8콜렛) 실패. channel=" + channel +
                        ", finder=" + finder + ", fb=" + fb + ", collet=" + collet +
                        ", dieIndex=" + dieIndex +
                        ", raw=" + (result != null ? result.RawError : "null"));
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
                    "Vision MATCHASYNC(8콜렛) 예외 발생. channel=" + channel +
                    ", finder=" + finder + ", fb=" + fb + ", collet=" + collet +
                    ", error=" + ex.Message);
                return BuildMatchFailure(ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>die_index 기준 MATCHRESULT 대기(신형, 2026-07-06 — 구 chip_uid 키 폐기).</summary>
        public static async Task<MatchResultDto> WaitMatchResultByDieAsync(
            AutoVisionChannel channel,
            string finder,
            int dieIndex,
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
                    AsyncMatchPoll poll = await VisionCommandService.GetMatchResultAsync(channel, finder, dieIndex, pollTimeoutMs, ct).ConfigureAwait(false);
                    if (poll == null)
                        return BuildMatchFailure("MATCHRESULT response is null.");
                    if (poll.Error)
                        return BuildMatchFailure(poll.Raw);
                    if (poll.Done)
                        return poll.Result ?? BuildMatchFailure("MATCHRESULT completed but result is null.");

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                return BuildMatchFailure("MATCHRESULT timeout. channel=" + channel + ", finder=" + finder + ", dieIndex=" + dieIndex + ", timeoutMs=" + timeoutMs);
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

        /// <summary>Bottom 픽업 오프셋(신형) — fb/collet 명시 MATCHASYNC 로 요청하고 콜렛 번호를 PickerNo 로 보고한다.
        /// 키=die_index, gridX/gridY=웨이퍼 격자 인덱스(2026-07-06).</summary>
        public static async Task<BottomVisionOffset> MatchBottomOffsetAsync(
            int fb,
            int collet,
            string finder,
            int dieIndex,
            int gridX,
            int gridY,
            double scoreThreshold,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                {
                    MatchResultDto simulated = BuildBypassMatchResult(AutoVisionChannel.BottomInspection, finder, fb * 4 + collet);
                    BottomVisionOffset bypassOffset = VisionCameraCalibrationTransform.ToBottomVisionOffset(collet, simulated, scoreThreshold);
                    LogSimulatedBottomOffset(collet, finder, fb * 4 + collet, bypassOffset);
                    return bypassOffset;
                }

                MatchResultDto match = await MatchColletAsync(AutoVisionChannel.BottomInspection, finder, fb, collet, dieIndex, gridX, gridY, timeoutMs, ct).ConfigureAwait(false);
                BottomVisionOffset offset = VisionCameraCalibrationTransform.ToBottomVisionOffset(collet, match, scoreThreshold);
                if (offset != null)
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-CAL",
                        "Bottom Vision 보정 완료(8콜렛). fb=" + fb +
                        ", collet=" + collet +
                        ", dieIndex=" + dieIndex +
                        ", ok=" + offset.IsOk +
                        ", dxMm=" + offset.OffsetX.ToString("F6") +
                        ", dyMm=" + offset.OffsetY.ToString("F6") +
                        ", dt=" + offset.OffsetT.ToString("F6") +
                        ", sideVisionYOffsetMm=" + offset.SideVisionYOffset.ToString("F6") +
                        ", pickerZOffsetMm=" + offset.PickerZOffset.ToString("F6") +
                        ", sideCorrectionValid=" + offset.HasSideInspectionCorrection);
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
                    "Bottom Vision 보정 예외 발생(8콜렛). fb=" + fb + ", collet=" + collet + ", error=" + ex.Message);
                return new BottomVisionOffset { PickerNo = collet, IsOk = false };
            }
            finally
            {
            }
        }

        /// <summary>
        /// Bottom SurfaceInspector 결과 기반 보정 구조.
        /// 현재는 원본 파라미터 로그만 확보하고 SideVisionY/PickerZ 보정값은 0으로 고정한다.
        /// </summary>
        public static async Task<BottomVisionOffset> InspectBottomOffsetAsync(
            int fb,
            int collet,
            string inspector,
            int dieIndex,
            int gridX,
            int gridY,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InspectionResultDto inspection = await InspectColletAsync(
                    AutoVisionChannel.BottomInspection,
                    inspector,
                    fb,
                    collet,
                    dieIndex,
                    0,
                    gridX,
                    gridY,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                BottomVisionOffset offset = VisionCameraCalibrationTransform.ToBottomVisionOffset(collet, inspection);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECT-CAL",
                    "Bottom SurfaceInspector 결과 구조 적용. fb=" + fb +
                    ", collet=" + collet +
                    ", dieIndex=" + dieIndex +
                    ", ok=" + (offset != null && offset.IsOk) +
                    ", rawValues=" + (inspection != null ? inspection.DescribeValues() : "null") +
                    ", sideVisionYOffsetMm=0.000000" +
                    ", pickerZOffsetMm=0.000000" +
                    ", sideCorrectionValid=False");

                return offset;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECT-CAL-EX",
                    "Bottom SurfaceInspector 결과 구조 적용 중 예외 발생. fb=" + fb +
                    ", collet=" + collet +
                    ", dieIndex=" + dieIndex +
                    ", error=" + ex.Message);
                return new BottomVisionOffset { PickerNo = collet, IsOk = false };
            }
            finally
            {
            }
        }

        /// <summary>동기 검사(신형) — "inspector|fb|collet|die_index|channel|chip_uid" 고정 8파트.
        /// 기존 pickerNo*10+side 인덱스 패킹을 대체한다.</summary>
        /// <summary>비동기 검사 시작(신형 8파트) — STARTED ACK 만 확인. 결과는 <see cref="WaitInspectResultByDieAsync"/> 로 회수.
        /// Side 는 같은 die_index 로 채널 0/1 두 번 시작 → 결과 1회(그룹 합산 판정).</summary>
        public static async Task<bool> StartInspectColletAsync(
            AutoVisionChannel channel,
            string inspector,
            int fb,
            int collet,
            int dieIndex,
            int visionChannel,
            int gridX,
            int gridY,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return true;

                if (!IsReady(channel, VisionProtocolCommand.InspectAsync, inspector, fb * 4 + collet))
                    return false;

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECTASYNC",
                    "Vision INSPECTASYNC(8콜렛) 시작 요청. channel=" + channel +
                    ", inspector=" + inspector +
                    ", fb=" + fb + ", collet=" + collet +
                    ", dieIndex=" + dieIndex +
                    ", ch=" + visionChannel +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                bool started = await VisionCommandService.InspectAsyncStartAsync(channel, inspector, fb, collet, dieIndex, visionChannel, gridX, gridY, timeoutMs, ct).ConfigureAwait(false);
                if (!started)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-INSPECTASYNC",
                        "Vision INSPECTASYNC(8콜렛) STARTED 응답 실패. channel=" + channel +
                        ", inspector=" + inspector +
                        ", fb=" + fb + ", collet=" + collet + ", dieIndex=" + dieIndex + ", ch=" + visionChannel);
                }
                return started;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-INSPECTASYNC",
                    "Vision INSPECTASYNC(8콜렛) 시작 예외 발생. channel=" + channel +
                    ", inspector=" + inspector +
                    ", fb=" + fb + ", collet=" + collet +
                    ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        /// <summary>die_index 기준 INSPECTRESULT 대기(신형) — 서버 대기형 응답(최대 6s/회) + 만료 재요청.</summary>
        public static async Task<InspectionResultDto> WaitInspectResultByDieAsync(
            AutoVisionChannel channel,
            string inspector,
            int dieIndex,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return BuildBypassInspectionResult(channel, inspector, dieIndex);

                DateTime timeoutAt = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (DateTime.UtcNow < timeoutAt)
                {
                    ct.ThrowIfCancellationRequested();

                    int remainMs = (int)Math.Max(1, (timeoutAt - DateTime.UtcNow).TotalMilliseconds);
                    int pollTimeoutMs = Math.Min(10000, Math.Max(8000, remainMs));   // 서버 대기 상한(6s)보다 길게
                    AsyncInspectPoll poll = await VisionCommandService.PollInspectResultAsync(channel, inspector, dieIndex, pollTimeoutMs, ct).ConfigureAwait(false);
                    if (poll == null)
                        return new InspectionResultDto { IsPass = false, Raw = "INSPECTRESULT response is null." };
                    if (poll.Error)
                        return new InspectionResultDto { IsPass = false, Raw = poll.Raw };
                    if (poll.Done)
                    {
                        InspectionResultDto done = poll.Result ?? new InspectionResultDto { IsPass = false, Raw = poll.Raw };
                        EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECTRESULT-RAW",
                            "Vision INSPECTRESULT 수신. channel=" + channel +
                            ", inspector=" + inspector +
                            ", dieIndex=" + dieIndex +
                            ", pass=" + done.IsPass +
                            ", values=" + done.DescribeValues() +
                            ", raw=" + (done.Raw ?? string.Empty));
                        return done;
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                return new InspectionResultDto
                {
                    IsPass = false,
                    Raw = "INSPECTRESULT timeout. channel=" + channel + ", inspector=" + inspector + ", dieIndex=" + dieIndex + ", timeoutMs=" + timeoutMs
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new InspectionResultDto { IsPass = false, Raw = ex.Message };
            }
            finally
            {
            }
        }

        /// <summary>콜렛 1개 검사(신형, 비동기 전용) — INSPECTASYNC 시작 후 die_index 로 결과 회수.
        /// 동기 INSPECT 와이어 폐기(2026-07-06)에 따른 대체 — 호출부 관점의 블로킹 동작은 동일.</summary>
        public static async Task<InspectionResultDto> InspectColletAsync(
            AutoVisionChannel channel,
            string inspector,
            int fb,
            int collet,
            int dieIndex,
            int visionChannel,
            int gridX,
            int gridY,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldBypassVisionResultRequests())
                    return BuildBypassInspectionResult(channel, inspector, fb * 4 + collet);

                bool started = await StartInspectColletAsync(channel, inspector, fb, collet, dieIndex, visionChannel, gridX, gridY, timeoutMs, ct).ConfigureAwait(false);
                if (!started)
                    return new InspectionResultDto { IsPass = false, Raw = "INSPECTASYNC STARTED ACK failed." };

                InspectionResultDto result = await WaitInspectResultByDieAsync(channel, inspector, dieIndex, timeoutMs, ct).ConfigureAwait(false);
                if (result == null || !result.IsPass)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-INSPECT",
                        "Vision INSPECTRESULT(8콜렛) 실패/NG. channel=" + channel +
                        ", inspector=" + inspector +
                        ", fb=" + fb + ", collet=" + collet + ", dieIndex=" + dieIndex + ", ch=" + visionChannel +
                        ", raw=" + (result != null ? result.Raw : "null"));
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
                    "Vision INSPECTASYNC(8콜렛) 예외 발생. channel=" + channel +
                    ", inspector=" + inspector +
                    ", fb=" + fb + ", collet=" + collet +
                    ", error=" + ex.Message);
                return new InspectionResultDto { IsPass = false, Raw = ex.Message };
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

                if (!IsReady(channel, VisionProtocolCommand.InspectAsync, inspector, index))
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
                {
                    InspectionResultDto simulated = BuildBypassInspectionResult(channel, inspector, index);
                    InspectionResultDto calibratedBypass = VisionCameraCalibrationTransform.ToInspectionResult(channel, simulated);
                    LogSimulatedInspectionResult(channel, inspector, index, calibratedBypass);
                    return calibratedBypass;
                }

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
            return IsDryRunMode() || IsSimulationVisionBypassed() || IsVisionDisabled();
        }

        /// <summary>바이패스 로그에 사용할 사유 문자열.</summary>
        private static string BypassReason()
        {
            if (IsVisionDisabled()) return "비전 미사용 설정이라";
            if (IsDryRunMode()) return "DryRun 모드라";
            if (IsSimulationVisionBypassed()) return "Simulation/Bypass 모드라";
            return "바이패스 설정이라";
        }

        private static MatchResultDto BuildBypassMatchResult(AutoVisionChannel channel, string finder, int index)
        {
            VisionCameraPixelCalibration camera = ResolveCameraCalibration(channel);
            bool simulateOffset = ShouldSimulateVisionOffset();
            double pixelX = simulateOffset ? NextSimulatedPixel(camera.ImageCenterPixelX, SimVisionMaxPixelOffset) : camera.ImageCenterPixelX;
            double pixelY = simulateOffset ? NextSimulatedPixel(camera.ImageCenterPixelY, SimVisionMaxPixelOffset) : camera.ImageCenterPixelY;
            double angle = simulateOffset ? NextSimulatedPixel(0.0, SimVisionMaxAngleDeg) : 0.0;
            double score = simulateOffset ? NextSimulatedScore() : 1.0;

            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH-BYPASS",
                BypassReason() + (simulateOffset ? " Vision 매칭 결과를 시뮬레이션합니다. " : " Vision 매칭 결과를 0 offset으로 통과합니다. ") +
                "channel=" + channel +
                ", finder=" + finder +
                ", index=" + index +
                ", centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + ", " + camera.ImageCenterPixelY.ToString("F3") + ")" +
                ", simulatedPixel=(" + pixelX.ToString("F3") + ", " + pixelY.ToString("F3") + ")" +
                ", scale=(" + camera.PixelToMmX.ToString("F9") + ", " + camera.PixelToMmY.ToString("F9") + ") mm/px" +
                ", image=(" + camera.ImageWidthPixel.ToString("F0") + "x" + camera.ImageHeightPixel.ToString("F0") + ")" +
                ", score=" + score.ToString("F6") +
                ", angle=" + angle.ToString("F6"));

            return new MatchResultDto
            {
                Success = true,
                X = pixelX,
                Y = pixelY,
                AngleDeg = angle,
                Score = score,
                HasImageSize = true,
                ImageWidthPixel = camera.ImageWidthPixel,
                ImageHeightPixel = camera.ImageHeightPixel,
                // Bottom SideY/PickerZ 보정 매핑은 실장비 SurfaceInspector 로그 확인 전까지 0으로 고정한다.
                HasSideInspectionCorrection = false,
                SideVisionYOffset = 0.0,
                PickerZOffset = 0.0,
                RawError = simulateOffset ? "SIMULATION:VisionPixelOffset" : "BYPASS:VisionDisabled"
            };
        }

        public static InspectionResultDto BuildSimulationInspectionResult(AutoVisionChannel channel, string inspector, int index)
        {
            return BuildInspectionResult(channel, inspector, index, true, "SIMULATION:VisionResult");
        }

        private static InspectionResultDto BuildBypassInspectionResult(AutoVisionChannel channel, string inspector, int index)
        {
            return BuildInspectionResult(channel, inspector, index, ShouldSimulateVisionOffset(), BypassReason());
        }

        private static InspectionResultDto BuildInspectionResult(AutoVisionChannel channel, string inspector, int index, bool simulateOffset, string reason)
        {
            VisionCameraPixelCalibration camera = ResolveCameraCalibration(channel);
            double pixelX = simulateOffset ? NextSimulatedPixel(camera.ImageCenterPixelX, SimVisionMaxPixelOffset) : camera.ImageCenterPixelX;
            double pixelY = simulateOffset ? NextSimulatedPixel(camera.ImageCenterPixelY, SimVisionMaxPixelOffset) : camera.ImageCenterPixelY;
            double angle = simulateOffset ? NextSimulatedPixel(0.0, SimVisionMaxAngleDeg) : 0.0;
            double score = simulateOffset ? NextSimulatedScore() : 1.0;

            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT-BYPASS",
                (reason ?? "") + (simulateOffset ? " Vision INSPECT 결과를 시뮬레이션합니다. " : " Vision INSPECT 결과를 0 offset으로 통과합니다. ") +
                "channel=" + channel +
                ", inspector=" + inspector +
                ", index=" + index +
                ", centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + ", " + camera.ImageCenterPixelY.ToString("F3") + ")" +
                ", simulatedPixel=(" + pixelX.ToString("F3") + ", " + pixelY.ToString("F3") + ")" +
                ", scale=(" + camera.PixelToMmX.ToString("F9") + ", " + camera.PixelToMmY.ToString("F9") + ") mm/px" +
                ", image=(" + camera.ImageWidthPixel.ToString("F0") + "x" + camera.ImageHeightPixel.ToString("F0") + ")" +
                ", score=" + score.ToString("F6") +
                ", angle=" + angle.ToString("F6"));

            var result = new InspectionResultDto
            {
                IsPass = true,
                HasOffset = true,
                OffsetX = pixelX,
                OffsetY = pixelY,
                OffsetT = angle,
                Score = score,
                HasImageSize = true,
                ImageWidthPixel = camera.ImageWidthPixel,
                ImageHeightPixel = camera.ImageHeightPixel,
                Raw = simulateOffset ? "SIMULATION:VisionPixelOffset" : "BYPASS:VisionDisabled"
            };

            AddBypassInspectionValues(result, channel, inspector, simulateOffset);
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT-BYPASS-DATA",
                "Simulation/Bypass 검사 측정값 생성. channel=" + channel +
                ", inspector=" + inspector +
                ", index=" + index +
                ", values=" + result.DescribeValues());

            return result;
        }

        private static void AddBypassInspectionValues(
            InspectionResultDto result,
            AutoVisionChannel channel,
            string inspector,
            bool simulateOffset)
        {
            if (result == null)
                return;

            if (channel == AutoVisionChannel.BottomInspection)
            {
                AddBypassBottomInspectionValues(result, simulateOffset);
                return;
            }

            if (channel == AutoVisionChannel.Bin || IsPlacementInspector(inspector))
            {
                AddBypassPlacementInspectionValues(result, simulateOffset);
                return;
            }

            if (channel == AutoVisionChannel.FrontSide || channel == AutoVisionChannel.RearSide)
            {
                AddBypassSideInspectionValues(result, simulateOffset);
                return;
            }

            if (!string.IsNullOrWhiteSpace(inspector))
            {
                result.SetValue("inspection_item_score", result.Score);
                result.SetValue("inspection_item_angle_deg", result.OffsetT);
            }
        }

        private static void AddBypassBottomInspectionValues(InspectionResultDto result, bool simulateOffset)
        {
            double width = simulateOffset ? NextSimulatedRange(0.985, 1.015) : 1.0;
            double height = simulateOffset ? NextSimulatedRange(0.985, 1.015) : 1.0;
            double angle = simulateOffset ? NextSimulatedMmOffset(0.08) : 0.0;
            double offsetX = simulateOffset ? NextSimulatedMmOffset(0.015) : 0.0;
            double offsetY = simulateOffset ? NextSimulatedMmOffset(0.015) : 0.0;

            result.SetValue("bottom_width_mm", width);
            result.SetValue("bottom_height_mm", height);
            result.SetValue("bottom_angle_deg", angle);
            result.SetValue("bottom_offset_x_mm", offsetX);
            result.SetValue("bottom_offset_y_mm", offsetY);
            SetBypassInspectionItem(result, "bottom_item_width", width, true);
            SetBypassInspectionItem(result, "bottom_item_height", height, true);
            SetBypassInspectionItem(result, "bottom_item_angle", angle, true);
            SetBypassInspectionItem(result, "bottom_item_offset_x", offsetX, true);
            SetBypassInspectionItem(result, "bottom_item_offset_y", offsetY, true);

            SetBypassInspectionItem(result, "bottom_item_chipping_top", simulateOffset ? NextSimulatedRange(0.000, 0.018) : 0.0, true);
            SetBypassInspectionItem(result, "bottom_item_chipping_right", simulateOffset ? NextSimulatedRange(0.000, 0.018) : 0.0, true);
            SetBypassInspectionItem(result, "bottom_item_chipping_bottom", simulateOffset ? NextSimulatedRange(0.000, 0.018) : 0.0, true);
            SetBypassInspectionItem(result, "bottom_item_chipping_left", simulateOffset ? NextSimulatedRange(0.000, 0.018) : 0.0, true);
            SetBypassInspectionItem(result, "bottom_item_chipping_ch1", simulateOffset ? NextSimulatedRange(0.000, 0.020) : 0.0, true);
            SetBypassInspectionItem(result, "bottom_item_chipping_ch2", simulateOffset ? NextSimulatedRange(0.000, 0.020) : 0.0, true);
            SetBypassInspectionItem(result, "bottom_item_foreign_max", simulateOffset ? NextSimulatedRange(0.000, 0.018) : 0.0, true);
            SetBypassInspectionItem(result, "bottom_item_foreign_count", simulateOffset ? NextSimulatedInteger(0, 3) : 0.0, true);
        }

        private static void AddBypassSideInspectionValues(InspectionResultDto result, bool simulateOffset)
        {
            AddBypassSideChannelInspectionValues(result, "ch0_", simulateOffset);
            AddBypassSideChannelInspectionValues(result, "ch1_", simulateOffset);
        }

        private static void AddBypassPlacementInspectionValues(InspectionResultDto result, bool simulateOffset)
        {
            double offsetX = simulateOffset ? NextSimulatedMmOffset(0.025) : 0.0;
            double offsetY = simulateOffset ? NextSimulatedMmOffset(0.025) : 0.0;
            double angle = simulateOffset ? NextSimulatedMmOffset(0.050) : 0.0;

            result.SetValue("placement_offset_x_mm", offsetX);
            result.SetValue("placement_offset_y_mm", offsetY);
            result.SetValue("placement_angle_deg", angle);

            SetBypassInspectionItem(result, "placement_item_offset_x", offsetX, true);
            SetBypassInspectionItem(result, "placement_item_offset_y", offsetY, true);
            SetBypassInspectionItem(result, "placement_item_angle", angle, true);
            SetBypassInspectionItem(result, "placement_item_top_gap_min", simulateOffset ? NextSimulatedRange(0.010, 0.060) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_top_gap_max", simulateOffset ? NextSimulatedRange(0.030, 0.090) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_top_gap_avg", simulateOffset ? NextSimulatedRange(0.020, 0.075) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_right_min", simulateOffset ? NextSimulatedRange(0.010, 0.060) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_right_max", simulateOffset ? NextSimulatedRange(0.030, 0.090) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_right_gap_avg", simulateOffset ? NextSimulatedRange(0.020, 0.075) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_bottom_min", simulateOffset ? NextSimulatedRange(0.010, 0.060) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_bottom_gap", simulateOffset ? NextSimulatedRange(0.030, 0.090) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_bottom_gap_avg", simulateOffset ? NextSimulatedRange(0.020, 0.075) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_left_min", simulateOffset ? NextSimulatedRange(0.010, 0.060) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_left_max", simulateOffset ? NextSimulatedRange(0.030, 0.090) : 0.0, true);
            SetBypassInspectionItem(result, "placement_item_left_gap_avg", simulateOffset ? NextSimulatedRange(0.020, 0.075) : 0.0, true);
        }

        private static void AddBypassSideChannelInspectionValues(InspectionResultDto result, string prefix, bool simulateOffset)
        {
            double maxChippingDepth = simulateOffset ? NextSimulatedRange(0.001, 0.030) : 0.0;
            double chippingTop = simulateOffset ? NextSimulatedRange(0.000, 0.020) : 0.0;
            double chippingBottom = simulateOffset ? NextSimulatedRange(0.000, 0.020) : 0.0;
            double chippingCount = simulateOffset ? NextSimulatedInteger(0, 3) : 0.0;
            double foreignCount = simulateOffset ? NextSimulatedInteger(0, 4) : 0.0;
            double foreignMax = simulateOffset ? NextSimulatedRange(0.000, 0.018) : 0.0;

            SetBypassInspectionItem(result, prefix + "side_item_max_chipping_depth", maxChippingDepth, true);
            SetBypassInspectionItem(result, prefix + "side_item_chipping_top", chippingTop, true);
            SetBypassInspectionItem(result, prefix + "side_item_chipping_bottom", chippingBottom, true);
            SetBypassInspectionItem(result, prefix + "side_item_chipping_count", chippingCount, true);
            SetBypassInspectionItem(result, prefix + "side_item_foreign_count", foreignCount, true);
            SetBypassInspectionItem(result, prefix + "side_item_foreign_max", foreignMax, true);
        }

        private static void SetBypassInspectionItem(InspectionResultDto result, string key, double value, bool pass)
        {
            if (result == null || string.IsNullOrWhiteSpace(key))
                return;

            result.SetValue(key, value);
            result.SetValue(key + "_pass", pass ? 1 : 0);
        }

        private static bool IsPlacementInspector(string inspector)
        {
            if (string.IsNullOrWhiteSpace(inspector))
                return false;

            return inspector.IndexOf("Placement", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   inspector.IndexOf("DieGap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   inspector.IndexOf("Bin", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldSimulateVisionOffset()
        {
            return IsVisionDisabled() || IsDryRunMode() || IsSimulationVisionBypassed();
        }

        private static VisionCameraPixelCalibration ResolveCameraCalibration(AutoVisionChannel channel)
        {
            VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(null, channel);
            if (camera == null)
                camera = new VisionCameraPixelCalibration();

            camera.EnsureDefaults(320.0, 240.0, 0.001, 0.001);
            return camera;
        }

        private static double NextSimulatedPixel(double center, double maxAbsOffset)
        {
            lock (SimVisionRandomLock)
            {
                return center + ((SimVisionRandom.NextDouble() * 2.0) - 1.0) * maxAbsOffset;
            }
        }

        private static double NextSimulatedMmOffset(double maxAbsOffset)
        {
            lock (SimVisionRandomLock)
            {
                return ((SimVisionRandom.NextDouble() * 2.0) - 1.0) * maxAbsOffset;
            }
        }

        private static double NextSimulatedRange(double min, double max)
        {
            lock (SimVisionRandomLock)
            {
                if (max < min)
                {
                    double temp = min;
                    min = max;
                    max = temp;
                }

                return min + (SimVisionRandom.NextDouble() * (max - min));
            }
        }

        private static int NextSimulatedInteger(int min, int maxInclusive)
        {
            lock (SimVisionRandomLock)
            {
                if (maxInclusive < min)
                {
                    int temp = min;
                    min = maxInclusive;
                    maxInclusive = temp;
                }

                return SimVisionRandom.Next(min, maxInclusive + 1);
            }
        }

        private static double NextSimulatedScore()
        {
            lock (SimVisionRandomLock)
            {
                return 0.985 + (SimVisionRandom.NextDouble() * 0.014);
            }
        }

        private static void LogSimulatedAlignResult(
            AutoVisionChannel channel,
            string finder,
            int index,
            VisionAlignResult result)
        {
            if (result == null)
                return;

            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH-CAL-BYPASS",
                BypassReason() + " Vision 매칭 보정을 시뮬레이션했습니다. channel=" + channel +
                ", finder=" + finder +
                ", index=" + index +
                ", dxMm=" + result.DeltaX.ToString("F6") +
                ", dyMm=" + result.DeltaY.ToString("F6") +
                ", dt=" + result.DeltaTheta.ToString("F6"));
        }

        private static void LogSimulatedBottomOffset(
            int pickerNo,
            string finder,
            int index,
            BottomVisionOffset result)
        {
            if (result == null)
                return;

            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-CAL-BYPASS",
                BypassReason() + " Bottom Vision 보정을 시뮬레이션했습니다. pickerNo=" + pickerNo +
                ", finder=" + finder +
                ", index=" + index +
                ", ok=" + result.IsOk +
                ", dxMm=" + result.OffsetX.ToString("F6") +
                ", dyMm=" + result.OffsetY.ToString("F6") +
                ", dt=" + result.OffsetT.ToString("F6") +
                ", sideVisionYOffsetMm=" + result.SideVisionYOffset.ToString("F6") +
                ", pickerZOffsetMm=" + result.PickerZOffset.ToString("F6") +
                ", sideCorrectionValid=" + result.HasSideInspectionCorrection);
        }

        private static void LogSimulatedInspectionResult(
            AutoVisionChannel channel,
            string inspector,
            int index,
            InspectionResultDto result)
        {
            if (result == null)
                return;

            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT-CAL-BYPASS",
                BypassReason() + " Vision INSPECT 보정을 시뮬레이션했습니다. channel=" + channel +
                ", inspector=" + inspector +
                ", index=" + index +
                ", pass=" + result.IsPass +
                ", offsetXmm=" + result.OffsetX.ToString("F6") +
                ", offsetYmm=" + result.OffsetY.ToString("F6") +
                ", offsetT=" + result.OffsetT.ToString("F6"));
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
