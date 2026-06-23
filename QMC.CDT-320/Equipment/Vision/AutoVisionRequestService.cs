using System;
using System.Threading;
using System.Threading.Tasks;
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

                // 주의: DryRun 은 여기서 바이패스하지 않는다(아래에서 실제 그랩 수행). Sim/Bypass/비전미사용만 생략.
                if (IsSimulationVisionBypassed() || IsVisionDisabled())
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB-BYPASS",
                        BypassReason() + " Vision GRAB 요청을 생략합니다. channel=" + channel + ", index=" + index);
                    return Task.FromResult(true);
                }

                VisionTcpClient client = ResolveClient(channel);
                if (IsDryRunMode())
                    return RunDryRunGrabAsync(client, channel, index, timeoutMs, ct);

                if (!IsReady(client, channel, "GRAB", string.Empty, index))
                    return Task.FromResult(false);

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-GRAB",
                    "Vision GRAB 요청. channel=" + channel + ", index=" + index + ", timeoutMs=" + timeoutMs);

                return client.ExposeAsync(index, timeoutMs, ct);
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

                VisionTcpClient client = ResolveClient(channel);
                if (!IsReady(client, channel, "MATCH", finder, index))
                    return BuildMatchFailure("Vision client is not connected.");

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH",
                    "Vision MATCH 요청. channel=" + channel +
                    ", finder=" + finder +
                    ", index=" + index +
                    ", timeoutMs=" + timeoutMs);

                MatchResultDto result = await client.MatchAsync(finder, index, timeoutMs, ct).ConfigureAwait(false);
                if (result == null || !result.Success)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-MATCH",
                        "Vision MATCH 실패. channel=" + channel +
                        ", finder=" + finder +
                        ", index=" + index +
                        ", raw=" + (result != null ? result.RawError : "null"));
                }
                else
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH",
                        "Vision MATCH 완료. channel=" + channel +
                        ", finder=" + finder +
                        ", index=" + index +
                        ", x=" + result.X.ToString("F6") +
                        ", y=" + result.Y.ToString("F6") +
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
                    "Vision MATCH 예외 발생. channel=" + channel +
                    ", finder=" + finder +
                    ", index=" + index +
                    ", error=" + ex.Message);
                return BuildMatchFailure(ex.Message);
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

                VisionTcpClient client = ResolveClient(channel);
                if (!IsReady(client, channel, "INSPECT", inspector, index))
                    return new InspectionResultDto { IsPass = false, Raw = "Vision client is not connected." };

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-INSPECT",
                    "Vision INSPECT 요청. channel=" + channel +
                    ", inspector=" + inspector +
                    ", index=" + index +
                    ", timeoutMs=" + timeoutMs);

                InspectionResultDto result = await client.InspectAsync(inspector, index, timeoutMs, ct).ConfigureAwait(false);
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
                        ", offsetX=" + result.OffsetX.ToString("F6") +
                        ", offsetY=" + result.OffsetY.ToString("F6") +
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

        private static VisionTcpClient ResolveClient(AutoVisionChannel channel)
        {
            switch (channel)
            {
                case AutoVisionChannel.Wafer:
                    return VisionHub.Wafer;
                case AutoVisionChannel.Bottom:
                    return VisionHub.Inspection;
                case AutoVisionChannel.Bin:
                    return VisionHub.Bin;
                case AutoVisionChannel.Main:
                    return VisionHub.Main;
                case AutoVisionChannel.FrontSide:
                    return VisionHub.TopSide != null && VisionHub.TopSide.IsConnected ? VisionHub.TopSide : VisionHub.Inspection;
                case AutoVisionChannel.RearSide:
                    return VisionHub.BottomSide != null && VisionHub.BottomSide.IsConnected ? VisionHub.BottomSide : VisionHub.Inspection;
                default:
                    return null;
            }
        }

        private static bool IsReady(VisionTcpClient client, AutoVisionChannel channel, string command, string toolName, int index)
        {
            if (client != null && client.IsConnected)
                return true;

            EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-NOT-CONNECTED",
                "Vision 연결이 없어 요청을 수행할 수 없습니다. channel=" + channel +
                ", command=" + command +
                ", tool=" + toolName +
                ", index=" + index);
            return false;
        }

        private static async Task<bool> RunDryRunGrabAsync(
            VisionTcpClient client,
            AutoVisionChannel channel,
            int index,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (client == null || !client.IsConnected)
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

                bool result = await client.ExposeAsync(index, timeoutMs, ct).ConfigureAwait(false);
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

        /// <summary>비전 미사용 설정(UseVision=false) — 연결/요청 없이 통과 처리.</summary>
        private static bool IsVisionDisabled()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && !settings.UseVision;
        }

        private static bool ShouldBypassVisionResultRequests()
        {
            return IsSimulationVisionBypassed() || IsDryRunMode() || IsVisionDisabled();
        }

        /// <summary>바이패스 로그용 사유 문자열(정확한 원인 표기).</summary>
        private static string BypassReason()
        {
            if (IsVisionDisabled())          return "비전 미사용 설정이라";
            if (IsSimulationVisionBypassed()) return "시뮬레이션 모드라";
            if (IsDryRunMode())              return "DryRun 모드라";
            return "바이패스 설정이라";
        }

        private static MatchResultDto BuildBypassMatchResult(AutoVisionChannel channel, string finder, int index)
        {
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-MATCH-BYPASS",
                BypassReason() + " Vision MATCH 결과 요청을 생략합니다. channel=" + channel +
                ", finder=" + finder +
                ", index=" + index);

            return new MatchResultDto
            {
                Success = true,
                X = 320.0,
                Y = 240.0,
                AngleDeg = 0.0,
                Score = 1.0,
                RawError = "BYPASS:SimulationOrDryRun"
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
