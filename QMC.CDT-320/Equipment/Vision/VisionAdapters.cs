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
        private const double ImageCenterX = 320.0;
        private const double ImageCenterY = 240.0;
        private const double PixelToMm = 0.001;
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

                MatchResultDto result = await AutoVisionRequestService.MatchAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    DefaultTimeoutMs,
                    CancellationToken.None).ConfigureAwait(false);

                VisionAlignResult align = AutoVisionRequestService.ToAlignResult(
                    result,
                    ImageCenterX,
                    ImageCenterY,
                    PixelToMm,
                    DiePitchMm);

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
    /// </summary>
    public class TpuVisionAdapter : IVisionTpuClient
    {
        private const double ImageCenterX = 320.0;
        private const double ImageCenterY = 240.0;
        private const double MatchScoreThreshold = 0.7;
        private readonly AutoVisionChannel _sideChannel;

        public TpuVisionAdapter()
            : this(AutoVisionChannel.Bottom)
        {
        }

        public TpuVisionAdapter(AutoVisionChannel sideChannel)
        {
            _sideChannel = sideChannel;
        }

        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000)
        {
            return TriggerBottomExposeAsync(pickerNo, timeoutMs, CancellationToken.None);
        }

        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            return AutoVisionRequestService.GrabAsync(
                AutoVisionChannel.Bottom,
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
                try
                {
                    ct.ThrowIfCancellationRequested();

                    MatchResultDto match = await AutoVisionRequestService.MatchAsync(
                        AutoVisionChannel.Bottom,
                        "DieFinder",
                        i,
                        timeoutMs,
                        ct).ConfigureAwait(false);

                    results[i] = new BottomVisionOffset
                    {
                        PickerNo = i + 1,
                        OffsetX = match != null && match.Success ? match.X - ImageCenterX : 0,
                        OffsetY = match != null && match.Success ? match.Y - ImageCenterY : 0,
                        OffsetT = match != null && match.Success ? match.AngleDeg : 0,
                        IsOk = match != null && match.Success && match.Score >= MatchScoreThreshold
                    };
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    results[i] = new BottomVisionOffset { PickerNo = i + 1, IsOk = false };
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
                bool[] ok = new bool[4];
                for (int side = 1; side <= 4; side++)
                {
                    ct.ThrowIfCancellationRequested();

                    InspectionResultDto inspection = await AutoVisionRequestService.InspectAsync(
                        _sideChannel,
                        "SurfaceInspector",
                        pickerNo * 10 + side,
                        timeoutMs,
                        ct).ConfigureAwait(false);

                    ok[side - 1] = inspection != null && inspection.IsPass;
                }

                return new SideVisionResult
                {
                    PickerNo = pickerNo,
                    Side1Ok = ok[0],
                    Side2Ok = ok[1],
                    Side3Ok = ok[2],
                    Side4Ok = ok[3]
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

                return await AutoVisionRequestService.InspectAsync(
                    AutoVisionChannel.Bin,
                    "PlacementInspector",
                    slotIndex,
                    timeoutMs,
                    ct).ConfigureAwait(false);
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
