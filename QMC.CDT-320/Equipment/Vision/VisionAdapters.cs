using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// InputStageUnit에서 사용하는 Wafer/Input vision adapter.
    /// Unit은 공정 의미를 유지하고, 실제 TCP 요청은 AutoVisionRequestService가 담당한다.
    /// </summary>
    public class WaferVisionAdapter : IVisionTcpClient
    {
        private const double VisionPitchUnavailableMm = 0.0;
        private const double MatchScoreThreshold = 0.7;
        private const int DefaultTimeoutMs = 5000;

        public Task<bool> TriggerExposeAsync(int dieIndex)
        {
            return AutoVisionRequestService.GrabAsync(
                AutoVisionChannel.Wafer,
                dieIndex,
                DefaultTimeoutMs,
                CancellationToken.None);
        }

        public async Task<bool> GetResultAsync(int dieIndex, int timeoutMs = DefaultTimeoutMs)
        {
            try
            {
                MatchResultDto result = await AutoVisionRequestService.MatchAsync(
                    AutoVisionChannel.Wafer,
                    VisionToolIds.Wafer.DieFinder,
                    dieIndex,
                    timeoutMs,
                    CancellationToken.None).ConfigureAwait(false);

                bool ok = result != null && result.Success && result.Score >= MatchScoreThreshold;
                QMC.CDT_320.Equipment.Vision.WaferVisionResultStore.RecordDieCheck(ok);
                return ok;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public async Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId)
        {
            string finder = ResolveAlignFinder(alignTargetId);

            try
            {
                if (IsDryRunMode() && !AutoVisionRequestService.IsRealVisionInSimulationActive())
                {
                    await AutoVisionRequestService.GrabAsync(
                        AutoVisionChannel.Wafer,
                        0,
                        DefaultTimeoutMs,
                        CancellationToken.None).ConfigureAwait(false);
                }

                VisionAlignResult align = await AutoVisionRequestService.MatchAlignAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    VisionPitchUnavailableMm,
                    DefaultTimeoutMs,
                    CancellationToken.None).ConfigureAwait(false);

                if (align != null)
                    QMC.CDT_320.Equipment.Vision.WaferVisionResultStore.RecordAlign(alignTargetId, align);

                return align;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static bool IsDryRunMode()
        {
            QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
            return settings != null && settings.DryRunMode;
        }

        private static string ResolveAlignFinder(string alignTargetId)
        {
            return VisionAlignTargetIds.ResolveWaferFinder(alignTargetId);
        }
    }

    /// <summary>
    /// Picker bottom/side vision adapter.
    /// Bottom은 BottomInspection 채널을 사용하고, Side는 FrontSide EPD 후 RearSide를 직렬로 사용한다.
    /// <para>8콜렛 규약: fb는 Picker 그룹(0=Front, 1=Rear)이며 카메라 채널과 독립적이다.
    /// Side 0도는 channel=0, Side 90도는 channel=1을 두 카메라에 동일하게 전송한다.
    /// Vision 요청은 CAMERA부터 LOT_ID까지 12개 고정 필드와 request_id/group_id META를 사용한다.</para>
    /// </summary>
    public class TpuVisionAdapter : IVisionTpuClient
    {
        private const double MatchScoreThreshold = 0.7;
        private readonly int _fb;
        private readonly object _inspectionRequestLock = new object();
        private readonly Dictionary<int, VisionRequestHandle> _bottomInspectionRequests =
            new Dictionary<int, VisionRequestHandle>();
        private readonly Dictionary<int, SideInspectionRequestBatch> _sideInspectionRequests =
            new Dictionary<int, SideInspectionRequestBatch>();

        private sealed class SideInspectionRequestBatch
        {
            public string GroupId;
            public VisionRequestHandle Front0;
            public VisionRequestHandle Rear0;
            public VisionRequestHandle Front90;
            public VisionRequestHandle Rear90;
        }

        public TpuVisionAdapter()
            : this(0)
        {
        }

        public TpuVisionAdapter(int fb)
        {
            if (fb < 0 || fb > 1)
                throw new ArgumentOutOfRangeException("fb", "Picker group must be 0(Front) or 1(Rear).");

            _fb = fb;
        }

        public TpuVisionAdapter(AutoVisionChannel pickerGroupChannel)
            : this(pickerGroupChannel == AutoVisionChannel.RearSide ? 1 : 0)
        {
        }

        /// <summary>콜렛 그룹 — 0=Front / 1=Back(Rear). 카메라 채널과 독립적이다.</summary>
        private int Fb
        {
            get { return _fb; }
        }

        private static string ResolveSideSurfaceInspector(AutoVisionChannel cameraChannel)
        {
            return cameraChannel == AutoVisionChannel.RearSide
                ? VisionToolIds.RearSide.ChippingInspector
                : VisionToolIds.FrontSide.ChippingInspector;
        }

        private static int ResolveSideChannel(int angleDegOrSideNo)
        {
            return angleDegOrSideNo == 90 || angleDegOrSideNo == 2 ? 1 : 0;
        }

        /// <summary>콜렛의 비전 주소(die_index/grid) 조회 — 시퀀스가 <see cref="VisionDieAddressStore"/> 에
        /// 기록한 값을 사용, 없으면 콜렛별 고유 음수 합성키(-1~-8, 메뉴얼 취급 — 키 충돌 방지).</summary>
        private void ResolveDieAddress(int collet, out int dieIndex, out int gridX, out int gridY)
        {
            VisionDieAddress addr;
            if (VisionDieAddressStore.TryGet(Fb, collet, out addr))
            {
                dieIndex = addr.DieIndex; gridX = addr.GridX; gridY = addr.GridY;
                return;
            }
            dieIndex = VisionDieAddressStore.FallbackDieIndex(Fb, collet);
            gridX = -1; gridY = -1;
        }

        private bool TryResolveAutoDieAddress(int collet, out VisionDieAddress address)
        {
            address = null;
            if (!VisionDieAddressStore.TryGet(Fb, collet, out address) || address == null)
                return false;
            return address.DieIndex >= 0 && address.DieIndex <= 9999 &&
                   !string.IsNullOrWhiteSpace(address.DieId);
        }

        public Task<bool> TriggerBottomExposeAsync(int pickerNo = 0, int timeoutMs = 1000)
        {
            return TriggerBottomExposeAsync(pickerNo, timeoutMs, CancellationToken.None);
        }

        public Task<bool> TriggerBottomExposeAsync(int pickerNo = 0, int timeoutMs = 1000, CancellationToken ct = default)
        {
            int dieIndex, gridX, gridY;
            ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-GRAB",
                "Bottom GRAB 요청 키 확인. fb=" + Fb +
                ", pickerNo=" + pickerNo +
                ", collet=" + pickerNo +
                ", dieIndex=" + dieIndex +
                ", grid=" + gridX + ";" + gridY +
                ", timeoutMs=" + timeoutMs);

            return AutoVisionRequestService.GrabAsync(
                AutoVisionChannel.BottomInspection,
                pickerNo,
                timeoutMs,
                ct);
        }

        public async Task<bool> StartBottomInspectAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (pickerNo < 1 || pickerNo > 4)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECTASYNC",
                        "Bottom 검사 시작 요청 실패. Picker 번호가 올바르지 않습니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo);
                    return false;
                }

                VisionDieAddress address;
                if (!TryResolveAutoDieAddress(pickerNo, out address))
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-CONTEXT",
                        "Bottom 자동 검사 필수 자재 문맥이 없습니다. 음수 DIE_INDEX로 대체하지 않습니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo);
                    return false;
                }
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECTASYNC",
                    "Bottom 검사 시작 요청. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + address.DieIndex +
                    ", grid=" + address.GridX + ";" + address.GridY +
                    ", timeoutMs=" + timeoutMs);

                VisionInspectionRequestContext context = VisionInspectionContextFactory.CreateAuto(
                    AutoVisionChannel.BottomInspection,
                    VisionToolIds.BottomInspection.SurfaceInspector,
                    Fb,
                    pickerNo,
                    address.DieIndex,
                    address.GridX,
                    address.GridY,
                    0,
                    address.DieId,
                    address.WaferId,
                    VisionInspectionOperations.Inspect,
                    VisionResultTimings.Deferred,
                    string.Empty);
                VisionRequestHandle handle = await AutoVisionRequestService.StartInspectionRequestAsync(
                    context,
                    timeoutMs,
                    ct).ConfigureAwait(false);
                if (handle == null)
                    return false;

                lock (_inspectionRequestLock)
                {
                    VisionRequestHandle stale;
                    if (_bottomInspectionRequests.TryGetValue(pickerNo, out stale) && stale != null)
                        stale.MarkError("같은 Picker의 새 Bottom 요청으로 교체되었습니다.");
                    _bottomInspectionRequests[pickerNo] = handle;
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECTASYNC",
                    "Bottom 검사 시작 요청 중 예외 발생. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        public async Task<BottomVisionOffset> WaitBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            VisionRequestHandle handle = null;
            try
            {
                ct.ThrowIfCancellationRequested();

                if (pickerNo < 1 || pickerNo > 4)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECTRESULT",
                        "Bottom 검사 결과 대기 실패. Picker 번호가 올바르지 않습니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo);
                    return null;
                }

                lock (_inspectionRequestLock)
                    _bottomInspectionRequests.TryGetValue(pickerNo, out handle);
                if (handle == null)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-RESULT-HANDLE",
                        "Bottom 결과와 연결할 요청 Handle이 없습니다. fb=" + Fb + ", pickerNo=" + pickerNo);
                    return null;
                }
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECTRESULT",
                    "Bottom 검사 결과 대기. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + handle.Request.DieIndex +
                    ", requestId=" + handle.Request.RequestId +
                    ", groupId=" + handle.Request.GroupId +
                    ", timeoutMs=" + timeoutMs);

                InspectionResultDto inspection = await AutoVisionRequestService.CompleteBottomInspectionAsync(
                    handle,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                if (AutoVisionRequestService.IsInspectionResultTransportFailure(inspection))
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECTRESULT",
                        "Bottom SurfaceInspector 결과 수신 실패. 검사 NG가 아니라 Vision ACK/RESULT 미수신입니다. fb=" + Fb +
                        ", collet=" + pickerNo +
                        ", dieIndex=" + handle.Request.DieIndex +
                        ", groupId=" + handle.Request.GroupId +
                        ", raw=" + (inspection != null ? inspection.Raw : "null"));
                    return null;
                }

                BottomVisionOffset offset = VisionCameraCalibrationTransform.ToBottomVisionOffset(pickerNo, inspection);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECT-CAL",
                    "Bottom SurfaceInspector 결과 구조 적용. fb=" + Fb +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + handle.Request.DieIndex +
                    ", ok=" + (offset != null && offset.IsOk) +
                    ", rawValues=" + (inspection != null ? inspection.DescribeValues() : "null") +
                    ", bottomCenterOffsetXmm=" + (offset != null ? offset.BottomCenterOffsetX.ToString("F6") : "null") +
                    ", bottomCenterOffsetYmm=" + (offset != null ? offset.BottomCenterOffsetY.ToString("F6") : "null") +
                    ", bottomCenterOffsetValid=" + (offset != null && offset.HasBottomCenterOffset) +
                    ", side0Source=BottomCenterX" +
                    ", side90Source=BottomCenterY");

                return offset;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECTRESULT",
                    "Bottom 검사 결과 대기 중 예외 발생. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
                return null;
            }
            finally
            {
                if (handle != null)
                {
                    lock (_inspectionRequestLock)
                    {
                        VisionRequestHandle current;
                        if (_bottomInspectionRequests.TryGetValue(pickerNo, out current) &&
                            object.ReferenceEquals(current, handle))
                            _bottomInspectionRequests.Remove(pickerNo);
                    }
                }
            }
        }

        public async Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs = 5000)
        {
            return await GetBottomResultAsync(pickerNo, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<BottomVisionOffset> GetBottomResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (pickerNo < 1 || pickerNo > 4)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECT-RESULT",
                        "Bottom 검사 결과 단건 요청 실패. Picker 번호가 올바르지 않습니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo);
                    return null;
                }

                int dieIndex, gridX, gridY;
                ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECT-REQUEST",
                    "Bottom 검사 결과 단건 요청. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                return await AutoVisionRequestService.InspectBottomOffsetAsync(
                    Fb,
                    pickerNo,
                    VisionToolIds.BottomInspection.SurfaceInspector,
                    dieIndex,
                    gridX,
                    gridY,
                    timeoutMs,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECT-RESULT",
                    "Bottom 검사 결과 단건 수신 중 예외 발생. 검사 NG가 아니라 Vision 결과 미수신입니다. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        public async Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000)
        {
            return await GetBottomResultsAsync(timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs, CancellationToken ct)
        {
            var results = new BottomVisionOffset[4];

            for (int i = 0; i < 4; i++)
            {
                int collet = i + 1;
                try
                {
                    ct.ThrowIfCancellationRequested();

                    results[i] = await GetBottomResultAsync(collet, timeoutMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECT-RESULT",
                        "Bottom 검사 결과 수신 중 예외 발생. 검사 NG가 아니라 Vision 결과 미수신입니다. fb=" + Fb +
                        ", collet=" + collet +
                        ", error=" + ex.Message);
                    results[i] = null;
                }
                finally
                {
                }
            }

            return results;
        }

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000)
        {
            return TriggerSideExposeAsync(pickerNo, sideNo, timeoutMs, CancellationToken.None);
        }

        public async Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct)
        {
            int dieIndex, gridX, gridY;
            ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
            int ch = ResolveSideChannel(sideNo);
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-GRAB",
                "Side 양쪽 카메라 GRAB 요청 키 확인. cameras=FrontSide,RearSide" +
                ", fb=" + Fb +
                ", pickerNo=" + pickerNo +
                ", collet=" + pickerNo +
                ", dieIndex=" + dieIndex +
                ", ch=" + ch +
                ", grid=" + gridX + ";" + gridY +
                ", grabIndex=" + (pickerNo * 10 + sideNo) +
                ", timeoutMs=" + timeoutMs);

            int grabIndex = pickerNo * 10 + sideNo;
            bool frontResult = await AutoVisionRequestService.GrabAsync(
                AutoVisionChannel.FrontSide,
                grabIndex,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (!frontResult)
                return false;
            bool rearResult = await AutoVisionRequestService.GrabAsync(
                AutoVisionChannel.RearSide,
                grabIndex,
                timeoutMs,
                ct).ConfigureAwait(false);
            return rearResult;
        }

        public async Task<bool> StartSideInspectAsync(int pickerNo, int angleDeg, int timeoutMs, CancellationToken ct)
        {
            SideInspectionRequestBatch batch = null;
            try
            {
                ct.ThrowIfCancellationRequested();

                VisionDieAddress address;
                if (!TryResolveAutoDieAddress(pickerNo, out address))
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-CONTEXT",
                        "Side 자동 검사 필수 자재 문맥이 없습니다. 음수 DIE_INDEX로 대체하지 않습니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo + ", angleDeg=" + angleDeg);
                    return false;
                }
                int ch = ResolveSideChannel(angleDeg);
                lock (_inspectionRequestLock)
                {
                    if (ch == 0)
                    {
                        SideInspectionRequestBatch stale;
                        if (_sideInspectionRequests.TryGetValue(pickerNo, out stale) && stale != null)
                            MarkSideBatchError(stale, "같은 Picker의 새 Side 요청으로 교체되었습니다.");
                        batch = new SideInspectionRequestBatch { GroupId = VisionCorrelationIdGenerator.NewGroupId() };
                        _sideInspectionRequests[pickerNo] = batch;
                    }
                    else if (!_sideInspectionRequests.TryGetValue(pickerNo, out batch) ||
                             batch == null || batch.Front0 == null || batch.Rear0 == null)
                    {
                        batch = null;
                    }
                }
                if (batch == null)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-ORDER",
                        "Side 90도 요청 전에 Front/Rear 0도 EPD가 모두 완료되지 않았습니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo);
                    return false;
                }

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                    "Side 카메라를 Front 후 Rear 순서로 검사 요청합니다." +
                    ", frontInspector=" + ResolveSideSurfaceInspector(AutoVisionChannel.FrontSide) +
                    ", rearInspector=" + ResolveSideSurfaceInspector(AutoVisionChannel.RearSide) +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + address.DieIndex +
                    ", ch=" + ch +
                    ", grid=" + address.GridX + ";" + address.GridY +
                    ", groupId=" + batch.GroupId +
                    ", timeoutMs=" + timeoutMs);

                VisionInspectionRequestContext frontContext = VisionInspectionContextFactory.CreateAuto(
                    AutoVisionChannel.FrontSide,
                    ResolveSideSurfaceInspector(AutoVisionChannel.FrontSide),
                    Fb, pickerNo, address.DieIndex, address.GridX, address.GridY, ch,
                    address.DieId, address.WaferId, VisionInspectionOperations.Inspect,
                    VisionResultTimings.Deferred, batch.GroupId);
                VisionRequestHandle frontHandle = await AutoVisionRequestService.StartInspectionRequestAsync(
                    frontContext, timeoutMs, ct).ConfigureAwait(false);
                if (frontHandle == null)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                        "Side Front EPD 수신 실패. Rear 요청을 전송하지 않습니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo + ", channel=" + ch + ", groupId=" + batch.GroupId);
                    MarkSideBatchError(batch, "Side Front EPD 수신 실패");
                    RemoveSideBatchIfCurrent(pickerNo, batch);
                    return false;
                }

                lock (_inspectionRequestLock)
                {
                    if (ch == 0) batch.Front0 = frontHandle;
                    else batch.Front90 = frontHandle;
                }

                VisionInspectionRequestContext rearContext = VisionInspectionContextFactory.CreateAuto(
                    AutoVisionChannel.RearSide,
                    ResolveSideSurfaceInspector(AutoVisionChannel.RearSide),
                    Fb, pickerNo, address.DieIndex, address.GridX, address.GridY, ch,
                    address.DieId, address.WaferId, VisionInspectionOperations.Inspect,
                    VisionResultTimings.Deferred, batch.GroupId);
                VisionRequestHandle rearHandle = await AutoVisionRequestService.StartInspectionRequestAsync(
                    rearContext, timeoutMs, ct).ConfigureAwait(false);
                if (rearHandle == null)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                        "Side Rear EPD 수신 실패. fb=" + Fb +
                        ", pickerNo=" + pickerNo + ", channel=" + ch + ", groupId=" + batch.GroupId);
                    MarkSideBatchError(batch, "Side Rear EPD 수신 실패");
                    RemoveSideBatchIfCurrent(pickerNo, batch);
                    return false;
                }

                lock (_inspectionRequestLock)
                {
                    if (ch == 0) batch.Rear0 = rearHandle;
                    else batch.Rear90 = rearHandle;
                }

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                    "Side Front→Rear EPD 순차 수신 완료. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", dieIndex=" + address.DieIndex +
                    ", channel=" + ch +
                    ", groupId=" + batch.GroupId +
                    ", frontReqToEpdMs=" + frontHandle.RequestToExposureDoneMs.ToString("F3") +
                    ", rearReqToEpdMs=" + rearHandle.RequestToExposureDoneMs.ToString("F3") +
                    ", frontEpdToRearReqMs=" + VisionRequestHandle.ElapsedMilliseconds(
                        frontHandle.ExposureDoneTimestamp,
                        rearHandle.RequestTxTimestamp).ToString("F3") +
                    (ch == 1 && batch.Front0 != null
                        ? ", pickerCaptureTotalMs=" + VisionRequestHandle.ElapsedMilliseconds(
                            batch.Front0.RequestTxTimestamp,
                            rearHandle.ExposureDoneTimestamp).ToString("F3")
                        : string.Empty));
                return true;
            }
            catch (OperationCanceledException)
            {
                if (batch != null)
                {
                    MarkSideBatchError(batch, "Side 검사 시작이 취소되었습니다.");
                    RemoveSideBatchIfCurrent(pickerNo, batch);
                }
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                    "Side 양쪽 카메라 검사 시작 요청 중 예외 발생. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", angleDeg=" + angleDeg +
                    ", error=" + ex.Message);
                if (batch != null)
                {
                    MarkSideBatchError(batch, ex.Message);
                    RemoveSideBatchIfCurrent(pickerNo, batch);
                }
                return false;
            }
            finally
            {
            }
        }

        public async Task<SideVisionResult> WaitSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            SideInspectionRequestBatch batch = null;
            try
            {
                ct.ThrowIfCancellationRequested();

                lock (_inspectionRequestLock)
                    _sideInspectionRequests.TryGetValue(pickerNo, out batch);
                if (batch == null || batch.Front0 == null || batch.Rear0 == null ||
                    batch.Front90 == null || batch.Rear90 == null)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-RESULT-HANDLE",
                        "Side RESULT 전에 Front0/Rear0/Front90/Rear90 EPD가 모두 필요합니다. fb=" + Fb +
                        ", pickerNo=" + pickerNo);
                    return null;
                }

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTRESULT",
                    "Side 양쪽 카메라 검사 결과 대기. cameras=FrontSide,RearSide" +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + batch.Front90.Request.DieIndex +
                    ", groupId=" + batch.GroupId +
                    ", timeoutMs=" + timeoutMs);

                Task<VisionInspectionResult> frontTask = AutoVisionRequestService.WaitInspectionStageAsync(
                    batch.Front90, VisionInspectionCommands.Result, timeoutMs, ct);
                Task<VisionInspectionResult> rearTask = AutoVisionRequestService.WaitInspectionStageAsync(
                    batch.Rear90, VisionInspectionCommands.Result, timeoutMs, ct);

                VisionInspectionResult[] correlatedResults = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                if (correlatedResults.Length > 0 && correlatedResults[0] != null && batch.Front0 != null)
                    batch.Front0.MarkStageDone(VisionInspectionCommands.Result, correlatedResults[0]);
                if (correlatedResults.Length > 1 && correlatedResults[1] != null && batch.Rear0 != null)
                    batch.Rear0.MarkStageDone(VisionInspectionCommands.Result, correlatedResults[1]);
                InspectionResultDto frontInspection = correlatedResults.Length > 0 && correlatedResults[0] != null
                    ? correlatedResults[0].InspectionResult
                    : null;
                InspectionResultDto rearInspection = correlatedResults.Length > 1 && correlatedResults[1] != null
                    ? correlatedResults[1].InspectionResult
                    : null;
                bool frontTransportFailed = AutoVisionRequestService.IsInspectionResultTransportFailure(frontInspection);
                bool rearTransportFailed = AutoVisionRequestService.IsInspectionResultTransportFailure(rearInspection);
                if (frontTransportFailed || rearTransportFailed)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTRESULT",
                        "Side 검사 결과 수신 실패. 두 카메라 중 INSPECTRESULT 미수신이 있습니다. " +
                        "frontMissing=" + frontTransportFailed +
                        ", rearMissing=" + rearTransportFailed +
                        ", fb=" + Fb +
                        ", pickerNo=" + pickerNo +
                        ", collet=" + pickerNo +
                        ", dieIndex=" + batch.Front90.Request.DieIndex +
                        ", groupId=" + batch.GroupId +
                        ", timeoutMs=" + timeoutMs +
                        ", frontRaw=" + (frontInspection != null ? frontInspection.Raw : "null") +
                        ", rearRaw=" + (rearInspection != null ? rearInspection.Raw : "null"));
                    return null;
                }

                bool frontPass = frontInspection != null && frontInspection.IsPass;
                bool rearPass = rearInspection != null && rearInspection.IsPass;
                Dictionary<string, string> values = MergeSideInspectionValues(frontInspection, rearInspection);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTRESULT",
                    "Side 양쪽 카메라 집계 결과 수신 완료. fb=" + Fb +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + batch.Front90.Request.DieIndex +
                    ", groupId=" + batch.GroupId +
                    ", frontPass=" + frontPass +
                    ", rearPass=" + rearPass +
                    ", allPass=" + (frontPass && rearPass) +
                    ", frontRaw=" + (frontInspection != null ? frontInspection.Raw : string.Empty) +
                    ", rearRaw=" + (rearInspection != null ? rearInspection.Raw : string.Empty));
                var sideResult = new SideVisionResult
                {
                    PickerNo = pickerNo,
                    Side1Ok = frontPass,
                    Side2Ok = rearPass,
                    Side3Ok = frontPass,
                    Side4Ok = rearPass,
                    Raw = "FrontSide=" + (frontInspection != null ? frontInspection.Raw : string.Empty) +
                          " | RearSide=" + (rearInspection != null ? rearInspection.Raw : string.Empty),
                    Values = values
                };
                return sideResult;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTRESULT",
                    "Side 양쪽 카메라 검사 결과 대기 중 예외 발생. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
                return null;
            }
            finally
            {
                if (batch != null)
                {
                    MarkIncompleteSideBatchHandles(batch, "Side RESULT Collection이 완료되지 않았습니다.");
                    RemoveSideBatchIfCurrent(pickerNo, batch);
                }
            }
        }

        public void AbandonPendingInspection(int pickerNo, string reason)
        {
            VisionRequestHandle bottom = null;
            SideInspectionRequestBatch side = null;
            lock (_inspectionRequestLock)
            {
                if (_bottomInspectionRequests.TryGetValue(pickerNo, out bottom))
                    _bottomInspectionRequests.Remove(pickerNo);
                if (_sideInspectionRequests.TryGetValue(pickerNo, out side))
                    _sideInspectionRequests.Remove(pickerNo);
            }

            string error = string.IsNullOrWhiteSpace(reason)
                ? "Vision 검사 요청이 완료 전에 폐기되었습니다."
                : reason;
            if (bottom != null && !bottom.IsResultDone && string.IsNullOrWhiteSpace(bottom.Error))
                bottom.MarkError(error);
            MarkIncompleteSideBatchHandles(side, error);

            if (bottom != null || side != null)
            {
                EventLogger.Write(EventKind.Warning, "VISION", "AUTO-VISION-PENDING-ABANDON",
                    "미완료 Vision Handle을 정리했습니다. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", bottom=" + (bottom != null) +
                    ", side=" + (side != null) +
                    ", reason=" + error);
            }
        }

        private void RemoveSideBatchIfCurrent(int pickerNo, SideInspectionRequestBatch batch)
        {
            if (batch == null)
                return;
            lock (_inspectionRequestLock)
            {
                SideInspectionRequestBatch current;
                if (_sideInspectionRequests.TryGetValue(pickerNo, out current) &&
                    object.ReferenceEquals(current, batch))
                    _sideInspectionRequests.Remove(pickerNo);
            }
        }

        private static void MarkSideBatchError(SideInspectionRequestBatch batch, string error)
        {
            if (batch == null)
                return;
            VisionRequestHandle[] handles = { batch.Front0, batch.Rear0, batch.Front90, batch.Rear90 };
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i] != null)
                    handles[i].MarkError(error);
            }
        }

        private static void MarkIncompleteSideBatchHandles(SideInspectionRequestBatch batch, string error)
        {
            if (batch == null)
                return;
            VisionRequestHandle[] handles = { batch.Front0, batch.Rear0, batch.Front90, batch.Rear90 };
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i] != null && !handles[i].IsResultDone && string.IsNullOrWhiteSpace(handles[i].Error))
                    handles[i].MarkError(error);
            }
        }

        private static Dictionary<string, string> MergeSideInspectionValues(
            InspectionResultDto frontInspection,
            InspectionResultDto rearInspection)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AppendSideInspectionValues(values, "FrontSide", frontInspection);
            AppendSideInspectionValues(values, "RearSide", rearInspection);
            return values;
        }

        private static void AppendSideInspectionValues(
            Dictionary<string, string> target,
            string prefix,
            InspectionResultDto inspection)
        {
            if (target == null || inspection == null)
                return;

            target[prefix + ".Pass"] = inspection.IsPass ? "1" : "0";
            if (inspection.Values == null)
                return;

            foreach (KeyValuePair<string, string> pair in inspection.Values)
                target[prefix + "." + pair.Key] = pair.Value;
        }

        public async Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000)
        {
            return await GetSideResultAsync(pickerNo, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            try
            {
                bool side0Started = await StartSideInspectAsync(pickerNo, 0, timeoutMs, ct).ConfigureAwait(false);
                if (!side0Started)
                    return null;

                bool side90Started = await StartSideInspectAsync(pickerNo, 90, timeoutMs, ct).ConfigureAwait(false);
                if (!side90Started)
                    return null;

                return await WaitSideResultAsync(pickerNo, timeoutMs, ct).ConfigureAwait(false);
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

    public static class TpuVisionAutoBatchCoordinator
    {
        public static async Task<Tuple<BottomVisionOffset[], SideVisionResult[]>> RunAsync(
            IVisionTpuClient vision,
            int fb,
            bool[] loadedPickers,
            Func<int, int> timeoutResolver,
            CancellationToken ct)
        {
            var bottomResults = new BottomVisionOffset[4];
            var sideResults = new SideVisionResult[4];
            if (vision == null)
                return Tuple.Create(bottomResults, sideResults);

            var loaded = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                if (loadedPickers == null || (i < loadedPickers.Length && loadedPickers[i]))
                    loaded.Add(i + 1);
            }
            if (loaded.Count == 0)
                return Tuple.Create(bottomResults, sideResults);

            bool fullFourPicker = loaded.Count == 4 && HasValidAutoContext(fb, 1) &&
                                  HasValidAutoContext(fb, 2) && HasValidAutoContext(fb, 3) &&
                                  HasValidAutoContext(fb, 4);
            var capturedSideOrder = new List<int>();

            if (fullFourPicker)
            {
                int[] firstBottomOrder = { 4, 3, 2 };
                for (int i = 0; i < firstBottomOrder.Length; i++)
                {
                    int pickerNo = firstBottomOrder[i];
                    bottomResults[pickerNo - 1] = await CompleteBottomAsync(
                        vision, pickerNo, ResolveTimeout(timeoutResolver, pickerNo), ct).ConfigureAwait(false);
                    if (bottomResults[pickerNo - 1] == null)
                        return Tuple.Create(bottomResults, sideResults);
                }

                // 특수 중첩 진입 직전에 P1/P4 자재 문맥을 다시 확인한다.
                bool overlapStillValid = HasValidAutoContext(fb, 1) && HasValidAutoContext(fb, 4);
                if (overlapStillValid)
                {
                    int bottomTimeout = ResolveTimeout(timeoutResolver, 1);
                    int sideTimeout = ResolveTimeout(timeoutResolver, 4);
                    Task<BottomVisionOffset> bottomP1Task = CompleteBottomAsync(vision, 1, bottomTimeout, ct);
                    Task<bool> sideP4Task = CaptureSideAsync(vision, 4, sideTimeout, ct);
                    await Task.WhenAll(new Task[] { bottomP1Task, sideP4Task }).ConfigureAwait(false);
                    bottomResults[0] = bottomP1Task.Result;
                    if (sideP4Task.Result)
                        capturedSideOrder.Add(4);
                    if (bottomResults[0] == null || !sideP4Task.Result)
                    {
                        await CollectCapturedSideResultsAsync(
                            vision, capturedSideOrder, sideResults, timeoutResolver, ct).ConfigureAwait(false);
                        return Tuple.Create(bottomResults, sideResults);
                    }

                    int[] remainingSideOrder = { 3, 2, 1 };
                    for (int i = 0; i < remainingSideOrder.Length; i++)
                    {
                        int pickerNo = remainingSideOrder[i];
                        if (!await CaptureSideAsync(
                            vision, pickerNo, ResolveTimeout(timeoutResolver, pickerNo), ct).ConfigureAwait(false))
                        {
                            await CollectCapturedSideResultsAsync(
                                vision, capturedSideOrder, sideResults, timeoutResolver, ct).ConfigureAwait(false);
                            return Tuple.Create(bottomResults, sideResults);
                        }
                        capturedSideOrder.Add(pickerNo);
                    }
                }
                else
                {
                    bottomResults[0] = await CompleteBottomAsync(
                        vision, 1, ResolveTimeout(timeoutResolver, 1), ct).ConfigureAwait(false);
                    if (bottomResults[0] == null)
                        return Tuple.Create(bottomResults, sideResults);
                }
            }
            else
            {
                // 부분 적재는 기존 일반 순서만 사용하고 Bottom/Side 중첩을 적용하지 않는다.
                for (int i = 0; i < loaded.Count; i++)
                {
                    int pickerNo = loaded[i];
                    bottomResults[pickerNo - 1] = await CompleteBottomAsync(
                        vision, pickerNo, ResolveTimeout(timeoutResolver, pickerNo), ct).ConfigureAwait(false);
                    if (bottomResults[pickerNo - 1] == null)
                        return Tuple.Create(bottomResults, sideResults);
                }
            }

            if (capturedSideOrder.Count == 0)
            {
                for (int i = 0; i < loaded.Count; i++)
                {
                    int pickerNo = loaded[i];
                    if (!await CaptureSideAsync(
                        vision, pickerNo, ResolveTimeout(timeoutResolver, pickerNo), ct).ConfigureAwait(false))
                    {
                        await CollectCapturedSideResultsAsync(
                            vision, capturedSideOrder, sideResults, timeoutResolver, ct).ConfigureAwait(false);
                        return Tuple.Create(bottomResults, sideResults);
                    }
                    capturedSideOrder.Add(pickerNo);
                }
            }

            // 모든 Side 촬영 EPD 완료 이후에만 RESULT를 수집한다. 한 건 실패해도 나머지 Handle은 회수한다.
            await CollectCapturedSideResultsAsync(
                vision, capturedSideOrder, sideResults, timeoutResolver, ct).ConfigureAwait(false);

            return Tuple.Create(bottomResults, sideResults);
        }

        private static async Task<BottomVisionOffset> CompleteBottomAsync(
            IVisionTpuClient vision,
            int pickerNo,
            int timeoutMs,
            CancellationToken ct)
        {
            bool started = await vision.StartBottomInspectAsync(pickerNo, timeoutMs, ct).ConfigureAwait(false);
            if (!started)
                return null;
            return await vision.WaitBottomResultAsync(pickerNo, timeoutMs, ct).ConfigureAwait(false);
        }

        private static async Task<bool> CaptureSideAsync(
            IVisionTpuClient vision,
            int pickerNo,
            int timeoutMs,
            CancellationToken ct)
        {
            if (!await vision.StartSideInspectAsync(pickerNo, 0, timeoutMs, ct).ConfigureAwait(false))
                return false;
            return await vision.StartSideInspectAsync(pickerNo, 90, timeoutMs, ct).ConfigureAwait(false);
        }

        private static async Task CollectCapturedSideResultsAsync(
            IVisionTpuClient vision,
            IList<int> capturedSideOrder,
            SideVisionResult[] sideResults,
            Func<int, int> timeoutResolver,
            CancellationToken ct)
        {
            if (vision == null || capturedSideOrder == null || sideResults == null)
                return;
            for (int i = 0; i < capturedSideOrder.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                int pickerNo = capturedSideOrder[i];
                if (pickerNo < 1 || pickerNo > sideResults.Length)
                    continue;
                sideResults[pickerNo - 1] = await vision.WaitSideResultAsync(
                    pickerNo,
                    ResolveTimeout(timeoutResolver, pickerNo),
                    ct).ConfigureAwait(false);
            }
        }

        private static bool HasValidAutoContext(int fb, int pickerNo)
        {
            VisionDieAddress address;
            if (!VisionDieAddressStore.TryGet(fb, pickerNo, out address) || address == null ||
                address.DieIndex < 0 || address.DieIndex > 9999 ||
                string.IsNullOrWhiteSpace(address.DieId))
                return false;

            VisionInspectionRequestContext context = VisionInspectionContextFactory.CreateAuto(
                AutoVisionChannel.BottomInspection,
                VisionToolIds.BottomInspection.SurfaceInspector,
                fb,
                pickerNo,
                address.DieIndex,
                address.GridX,
                address.GridY,
                0,
                address.DieId,
                address.WaferId,
                VisionInspectionOperations.Inspect,
                VisionResultTimings.Deferred,
                string.Empty);
            string validationError;
            return VisionInspectionEnvelope.Create(context).Validate(out validationError);
        }

        private static int ResolveTimeout(Func<int, int> timeoutResolver, int pickerNo)
        {
            int timeout = timeoutResolver != null ? timeoutResolver(pickerNo) : 5000;
            return timeout > 0 ? timeout : 5000;
        }
    }

    public static class BinVisionHelper
    {
        public static Task<InspectionResultDto> CheckPlacementAsync(int slotIndex, int timeoutMs = 3000)
        {
            return CheckPlacementAsync(slotIndex, timeoutMs, CancellationToken.None);
        }

        public static async Task<InspectionResultDto> CheckPlacementAsync(int slotIndex, int timeoutMs, CancellationToken ct)
        {
            // 비전 미사용(UseVision=false) — Bin 배치검사를 수행하지 않고 PASS 통과(연결 불필요).
            if (QMC.CDT320.AppSettingsStore.Current != null && !QMC.CDT320.AppSettingsStore.Current.UseVision)
                return QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToInspectionResult(
                    AutoVisionChannel.Bin,
                    AutoVisionRequestService.BuildSimulationInspectionResult(AutoVisionChannel.Bin, VisionToolIds.Bin.PlacementInspector, slotIndex));
            if (VisionHub.Bin == null || !VisionHub.Bin.IsConnected)
            {
                if (AutoVisionRequestService.IsRealVisionInSimulationActive())
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BIN-NOT-CONNECTED",
                        "Simulation 실제 Vision 사용 중 Bin Vision이 연결되지 않아 배치 검사를 수행할 수 없습니다. slotIndex=" + slotIndex);
                    return new InspectionResultDto
                    {
                        IsPass = false,
                        Raw = "Simulation real Vision is enabled, but Bin Vision is not connected."
                    };
                }

                return QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToInspectionResult(
                    AutoVisionChannel.Bin,
                    AutoVisionRequestService.BuildSimulationInspectionResult(AutoVisionChannel.Bin, VisionToolIds.Bin.PlacementInspector, slotIndex));
            }

            try
            {
                if (IsDryRunMode() && !AutoVisionRequestService.IsRealVisionInSimulationActive())
                {
                    await AutoVisionRequestService.GrabAsync(
                        AutoVisionChannel.Bin,
                        slotIndex,
                        timeoutMs,
                        ct).ConfigureAwait(false);

                    return QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToInspectionResult(
                        AutoVisionChannel.Bin,
                        AutoVisionRequestService.BuildSimulationInspectionResult(AutoVisionChannel.Bin, VisionToolIds.Bin.PlacementInspector, slotIndex));
                }

                // 현재 기준: Output 안착 검사는 Vision Bin 시퀀스와 동일하게 PlacementInspector INSPECT를 사용한다.
                InspectionResultDto inspection = await AutoVisionRequestService.InspectCalibratedAsync(
                    AutoVisionChannel.Bin,
                    VisionToolIds.Bin.PlacementInspector,
                    slotIndex,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                if (inspection != null)
                {
                    inspection.SetValue("placement_inspector", VisionToolIds.Bin.PlacementInspector);
                    inspection.SetValue("placement_slot_index", slotIndex);
                }

                return inspection;
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

        private static bool IsDryRunMode()
        {
            QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
            return settings != null && settings.DryRunMode;
        }
    }
}
