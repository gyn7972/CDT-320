using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// InputStageUnit �� <see cref="IVisionTcpClient"/> �Ǳ��� ? Wafer vision ���� ���.
    /// </summary>
    /// <remarks>
    /// [index��chipUid] MATCH/INSPECT�� index ���ڴ� ���� ������ chipUid(�̹����αס�MaterialTracker Ű)�� �ؼ��Ѵ�.
    /// ����� dieIndex/pickerNo/slotIndex ���ڸ� �״�� �����Ѵ�. ���� Ĩ UID ������ �ʿ��ϸ�
    /// ȣ���(Material ��)���� UID�� �����ϵ��� ���� �ʿ�. TODO: chipUid �ҽ� ����.
    /// </remarks>
    public class WaferVisionAdapter : IVisionTcpClient
    {
        // ���� �ӽ� Ķ���극�̼� ��� (TODO: SCALE ������/���������� ��ü) ����
        private const double ImageCenterX        = 320.0;  // �̹��� �߽� X [px] (TODO: ���� �ػ� ���)
        private const double ImageCenterY        = 240.0;  // �̹��� �߽� Y [px]
        private const double PixelToMm           = 0.001;  // �ȼ���mm �ӽ� ������ (TODO: SCALE Ķ���극�̼� ��)
        private const double DiePitchMm          = 0.15;   // ���� ��ġ [mm] (TODO: ���۷��� ��ũ ���� ��ġ)
        private const double MatchScoreThreshold = 0.7;    // ��Ī �հ� ���ھ� �Ӱ谪

        public Task<bool> TriggerExposeAsync(int dieIndex)
        {
            var c = VisionHub.Wafer;
            if (c == null || !c.IsConnected) return Task.FromResult(false);
            return c.ExposeAsync(dieIndex);
        }

        public async Task<bool> GetResultAsync(int dieIndex, int timeoutMs = 5000)
        {
            var c = VisionHub.Wafer;
            if (c == null || !c.IsConnected) return false;
            // WaferVision DieFinder �� ��Ī �� score>=0.7 �̸� OK
            try
            {
                var r = await c.MatchAsync("DieFinder", dieIndex, timeoutMs);
                bool ok = r.Success && r.Score >= MatchScoreThreshold;
                QMC.CDT_320.Equipment.Vision.WaferVisionResultStore.RecordDieCheck(ok);
                return ok;
            }
            catch { return false; }
        }

        public async Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId)
        {
            var c = VisionHub.Wafer;
            if (c == null || !c.IsConnected) return null;

            // Ÿ�ٺ� ����
            string finder;
            switch (alignTargetId)
            {
                // �߾� ���� Finder ����
                case "Center": finder = "AlignDieFinder";        break;
                // ù ��° Reference Finder ����
                case "Ref1":   finder = "FirstReferenceFinder";  break;
                // �� ��° Reference Finder ����
                case "Ref2":   finder = "SecondReferenceFinder"; break;
                default:       finder = alignTargetId;           break;
            }

            try
            {
                var r = await c.MatchAsync(finder);
                if (!r.Success) return null;
                // �̹��� �߽��� 0���� �ϴ� Delta ��ȯ (�ӽ� ������ ? TODO: SCALE ������ ����)
                var align = new VisionAlignResult
                {
                    DeltaX     = (r.X - ImageCenterX) * PixelToMm,
                    DeltaY     = (r.Y - ImageCenterY) * PixelToMm,
                    DeltaTheta = r.AngleDeg,
                    PitchX     = DiePitchMm,
                    PitchY     = DiePitchMm
                };
                QMC.CDT_320.Equipment.Vision.WaferVisionResultStore.RecordAlign(alignTargetId, align);
                return align;
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// TransferPickerUnit �� <see cref="IVisionTpuClient"/> �Ǳ��� ?
    /// Bottom(Inspection) / Side(TopSide/BottomSide) vision ȣ��.
    /// ���� Side �� Bottom �� ���� ��Ʈ(Inspection) ���� ? �Ŵ��� ����.
    /// </summary>
    /// <remarks>
    /// [index��chipUid] EXPOSE/MATCH/INSPECT�� index(=pickerNo, �Ǵ� pickerNo*10+side)��
    /// ���� �������� chipUid(�̹����αס����� Ű)�� �ؼ��ȴ�. ���� Ĩ UID ������ �ʿ��ϸ� ȣ��ο��� UID ���� ���� �ʿ�. TODO.
    /// </remarks>
    public class TpuVisionAdapter : IVisionTpuClient
    {
        // ���� �ӽ� Ķ���극�̼� ��� (TODO: ���������� ��ü) ����
        private const double ImageCenterX        = 320.0;  // �̹��� �߽� X [px]
        private const double ImageCenterY        = 240.0;  // �̹��� �߽� Y [px]
        private const double MatchScoreThreshold = 0.7;    // ��Ī �հ� ���ھ� �Ӱ谪

        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000)
        {
            return TriggerBottomExposeAsync(pickerNo, timeoutMs, CancellationToken.None);
        }

        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return Task.FromResult(false);
            return c.ExposeAsync(pickerNo, timeoutMs, ct);
        }

        public async Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000)
        {
            return await GetBottomResultsAsync(timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs, CancellationToken ct)
        {
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return null;

            // 4�� Picker ������ ���� DieFinder ��Ī �� OffsetX/Y/IsOk
            var result = new BottomVisionOffset[4];
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var r = await c.MatchAsync("DieFinder", i, timeoutMs, ct);
                    result[i] = new BottomVisionOffset
                    {
                        PickerNo = i + 1,
                        OffsetX  = r.Success ? r.X - ImageCenterX : 0,
                        OffsetY  = r.Success ? r.Y - ImageCenterY : 0,
                        IsOk     = r.Success && r.Score >= MatchScoreThreshold
                    };
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    result[i] = new BottomVisionOffset { PickerNo = i + 1, IsOk = false };
                }
            }
            return result;
        }

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000)
        {
            return TriggerSideExposeAsync(pickerNo, sideNo, timeoutMs, CancellationToken.None);
        }

        public Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs, CancellationToken ct)
        {
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return Task.FromResult(false);
            // Side exposure �� Inspection ��Ʈ�� ���� ȣ�� (index �� sideNo ���ڵ�)
            return c.ExposeAsync(pickerNo * 10 + sideNo, timeoutMs, ct);
        }

        public async Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000)
        {
            return await GetSideResultAsync(pickerNo, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs, CancellationToken ct)
        {
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return null;

            // 4�� ���� SurfaceInspector ȣ��. index = pickerNo*10+side (TriggerSideExposeAsync ���ڵ��� ��ġ)
            try
            {
                bool[] ok = new bool[4];
                for (int side = 1; side <= 4; side++)
                {
                    ct.ThrowIfCancellationRequested();
                    var ins = await c.InspectAsync("SurfaceInspector", pickerNo * 10 + side, timeoutMs, ct);
                    ok[side - 1] = ins.IsPass;
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
            catch { return null; }
        }
    }

    /// <summary>OutputStage �� ITpuUnit ? ����� TransferPicker �� ���� �̺�Ʈ�θ� ����, ���� ����.</summary>
    /// ���⼭�� VisionHub �� Bin Ŭ���̾�Ʈ�� Ȱ���� "PlacementInspector" ȣ�� ���۸� ����.
    public static class BinVisionHelper
    {
        public static async Task<InspectionResultDto> CheckPlacementAsync(int slotIndex, int timeoutMs = 3000)
        {
            var c = VisionHub.Bin;
            if (c == null || !c.IsConnected)
                return new InspectionResultDto { IsPass = true, Raw = "BYPASS:BinVisionNotConnected" };

            try
            {
                return await c.InspectAsync("PlacementInspector", slotIndex, timeoutMs);
            }
            catch (Exception ex)
            {
                return new InspectionResultDto { IsPass = true, Raw = "BYPASS:" + ex.Message };
            }
        }
    }
}
