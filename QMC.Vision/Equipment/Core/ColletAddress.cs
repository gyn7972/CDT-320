using System;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 8콜렛(Front 4 + Back 4) 주소 체계 — 와이어 [fb][collet] ↔ 내부 전역 픽커(1~8) 변환 SSOT.
    /// <para>실기 구조: 콜렛 8개 = Front 1~4 + Back 1~4. 공정은 순차(Front 배치 → Front 카메라 →
    /// Back 배치 → Back 카메라)이며 Front/Back 은 동시에 촬영하지 않는다(상호배제).</para>
    /// <para>와이어 신형(고정 8파트, 생략 없음):
    /// <c>MODULE|INSPECTASYNC|inspector|fb|collet|die_index|channel|chip_uid</c>
    ///  • fb        : 0=Front / 1=Back
    ///  • collet    : 1~4
    ///  • die_index : 픽업 순서 1-base, -1=다이 없음(메뉴얼 테스트 — 맵 매칭/다이 집계 생략)
    ///  • channel   : Side 0=0° / 1=90°, Bottom/Bin=-1
    ///  • chip_uid  : 핸들러 자재 고유 ID(결과 매칭 키)
    /// 신/구형 판별 = 파트 수(구형 ≤7, 신형 =8). 구형 포맷은 이행기 하위호환으로 계속 수용한다.</para>
    /// </summary>
    public static class ColletAddress
    {
        /// <summary>한쪽(Front/Back) 콜렛 수 = 배치 그랩 수.</summary>
        public const int ColletsPerGroup = 4;
        /// <summary>그룹 수(Front/Back).</summary>
        public const int GroupCount = 2;
        /// <summary>전체 콜렛 수(=전역 픽커 최대값).</summary>
        public const int TotalCollets = ColletsPerGroup * GroupCount;   // 8

        public const int Front = 0;
        public const int Back  = 1;

        /// <summary>(fb, collet) → 전역 픽커(1~8). Front 1~4, Back 5~8. 범위를 벗어나면 0.</summary>
        public static int ToGlobalPicker(int fb, int collet)
        {
            if (fb < Front || fb > Back) return 0;
            if (collet < 1 || collet > ColletsPerGroup) return 0;
            return fb * ColletsPerGroup + collet;
        }

        /// <summary>전역 픽커(1~8) → fb(0/1). 범위 밖이면 -1.</summary>
        public static int FbOf(int globalPicker)
            => globalPicker >= 1 && globalPicker <= TotalCollets ? (globalPicker - 1) / ColletsPerGroup : -1;

        /// <summary>전역 픽커(1~8) → 콜렛(1~4). 범위 밖이면 0.</summary>
        public static int ColletOf(int globalPicker)
            => globalPicker >= 1 && globalPicker <= TotalCollets ? ((globalPicker - 1) % ColletsPerGroup) + 1 : 0;

        /// <summary>전역 픽커가 Front 그룹(1~4)인지.</summary>
        public static bool IsFrontPicker(int globalPicker) => FbOf(globalPicker) == Front;

        /// <summary>
        /// 신형 고정 8파트 와이어 파싱 — <c>MODULE|CMD|tool|fb|collet|die_index|channel|chip_uid</c>.
        /// 파트 수가 8이고 fb(0/1)/collet(1~4)이 유효할 때만 true(구형은 false → 기존 파서 사용).
        /// die_index=-1 은 다이 없는 메뉴얼 테스트 — 호출부는 uid 숫자 폴백을 적용하지 않는다.
        /// </summary>
        public static bool TryParseWire(string[] parts,
            out int fb, out int collet, out int dieIndex, out int channel, out string chipUid)
        {
            fb = -1; collet = 0; dieIndex = 0; channel = -1; chipUid = string.Empty;
            try
            {
                if (parts == null || parts.Length != 8) return false;
                if (!int.TryParse(parts[3], out fb) || fb < Front || fb > Back) { fb = -1; return false; }
                if (!int.TryParse(parts[4], out collet) || collet < 1 || collet > ColletsPerGroup) { collet = 0; return false; }
                if (!int.TryParse(parts[5], out dieIndex)) dieIndex = 0;
                if (!int.TryParse(parts[6], out channel)) channel = -1;
                chipUid = parts[7] ?? string.Empty;
                return true;
            }
            catch
            {
                fb = -1; collet = 0; dieIndex = 0; channel = -1; chipUid = string.Empty;
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 디스패처 인자 배열(신형 6인자: [tool, fb, collet, die_index, channel, chip_uid]) 파싱.
        /// TCP 파트 배열과 동일 규칙(자리 고정, 생략 없음). 구형(≤5인자)은 false.
        /// </summary>
        public static bool TryParseArgs(string[] args,
            out int fb, out int collet, out int dieIndex, out int channel, out string chipUid)
        {
            fb = -1; collet = 0; dieIndex = 0; channel = -1; chipUid = string.Empty;
            try
            {
                if (args == null || args.Length != 6) return false;
                if (!int.TryParse(args[1], out fb) || fb < Front || fb > Back) { fb = -1; return false; }
                if (!int.TryParse(args[2], out collet) || collet < 1 || collet > ColletsPerGroup) { collet = 0; return false; }
                if (!int.TryParse(args[3], out dieIndex)) dieIndex = 0;
                if (!int.TryParse(args[4], out channel)) channel = -1;
                chipUid = args[5] ?? string.Empty;
                return true;
            }
            catch
            {
                fb = -1; collet = 0; dieIndex = 0; channel = -1; chipUid = string.Empty;
                return false;
            }
            finally
            {
            }
        }
    }
}
