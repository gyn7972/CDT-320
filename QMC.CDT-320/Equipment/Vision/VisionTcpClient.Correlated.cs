using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    public partial class VisionTcpClient
    {
        private sealed class InspectionExposureWaiter
        {
            public VisionInspectionEnvelope Request { get; private set; }
            public TaskCompletionSource<VisionProtocolResponse> Completion { get; private set; }

            public InspectionExposureWaiter(VisionInspectionEnvelope request)
            {
                Request = request ?? throw new ArgumentNullException("request");
                Completion = new TaskCompletionSource<VisionProtocolResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        private readonly object _inspectionWaiterLock = new object();
        private readonly Dictionary<string, InspectionExposureWaiter> _exposureWaiters =
            new Dictionary<string, InspectionExposureWaiter>(StringComparer.Ordinal);

        public async Task<VisionRequestHandle> SendInspectionRequestAsync(
            VisionInspectionEnvelope envelope,
            int timeoutMs,
            CancellationToken ct)
        {
            if (envelope == null)
                throw new ArgumentNullException("envelope");
            if (!IsConnected)
                throw new InvalidOperationException("Vision client is not connected. camera=" + envelope.Camera);

            string validationError;
            if (!envelope.Validate(out validationError))
                throw new InvalidOperationException(validationError);

            var handle = new VisionRequestHandle(envelope);
            var waiter = new InspectionExposureWaiter(envelope);
            lock (_inspectionWaiterLock)
            {
                if (_exposureWaiters.ContainsKey(envelope.RequestId))
                    throw new InvalidOperationException("Duplicate Vision request_id. requestId=" + envelope.RequestId);
                _exposureWaiters.Add(envelope.RequestId, waiter);
            }

            try
            {
                handle.MarkRequestTx();
                await WriteLineOnlyAsync(envelope.ToLine(), ct).ConfigureAwait(false);
                VisionProtocolResponse response = await WaitForInspectionResponseAsync(
                    waiter.Completion,
                    timeoutMs,
                    ct,
                    delegate
                    {
                        RemoveExposureWaiter(envelope.RequestId, waiter);
                    }).ConfigureAwait(false);

                if (response == null || response.IsError)
                {
                    string error = response != null ? response.ErrorMessage : "EPD response is null.";
                    if (response != null && !string.IsNullOrWhiteSpace(response.ErrorCode))
                        error = response.ErrorCode + ": " + error;
                    handle.MarkError(error);
                    throw new InvalidOperationException(error);
                }

                handle.MarkExposureDone();
                return handle;
            }
            catch
            {
                RemoveExposureWaiter(envelope.RequestId, waiter);
                throw;
            }
        }

        private Task WriteLineOnlyAsync(string line, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Vision client is not connected.");

            byte[] data = Encoding.UTF8.GetBytes((line ?? string.Empty) + "\n");
            try
            {
                lock (_ioLock)
                {
                    if (!IsConnected || _stream == null)
                        throw new InvalidOperationException("Vision stream is not available.");
                    _stream.Write(data, 0, data.Length);
                }
                LogMsg("TX: " + line);
                EventLogger.Write(EventKind.Event, "VISION", "VISION-CORRELATED-TX",
                    "신규 Vision 상관관계 요청 송신. module=" + ModuleName +
                    ", host=" + Host + ", port=" + Port + ", wireLine=" + (line ?? string.Empty));
                return Task.CompletedTask;
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        private static async Task<VisionProtocolResponse> WaitForInspectionResponseAsync(
            TaskCompletionSource<VisionProtocolResponse> waiter,
            int timeoutMs,
            CancellationToken ct,
            Action removeWaiter)
        {
            int safeTimeoutMs = Math.Max(1, timeoutMs);
            Task delay = Task.Delay(safeTimeoutMs, ct);
            Task completed = await Task.WhenAny(waiter.Task, delay).ConfigureAwait(false);
            if (completed != waiter.Task)
            {
                // A response may complete at the same timeout boundary after WhenAny selected delay.
                if (waiter.Task.IsCompleted)
                    return await waiter.Task.ConfigureAwait(false);
                if (removeWaiter != null)
                    removeWaiter();
                if (waiter.Task.IsCompleted)
                    return await waiter.Task.ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                throw new TimeoutException("Vision correlated response timeout. timeoutMs=" + safeTimeoutMs);
            }
            return await waiter.Task.ConfigureAwait(false);
        }

        private bool TryRouteInspectionProtocolResponse(VisionProtocolResponse response)
        {
            if (response == null)
                return false;

            string requestId = response.GetValueAny("request_id", "requestId", "requestid");
            string groupId = response.GetValueAny("group_id", "groupId", "groupid");
            bool exposureDone = response.IsPush &&
                (string.Equals(response.Command, VisionProtocolPushCommands.ExposureDone, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(response.Command, VisionProtocolPushCommands.LegacyExposureDone, StringComparison.OrdinalIgnoreCase));

            if (exposureDone && !string.IsNullOrWhiteSpace(requestId))
            {
                InspectionExposureWaiter exposureWaiter = null;
                string correlationError = string.Empty;
                lock (_inspectionWaiterLock)
                {
                    if (_exposureWaiters.TryGetValue(requestId, out exposureWaiter))
                    {
                        if (IsExposureCorrelationMatch(response, exposureWaiter.Request, out correlationError))
                        {
                            _exposureWaiters.Remove(requestId);
                            exposureWaiter.Completion.TrySetResult(response);
                        }
                    }
                }

                if (exposureWaiter != null)
                {
                    if (!string.IsNullOrWhiteSpace(correlationError))
                    {
                        LogMsg("correlated EPD ignored: " + correlationError + ", request_id=" + requestId);
                        return true;
                    }
                    return true;
                }

                LogMsg("late/unmatched correlated EPD ignored. request_id=" + requestId);
                return true;
            }

            string responseCommand = response.Command;
            if (VisionInspectionCommands.IsInspectionRequest(responseCommand))
            {
                if (response.IsError && !string.IsNullOrWhiteSpace(requestId))
                {
                    InspectionExposureWaiter exposureWaiter = null;
                    string correlationError = string.Empty;
                    lock (_inspectionWaiterLock)
                    {
                        if (_exposureWaiters.TryGetValue(requestId, out exposureWaiter))
                        {
                            if (IsInspectionErrorCorrelationMatch(response, exposureWaiter.Request, out correlationError))
                            {
                                _exposureWaiters.Remove(requestId);
                                exposureWaiter.Completion.TrySetResult(response);
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(correlationError))
                        LogMsg("correlated inspection ERR ignored: " + correlationError + ", request_id=" + requestId);
                }

                // INSPECT_SYNC/ASYNC ACK는 진단 정보일 뿐 촬상 완료 조건이 아니다.
                LogMsg("diagnostic inspection ACK/ERR. command=" + responseCommand +
                       ", request_id=" + (requestId ?? string.Empty));
                return true;
            }

            if (VisionInspectionCommands.IsResultRequest(responseCommand))
            {
                // Pull 요청이 사라졌으므로 수신되는 MRESULT/RESULT 라인은 전부 비전의 자발 푸시다.
                if (response.IsAck)
                {
                    LogMsg("diagnostic result ACK ignored. command=" + responseCommand +
                           ", group_id=" + (groupId ?? string.Empty));
                    return true;
                }
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    // group_id 없는 결과는 상관관계를 잡을 수 없어 폐기한다.
                    // legacy FIFO(_pending)로 흘려보내면 구형 응답 큐가 오염되므로 여기서 소비한다.
                    LogMsg("push result dropped: group_id missing. command=" + responseCommand);
                    EventLogger.Write(EventKind.Warning, "VISION", "VISION-PUSH-RESULT-DROP",
                        "group_id 없는 Vision 결과 푸시를 폐기했습니다. module=" + ModuleName +
                        ", command=" + responseCommand +
                        ", rawLine=" + (response.RawLine ?? string.Empty));
                    return true;
                }

                // 수신 스토어에 보관(대기 소비자가 있으면 즉시 전달) — 소비는 AutoVisionRequestService가 수행.
                VisionInspectionResultStore.Add(new VisionInspectionResultEntry(
                    response.Module,
                    responseCommand,
                    groupId,
                    response));
                return true;
            }

            return false;
        }


        private static bool IsExposureCorrelationMatch(
            VisionProtocolResponse response,
            VisionInspectionEnvelope request,
            out string error)
        {
            error = string.Empty;
            if (response == null || request == null)
            {
                error = "EPD 또는 요청 문맥이 없습니다.";
                return false;
            }
            if (!string.Equals(response.Module, request.Camera, StringComparison.OrdinalIgnoreCase))
            {
                error = "CAMERA mismatch. expected=" + request.Camera + ", actual=" + response.Module;
                return false;
            }
            if (response.Fields == null || response.Fields.Length < 8)
            {
                error = "canonical EPD 공통 correlation 필드가 부족합니다.";
                return false;
            }

            int headIndex;
            int dieIndex;
            int channel;
            if (!string.Equals(response.Fields[0], request.Head, StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(response.Fields[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out headIndex) ||
                headIndex != request.HeadIndex ||
                !int.TryParse(response.Fields[2], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out dieIndex) ||
                dieIndex != request.DieIndex ||
                !int.TryParse(response.Fields[3], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out channel) ||
                channel != request.VisionChannel ||
                !string.Equals(response.Fields[4], request.WaferId, StringComparison.Ordinal) ||
                !string.Equals(response.Fields[5], request.RecipeId, StringComparison.Ordinal) ||
                !string.Equals(response.Fields[6], request.LotId, StringComparison.Ordinal) ||
                !string.Equals(response.GetValueAny("group_id", "groupId", "groupid"), request.GroupId, StringComparison.Ordinal) ||
                !string.Equals(response.GetValueAny("finder", "tool"), request.Finder, StringComparison.Ordinal))
            {
                error = "EPD 공통 correlation mismatch.";
                return false;
            }

            return true;
        }

        private static bool IsInspectionErrorCorrelationMatch(
            VisionProtocolResponse response,
            VisionInspectionEnvelope request,
            out string error)
        {
            error = string.Empty;
            if (response == null || request == null)
            {
                error = "ERR 또는 요청 문맥이 없습니다.";
                return false;
            }
            if (!string.Equals(response.Module, request.Camera, StringComparison.OrdinalIgnoreCase))
            {
                error = "ERR CAMERA mismatch. expected=" + request.Camera + ", actual=" + response.Module;
                return false;
            }
            if (!string.Equals(response.Command, request.Command, StringComparison.OrdinalIgnoreCase))
            {
                error = "ERR CMD mismatch. expected=" + request.Command + ", actual=" + response.Command;
                return false;
            }

            // Canonical ERR with META request_id also carries the common positional context.
            if (response.Fields != null && response.Fields.Length >= 9)
            {
                int headIndex;
                int dieIndex;
                int channel;
                if (!string.Equals(response.Fields[2], request.Head, StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(response.Fields[3], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out headIndex) ||
                    headIndex != request.HeadIndex ||
                    !int.TryParse(response.Fields[4], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out dieIndex) ||
                    dieIndex != request.DieIndex ||
                    !int.TryParse(response.Fields[5], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out channel) ||
                    channel != request.VisionChannel ||
                    !string.Equals(response.Fields[6], request.WaferId, StringComparison.Ordinal) ||
                    !string.Equals(response.Fields[7], request.RecipeId, StringComparison.Ordinal) ||
                    !string.Equals(response.Fields[8], request.LotId, StringComparison.Ordinal))
                {
                    error = "ERR 공통 correlation mismatch.";
                    return false;
                }
            }

            return true;
        }

        private void RemoveExposureWaiter(string requestId, InspectionExposureWaiter waiter)
        {
            lock (_inspectionWaiterLock)
            {
                InspectionExposureWaiter current;
                if (_exposureWaiters.TryGetValue(requestId, out current) && object.ReferenceEquals(current, waiter))
                    _exposureWaiters.Remove(requestId);
            }
        }

        private void CancelInspectionWaiters()
        {
            List<TaskCompletionSource<VisionProtocolResponse>> waiters = new List<TaskCompletionSource<VisionProtocolResponse>>();
            lock (_inspectionWaiterLock)
            {
                foreach (InspectionExposureWaiter waiter in _exposureWaiters.Values)
                    waiters.Add(waiter.Completion);
                _exposureWaiters.Clear();
            }

            for (int i = 0; i < waiters.Count; i++)
                waiters[i].TrySetException(new InvalidOperationException(
                    "Vision connection was disconnected while waiting for a correlated response."));

            // 연결 단절 시 이 채널의 미소비 푸시 결과와 스토어 대기자도 함께 정리한다.
            // 재연결 후 이전 검사 결과는 무효라는 정책 (요청/폴링 waiter 정리와 같은 위치).
            AutoVisionChannel channel;
            if (VisionModuleNames.TryResolveByModule(ModuleName, out channel))
            {
                string camera = VisionCameraNames.FromChannel(channel);
                if (!string.IsNullOrWhiteSpace(camera))
                    VisionInspectionResultStore.ClearChannel(
                        camera,
                        "Vision 연결 종료로 미소비 결과를 정리했습니다. module=" + ModuleName);
            }
        }
    }
}
