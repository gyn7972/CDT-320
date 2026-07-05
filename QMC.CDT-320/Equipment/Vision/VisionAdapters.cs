using System;
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
    /// Vision 요청은 신형 고정 8파트("tool|fb|collet|die_index|channel|chip_uid")로 전송한다.</para>
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

        /// <summary>어댑터 수준 임시 chip_uid("F1"~"B4") — 시퀀스가 자재 DieId 를 넘기기 전까지의 결과 매칭 키.</summary>
        private string BuildColletUid(int collet, int channel)
        {
            string baseUid = (Fb == 0 ? "F" : "B") + collet;
            return channel >= 0 ? baseUid + "-C" + channel : baseUid;
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

                    // 신형 8파트: fb/collet 명시. die_index=0(이 계층은 다이 정보 없음 — 오프셋 매칭엔 미사용).
                    results[i] = await AutoVisionRequestService.MatchBottomOffsetAsync(
                        Fb,
                        collet,
                        "DieFinder",
                        0,
                        BuildColletUid(collet, -1),
                        MatchScoreThreshold,
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
                // 8콜렛 규약: 콜렛(=pickerNo 1~4) 다이는 자기 그룹 카메라(fb)만 촬영한다 —
                // 채널 0(0°)/1(90°) 2회 검사(기존 pickerNo*10+side 패킹·4채널 루프 대체).
                bool[] ok = new bool[2];
                for (int ch = 0; ch <= 1; ch++)
                {
                    ct.ThrowIfCancellationRequested();

                    InspectionResultDto inspection = await AutoVisionRequestService.InspectColletAsync(
                        _sideChannel,
                        "SurfaceInspector",
                        Fb,
                        pickerNo,
                        0,
                        ch,
                        BuildColletUid(pickerNo, ch),
                        timeoutMs,
                        ct).ConfigureAwait(false);

                    ok[ch] = inspection != null && inspection.IsPass;
                }

                return new SideVisionResult
                {
                    PickerNo = pickerNo,
                    Side1Ok = ok[0],
                    Side2Ok = ok[1],
                    Side3Ok = true,   // 미사용 — 콜렛당 자기 카메라 0°/90° 2촬영 체계(합산 판정은 1·2만 반영)
                    Side4Ok = true
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
                return new InspectionResultDto { IsPass = true, Raw = "BYPASS:VisionDisabled" };
            if (VisionHub.Bin == null || !VisionHub.Bin.IsConnected)
                return new InspectionResultDto { IsPass = true, Raw = "BYPASS:BinVisionNotConnected" };

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
