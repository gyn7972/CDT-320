using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    public static partial class AutoVisionRequestService
    {
        private const int CorrelatedResultPollIntervalMs = 100;
        private static readonly object SyncMatchHandleLock = new object();
        private static readonly Dictionary<string, VisionRequestHandle> SyncMatchHandles =
            new Dictionary<string, VisionRequestHandle>(StringComparer.OrdinalIgnoreCase);

        public static async Task<VisionRequestHandle> StartInspectionRequestAsync(
            VisionInspectionRequestContext context,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                VisionInspectionEnvelope envelope = VisionInspectionEnvelope.Create(context);
                string validationError;
                if (!envelope.Validate(out validationError))
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-CORRELATED-VALIDATION",
                        "신규 Vision 요청 문맥 검증 실패. " + validationError);
                    return null;
                }

                if (ShouldBypassVisionResultRequests())
                {
                    var bypassHandle = new VisionRequestHandle(envelope);
                    bypassHandle.MarkBypassed();
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-CORRELATED-BYPASS",
                        BypassReason() + " 신규 Vision 요청을 생략합니다. camera=" + envelope.Camera +
                        ", finder=" + envelope.Finder +
                        ", requestId=" + envelope.RequestId +
                        ", groupId=" + envelope.GroupId);
                    return bypassHandle;
                }

                VisionTcpClient client = VisionCommandService.ResolveInspectionClient(context.Channel);
                if (client == null || !client.IsConnected)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-CORRELATED-NOT-CONNECTED",
                        "신규 Vision CAMERA 연결이 없습니다. 다른 카메라 연결로 우회하지 않습니다. camera=" + envelope.Camera +
                        ", finder=" + envelope.Finder +
                        ", requestId=" + envelope.RequestId);
                    return null;
                }

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-CORRELATED-REQ",
                    "신규 Vision 검사 요청. camera=" + envelope.Camera +
                    ", command=" + envelope.Command +
                    ", finder=" + envelope.Finder +
                    ", head=" + envelope.Head + envelope.HeadIndex +
                    ", dieIndex=" + envelope.DieIndex +
                    ", grid=" + envelope.GridX + ";" + envelope.GridY +
                    ", channel=" + envelope.VisionChannel +
                    ", waferId=" + envelope.WaferId +
                    ", recipeId=" + envelope.RecipeId +
                    ", lotId=" + envelope.LotId +
                    ", requestId=" + envelope.RequestId +
                    ", groupId=" + envelope.GroupId);

                VisionRequestHandle handle = await client.SendInspectionRequestAsync(envelope, timeoutMs, ct).ConfigureAwait(false);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-CORRELATED-EPD",
                    "신규 Vision EPD 수신. camera=" + envelope.Camera +
                    ", requestId=" + envelope.RequestId +
                    ", groupId=" + envelope.GroupId +
                    ", requestToEpdMs=" + handle.RequestToExposureDoneMs.ToString("F3") +
                    ", ACK는 완료 조건에서 제외합니다.");
                return handle;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-CORRELATED-REQ",
                    "신규 Vision 요청/EPD 처리 실패. error=" + ex.Message);
                return null;
            }
        }

        public static async Task<VisionInspectionResult> WaitInspectionStageAsync(
            VisionRequestHandle handle,
            string command,
            int timeoutMs,
            CancellationToken ct)
        {
            if (handle == null || handle.Request == null)
                return null;
            if (!VisionInspectionCommands.IsResultRequest(command))
                throw new ArgumentException("Unknown Vision result stage. command=" + command, "command");
            if (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase) &&
                (!string.Equals(handle.Request.Camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(handle.Request.Operation, VisionInspectionOperations.Inspect, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("MRESULT는 BOTTOM INSPECT에서만 요청할 수 있습니다.");

            try
            {
                ct.ThrowIfCancellationRequested();

                if (handle.IsBypassed)
                {
                    VisionInspectionResult bypass = BuildBypassCorrelatedResult(handle, command);
                    handle.MarkStageDone(command, bypass);
                    return bypass;
                }

                VisionTcpClient client = VisionCommandService.ResolveInspectionClient(handle.Request.Channel);
                if (client == null || !client.IsConnected)
                {
                    handle.MarkError("Vision client is not connected. camera=" + handle.Request.Camera);
                    return null;
                }

                DateTime timeoutAt = DateTime.UtcNow.AddMilliseconds(Math.Max(1, timeoutMs));
                while (DateTime.UtcNow < timeoutAt)
                {
                    ct.ThrowIfCancellationRequested();
                    int remainMs = (int)Math.Max(1, (timeoutAt - DateTime.UtcNow).TotalMilliseconds);
                    int requestTimeoutMs = Math.Min(8000, remainMs);
                    handle.MarkStageTx(command);

                    VisionProtocolResponse response = await client.RequestInspectionResultAsync(
                        handle.Request.Camera,
                        command,
                        handle.Request.GroupId,
                        requestTimeoutMs,
                        ct).ConfigureAwait(false);
                    VisionInspectionResult parsed = VisionInspectionResult.Parse(response, handle.Request, command);

                    if (parsed.IsPending)
                    {
                        handle.MarkStagePending(command);
                        EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-CORRELATED-PENDING",
                            "Vision 결과 단계 PENDING. 같은 단계만 재요청합니다. camera=" + handle.Request.Camera +
                            ", command=" + command +
                            ", groupId=" + handle.Request.GroupId +
                            ", pendingCount=" + (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase)
                                ? handle.MResultPendingCount
                                : handle.ResultPendingCount));
                        await Task.Delay(CorrelatedResultPollIntervalMs, ct).ConfigureAwait(false);
                        continue;
                    }

                    if (parsed.IsError)
                    {
                        string error = string.IsNullOrWhiteSpace(parsed.Error) ? parsed.Raw : parsed.Error;
                        string errorWithCode = string.IsNullOrWhiteSpace(parsed.ErrorCode)
                            ? error
                            : parsed.ErrorCode + ": " + error;
                        handle.MarkError(errorWithCode);
                        EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-CORRELATED-RESULT",
                            "Vision 결과 단계 오류. camera=" + handle.Request.Camera +
                            ", command=" + command +
                            ", groupId=" + handle.Request.GroupId +
                            ", errorCode=" + (parsed.ErrorCode ?? string.Empty) +
                            ", error=" + error);
                        return null;
                    }

                    handle.MarkStageDone(command, parsed);
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-CORRELATED-RESULT",
                        "Vision 결과 단계 완료. camera=" + handle.Request.Camera +
                        ", command=" + command +
                        ", groupId=" + handle.Request.GroupId +
                        ", status=" + parsed.Status +
                        ", stageRoundTripMs=" +
                            (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase)
                                ? handle.MResultRoundTripMs
                                : handle.ResultRoundTripMs).ToString("F3") +
                        ", epdToStageRequestMs=" +
                            (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase)
                                ? handle.ExposureToMResultRequestMs
                                : handle.ExposureToResultRequestMs).ToString("F3") +
                        ", pendingCount=" +
                            (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase)
                                ? handle.MResultPendingCount
                                : handle.ResultPendingCount) +
                        ", requestToFinalMs=" + handle.RequestToFinalMs.ToString("F3") +
                        ", raw=" + parsed.Raw);
                    return parsed;
                }

                string timeoutError = "Vision 결과 단계 timeout. camera=" + handle.Request.Camera +
                                      ", command=" + command +
                                      ", groupId=" + handle.Request.GroupId +
                                      ", timeoutMs=" + timeoutMs;
                handle.MarkError(timeoutError);
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-CORRELATED-TIMEOUT", timeoutError);
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                handle.MarkError(ex.Message);
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-CORRELATED-RESULT",
                    "Vision 결과 단계 처리 예외. camera=" + handle.Request.Camera +
                    ", command=" + command +
                    ", groupId=" + handle.Request.GroupId +
                    ", error=" + ex.Message);
                return null;
            }
        }

        public static async Task<InspectionResultDto> CompleteBottomInspectionAsync(
            VisionRequestHandle handle,
            int timeoutMs,
            CancellationToken ct)
        {
            VisionInspectionResult mresult = await WaitInspectionStageAsync(
                handle,
                VisionInspectionCommands.MResult,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (mresult == null)
                return null;
            if (!IsBottomMResultSuccessStatus(mresult.Status))
            {
                string statusError = "MRESULT 완료 상태가 아닙니다. status=" + (mresult.Status ?? string.Empty);
                handle.MarkError(statusError);
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-MRESULT",
                    "Bottom MRESULT 상태 오류. RESULT를 조회하지 않습니다. groupId=" + handle.Request.GroupId +
                    ", error=" + statusError +
                    ", raw=" + (mresult.Raw ?? string.Empty));
                return null;
            }
            if (!IsValidBottomMResult(mresult, out string mresultError))
            {
                handle.MarkError(mresultError);
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-MRESULT",
                    "Bottom MRESULT 검증 실패. groupId=" + handle.Request.GroupId +
                    ", error=" + mresultError +
                    ", raw=" + (mresult.Raw ?? string.Empty));

                // MRESULT reached a terminal response, so drain the final RESULT for the same group.
                // The original MRESULT validation failure remains authoritative and is returned as failure.
                await WaitInspectionStageAsync(
                    handle,
                    VisionInspectionCommands.Result,
                    timeoutMs,
                    ct).ConfigureAwait(false);
                handle.MarkError(mresultError);
                return null;
            }

            VisionInspectionResult finalResult = await WaitInspectionStageAsync(
                handle,
                VisionInspectionCommands.Result,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (finalResult == null || finalResult.InspectionResult == null)
                return null;

            InspectionResultDto merged = finalResult.InspectionResult;
            MergeBottomMResultValues(merged, mresult);
            merged.SetValue("mresult_raw", mresult.Raw ?? string.Empty);
            merged.SetValue("result_raw", finalResult.Raw ?? string.Empty);
            return merged;
        }

        public static async Task<MatchResultDto> RunSyncMatchAsync(
            VisionInspectionRequestContext context,
            int timeoutMs,
            CancellationToken ct)
        {
            VisionRequestHandle handle = await StartInspectionRequestAsync(context, timeoutMs, ct).ConfigureAwait(false);
            if (handle == null)
                return null;

            return await CompleteSyncMatchHandleAsync(handle, timeoutMs, ct).ConfigureAwait(false);
        }

        internal static async Task<MatchResultDto> CompleteSyncMatchHandleAsync(
            VisionRequestHandle handle,
            int timeoutMs,
            CancellationToken ct)
        {
            if (handle == null || handle.Request == null)
                return null;

            VisionInspectionResult finalResult = await WaitInspectionStageAsync(
                handle,
                VisionInspectionCommands.Result,
                timeoutMs,
                ct).ConfigureAwait(false);
            return finalResult != null ? finalResult.MatchResult : null;
        }

        internal static bool TryStoreSyncMatchHandle(
            AutoVisionChannel channel,
            string finder,
            int dieIndex,
            VisionRequestHandle handle)
        {
            if (handle == null)
                return false;
            string key = BuildSyncMatchHandleKey(channel, finder, dieIndex);
            lock (SyncMatchHandleLock)
            {
                VisionRequestHandle stale;
                if (SyncMatchHandles.TryGetValue(key, out stale) && stale != null)
                {
                    stale.MarkError("같은 수동 Sync 키의 새 요청으로 교체되었습니다.");
                    EventLogger.Write(EventKind.Warning, "VISION", "AUTO-VISION-SYNC-HANDLE-REPLACE",
                        "대기 중이던 수동 Sync Handle을 새 request_id로 교체합니다. channel=" + channel +
                        ", finder=" + finder +
                        ", dieIndex=" + dieIndex +
                        ", oldRequestId=" + stale.Request.RequestId +
                        ", newRequestId=" + handle.Request.RequestId);
                }
                SyncMatchHandles[key] = handle;
                return true;
            }
        }

        internal static void ClearSyncMatchHandlesForChannel(AutoVisionChannel channel, string reason)
        {
            string prefix = channel + "\u001f";
            lock (SyncMatchHandleLock)
            {
                var keys = new List<string>();
                foreach (KeyValuePair<string, VisionRequestHandle> item in SyncMatchHandles)
                {
                    if (!item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (item.Value != null)
                        item.Value.MarkError(reason ?? "Vision 연결이 종료되었습니다.");
                    keys.Add(item.Key);
                }
                for (int i = 0; i < keys.Count; i++)
                    SyncMatchHandles.Remove(keys[i]);
            }
        }

        internal static VisionRequestHandle TakeSyncMatchHandle(
            AutoVisionChannel channel,
            string finder,
            int dieIndex)
        {
            string key = BuildSyncMatchHandleKey(channel, finder, dieIndex);
            lock (SyncMatchHandleLock)
            {
                VisionRequestHandle handle;
                if (!SyncMatchHandles.TryGetValue(key, out handle))
                    return null;
                SyncMatchHandles.Remove(key);
                return handle;
            }
        }

        private static string BuildSyncMatchHandleKey(
            AutoVisionChannel channel,
            string finder,
            int dieIndex)
        {
            return channel + "\u001f" + (finder ?? string.Empty).Trim() + "\u001f" + dieIndex;
        }

        public static async Task<InspectionResultDto> RunSyncInspectionAsync(
            VisionInspectionRequestContext context,
            int timeoutMs,
            CancellationToken ct)
        {
            VisionRequestHandle handle = await StartInspectionRequestAsync(context, timeoutMs, ct).ConfigureAwait(false);
            if (handle == null)
                return null;

            if (string.Equals(handle.Request.Camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase))
                return await CompleteBottomInspectionAsync(handle, timeoutMs, ct).ConfigureAwait(false);

            VisionInspectionResult finalResult = await WaitInspectionStageAsync(
                handle,
                VisionInspectionCommands.Result,
                timeoutMs,
                ct).ConfigureAwait(false);
            return finalResult != null ? finalResult.InspectionResult : null;
        }

        private static VisionInspectionResult BuildBypassCorrelatedResult(VisionRequestHandle handle, string command)
        {
            if (string.Equals(handle.Request.Operation, VisionInspectionOperations.Match, StringComparison.OrdinalIgnoreCase))
            {
                MatchResultDto match = BuildBypassMatchResult(
                    handle.Request.Channel,
                    handle.Request.Finder,
                    handle.Request.DieIndex);
                return VisionInspectionResult.FromBypass(handle.Request, command, match, null);
            }

            InspectionResultDto inspection = BuildBypassInspectionResult(
                handle.Request.Channel,
                handle.Request.Finder,
                handle.Request.DieIndex);
            if (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase))
            {
                inspection.SetValue("bottom_offset_x_mm", inspection.OffsetX);
                inspection.SetValue("bottom_offset_y_mm", inspection.OffsetY);
                inspection.SetValue("bottom_angle_deg", inspection.OffsetT);
            }
            return VisionInspectionResult.FromBypass(handle.Request, command, null, inspection);
        }

        private static void MergeBottomMResultValues(InspectionResultDto destination, VisionInspectionResult mresult)
        {
            if (destination == null || mresult == null)
                return;

            if (mresult.Values != null)
            {
                foreach (KeyValuePair<string, string> item in mresult.Values)
                    destination.SetValue(item.Key, item.Value);
            }

            InspectionResultDto intermediate = mresult.InspectionResult;
            if (intermediate == null)
                return;

            CopyAuthoritativeBottomValue(destination, intermediate, "bottom_offset_x_mm");
            CopyAuthoritativeBottomValue(destination, intermediate, "bottom_offset_y_mm");
            CopyAuthoritativeBottomValue(destination, intermediate, "bottom_angle_deg");
        }

        private static void CopyAuthoritativeBottomValue(
            InspectionResultDto destination,
            InspectionResultDto intermediate,
            string key)
        {
            if (intermediate.Values == null)
                return;
            string value;
            if (intermediate.Values.TryGetValue(key, out value))
                destination.SetValue(key, value);
        }

        private static bool IsValidBottomMResult(VisionInspectionResult result, out string error)
        {
            error = string.Empty;
            if (result == null)
            {
                error = "MRESULT 응답이 없습니다.";
                return false;
            }
            if (!IsBottomMResultSuccessStatus(result.Status))
            {
                error = "MRESULT 완료 상태가 아닙니다. status=" + (result.Status ?? string.Empty);
                return false;
            }

            string[] requiredKeys = { "bottom_offset_x_mm", "bottom_offset_y_mm", "bottom_angle_deg" };
            for (int i = 0; i < requiredKeys.Length; i++)
            {
                string raw;
                double parsed;
                if (result.Values == null ||
                    !result.Values.TryGetValue(requiredKeys[i], out raw) ||
                    !VisionProtocolResponse.TryParseDouble(raw, out parsed) ||
                    double.IsNaN(parsed) || double.IsInfinity(parsed))
                {
                    error = "MRESULT 필수 보정값이 없거나 숫자가 아닙니다. key=" + requiredKeys[i];
                    return false;
                }
            }
            return true;
        }

        private static bool IsBottomMResultSuccessStatus(string status)
        {
            return string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase);
        }
    }
}
