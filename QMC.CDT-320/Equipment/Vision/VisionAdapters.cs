using System;
using System.Threading.Tasks;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// InputStageUnit 용 <see cref="IVisionTcpClient"/> 실구현 — Wafer vision 모듈과 통신.
    /// </summary>
    /// <remarks>
    /// [index↔chipUid] MATCH/INSPECT의 index 인자는 비전 서버가 chipUid(이미지로그·MaterialTracker 키)로 해석한다.
    /// 현재는 dieIndex/pickerNo/slotIndex 숫자를 그대로 전달한다. 실제 칩 UID 추적이 필요하면
    /// 호출부(Material 모델)에서 UID를 주입하도록 정리 필요. TODO: chipUid 소스 연결.
    /// </remarks>
    public class WaferVisionAdapter : IVisionTcpClient
    {
        // ── 임시 캘리브레이션 상수 (TODO: SCALE 레시피/실측값으로 교체) ──
        private const double ImageCenterX        = 320.0;  // 이미지 중심 X [px] (TODO: 실제 해상도 기반)
        private const double ImageCenterY        = 240.0;  // 이미지 중심 Y [px]
        private const double PixelToMm           = 0.001;  // 픽셀→mm 임시 스케일 (TODO: SCALE 캘리브레이션 값)
        private const double DiePitchMm          = 0.15;   // 다이 피치 [mm] (TODO: 레퍼런스 마크 실측 피치)
        private const double MatchScoreThreshold = 0.7;    // 매칭 합격 스코어 임계값

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
                return r.Success && r.Score >= MatchScoreThreshold;
            }
            catch { return false; }
        }

        public async Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId)
        {
            var c = VisionHub.Wafer;
            if (c == null || !c.IsConnected) return null;

            // 타겟별 매핑
            string finder;
            switch (alignTargetId)
            {
                // 중앙 정렬 Finder 선택
                case "Center": finder = "AlignDieFinder";        break;
                // 첫 번째 Reference Finder 선택
                case "Ref1":   finder = "FirstReferenceFinder";  break;
                // 두 번째 Reference Finder 선택
                case "Ref2":   finder = "SecondReferenceFinder"; break;
                default:       finder = alignTargetId;           break;
            }

            try
            {
                var r = await c.MatchAsync(finder);
                if (!r.Success) return null;
                // 이미지 중심을 0으로 하는 Delta 변환 (임시 스케일 — TODO: SCALE 레시피 적용)
                return new VisionAlignResult
                {
                    DeltaX     = (r.X - ImageCenterX) * PixelToMm,
                    DeltaY     = (r.Y - ImageCenterY) * PixelToMm,
                    DeltaTheta = r.AngleDeg,
                    PitchX     = DiePitchMm,
                    PitchY     = DiePitchMm
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
    /// <remarks>
    /// [index↔chipUid] EXPOSE/MATCH/INSPECT의 index(=pickerNo, 또는 pickerNo*10+side)는
    /// 비전 서버에서 chipUid(이미지로그·추적 키)로 해석된다. 실제 칩 UID 추적이 필요하면 호출부에서 UID 주입 정리 필요. TODO.
    /// </remarks>
    public class TpuVisionAdapter : IVisionTpuClient
    {
        // ── 임시 캘리브레이션 상수 (TODO: 실측값으로 교체) ──
        private const double ImageCenterX        = 320.0;  // 이미지 중심 X [px]
        private const double ImageCenterY        = 240.0;  // 이미지 중심 Y [px]
        private const double MatchScoreThreshold = 0.7;    // 매칭 합격 스코어 임계값

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
                        OffsetX  = r.Success ? r.X - ImageCenterX : 0,
                        OffsetY  = r.Success ? r.Y - ImageCenterY : 0,
                        IsOk     = r.Success && r.Score >= MatchScoreThreshold
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

            // 4면 각각 SurfaceInspector 호출. index = pickerNo*10+side (TriggerSideExposeAsync 인코딩과 일치)
            try
            {
                bool[] ok = new bool[4];
                for (int side = 1; side <= 4; side++)
                {
                    var ins = await c.InspectAsync("SurfaceInspector", pickerNo * 10 + side, timeoutMs);
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
