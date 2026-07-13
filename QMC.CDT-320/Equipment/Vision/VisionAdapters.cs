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
    /// Bottom은 BottomInspection 채널을 사용하고, Side는 FrontSide/RearSide 두 카메라를 동시에 사용한다.
    /// <para>8콜렛 규약: fb는 Picker 그룹(0=Front, 1=Rear)이며 카메라 채널과 독립적이다.
    /// Side 0도는 channel=0, Side 90도는 channel=1을 두 카메라에 동일하게 전송한다.
    /// Vision 요청은 신형 고정 8파트("tool|fb|collet|die_index|channel|gridx;gridy")로 전송한다(키=die_index, 2026-07-06).</para>
    /// </summary>
    public class TpuVisionAdapter : IVisionTpuClient
    {
        private const double MatchScoreThreshold = 0.7;
        private readonly int _fb;

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
                ? VisionToolIds.RearSide.SurfaceInspector
                : VisionToolIds.FrontSide.SurfaceInspector;
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

            Task<bool> frontTask = AutoVisionRequestService.GrabInspectAsync(
                AutoVisionChannel.FrontSide,
                ResolveSideSurfaceInspector(AutoVisionChannel.FrontSide),
                Fb, pickerNo, dieIndex, ch, gridX, gridY, timeoutMs, ct);
            Task<bool> rearTask = AutoVisionRequestService.GrabInspectAsync(
                AutoVisionChannel.RearSide,
                ResolveSideSurfaceInspector(AutoVisionChannel.RearSide),
                Fb, pickerNo, dieIndex, ch, gridX, gridY, timeoutMs, ct);

            bool[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
            return results.Length == 2 && results[0] && results[1];
        }

        public async Task<bool> StartSideInspectAsync(int pickerNo, int angleDeg, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int dieIndex, gridX, gridY;
                ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);
                int ch = ResolveSideChannel(angleDeg);

                EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                    "Side 양쪽 카메라 검사 시작 요청. cameras=FrontSide,RearSide" +
                    ", frontInspector=" + ResolveSideSurfaceInspector(AutoVisionChannel.FrontSide) +
                    ", rearInspector=" + ResolveSideSurfaceInspector(AutoVisionChannel.RearSide) +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
                    ", ch=" + ch +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                Task<bool> frontTask = AutoVisionRequestService.StartInspectColletAsync(
                    AutoVisionChannel.FrontSide,
                    ResolveSideSurfaceInspector(AutoVisionChannel.FrontSide),
                    Fb, pickerNo, dieIndex, ch, gridX, gridY, timeoutMs, ct);
                Task<bool> rearTask = AutoVisionRequestService.StartInspectColletAsync(
                    AutoVisionChannel.RearSide,
                    ResolveSideSurfaceInspector(AutoVisionChannel.RearSide),
                    Fb, pickerNo, dieIndex, ch, gridX, gridY, timeoutMs, ct);

                bool[] startResults = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                bool started = startResults.Length == 2 && startResults[0] && startResults[1];

                if (!started)
                {
                    EventLogger.Write(EventKind.Alarm, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                        "Side 검사 시작 EPD 수신 실패. 두 카메라 중 촬상 완료 미수신이 있습니다. " +
                        "frontEpd=" + (startResults.Length > 0 && startResults[0]) +
                        ", rearEpd=" + (startResults.Length > 1 && startResults[1]) +
                        ", fb=" + Fb +
                        ", pickerNo=" + pickerNo +
                        ", collet=" + pickerNo +
                        ", dieIndex=" + dieIndex +
                        ", ch=" + ch +
                        ", grid=" + gridX + ";" + gridY +
                        ", timeoutMs=" + timeoutMs);
                }
                else
                {
                    EventLogger.Write(EventKind.Event, "VISION", "AUTO-VISION-SIDE-INSPECTASYNC",
                        "Side 양쪽 카메라 EPD 수신 완료. frontEpd=" + startResults[0] +
                        ", rearEpd=" + startResults[1] +
                        ", fb=" + Fb +
                        ", collet=" + pickerNo +
                        ", dieIndex=" + dieIndex +
                        ", channel=" + ch +
                        ", grid=" + gridX + ";" + gridY);
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
                    "Side 양쪽 카메라 검사 시작 요청 중 예외 발생. fb=" + Fb +
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
                    "Side 양쪽 카메라 검사 결과 대기. cameras=FrontSide,RearSide" +
                    ", fb=" + Fb +
                    ", pickerNo=" + pickerNo +
                    ", collet=" + pickerNo +
                    ", dieIndex=" + dieIndex +
                    ", grid=" + gridX + ";" + gridY +
                    ", timeoutMs=" + timeoutMs);

                Task<InspectionResultDto> frontTask = AutoVisionRequestService.WaitInspectResultByDieAsync(
                    AutoVisionChannel.FrontSide,
                    ResolveSideSurfaceInspector(AutoVisionChannel.FrontSide),
                    dieIndex, timeoutMs, ct);
                Task<InspectionResultDto> rearTask = AutoVisionRequestService.WaitInspectResultByDieAsync(
                    AutoVisionChannel.RearSide,
                    ResolveSideSurfaceInspector(AutoVisionChannel.RearSide),
                    dieIndex, timeoutMs, ct);

                InspectionResultDto[] inspections = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                InspectionResultDto frontInspection = inspections.Length > 0 ? inspections[0] : null;
                InspectionResultDto rearInspection = inspections.Length > 1 ? inspections[1] : null;
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
                        ", dieIndex=" + dieIndex +
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
                    ", dieIndex=" + dieIndex +
                    ", frontPass=" + frontPass +
                    ", rearPass=" + rearPass +
                    ", allPass=" + (frontPass && rearPass) +
                    ", frontRaw=" + (frontInspection != null ? frontInspection.Raw : string.Empty) +
                    ", rearRaw=" + (rearInspection != null ? rearInspection.Raw : string.Empty));
                return new SideVisionResult
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
