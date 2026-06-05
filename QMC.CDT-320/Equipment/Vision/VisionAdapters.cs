using System;
using System.Threading.Tasks;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// InputStageUnit 용 <see cref="IVisionTcpClient"/> 실구현 — Wafer vision 모듈과 통신.
    /// </summary>
    public class WaferVisionAdapter : IVisionTcpClient
    {
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
            // WaferVision DieFinder 로 매칭 → score>=0.7 이면 OK
            try
            {
                var r = await c.MatchAsync("DieFinder", dieIndex, timeoutMs);
                return r.Success && r.Score >= 0.7;
            }
            catch { return false; }
        }

        public async Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId)
        {
            var c = VisionHub.Wafer;
            if (c == null || !c.IsConnected) return new VisionAlignResult();

            // 타겟별 매핑
            string finder;
            switch (alignTargetId)
            {
                case "Center": finder = "AlignDieFinder";        break;
                case "Ref1":   finder = "FirstReferenceFinder";  break;
                case "Ref2":   finder = "SecondReferenceFinder"; break;
                default:       finder = alignTargetId;           break;
            }

            try
            {
                var r = await c.MatchAsync(finder);
                if (!r.Success) return null;
                // 이미지 중심(320/240)을 0으로 하는 Delta 변환
                return new VisionAlignResult
                {
                    DeltaX     = (r.X - 320) * 0.001,   // 단순 스케일 (실제는 scale recipe 필요)
                    DeltaY     = (r.Y - 240) * 0.001,
                    DeltaTheta = r.AngleDeg,
                    PitchX     = 0.15,
                    PitchY     = 0.15
                };
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// TransferPickerUnit 용 <see cref="IVisionTpuClient"/> 실구현 —
    /// Bottom(Inspection) / Side(TopSide/BottomSide) vision 호출.
    /// 현재 Side 는 Bottom 과 같은 포트(Inspection) 공유 — 매뉴얼 기준.
    /// </summary>
    public class TpuVisionAdapter : IVisionTpuClient
    {
        public Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000)
        {
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return Task.FromResult(false);
            return c.ExposeAsync(pickerNo, timeoutMs);
        }

        public async Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000)
        {
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return null;

            // 4개 Picker 각각에 대해 DieFinder 매칭 → OffsetX/Y/IsOk
            var result = new BottomVisionOffset[4];
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    var r = await c.MatchAsync("DieFinder", i, timeoutMs);
                    result[i] = new BottomVisionOffset
                    {
                        PickerNo = i + 1,
                        OffsetX  = r.Success ? r.X - 320 : 0,
                        OffsetY  = r.Success ? r.Y - 240 : 0,
                        IsOk     = r.Success && r.Score >= 0.7
                    };
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
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return Task.FromResult(false);
            // Side exposure 는 Inspection 포트로 통합 호출 (index 에 sideNo 인코딩)
            return c.ExposeAsync(pickerNo * 10 + sideNo, timeoutMs);
        }

        public async Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000)
        {
            var c = VisionHub.Inspection;
            if (c == null || !c.IsConnected) return null;

            // Surface inspector 호출 → 4면 모두 PASS/FAIL
            try
            {
                var r1 = await c.InspectAsync("SurfaceInspector", pickerNo,      timeoutMs);
                return new SideVisionResult
                {
                    PickerNo = pickerNo,
                    Side1Ok = r1.IsPass,
                    Side2Ok = r1.IsPass,
                    Side3Ok = r1.IsPass,
                    Side4Ok = r1.IsPass
                };
            }
            catch { return null; }
        }
    }

    /// <summary>OutputStage 의 ITpuUnit — 현재는 TransferPicker 와 내부 이벤트로만 동작, 변경 없음.</summary>
    /// 여기서는 VisionHub 의 Bin 클라이언트를 활용한 "PlacementInspector" 호출 헬퍼만 제공.
    public static class BinVisionHelper
    {
        public static async Task<bool> CheckPlacementAsync(int slotIndex, int timeoutMs = 3000)
        {
            var c = VisionHub.Bin;
            if (c == null || !c.IsConnected) return true; // 연결 안 된 경우 skip
            try
            {
                var r = await c.InspectAsync("PlacementInspector", slotIndex, timeoutMs);
                return r.IsPass;
            }
            catch { return true; }
        }
    }
}
