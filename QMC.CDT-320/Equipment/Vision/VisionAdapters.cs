using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// InputStageUnit에서 사용하는 Wafer/Input vision adapter.
    /// Unit은 공정 의미를 유지하고, 실제 TCP 요청은 AutoVisionRequestService가 담당한다.
    /// </summary>
    public class WaferVisionAdapter : IVisionTcpClient
    {
        private const double DiePitchMm = 0.15;
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
                    "DieFinder",
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
                bool grabbed = await AutoVisionRequestService.GrabAsync(
                    AutoVisionChannel.Wafer,
                    0,
                    DefaultTimeoutMs,
                    CancellationToken.None).ConfigureAwait(false);
                if (!grabbed)
                    return null;

                VisionAlignResult align = await AutoVisionRequestService.MatchAlignAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    DiePitchMm,
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

        private static string ResolveAlignFinder(string alignTargetId)
        {
            switch (alignTargetId)
            {
                case "Center":
                    return "AlignDieFinder";
                case "Ref1":
                    return "FirstReferenceFinder";
                case "Ref2":
                    return "SecondReferenceFinder";
                case "InputPickDie":
                    return "DieFinder";
                default:
                    return alignTargetId;
            }
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
            return AutoVisionRequestService.GrabAsync(
                AutoVisionChannel.BottomInspection,
                pickerNo,
                timeoutMs,
                ct);
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

                    // 신형 8파트: fb/collet 명시 + die_index(키)/grid — 시퀀스가 기록한 다이 주소 사용.
                    int dieIndex, gridX, gridY;
                    ResolveDieAddress(collet, out dieIndex, out gridX, out gridY);
                    results[i] = await AutoVisionRequestService.InspectBottomOffsetAsync(
                        Fb,
                        collet,
                        "SurfaceInspector",
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
                catch
                {
                    results[i] = new BottomVisionOffset { PickerNo = collet, IsOk = false };
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
            return AutoVisionRequestService.GrabAsync(
                _sideChannel,
                pickerNo * 10 + sideNo,
                timeoutMs,
                ct);
        }

        public async Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000)
        {
            return await GetSideResultAsync(pickerNo, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            try
            {
                // 8콜렛 규약: 콜렛(=pickerNo 1~4) 다이는 자기 그룹 카메라(fb)만 촬영한다.
                // 비동기 전용(2026-07-06): 같은 die_index 로 채널 0(0°)/1(90°) INSPECTASYNC 를 모두 시작하고
                // INSPECTRESULT 1회로 그룹 합산 판정(모두 PASS 여야 PASS)을 회수한다.
                int dieIndex, gridX, gridY;
                ResolveDieAddress(pickerNo, out dieIndex, out gridX, out gridY);

                for (int ch = 0; ch <= 1; ch++)
                {
                    ct.ThrowIfCancellationRequested();

                    bool started = await AutoVisionRequestService.StartInspectColletAsync(
                        _sideChannel,
                        "SurfaceInspector",
                        Fb,
                        pickerNo,
                        dieIndex,
                        ch,
                        gridX,
                        gridY,
                        timeoutMs,
                        ct).ConfigureAwait(false);

                    if (!started)
                        return new SideVisionResult { PickerNo = pickerNo, Side1Ok = false, Side2Ok = false, Side3Ok = true, Side4Ok = true };
                }

                InspectionResultDto inspection = await AutoVisionRequestService.WaitInspectResultByDieAsync(
                    _sideChannel,
                    "SurfaceInspector",
                    dieIndex,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                bool pass = inspection != null && inspection.IsPass;
                return new SideVisionResult
                {
                    PickerNo = pickerNo,
                    Side1Ok = pass,   // 그룹 합산 판정(0°/90° 모두 PASS 여야 PASS) — 채널별 상세는 Vision 결과 스토어 참조
                    Side2Ok = pass,
                    Side3Ok = true,   // 미사용 — 콜렛당 자기 카메라 0°/90° 2촬영 체계(합산 판정은 1·2만 반영)
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
                    AutoVisionRequestService.BuildSimulationInspectionResult(AutoVisionChannel.Bin, "PlacementInspector", slotIndex));
            if (VisionHub.Bin == null || !VisionHub.Bin.IsConnected)
                return QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToInspectionResult(
                    AutoVisionChannel.Bin,
                    AutoVisionRequestService.BuildSimulationInspectionResult(AutoVisionChannel.Bin, "PlacementInspector", slotIndex));

            try
            {
                bool grabbed = await AutoVisionRequestService.GrabAsync(
                    AutoVisionChannel.Bin,
                    slotIndex,
                    timeoutMs,
                    ct).ConfigureAwait(false);
                if (!grabbed)
                    return new InspectionResultDto { IsPass = false, Raw = "Bin vision GRAB failed." };

                InspectionResultDto result = await AutoVisionRequestService.InspectCalibratedAsync(
                    AutoVisionChannel.Bin,
                    "PlacementInspector",
                    slotIndex,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                return result;
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
    }
}
