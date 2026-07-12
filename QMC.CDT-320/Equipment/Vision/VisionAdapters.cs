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
                if (IsDryRunMode())
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
    /// Bottom은 BottomInspection 채널을 사용하고, Side는 생성 시 전달받은 Front/Rear side 채널을 사용한다.
    /// <para>8콜렛 규약: 이 어댑터의 side 채널이 곧 콜렛 그룹(fb)이다 — FrontSide=fb0, RearSide=fb1.
    /// Vision 요청은 신형 고정 8파트("tool|fb|collet|die_index|channel|gridx;gridy")로 전송한다(키=die_index, 2026-07-06).</para>
    /// </summary>
    public class TpuVisionAdapter : IVisionTpuClient
    {
        private const double MatchScoreThreshold = 0.7;
        private readonly AutoVisionChannel _sideChannel;

        public TpuVisionAdapter()
            : this(AutoVisionChannel.BottomInspection)
        {
        }

        public TpuVisionAdapter(AutoVisionChannel sideChannel)
        {
            _sideChannel = sideChannel;
        }

        /// <summary>콜렛 그룹 — 0=Front / 1=Back(Rear). side 채널 기준(SSOT=유닛 생성부).</summary>
        private int Fb
        {
            get { return _sideChannel == AutoVisionChannel.RearSide ? 1 : 0; }
        }

        private string SideSurfaceInspector
        {
            get { return _sideChannel == AutoVisionChannel.RearSide ? VisionToolIds.RearSide.SurfaceInspector : VisionToolIds.FrontSide.SurfaceInspector; }
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

                int dieIndex, gridX, gridY;
                ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECTASYNC",
                    "Bottom 검사 시작 요청. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                return await AutoVisionRequestService.StartInspectColletAsync(
                    AutoVisionChannel.BottomInspection,
                    VisionToolIds.BottomInspection.SurfaceInspector,
                    Fb,
                    pickerNo,
                    dieIndex,
                    0,
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

                int dieIndex, gridX, gridY;
                ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECTRESULT",
                    "Bottom 검사 결과 대기. fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                InspectionResultDto inspection = await AutoVisionRequestService.WaitInspectResultByDieAsync(
                    AutoVisionChannel.BottomInspection,
                    VisionToolIds.BottomInspection.SurfaceInspector,
                    dieIndex,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                if (AutoVisionRequestService.IsInspectionResultTransportFailure(inspection))
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-BOTTOM-INSPECTRESULT",
                        "Bottom SurfaceInspector 결과 수신 실패. 검사 NG가 아니라 Vision ACK/RESULT 미수신입니다. fb=" + Fb +
                        ", collet=" + pickerNo +
                        ", dieIndex=" + dieIndex +
                        ", raw=" + (inspection != null ? inspection.Raw : "null"));
                    return null;
                }

                BottomVisionOffset offset = VisionCameraCalibrationTransform.ToBottomVisionOffset(pickerNo, inspection);
                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-BOTTOM-INSPECT-CAL",
                    "Bottom SurfaceInspector 결과 구조 적용. fb=" + Fb +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
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

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct)
        {
            int dieIndex, gridX, gridY;
            ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
            int ch = sideNo == 2 ? 1 : 0;
            string inspector = SideSurfaceInspector;
            EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-GRAB",
                "Side GRAB 요청 키 확인. camera=" + _sideChannel +
                ", inspector=" + inspector +
                ", fb=" + Fb +
                ", pickerNo=" + pickerNo +
                ", collet=" + pickerNo +
                ", dieIndex=" + dieIndex +
                ", ch=" + ch +
                ", grid=" + gridX + ";" + gridY +
                ", grabIndex=" + (pickerNo * 10 + sideNo) +
                ", timeoutMs=" + timeoutMs);

            return AutoVisionRequestService.GrabInspectAsync(
                _sideChannel,
                inspector,
                Fb,
                pickerNo,
                dieIndex,
                ch,
                gridX,
                gridY,
                timeoutMs,
                ct);
        }

        public async Task<bool> StartSideInspectAsync(int pickerNo, int angleDeg, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int dieIndex, gridX, gridY;
                ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
                int ch = angleDeg == 90 ? 1 : 0;
                string inspector = SideSurfaceInspector;

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                    "Side 검사 시작 단건 요청. camera=" + _sideChannel +
                    ", inspector=" + inspector +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
                    ", ch=" + ch +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                bool started = await AutoVisionRequestService.StartInspectColletAsync(
                    _sideChannel,
                    inspector,
                    Fb,
                    pickerNo,
                    dieIndex,
                    ch,
                    gridX,
                    gridY,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                if (!started)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                        "Side 검사 시작 ACK 수신 실패. 검사 NG가 아니라 Vision INSPECTASYNC STARTED 미수신입니다. camera=" + _sideChannel +
                        ", fb=" + Fb +
                        ", pickerNo=" + pickerNo +
                        ", collet=" + pickerNo +
                        ", dieIndex=" + dieIndex +
                        ", ch=" + ch +
                        ", grid=" + gridX + ";" + gridY +
                        ", timeoutMs=" + timeoutMs);
                }

                return started;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                    "Side 검사 시작 단건 요청 중 예외 발생. camera=" + _sideChannel +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", angleDeg=" + angleDeg +
                    ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        public async Task<SideVisionResult> WaitSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int dieIndex, gridX, gridY;
                ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTRESULT",
                    "Side 검사 결과 대기. camera=" + _sideChannel +
                    ", inspector=" + SideSurfaceInspector +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                InspectionResultDto inspection = await AutoVisionRequestService.WaitInspectResultByDieAsync(
                    _sideChannel,
                    SideSurfaceInspector,
                    dieIndex,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                if (AutoVisionRequestService.IsInspectionResultTransportFailure(inspection))
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTRESULT",
                        "Side 검사 결과 수신 실패. 검사 NG가 아니라 Vision ACK/RESULT 미수신입니다. camera=" + _sideChannel +
                        ", fb=" + Fb +
                        ", pickerNo=" + pickerNo +
                        ", collet=" + pickerNo +
                        ", dieIndex=" + dieIndex +
                        ", timeoutMs=" + timeoutMs +
                        ", raw=" + (inspection != null ? inspection.Raw : "null"));
                    return null;
                }

                bool pass = inspection != null && inspection.IsPass;
                return new SideVisionResult
                {
                    PickerNo = pickerNo,
                    Side1Ok = pass,
                    Side2Ok = pass,
                    Side3Ok = true,
                    Side4Ok = true,
                    Raw = inspection != null ? inspection.Raw : string.Empty,
                    Values = inspection != null && inspection.Values != null
                        ? new Dictionary<string, string>(inspection.Values, StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTRESULT",
                    "Side 검사 결과 대기 중 예외 발생. camera=" + _sideChannel +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
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
                return QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToInspectionResult(
                    AutoVisionChannel.Bin,
                    AutoVisionRequestService.BuildSimulationInspectionResult(AutoVisionChannel.Bin, VisionToolIds.Bin.PlacementInspector, slotIndex));

            try
            {
                if (IsDryRunMode())
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
